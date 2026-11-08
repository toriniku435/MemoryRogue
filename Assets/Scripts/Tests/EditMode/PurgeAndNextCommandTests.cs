using System.Linq;
using System.Reflection;
using Live2DCardRogue.BattleScene.Data;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.UseCase;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Models.Cards;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Statuses;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    // パージ（除外）・ループバック・マルチスレッド・マイニング・デッキ切れの敗北、と敵の新しい効果
    public sealed class PurgeAndNextCommandTests
    {
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = null;
            for (var type = target.GetType(); type != null && field == null; type = type.BaseType)
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static CardData NewCard(string id, int cost, bool purgeOnUse, params CardEffectBase[] effects)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(card, "_id", id);
            SetPrivateField(card, "_cost", cost);
            SetPrivateField(card, "_purgeOnUse", purgeOnUse);
            SetPrivateField(card, "_effects", effects);
            return card;
        }

        private static CardData Attack(int damage)
        {
            var effect = new BasicAttackCardEffect();
            SetPrivateField(effect, "_damage", damage);
            return NewCard("Attack", 0, false, effect);
        }

        private static CardData Loopback() => NewCard("Loopback", 0, false, new LoopbackCardEffect());

        private static CardData Multithread(int repeat)
        {
            var effect = new MultithreadCardEffect();
            SetPrivateField(effect, "_repeat", repeat);
            return NewCard("Multithread", 0, false, effect);
        }

        private sealed class Fixture
        {
            public UnitStatusModel Player;
            public UnitStatusModel Enemy;
            public CardPileModel Deck, Hand, Discard, Purge;
            public BattleCommandRig UseCase;

            public void Queue(CardData card, UnitStatusModel target)
            {
                Assert.IsTrue(UseCase.Queue.TrySelectCard(card));
                Assert.IsTrue(UseCase.Queue.SelectTarget(target));
            }

            public void ExecuteAll()
            {
                while (UseCase.Execute.ExecuteNext()) { }
            }
        }

        private static Fixture NewFixture(CardData[] hand, CardData[] deck = null)
        {
            var f = new Fixture
            {
                Player = new UnitStatusModel(100, 10),
                Enemy = new UnitStatusModel(100, 0),
                Deck = new CardPileModel(deck ?? new CardData[0]),
                Hand = new CardPileModel(hand),
                Discard = new CardPileModel(new CardData[0]),
                Purge = new CardPileModel(new CardData[0]),
            };
            f.UseCase = new BattleCommandRig(f.Player, f.Deck, f.Hand, f.Discard, new CardCommandModel(3), new CardSelectionModel(), _ => null, null, f.Purge);
            return f;
        }

        [Test]
        public void Loopback_ReturnsTheNextCommandToTheNextHand()
        {
            var attack = Attack(6);
            var f = NewFixture(new[] { Loopback(), attack });
            f.Queue(f.Hand.Cards[0], f.Player);
            f.Queue(attack, f.Enemy);
            f.ExecuteAll();

            CollectionAssert.DoesNotContain(f.Discard.Cards, attack, "戻るコマンドは捨て札へ行かない");
            Assert.IsFalse(f.Player.StatusEffects.Any(e => e.Id == LoopbackStatusEffect.EffectId), "使い切ったら消える");

            f.UseCase.TurnEnd.EndPlayerTurn();
            f.UseCase.Draw.Draw();
            CollectionAssert.Contains(f.Hand.Cards, attack, "次に引くとき、先に手札へ戻る");
        }

        // 付けたコマンド自身では使い切らない（次のコマンドに効く）。ターンをまたいでも残る
        [Test]
        public void Loopback_IsNotSpentByTheCommandThatAppliedIt()
        {
            var f = NewFixture(new[] { Loopback() });
            f.Queue(f.Hand.Cards[0], f.Player);
            f.ExecuteAll();

            Assert.IsTrue(f.Player.StatusEffects.Any(e => e.Id == LoopbackStatusEffect.EffectId));
            Assert.AreEqual(1, f.Discard.Count, "ループバック自身は捨て札へ");
        }

        // 戻ったコマンドも手札の枚数に数え、そのうえで規定の枚数まで引く
        [Test]
        public void ReturnedCommands_CountTowardsTheHandSize()
        {
            var attack = Attack(6);
            var deck = Enumerable.Range(0, 10).Select(_ => Attack(1)).ToArray();
            var f = NewFixture(new[] { Loopback(), attack }, deck);
            f.Queue(f.Hand.Cards[0], f.Player);
            f.Queue(attack, f.Enemy);
            f.ExecuteAll();
            f.UseCase.TurnEnd.EndPlayerTurn();
            f.UseCase.Draw.Draw();

            Assert.AreEqual(5, f.Hand.Count);
            CollectionAssert.Contains(f.Hand.Cards, attack);
        }

        [Test]
        public void Multithread_RepeatsTheNextCommand_AndPurgesIt()
        {
            var attack = Attack(6);
            var f = NewFixture(new[] { Multithread(3), attack });
            f.Queue(f.Hand.Cards[0], f.Player);
            f.Queue(attack, f.Enemy);
            f.ExecuteAll();

            Assert.AreEqual(100 - 18, f.Enemy.CurrentHp, "3回当たる");
            CollectionAssert.Contains(f.Purge.Cards, attack);
            CollectionAssert.DoesNotContain(f.Discard.Cards, attack);
            Assert.IsFalse(f.Player.StatusEffects.Any(e => e.Id == MultithreadStatusEffect.EffectId));
        }

        // マルチスレッドとループバックが同じコマンドに効いたら、パージが勝つ
        [Test]
        public void PurgeWinsOverLoopback()
        {
            var attack = Attack(6);
            var f = NewFixture(new[] { Loopback(), Multithread(3), attack });
            f.Queue(f.Hand.Cards[0], f.Player);   // ループバック → 次（マルチスレッド）が戻る
            f.Queue(f.Hand.Cards[0], f.Player);   // マルチスレッド → 次（アタック）が3回・パージ
            f.Queue(attack, f.Enemy);
            f.ExecuteAll();

            CollectionAssert.Contains(f.Purge.Cards, attack);
            f.UseCase.TurnEnd.EndPlayerTurn();
            f.UseCase.Draw.Draw();
            Assert.IsTrue(f.Hand.Cards.Any(c => c.Id == "Multithread"), "マルチスレッドはループバックで戻る");
        }

        [Test]
        public void PurgeOnUse_SendsTheCommandToPurge_AndMiningAddsATokenBonus()
        {
            var bonus = new TokenBonusCardEffect();
            SetPrivateField(bonus, "_percent", 50);
            var mining = NewCard("Mining", 0, true, bonus);
            var f = NewFixture(new[] { mining, NewCard("Mining2", 0, true, bonus) });
            f.Queue(f.Hand.Cards[0], f.Player);
            f.Queue(f.Hand.Cards[0], f.Player);
            f.ExecuteAll();

            Assert.AreEqual(2, f.Purge.Count);
            Assert.AreEqual(100, f.Player.StatusEffects.Single(e => e.Id == TokenBonusStatusEffect.EffectId).Value, "上乗せは重ねるたびに足される");
        }

        [Test]
        public void DeckIsExhausted_OnlyWhenEveryCommandIsPurged()
        {
            var mining = NewCard("Mining", 0, true, new TokenBonusCardEffect());
            var f = NewFixture(new[] { mining });
            Assert.IsFalse(f.UseCase.Execute.IsDeckExhausted);

            f.Queue(mining, f.Player);
            f.ExecuteAll();
            Assert.IsTrue(f.UseCase.Execute.IsDeckExhausted);
        }

        [Test]
        public void DeckIsNotExhausted_WhileACommandWaitsToReturn()
        {
            var attack = Attack(1);
            var f = NewFixture(new[] { Loopback(), attack });
            f.Queue(f.Hand.Cards[0], f.Player);
            f.Queue(attack, f.Enemy);
            f.ExecuteAll();
            f.UseCase.TurnEnd.EndPlayerTurn();
            f.Discard.Clear();

            Assert.IsFalse(f.UseCase.Execute.IsDeckExhausted, "次の手札に戻る分が残っている");
        }

        [Test]
        public void StageReward_WithTokenBonus_RaisesTheTokens()
        {
            var reward = new StageReward(true, 150, null).WithTokenBonus(50);
            Assert.AreEqual(225, reward.Tokens);
            Assert.AreEqual(50, reward.BonusPercent);
            Assert.AreSame(reward, reward.WithTokenBonus(0));
        }

        [Test]
        public void FindWeakestLiving_PicksTheLowestHpRatio()
        {
            var a = new UnitStatusModel(100, 0);
            var b = new UnitStatusModel(50, 0);
            var party = new EnemyPartyModel(new[] { new BattleEnemy(null, a), new BattleEnemy(null, b) });
            a.ChangeHp(HpChangeType.TakePiercingDamage, -40); // 60%
            b.ChangeHp(HpChangeType.TakePiercingDamage, -30); // 40%
            Assert.AreSame(b, party.FindWeakestLiving());

            b.ChangeHp(HpChangeType.TakePiercingDamage, -20); // 倒れた
            Assert.AreSame(a, party.FindWeakestLiving());
        }

        [Test]
        public void AllyHeal_HealsTheTarget_NotTheUser()
        {
            var effect = new AllyHealCardEffect();
            SetPrivateField(effect, "_healAmount", 15);
            var user = new UnitStatusModel(100, 0);
            var ally = new UnitStatusModel(100, 0);
            ally.ChangeHp(HpChangeType.TakePiercingDamage, -50);
            user.ChangeHp(HpChangeType.TakePiercingDamage, -50);

            effect.Execute(user, ally);

            Assert.AreEqual(65, ally.CurrentHp);
            Assert.AreEqual(50, user.CurrentHp);
            Assert.IsTrue(effect.ChoosesAlly);
            Assert.IsFalse(effect.TargetsOpponent);
        }

        [Test]
        public void HealReversalAll_ReversesTheUserToo()
        {
            var effect = new HealReversalAllCardEffect();
            SetPrivateField(effect, "_turns", 2);
            var user = new UnitStatusModel(100, 0);
            var target = new UnitStatusModel(100, 0);

            effect.Execute(user, target);

            Assert.IsTrue(effect.TargetsAllOpponents);
            Assert.IsTrue(user.StatusEffects.Any(e => e.Id == HealReversalStatusEffect.EffectId));
            Assert.IsTrue(target.StatusEffects.Any(e => e.Id == HealReversalStatusEffect.EffectId));
        }

        [Test]
        public void HandJam_LowersTheTargetsHandSize()
        {
            var effect = new HandJamCardEffect();
            SetPrivateField(effect, "_turns", 2);
            SetPrivateField(effect, "_amount", 1);
            var user = new UnitStatusModel(50, 0);
            var target = new UnitStatusModel(100, 10);

            effect.Execute(user, target);

            Assert.AreEqual(4, target.CalculateLimit(StatLimitType.HandSize, 5));
            Assert.AreEqual(5, user.CalculateLimit(StatLimitType.HandSize, 5));
        }
    }
}

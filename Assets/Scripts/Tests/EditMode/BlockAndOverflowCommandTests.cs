using System.Linq;
using System.Reflection;
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
    // バイパス（ブロック解除）・ガベージコレクション（手札をパージしてダメージ）・リペアパケット（相手を回復）・ワーム
    public sealed class BlockAndOverflowCommandTests
    {
        private static T Fx<T>(params (string field, object value)[] fields) where T : new()
        {
            var fx = new T();
            foreach (var (field, value) in fields)
            {
                FieldInfo f = null;
                for (var t = typeof(T); t != null && f == null; t = t.BaseType)
                    f = t.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
                f.SetValue(fx, value);
            }
            return fx;
        }

        private static CardData NewCard(string id, params CardEffectBase[] effects)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            typeof(CardData).GetField("_id", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(card, id);
            typeof(CardData).GetField("_effects", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(card, effects);
            return card;
        }

        [Test]
        public void Bypass_RemovesEveryBlock_AndDealsNoDamage()
        {
            var user = new UnitStatusModel(100, 0);
            var target = new UnitStatusModel(100, 0);
            target.AddStatusEffect(new BlockStatusEffect(12));
            int notified = 0;
            target.OnStatusEffectsChanged += () => notified++;

            new BlockBreakCardEffect().Execute(user, target);

            Assert.AreEqual(0, target.BlockValue);
            Assert.AreEqual(100, target.CurrentHp);
            Assert.AreEqual(1, notified, "取り除いたら一覧の変化として通知する（バリアが割れる・ログが書ける）");
        }

        [Test]
        public void RemoveStatusEffects_DoesNotNotify_WhenNothingMatched()
        {
            var unit = new UnitStatusModel(100, 0);
            int notified = 0;
            unit.OnStatusEffectsChanged += () => notified++;

            Assert.AreEqual(0, unit.RemoveStatusEffects(e => e is BlockStatusEffect));
            Assert.AreEqual(0, notified);
        }

        private static (BattleCommandRig UseCase, UnitStatusModel Player, UnitStatusModel Enemy, CardPileModel Hand, CardPileModel Purge, CardSelectionModel Selection, CardCommandModel Commands) NewBattle(CardData overflow, int otherCards)
        {
            var player = new UnitStatusModel(100, 10);
            var enemy = new UnitStatusModel(200, 0);
            var hand = new CardPileModel(new[] { overflow }.Concat(Enumerable.Range(0, otherCards).Select(i => NewCard("Filler" + i))).ToArray());
            var purge = new CardPileModel(new CardData[0]);
            var selection = new CardSelectionModel();
            var commands = new CardCommandModel(3);
            var useCase = new BattleCommandRig(player, new CardPileModel(new CardData[0]), hand, new CardPileModel(new CardData[0]),
                commands, selection, _ => null, null, purge);
            return (useCase, player, enemy, hand, purge, selection, commands);
        }

        private static CardData Overflow() => NewCard("GarbageCollection", Fx<OverflowAttackCardEffect>(("_purgeCount", 2), ("_damagePerPurge", 25)));

        // 対象を選ぶと、すぐには積まれず「どのバッファをパージするか」の選択に移る
        [Test]
        public void GarbageCollection_AsksWhichBufferToPurge_AfterTheTarget()
        {
            var overflow = Overflow();
            var b = NewBattle(overflow, otherCards: 3);
            b.UseCase.Queue.TrySelectCard(overflow);

            Assert.IsTrue(b.UseCase.Queue.SelectTarget(b.Enemy));
            Assert.IsTrue(b.Selection.IsChoosingPurge);
            Assert.AreEqual(2, b.Selection.PurgeCapacity);
            Assert.AreEqual(0, b.Commands.Count, "まだ積まない");
        }

        // 選んだバッファを預かって積み、実行でパージして、パージした数 × 25 のダメージ
        [Test]
        public void GarbageCollection_PurgesTheChosenBuffer_AndDealsDamagePerPurge()
        {
            var overflow = Overflow();
            var b = NewBattle(overflow, otherCards: 3);
            var chosen = new[] { b.Hand.Cards[1], b.Hand.Cards[3] };
            b.UseCase.Queue.TrySelectCard(overflow);
            b.UseCase.Queue.SelectTarget(b.Enemy);

            Assert.IsTrue(b.UseCase.Queue.ConfirmPurgeChoice(chosen));
            Assert.AreEqual(1, b.Hand.Count, "積んだ時点で、元のコマンドと選んだバッファが抜ける");
            CollectionAssert.DoesNotContain(b.Hand.Cards, chosen[0]);

            b.UseCase.Execute.ExecuteNext();
            Assert.AreEqual(150, b.Enemy.CurrentHp);
            CollectionAssert.AreEquivalent(chosen, b.Purge.Cards, "パージされるのは選んだものだけ（撃ったコマンド自身は捨て札へ）");
        }

        [Test]
        public void GarbageCollection_OnePurge_DealsOneShare()
        {
            var overflow = Overflow();
            var b = NewBattle(overflow, otherCards: 3);
            b.UseCase.Queue.TrySelectCard(overflow);
            b.UseCase.Queue.SelectTarget(b.Enemy);
            b.UseCase.Queue.ConfirmPurgeChoice(new[] { b.Hand.Cards[2] });
            b.UseCase.Execute.ExecuteNext();

            Assert.AreEqual(175, b.Enemy.CurrentHp);
        }

        [Test]
        public void ConfirmPurgeChoice_RefusesNoneOrTooMany_OrCardsNotInTheBuffer()
        {
            var overflow = Overflow();
            var b = NewBattle(overflow, otherCards: 3);
            b.UseCase.Queue.TrySelectCard(overflow);
            b.UseCase.Queue.SelectTarget(b.Enemy);

            Assert.IsFalse(b.UseCase.Queue.ConfirmPurgeChoice(new CardData[0]));
            Assert.IsFalse(b.UseCase.Queue.ConfirmPurgeChoice(new[] { b.Hand.Cards[1], b.Hand.Cards[2], b.Hand.Cards[3] }));
            Assert.IsFalse(b.UseCase.Queue.ConfirmPurgeChoice(new[] { NewCard("Elsewhere") }));
            Assert.IsFalse(b.UseCase.Queue.ConfirmPurgeChoice(new[] { overflow }), "積むコマンド自身はパージに選べない");
            Assert.IsTrue(b.Selection.IsChoosingPurge, "断っても選択は続く");
        }

        // 実行リストから戻すと、預かっていたバッファも戻る
        [Test]
        public void ReturningTheCommand_GivesThePurgeChoiceBack()
        {
            var overflow = Overflow();
            var b = NewBattle(overflow, otherCards: 3);
            b.UseCase.Queue.TrySelectCard(overflow);
            b.UseCase.Queue.SelectTarget(b.Enemy);
            b.UseCase.Queue.ConfirmPurgeChoice(new[] { b.Hand.Cards[1], b.Hand.Cards[2] });

            b.UseCase.Queue.ReturnCommandToHand(0);

            Assert.AreEqual(4, b.Hand.Count);
            Assert.AreEqual(0, b.Purge.Count);
        }

        [Test]
        public void GarbageCollection_CannotBeSelected_WithNothingElseInTheBuffer()
        {
            var overflow = Overflow();
            var b = NewBattle(overflow, otherCards: 0);

            Assert.IsFalse(b.UseCase.Queue.CanSelectCard(overflow));
        }

        [Test]
        public void CancelSelection_EndsThePurgeChoice()
        {
            var overflow = Overflow();
            var b = NewBattle(overflow, otherCards: 2);
            b.UseCase.Queue.TrySelectCard(overflow);
            b.UseCase.Queue.SelectTarget(b.Enemy);

            b.UseCase.Queue.CancelSelection();

            Assert.IsFalse(b.Selection.IsChoosingPurge);
            Assert.AreEqual(3, b.Hand.Count);
        }

        [Test]
        public void RepairPacket_HealsTheTarget_OrHurtsItUnderHealReversal()
        {
            var effect = Fx<OpponentHealCardEffect>(("_healAmount", 20));
            var user = new UnitStatusModel(100, 0);
            var target = new UnitStatusModel(100, 0);
            target.ChangeHp(HpChangeType.TakePiercingDamage, -50);

            effect.Execute(user, target);
            Assert.AreEqual(70, target.CurrentHp);
            Assert.IsTrue(effect.TargetsOpponent);

            target.AddStatusEffect(new HealReversalStatusEffect(2));
            effect.Execute(user, target);
            Assert.AreEqual(50, target.CurrentHp);
        }

        [Test]
        public void Worm_DealsItsValuePlusHalfTheOwnersBlock()
        {
            var owner = new UnitStatusModel(100, 0);
            owner.AddStatusEffect(new WormStatusEffect(2));
            owner.AddStatusEffect(new BlockStatusEffect(10));

            owner.EndTurn();

            Assert.AreEqual(93, owner.CurrentHp, "2 + ブロック10の半分");
            Assert.AreEqual(10, owner.BlockValue, "ブロックは数えるだけで減らさない（ウイルスと同じくブロックでは防げない）");
        }

        [Test]
        public void WormCard_InfectsWithAWorm_NotAVirus()
        {
            var effect = Fx<WormCardEffect>(("_wormAmount", 2));
            var target = new UnitStatusModel(100, 0);

            effect.Execute(new UnitStatusModel(100, 0), target);

            Assert.IsTrue(target.StatusEffects.Any(e => e.Id == WormStatusEffect.EffectId));
            Assert.IsFalse(target.StatusEffects.Any(e => e.Id == PoisonStatusEffect.EffectId));
            Assert.AreEqual(CardRole.Attack, AutoDeckBuilder.RoleOf(NewCard("Worm", effect)), "ウイルスの仲間として攻撃に数える");
        }
    }
}

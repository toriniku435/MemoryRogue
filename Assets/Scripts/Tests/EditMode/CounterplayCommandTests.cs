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
using Live2DCardRogue.Window.DeckEdit.Models;

namespace Live2DCardRogue.Tests.EditMode
{
    // 2026-09-29 の追加分：無効化（ウイルス・データ破損・即死）、バッファロック、エクスプロイトチェーン、
    // グリッチ、メモリーリーク、デッキの投入上限
    public sealed class CounterplayCommandTests
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

        private static CardData NewCard(string id, int maxCopies = 0, params CardEffectBase[] effects)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            typeof(CardData).GetField("_id", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(card, id);
            typeof(CardData).GetField("_effects", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(card, effects);
            typeof(CardData).GetField("_maxCopiesInDeck", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(card, maxCopies);
            return card;
        }

        [Test]
        public void VirusImmunity_BlocksVirusAndWorm_ButNotOtherEffects()
        {
            var unit = new UnitStatusModel(100, 0);
            unit.AddStatusEffect(new VirusImmunityStatusEffect());
            int blocked = 0;
            unit.OnStatusEffectBlocked += _ => blocked++;

            unit.AddStatusEffect(new PoisonStatusEffect(5));
            unit.AddStatusEffect(new WormStatusEffect(2));
            unit.AddStatusEffect(new BlockStatusEffect(5));

            Assert.AreEqual(2, blocked);
            Assert.IsFalse(unit.StatusEffects.Any(e => e.Id == PoisonStatusEffect.EffectId || e.Id == WormStatusEffect.EffectId));
            Assert.AreEqual(5, unit.BlockValue);
        }

        [Test]
        public void CorruptionImmunity_BlocksHealReversal()
        {
            var unit = new UnitStatusModel(100, 0);
            unit.AddStatusEffect(new CorruptionImmunityStatusEffect());

            unit.AddStatusEffect(new HealReversalStatusEffect(2));

            Assert.IsFalse(unit.StatusEffects.Any(e => e.Id == HealReversalStatusEffect.EffectId));
        }

        [Test]
        public void KillImmunity_CancelsInstantKill_ButNotOrdinaryDamage()
        {
            var unit = new UnitStatusModel(100, 0);
            unit.AddStatusEffect(new KillImmunityStatusEffect());

            unit.ChangeHp(HpChangeType.InstantKill, -100);
            Assert.AreEqual(100, unit.CurrentHp);

            unit.ChangeHp(HpChangeType.TakeAttackDamage, -30);
            Assert.AreEqual(70, unit.CurrentHp);
        }

        private static (BattleCommandRig UseCase, CardPileModel Hand) NewDrawBattle(UnitStatusModel player)
        {
            var deck = new CardPileModel(Enumerable.Range(0, 10).Select(i => NewCard("C" + i)).ToArray());
            var hand = new CardPileModel(new CardData[0]);
            var useCase = new BattleCommandRig(player, deck, hand, new CardPileModel(new CardData[0]),
                new CardCommandModel(3), new CardSelectionModel(), _ => null);
            return (useCase, hand);
        }

        [Test]
        public void BufferLock_DrawsNothing_OnTheNextTurn_ThenWearsOff()
        {
            var player = new UnitStatusModel(100, 10);
            var (useCase, hand) = NewDrawBattle(player);

            // 自分のターン中に付く（その番は数えない）
            Fx<BufferLockCardEffect>(("_turns", 1)).Execute(player, player);
            player.EndTurn();

            player.StartTurn();
            useCase.Draw.Draw();
            Assert.AreEqual(0, hand.Count, "次のターンはバッファが空");
            player.EndTurn();

            player.StartTurn();
            useCase.Draw.Draw();
            Assert.Greater(hand.Count, 0, "その次のターンは元に戻る");
        }

        [Test]
        public void BufferLock_UsedTwice_LocksTwoTurns()
        {
            var player = new UnitStatusModel(100, 10);
            var effect = Fx<BufferLockCardEffect>(("_turns", 1));
            effect.Execute(player, player);
            effect.Execute(player, player);
            player.EndTurn();

            for (int turn = 0; turn < 2; turn++)
            {
                player.StartTurn();
                Assert.AreEqual(0, player.CalculateLimit(StatLimitType.DrawCap, int.MaxValue), $"{turn + 1}ターン目もロック");
                player.EndTurn();
            }

            player.StartTurn();
            Assert.AreEqual(int.MaxValue, player.CalculateLimit(StatLimitType.DrawCap, int.MaxValue));
        }

        private static ExploitChainCardEffect Chain(int step) => Fx<ExploitChainCardEffect>(("_step", step), ("_finalStep", 3));

        [Test]
        public void ExploitChain_InOrder_KillsTheTarget()
        {
            var player = new UnitStatusModel(100, 10);
            var enemy = new UnitStatusModel(300, 0);

            Chain(1).Execute(player, player);
            Chain(2).Execute(player, player);
            Chain(3).Execute(player, enemy);

            Assert.AreEqual(0, enemy.CurrentHp);
            Assert.IsFalse(player.StatusEffects.Any(e => e.Id == ExploitChainStatusEffect.EffectId), "使い切ったチェーンは残らない");
        }

        [TestCase(2, 1, 3)]
        [TestCase(1, 3, 2)]
        [TestCase(3, 2, 1)]
        public void ExploitChain_OutOfOrder_DoesNothing(int first, int second, int third)
        {
            var player = new UnitStatusModel(100, 10);
            var enemy = new UnitStatusModel(300, 0);

            foreach (int step in new[] { first, second, third }) Chain(step).Execute(player, step == 3 ? enemy : player);

            Assert.AreEqual(300, enemy.CurrentHp);
        }

        [Test]
        public void ExploitChain_BreaksAtTheEndOfTheTurn()
        {
            var player = new UnitStatusModel(100, 10);
            var enemy = new UnitStatusModel(300, 0);

            Chain(1).Execute(player, player);
            Chain(2).Execute(player, player);
            player.EndTurn();
            player.StartTurn();
            Chain(3).Execute(player, enemy);

            Assert.AreEqual(300, enemy.CurrentHp);
        }

        [Test]
        public void ExploitChain_OnlyTheLastStepTargetsTheOpponent()
        {
            Assert.IsFalse(Chain(1).TargetsOpponent);
            Assert.IsFalse(Chain(2).TargetsOpponent);
            Assert.IsTrue(Chain(3).TargetsOpponent);
        }

        [Test]
        public void ExploitChain_DoesNotKill_AKillImmuneTarget()
        {
            var player = new UnitStatusModel(100, 10);
            var enemy = new UnitStatusModel(300, 0);
            enemy.AddStatusEffect(new KillImmunityStatusEffect());

            Chain(1).Execute(player, player);
            Chain(2).Execute(player, player);
            Chain(3).Execute(player, enemy);

            Assert.AreEqual(300, enemy.CurrentHp);
        }

        [Test]
        public void GlitchInject_GivesTheTargetAGlitchChance_FromItsNextTurn()
        {
            var player = new UnitStatusModel(100, 10);
            var enemy = new UnitStatusModel(100, 0);

            Fx<GlitchInjectCardEffect>(("_turns", 2), ("_percent", 50)).Execute(player, enemy);

            Assert.AreEqual(50, enemy.CalculateLimit(StatLimitType.GlitchChance, 0));
            Assert.AreEqual(0, player.CalculateLimit(StatLimitType.GlitchChance, 0));
        }

        [Test]
        public void StaminaDrain_ReducesTheTargetsMemory()
        {
            var enemy = new UnitStatusModel(100, 0);
            var player = new UnitStatusModel(100, 10);

            Fx<StaminaDrainCardEffect>(("_amount", 3)).Execute(enemy, player);

            Assert.AreEqual(7, player.Stamina);
        }

        [Test]
        public void DeckEdit_RefusesASecondCopy_OfALimitedCommand()
        {
            var limited = NewCard("Terminate", maxCopies: 1);
            var model = new DeckEditModel(new[] { limited, limited }, new CardData[0], 20);

            Assert.IsTrue(model.TryAddToDeck(limited));
            Assert.IsFalse(model.CanAddToDeck(limited));
            Assert.IsFalse(model.TryAddToDeck(limited));
        }

        [Test]
        public void AutoDeck_TakesAtMostTheLimit_OfALimitedCommand()
        {
            var limited = NewCard("Terminate", maxCopies: 1);
            var owned = Enumerable.Repeat(limited, 5).ToArray();

            var deck = AutoDeckBuilder.Build(owned, new AutoDeckRule(0, 0, 5));

            Assert.AreEqual(1, deck.Count(c => c == limited));
        }
    }
}

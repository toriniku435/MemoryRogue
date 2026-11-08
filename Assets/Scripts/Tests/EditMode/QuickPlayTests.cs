using System.Collections.Generic;
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
    // 手札のクリックで、対象を選ぶ余地が無いカードはそのまま積む（TryPlayWithoutChoosing）。
    // 状態効果が付いたときの通知（文字の演出に使う）もここで見る
    public sealed class QuickPlayTests
    {
        private static CardData NewCard(int cost, params CardEffectBase[] effects)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            typeof(CardData).GetField("_cost", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(card, cost);
            typeof(CardData).GetField("_effects", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(card, effects);
            return card;
        }

        private static (BattleCommandRig UseCase, UnitStatusModel Player, CardCommandModel Commands, CardSelectionModel Selection) NewBattle(
            CardData card, params UnitStatusModel[] livingEnemies)
        {
            var player = new UnitStatusModel(100, 10);
            var hand = new CardPileModel(new[] { card });
            var commands = new CardCommandModel(3);
            var selection = new CardSelectionModel();
            var useCase = new BattleCommandRig(player, new CardPileModel(new CardData[0]), hand, new CardPileModel(new CardData[0]),
                commands, selection, _ => null, () => new List<UnitStatusModel>(livingEnemies));
            return (useCase, player, commands, selection);
        }

        [Test]
        public void SelfCard_IsQueuedOnThePlayer_WithoutChoosing()
        {
            var card = NewCard(1, new BasicBlockCardEffect());
            var (useCase, player, commands, selection) = NewBattle(card, new UnitStatusModel(50, 0), new UnitStatusModel(50, 0));

            Assert.AreEqual(QuickPlayResult.Played, useCase.Queue.TryPlayWithoutChoosing(card));
            Assert.AreEqual(1, commands.Count);
            Assert.AreSame(player, commands.Commands[0].Target);
            Assert.IsFalse(selection.IsSelectingTarget);
        }

        [Test]
        public void SingleTargetCard_NeedsATarget_WhenTwoEnemiesAreAlive()
        {
            var card = NewCard(1, new BasicAttackCardEffect());
            var (useCase, _, commands, _) = NewBattle(card, new UnitStatusModel(50, 0), new UnitStatusModel(50, 0));

            Assert.AreEqual(QuickPlayResult.NeedsTarget, useCase.Queue.TryPlayWithoutChoosing(card));
            Assert.AreEqual(0, commands.Count);
        }

        // クリックで使ったときは、敵が複数いても一番手前の生きている敵に向けて積む
        [Test]
        public void SingleTargetCard_PreferringFront_IsQueuedOnTheFrontEnemy()
        {
            var card = NewCard(1, new BasicAttackCardEffect());
            var front = new UnitStatusModel(50, 0);
            var (useCase, _, commands, _) = NewBattle(card, front, new UnitStatusModel(50, 0));

            Assert.AreEqual(QuickPlayResult.Played, useCase.Queue.TryPlayWithoutChoosing(card, preferFrontTarget: true));
            Assert.AreSame(front, commands.Commands[0].Target);
        }

        [Test]
        public void SingleTargetCard_IsQueuedOnTheOnlyLivingEnemy()
        {
            var card = NewCard(1, new BasicAttackCardEffect());
            var enemy = new UnitStatusModel(50, 0);
            var (useCase, _, commands, _) = NewBattle(card, enemy);

            Assert.AreEqual(QuickPlayResult.Played, useCase.Queue.TryPlayWithoutChoosing(card));
            Assert.AreSame(enemy, commands.Commands[0].Target);
        }

        [Test]
        public void AreaAttack_IsQueuedWithoutChoosing_EvenWithTwoEnemies()
        {
            var card = NewCard(1, new AreaAttackCardEffect());
            var (useCase, _, commands, _) = NewBattle(card, new UnitStatusModel(50, 0), new UnitStatusModel(50, 0));

            Assert.AreEqual(QuickPlayResult.Played, useCase.Queue.TryPlayWithoutChoosing(card));
            Assert.AreEqual(1, commands.Count);
        }

        // 同じカードが複数あるとき、押した位置の1枚が抜ける（先頭の1枚ではない）。抜ける位置がずれると、表示では別の1枚が飛んでいく
        [Test]
        public void SameCardTwice_TheClickedCopyLeavesTheHand()
        {
            var copy = NewCard(1, new BasicBlockCardEffect());
            var other = NewCard(1, new BasicBlockCardEffect());
            var player = new UnitStatusModel(100, 10);
            var hand = new CardPileModel(new[] { copy, other, copy });
            var useCase = new BattleCommandRig(player, new CardPileModel(new CardData[0]), hand, new CardPileModel(new CardData[0]),
                new CardCommandModel(3), new CardSelectionModel(), _ => null, () => new List<UnitStatusModel>());

            Assert.AreEqual(QuickPlayResult.Played, useCase.Queue.TryPlayWithoutChoosing(copy, handIndex: 2));

            CollectionAssert.AreEqual(new[] { copy, other }, hand.Cards, "3枚目を押したので、並びは 1枚目・2枚目のまま残る");
        }

        [Test]
        public void SameCardTwice_ThroughTargetSelection_TheSelectedCopyLeavesTheHand()
        {
            var copy = NewCard(1, new BasicAttackCardEffect());
            var other = NewCard(1, new BasicBlockCardEffect());
            var player = new UnitStatusModel(100, 10);
            var enemy = new UnitStatusModel(50, 0);
            var hand = new CardPileModel(new[] { copy, other, copy });
            var useCase = new BattleCommandRig(player, new CardPileModel(new CardData[0]), hand, new CardPileModel(new CardData[0]),
                new CardCommandModel(3), new CardSelectionModel(), _ => null,
                () => new List<UnitStatusModel> { enemy, new UnitStatusModel(50, 0) });

            Assert.IsTrue(useCase.Queue.TrySelectCard(copy, handIndex: 2));
            Assert.IsTrue(useCase.Queue.SelectTarget(enemy));

            CollectionAssert.AreEqual(new[] { copy, other }, hand.Cards);
        }

        [Test]
        public void StatusEffectApplied_IsRaised_ForNewAndStackedEffects_ButNotForBlockedOnes()
        {
            var unit = new UnitStatusModel(100, 0);
            var applied = new List<IStatusEffect>();
            unit.OnStatusEffectApplied += applied.Add;

            unit.AddStatusEffect(new BlockStatusEffect(5));
            unit.AddStatusEffect(new BlockStatusEffect(3));
            unit.AddStatusEffect(new VirusImmunityStatusEffect());
            unit.AddStatusEffect(new PoisonStatusEffect(4));

            Assert.AreEqual(3, applied.Count, "ブロック2回と無効化の1回。弾かれたウイルスは数えない");
            Assert.AreEqual(8, applied[1].Value, "重なったときは重ねた後の値で知らせる");
        }
    }
}

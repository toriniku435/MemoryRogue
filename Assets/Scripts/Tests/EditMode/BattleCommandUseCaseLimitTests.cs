using System.Collections.Generic;
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
    // 手札の枚数の増減と、全体攻撃の当て方（BattleCommandExecuteUseCase）、全体攻撃の当たり先（EnemyPartyModel）
    public sealed class BattleCommandUseCaseLimitTests
    {
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static CardData NewCard(int cost, params CardEffectBase[] effects)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(card, "_cost", cost);
            SetPrivateField(card, "_effects", effects);
            return card;
        }

        private static CardData[] NewCards(int count)
        {
            var cards = new CardData[count];
            for (int i = 0; i < count; i++) cards[i] = NewCard(1);
            return cards;
        }

        private static BattleCommandRig NewUseCase(
            UnitStatusModel player, CardPileModel deck, CardPileModel hand, CardCommandModel commands,
            System.Func<IReadOnlyList<UnitStatusModel>> getLivingOpponents = null)
        {
            return new BattleCommandRig(
                player, deck, hand, new CardPileModel(new CardData[0]), commands, new CardSelectionModel(),
                _ => null, getLivingOpponents);
        }

        [TestCase(0, 0, 5)]
        [TestCase(2, 0, 7)]
        [TestCase(0, 2, 3)]
        public void DrawPlayerCard_DrawsUpToTheHandSizeIncludingStatusEffects(int up, int down, int expected)
        {
            var player = new UnitStatusModel(maxHp: 30, maxStamina: 10);
            if (up > 0) player.AddStatusEffect(new HandSizeUpStatusEffect(turns: 1, amount: up));
            if (down > 0) player.AddStatusEffect(new HandSizeDownStatusEffect(turns: 1, amount: down));
            var hand = new CardPileModel(new CardData[0]);
            var useCase = NewUseCase(player, new CardPileModel(NewCards(10)), hand, new CardCommandModel(3));

            useCase.Draw.Draw();

            Assert.AreEqual(expected, hand.Count);
        }

        // 減らす効果が重なっても、最低1枚は引く
        [Test]
        public void DrawPlayerCard_AlwaysDrawsAtLeastOne()
        {
            var player = new UnitStatusModel(maxHp: 30, maxStamina: 10);
            player.AddStatusEffect(new HandSizeDownStatusEffect(turns: 1, amount: 99));
            var hand = new CardPileModel(new CardData[0]);
            var useCase = NewUseCase(player, new CardPileModel(NewCards(10)), hand, new CardCommandModel(3));

            useCase.Draw.Draw();

            Assert.AreEqual(1, hand.Count);
        }

        [Test]
        public void ExecuteNextCommand_AreaAttack_HitsEveryLivingOpponent()
        {
            var player = new UnitStatusModel(maxHp: 30, maxStamina: 10);
            var first = new UnitStatusModel(maxHp: 20, maxStamina: 0);
            var second = new UnitStatusModel(maxHp: 20, maxStamina: 0);
            var area = new AreaAttackCardEffect();
            SetPrivateField(area, "_damage", 7);
            var commands = new CardCommandModel(3);
            commands.TryAdd(new CardCommand(NewCard(1, area), first));
            var useCase = NewUseCase(player, new CardPileModel(new CardData[0]), new CardPileModel(new CardData[0]), commands,
                () => new[] { first, second });

            useCase.Execute.ExecuteNext();

            Assert.AreEqual(13, first.CurrentHp);
            Assert.AreEqual(13, second.CurrentHp);
        }

        // 当たり先を渡されなければ、全体攻撃も選んだ1体にだけ当たる
        [Test]
        public void ExecuteNextCommand_AreaAttack_WithoutOpponentList_HitsTheChosenTarget()
        {
            var player = new UnitStatusModel(maxHp: 30, maxStamina: 10);
            var target = new UnitStatusModel(maxHp: 20, maxStamina: 0);
            var area = new AreaAttackCardEffect();
            SetPrivateField(area, "_damage", 7);
            var commands = new CardCommandModel(3);
            commands.TryAdd(new CardCommand(NewCard(1, area), target));
            var useCase = NewUseCase(player, new CardPileModel(new CardData[0]), new CardPileModel(new CardData[0]), commands);

            useCase.Execute.ExecuteNext();

            Assert.AreEqual(13, target.CurrentHp);
        }

        [Test]
        public void GetLivingStatuses_SkipsDefeatedEnemies_InOrder()
        {
            var first = new BattleEnemy(ScriptableObject.CreateInstance<EnemyData>(), new UnitStatusModel(maxHp: 10, maxStamina: 0));
            var second = new BattleEnemy(ScriptableObject.CreateInstance<EnemyData>(), new UnitStatusModel(maxHp: 10, maxStamina: 0));
            var third = new BattleEnemy(ScriptableObject.CreateInstance<EnemyData>(), new UnitStatusModel(maxHp: 10, maxStamina: 0));
            second.Status.ChangeHp(HpChangeType.TakeAttackDamage, -10);
            var party = new EnemyPartyModel(new[] { first, second, third });

            var living = party.GetLivingStatuses();

            Assert.AreEqual(2, living.Count);
            Assert.AreSame(first.Status, living[0]);
            Assert.AreSame(third.Status, living[1]);
        }
    }
}

using System.Collections;
using System.Reflection;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.UseCase;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Models.Cards;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Live2DCardRogue.Tests.PlayMode
{
    // TrySelectCardの「断る」経路だけがここにある: MessageUseCase.ShowMessage -> MessageToast を通り、
    // そのDontDestroyOnLoadがEdit Modeでは例外になるため（他の積む・実行するケースは
    // MessageToastに触れないので EditMode/BattleCommandUseCaseTests.cs 側にある）。
    public sealed class BattleCommandUseCasePlayModeTests
    {
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static CardData NewCard(int cost, CardEffectBase[] effects)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(card, "_cost", cost);
            SetPrivateField(card, "_effects", effects);
            return card;
        }

        private static BasicAttackCardEffect NewAttackEffect(int damage)
        {
            var effect = new BasicAttackCardEffect();
            SetPrivateField(effect, "_damage", damage);
            return effect;
        }

        private static BattleCommandQueueUseCase NewQueueUseCase(UnitStatusModel player, CardPileModel hand, CardCommandModel commands,
            CardSelectionModel selection)
        {
            var piles = new BattleCardPilesModel(new CardPileModel(new CardData[0]), hand, new CardPileModel(new CardData[0]));
            return new BattleCommandQueueUseCase(player, piles, commands, selection,
                new BattleTargetResolver(player, _ => null, null, null), new BattleTokenModel(0));
        }

        [UnityTest]
        public IEnumerator TrySelectCard_InsufficientStamina_ReturnsFalse_AndCardStaysInHand()
        {
            var player = new UnitStatusModel(maxHp: 30, maxStamina: 1);
            var card = NewCard(cost: 2, effects: new CardEffectBase[] { NewAttackEffect(6) });
            var hand = new CardPileModel(new[] { card });
            var commands = new CardCommandModel(maxCount: 3);
            var selection = new CardSelectionModel();
            var useCase = NewQueueUseCase(player, hand, commands, selection);

            bool accepted = useCase.TrySelectCard(card);
            yield return null;

            Assert.IsFalse(accepted);
            Assert.AreEqual(1, player.Stamina); // 何も消費されない
            Assert.AreEqual(1, hand.Count); // カードは手札に残る
            Assert.AreEqual(0, commands.Count);
            Assert.IsFalse(selection.IsSelectingTarget); // 対象選択にも入らない
        }

        [UnityTest]
        public IEnumerator TrySelectCard_CommandListIsFull_ReturnsFalse_AndCardStaysInHand()
        {
            var player = new UnitStatusModel(maxHp: 30, maxStamina: 5);
            var queuedCard = NewCard(cost: 0, effects: new CardEffectBase[] { NewAttackEffect(1) });
            var extraCard = NewCard(cost: 0, effects: new CardEffectBase[] { NewAttackEffect(1) });
            var hand = new CardPileModel(new[] { queuedCard, extraCard });
            var commands = new CardCommandModel(maxCount: 1);
            var selection = new CardSelectionModel();
            var enemy = new UnitStatusModel(maxHp: 30, maxStamina: 0);
            var useCase = NewQueueUseCase(player, hand, commands, selection);

            useCase.TrySelectCard(queuedCard);
            useCase.SelectTarget(enemy);
            bool accepted = useCase.TrySelectCard(extraCard);
            yield return null;

            Assert.IsFalse(accepted);
            Assert.AreEqual(1, commands.Count); // 上限のまま
            Assert.AreEqual(1, hand.Count); // 積めなかったカードは手札に残る
            Assert.IsFalse(selection.IsSelectingTarget);
        }
    }
}

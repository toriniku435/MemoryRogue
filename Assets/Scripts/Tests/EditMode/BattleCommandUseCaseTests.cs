using System;
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
    public sealed class BattleCommandUseCaseTests
    {
        // CardData's fields are all private [SerializeField]s (Inspector-only by design, see CardData.cs),
        // so tests build one via reflection rather than adding a test-only constructor to production code.
        private static CardData NewCard(int cost, CardEffectBase[] effects)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(card, "_cost", cost);
            SetPrivateField(card, "_effects", effects);
            return card;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static BasicAttackCardEffect NewAttackEffect(int damage)
        {
            var effect = new BasicAttackCardEffect();
            SetPrivateField(effect, "_damage", damage);
            return effect;
        }

        private static BasicBlockCardEffect NewBlockEffect(int blockAmount)
        {
            var effect = new BasicBlockCardEffect();
            SetPrivateField(effect, "_blockAmount", blockAmount);
            return effect;
        }

        // 1回の戦闘分のモデル一式。テストごとに必要なものだけ触れるよう、まとめて組み立てて返す
        private sealed class Fixture
        {
            public UnitStatusModel Player;
            public UnitStatusModel Enemy;
            public CardPileModel Deck;
            public CardPileModel Hand;
            public CardPileModel Discard;
            public CardCommandModel Commands;
            public CardSelectionModel Selection;
            public BattleCommandRig UseCase;
        }

        // findAlternativeTarget は「対象が倒れていたときの振り直し先」。既定では振り直さない
        private static Fixture NewFixture(
            CardData[] handCards,
            int maxStamina = 3,
            int maxCommandCount = 3,
            Func<UnitStatusModel, UnitStatusModel> findAlternativeTarget = null)
        {
            var fixture = new Fixture
            {
                Player = new UnitStatusModel(maxHp: 30, maxStamina: maxStamina),
                Enemy = new UnitStatusModel(maxHp: 30, maxStamina: 0),
                Deck = new CardPileModel(new CardData[0]),
                Hand = new CardPileModel(handCards),
                Discard = new CardPileModel(new CardData[0]),
                Commands = new CardCommandModel(maxCommandCount),
                Selection = new CardSelectionModel(),
            };

            fixture.UseCase = new BattleCommandRig(
                fixture.Player, fixture.Deck, fixture.Hand, fixture.Discard, fixture.Commands, fixture.Selection,
                findAlternativeTarget ?? (_ => null));

            return fixture;
        }

        // 敵には手札・捨て札が無いので、カードを使っても山札系モデルは一切動かない（プレイヤー側との違い）
        [Test]
        public void ExecuteEnemyCard_AppliesEffectToTarget_AndDoesNotConsumeTheCard()
        {
            var enemyCard = NewCard(cost: 1, effects: new CardEffectBase[] { NewAttackEffect(6) });
            var fixture = NewFixture(new CardData[0]);

            fixture.UseCase.Execute.ExecuteEnemyCard(enemyCard, fixture.Enemy, fixture.Player);

            Assert.AreEqual(24, fixture.Player.CurrentHp); // 効果は対象に適用される
            Assert.AreEqual(0, fixture.Hand.Count); // 敵のカードはプレイヤーの山札系に混ざらない
            Assert.AreEqual(0, fixture.Deck.Count);
            Assert.AreEqual(0, fixture.Discard.Count);
        }

        // 敵はスタミナを持たない。行動量はEnemyDataの行動回数とIEnemyCardPickerが決めるので、ここではコストを見ない
        [Test]
        public void ExecuteEnemyCard_DoesNotConsumeStamina()
        {
            var enemyCard = NewCard(cost: 2, effects: new CardEffectBase[] { NewAttackEffect(6) });
            var fixture = NewFixture(new CardData[0]);

            fixture.UseCase.Execute.ExecuteEnemyCard(enemyCard, fixture.Enemy, fixture.Player);

            Assert.AreEqual(24, fixture.Player.CurrentHp); // コストを払えなくても効果は実行される
            Assert.AreEqual(0, fixture.Enemy.Stamina);
        }

        // 操作の形を揃えるため、自分に効くカードも相手に作用するカードも等しく対象選択に入る。
        // どちらも対象を選ぶまでは積まれず、コストも払わない
        [TestCase(true)]
        [TestCase(false)]
        public void TrySelectCard_StartsTargetSelection_WithoutQueueing(bool targetsOpponent)
        {
            var effect = targetsOpponent
                ? (CardEffectBase)NewAttackEffect(6)
                : NewBlockEffect(5);
            var card = NewCard(cost: 1, effects: new[] { effect });
            var fixture = NewFixture(new[] { card });

            bool accepted = fixture.UseCase.Queue.TrySelectCard(card);

            Assert.IsTrue(accepted);
            Assert.AreSame(card, fixture.Selection.PendingCard);
            Assert.AreEqual(0, fixture.Commands.Count);
            Assert.AreEqual(3, fixture.Player.Stamina); // まだ払っていない
            Assert.AreEqual(1, fixture.Hand.Count);
        }

        // 選べる側はカード次第。表示（ハイライト）もこの判定を使う
        [Test]
        public void CanSelectTarget_AllowsOnlyTheSideTheCardActsOn()
        {
            var attackCard = NewCard(cost: 1, effects: new CardEffectBase[] { NewAttackEffect(6) });
            var blockCard = NewCard(cost: 1, effects: new CardEffectBase[] { NewBlockEffect(5) });
            var fixture = NewFixture(new[] { attackCard, blockCard });

            Assert.IsFalse(fixture.UseCase.Queue.CanSelectTarget(fixture.Enemy)); // 選択中のカードが無ければ誰も選べない

            fixture.UseCase.Queue.TrySelectCard(attackCard);
            Assert.IsTrue(fixture.UseCase.Queue.CanSelectTarget(fixture.Enemy));
            Assert.IsFalse(fixture.UseCase.Queue.CanSelectTarget(fixture.Player));

            fixture.UseCase.Queue.TrySelectCard(blockCard);
            Assert.IsFalse(fixture.UseCase.Queue.CanSelectTarget(fixture.Enemy));
            Assert.IsTrue(fixture.UseCase.Queue.CanSelectTarget(fixture.Player));
        }

        // 敵が何体いても、生きている相手なら誰でも選べる
        [Test]
        public void CanSelectTarget_AllowsEveryLivingOpponent()
        {
            var card = NewCard(cost: 1, effects: new CardEffectBase[] { NewAttackEffect(6) });
            var fixture = NewFixture(new[] { card });
            var secondEnemy = new UnitStatusModel(maxHp: 30, maxStamina: 0);
            fixture.UseCase.Queue.TrySelectCard(card);

            Assert.IsTrue(fixture.UseCase.Queue.CanSelectTarget(fixture.Enemy));
            Assert.IsTrue(fixture.UseCase.Queue.CanSelectTarget(secondEnemy));
        }

        // 倒れたユニットは選べない(既に積んであるカードの対象が倒れた場合の撃ち直しはまだ無い)
        [Test]
        public void CanSelectTarget_RejectsDefeatedUnit()
        {
            var card = NewCard(cost: 1, effects: new CardEffectBase[] { NewAttackEffect(6) });
            var fixture = NewFixture(new[] { card });
            fixture.Enemy.ChangeHp(HpChangeType.TakeAttackDamage, -fixture.Enemy.CurrentHp);
            fixture.UseCase.Queue.TrySelectCard(card);

            Assert.IsFalse(fixture.UseCase.Queue.CanSelectTarget(fixture.Enemy));
            Assert.IsFalse(fixture.UseCase.Queue.SelectTarget(fixture.Enemy));
            Assert.AreEqual(0, fixture.Commands.Count);
            Assert.AreEqual(3, fixture.Player.Stamina); // 何も払われない
        }

        // 自分に効くカードはプレイヤー自身を選んで積む
        [Test]
        public void SelectTarget_SelfCard_QueuesAgainstThePlayer()
        {
            var card = NewCard(cost: 1, effects: new CardEffectBase[] { NewBlockEffect(5) });
            var fixture = NewFixture(new[] { card });
            fixture.UseCase.Queue.TrySelectCard(card);

            bool queued = fixture.UseCase.Queue.SelectTarget(fixture.Player);

            Assert.IsTrue(queued);
            Assert.AreSame(fixture.Player, fixture.Commands.Commands[0].Target);
            Assert.AreEqual(2, fixture.Player.Stamina);
            Assert.AreEqual(0, fixture.Hand.Count);
        }

        // 選べない側をクリックしても、選択中のまま何も起きない
        [Test]
        public void SelectTarget_WrongSide_DoesNothing_AndKeepsTheCardPending()
        {
            var card = NewCard(cost: 1, effects: new CardEffectBase[] { NewAttackEffect(6) });
            var fixture = NewFixture(new[] { card });
            fixture.UseCase.Queue.TrySelectCard(card);

            bool queued = fixture.UseCase.Queue.SelectTarget(fixture.Player); // 攻撃カードで自分を選ぼうとする

            Assert.IsFalse(queued);
            Assert.AreSame(card, fixture.Selection.PendingCard);
            Assert.AreEqual(0, fixture.Commands.Count);
            Assert.AreEqual(3, fixture.Player.Stamina);
            Assert.AreEqual(1, fixture.Hand.Count);
        }

        [Test]
        public void SelectTarget_QueuesPendingCard_WithChosenTarget()
        {
            var card = NewCard(cost: 1, effects: new CardEffectBase[] { NewAttackEffect(6) });
            var fixture = NewFixture(new[] { card });
            fixture.UseCase.Queue.TrySelectCard(card);

            bool queued = fixture.UseCase.Queue.SelectTarget(fixture.Enemy);

            Assert.IsTrue(queued);
            Assert.AreEqual(1, fixture.Commands.Count);
            Assert.AreSame(fixture.Enemy, fixture.Commands.Commands[0].Target);
            Assert.AreEqual(2, fixture.Player.Stamina);
            Assert.AreEqual(0, fixture.Hand.Count);
            Assert.IsFalse(fixture.Selection.IsSelectingTarget);
            Assert.AreEqual(30, fixture.Enemy.CurrentHp); // 積んだだけではまだ効果は出ない
        }

        // 対象選択中でないユニットクリックは、ただの空振りにする
        [Test]
        public void SelectTarget_WithoutPendingCard_DoesNothing()
        {
            var fixture = NewFixture(new CardData[0]);

            Assert.IsFalse(fixture.UseCase.Queue.SelectTarget(fixture.Enemy));
            Assert.AreEqual(0, fixture.Commands.Count);
        }

        // 「選択中のカードをもう一度押したら解除」はPresenter側（押されたViewで判断する）の仕事。
        // ここに置くと、同じCardDataの別の1枚を押したときまで解除になってしまう
        [Test]
        public void TrySelectCard_WhileSameCardIsPending_KeepsItPending()
        {
            var card = NewCard(cost: 1, effects: new CardEffectBase[] { NewAttackEffect(6) });
            var fixture = NewFixture(new[] { card, card }); // 同じカードが手札に2枚ある状態
            fixture.UseCase.Queue.TrySelectCard(card);

            bool accepted = fixture.UseCase.Queue.TrySelectCard(card);

            Assert.IsTrue(accepted);
            Assert.AreSame(card, fixture.Selection.PendingCard);
            Assert.AreEqual(0, fixture.Commands.Count);
            Assert.AreEqual(2, fixture.Hand.Count);
        }

        [Test]
        public void CancelSelection_DropsPendingCard_WithoutQueueingIt()
        {
            var card = NewCard(cost: 1, effects: new CardEffectBase[] { NewAttackEffect(6) });
            var fixture = NewFixture(new[] { card });
            fixture.UseCase.Queue.TrySelectCard(card);

            fixture.UseCase.Queue.CancelSelection();

            Assert.IsFalse(fixture.Selection.IsSelectingTarget);
            Assert.AreEqual(0, fixture.Commands.Count);
            Assert.AreEqual(1, fixture.Hand.Count);
            Assert.AreEqual(3, fixture.Player.Stamina);
        }

        [Test]
        public void ExecuteNextCommand_RunsHeadOfList_AndMovesCardToDiscard()
        {
            var card = NewCard(cost: 1, effects: new CardEffectBase[] { NewAttackEffect(6) });
            var fixture = NewFixture(new[] { card });
            fixture.UseCase.Queue.TrySelectCard(card);
            fixture.UseCase.Queue.SelectTarget(fixture.Enemy);

            bool executed = fixture.UseCase.Execute.ExecuteNext();

            Assert.IsTrue(executed);
            Assert.AreEqual(24, fixture.Enemy.CurrentHp);
            Assert.AreEqual(0, fixture.Commands.Count);
            Assert.AreEqual(1, fixture.Discard.Count);
            Assert.AreEqual(2, fixture.Player.Stamina); // 実行では二重に払わない
        }

        // Directorは「リストが尽きるまで」回すので、空振りが false で返ることに依存している
        [Test]
        public void ExecuteNextCommand_EmptyList_ReturnsFalse()
        {
            var fixture = NewFixture(new CardData[0]);

            Assert.IsFalse(fixture.UseCase.Execute.ExecuteNext());
        }

        // 積んだあと実行までの間に対象が倒れた場合は、次の敵に振り直して撃つ
        [Test]
        public void ExecuteNextCommand_TargetDefeated_RetargetsToTheAlternative()
        {
            var card = NewCard(cost: 1, effects: new CardEffectBase[] { NewAttackEffect(6) });
            var secondEnemy = new UnitStatusModel(maxHp: 30, maxStamina: 0);
            var fixture = NewFixture(new[] { card }, findAlternativeTarget: _ => secondEnemy);
            fixture.UseCase.Queue.TrySelectCard(card);
            fixture.UseCase.Queue.SelectTarget(fixture.Enemy);

            // 実行までの間に、狙っていた相手が倒れる
            fixture.Enemy.ChangeHp(HpChangeType.TakeAttackDamage, -fixture.Enemy.CurrentHp);
            fixture.UseCase.Execute.ExecuteNext();

            Assert.AreEqual(24, secondEnemy.CurrentHp); // 次の敵に入る
            Assert.AreEqual(0, fixture.Enemy.CurrentHp); // 死体には何も起きない
            Assert.AreEqual(1, fixture.Discard.Count); // カードは普通に消費される
        }

        // 振り直し先が無い（全滅済みなど）場合は元の対象のまま撃つ。Director側がその前にループを抜ける
        [Test]
        public void ExecuteNextCommand_TargetDefeated_WithoutAlternative_KeepsTheOriginalTarget()
        {
            var card = NewCard(cost: 1, effects: new CardEffectBase[] { NewAttackEffect(6) });
            var fixture = NewFixture(new[] { card });
            fixture.UseCase.Queue.TrySelectCard(card);
            fixture.UseCase.Queue.SelectTarget(fixture.Enemy);
            fixture.Enemy.ChangeHp(HpChangeType.TakeAttackDamage, -fixture.Enemy.CurrentHp);

            Assert.IsTrue(fixture.UseCase.Execute.ExecuteNext());
            Assert.AreEqual(0, fixture.Enemy.CurrentHp);
            Assert.AreEqual(1, fixture.Discard.Count);
        }

        // 対象が生きている限り、振り直し先は聞きに行かない
        [Test]
        public void ExecuteNextCommand_LivingTarget_DoesNotAskForAnAlternative()
        {
            var card = NewCard(cost: 1, effects: new CardEffectBase[] { NewAttackEffect(6) });
            bool asked = false;
            var fixture = NewFixture(new[] { card }, findAlternativeTarget: _ => { asked = true; return null; });
            fixture.UseCase.Queue.TrySelectCard(card);
            fixture.UseCase.Queue.SelectTarget(fixture.Enemy);

            fixture.UseCase.Execute.ExecuteNext();

            Assert.IsFalse(asked);
            Assert.AreEqual(24, fixture.Enemy.CurrentHp);
        }

        [Test]
        public void ReturnCommandToHand_PutsCardBack_AndRefundsStamina()
        {
            var card = NewCard(cost: 2, effects: new CardEffectBase[] { NewBlockEffect(5) });
            var fixture = NewFixture(new[] { card });
            fixture.UseCase.Queue.TrySelectCard(card);
            fixture.UseCase.Queue.SelectTarget(fixture.Player);

            fixture.UseCase.Queue.ReturnCommandToHand(0);

            Assert.AreEqual(0, fixture.Commands.Count);
            Assert.AreEqual(1, fixture.Hand.Count);
            Assert.AreSame(card, fixture.Hand.Cards[0]);
            Assert.AreEqual(3, fixture.Player.Stamina); // 払ったコストが戻る
            Assert.AreEqual(0, fixture.Discard.Count);
        }

        [Test]
        public void SwapCommands_ChangesExecutionOrder()
        {
            var first = NewCard(cost: 0, effects: new CardEffectBase[] { NewAttackEffect(1) });
            var second = NewCard(cost: 0, effects: new CardEffectBase[] { NewAttackEffect(9) });
            var fixture = NewFixture(new[] { first, second });
            fixture.UseCase.Queue.TrySelectCard(first);
            fixture.UseCase.Queue.SelectTarget(fixture.Enemy);
            fixture.UseCase.Queue.TrySelectCard(second);
            fixture.UseCase.Queue.SelectTarget(fixture.Enemy);

            fixture.UseCase.Queue.SwapCommands(0, 1);
            fixture.UseCase.Execute.ExecuteNext();

            Assert.AreEqual(21, fixture.Enemy.CurrentHp); // 入れ替えた結果、先に9ダメージの方が出る
        }

        [Test]
        public void RequestExecuteCommands_RaisesOnExecuteRequested_AndClearsSelection()
        {
            var card = NewCard(cost: 1, effects: new CardEffectBase[] { NewAttackEffect(6) });
            var fixture = NewFixture(new[] { card });
            fixture.UseCase.Queue.TrySelectCard(card); // 対象選択中のまま実行ボタンを押した場合
            bool raised = false;
            fixture.UseCase.Commands.OnExecuteRequested += () => raised = true;

            fixture.UseCase.Queue.RequestExecuteCommands();

            Assert.IsTrue(raised);
            Assert.IsFalse(fixture.Selection.IsSelectingTarget);
        }

        [Test]
        public void EndPlayerTurn_DiscardsWholeHand_AndRestoresStamina()
        {
            var card = NewCard(cost: 0, effects: new CardEffectBase[0]);
            var fixture = NewFixture(new[] { card });
            fixture.Player.ChangeStamina(StaminaChangeType.Consume, -2);

            fixture.UseCase.TurnEnd.EndPlayerTurn();

            Assert.AreEqual(0, fixture.Hand.Count);
            Assert.AreEqual(1, fixture.Discard.Count);
            Assert.AreEqual(3, fixture.Player.Stamina);
        }

        // 実行し切らずにターンが終わる経路は今は無いが、残っていても手札と同じく捨て札へ流す
        [Test]
        public void EndPlayerTurn_DiscardsLeftoverCommands()
        {
            var card = NewCard(cost: 1, effects: new CardEffectBase[] { NewBlockEffect(5) });
            var fixture = NewFixture(new[] { card });
            fixture.UseCase.Queue.TrySelectCard(card);
            fixture.UseCase.Queue.SelectTarget(fixture.Player);

            fixture.UseCase.TurnEnd.EndPlayerTurn();

            Assert.AreEqual(0, fixture.Commands.Count);
            Assert.AreEqual(1, fixture.Discard.Count);
        }

        // TrySelectCardの失敗経路（スタミナ不足・実行リストが一杯）は MessageUseCase -> MessageToast を通るので、
        // Edit Modeでは動かせない。PlayMode/BattleCommandUseCasePlayModeTests.cs を参照。

        [Test]
        public void DrawPlayerCard_ReshufflesDiscardIntoDeck_WhenDeckRunsOut()
        {
            var deckCard = NewCard(cost: 0, effects: new CardEffectBase[0]);
            var discardCard = NewCard(cost: 0, effects: new CardEffectBase[0]);
            var fixture = NewFixture(new CardData[0]);
            fixture.Deck.AddCard(deckCard);
            fixture.Discard.AddCard(discardCard);

            fixture.UseCase.Draw.Draw();

            Assert.AreEqual(2, fixture.Hand.Count); // deckCardを引いたあと、捨て札を戻してdiscardCardも引く
            Assert.AreEqual(0, fixture.Deck.Count);
            Assert.AreEqual(0, fixture.Discard.Count);
        }

        // カーソルを合わせるだけで選ぶ操作のための、メッセージを出さない判定。
        // TrySelectCard と同じ条件で断るが、選択状態も変えない（合わせただけでは何も起きない）
        [Test]
        public void CanSelectCard_ReturnsTrue_WithoutChangingSelection_WhenAffordable()
        {
            var card = NewCard(cost: 1, effects: new CardEffectBase[] { NewAttackEffect(6) });
            var fixture = NewFixture(new[] { card });

            Assert.IsTrue(fixture.UseCase.Queue.CanSelectCard(card));
            Assert.IsNull(fixture.Selection.PendingCard);
        }

        [Test]
        public void CanSelectCard_ReturnsFalse_WhenNotEnoughStamina()
        {
            var card = NewCard(cost: 4, effects: new CardEffectBase[] { NewAttackEffect(6) });
            var fixture = NewFixture(new[] { card }, maxStamina: 3);

            Assert.IsFalse(fixture.UseCase.Queue.CanSelectCard(card));
        }

        // 選べない理由がメモリー不足かどうか（コストの数字の色を変えるのに使う）。実行リストが一杯かどうかは見ない
        [Test]
        public void CanAfford_OnlyLooksAtStamina()
        {
            var cheap = NewCard(cost: 0, effects: new CardEffectBase[] { NewAttackEffect(6) });
            var expensive = NewCard(cost: 4, effects: new CardEffectBase[] { NewAttackEffect(6) });
            var fixture = NewFixture(new[] { cheap, cheap, expensive }, maxStamina: 3, maxCommandCount: 1);
            fixture.UseCase.Queue.TrySelectCard(cheap);
            fixture.UseCase.Queue.SelectTarget(fixture.Enemy);

            Assert.IsTrue(fixture.UseCase.Queue.CanAfford(cheap)); // 実行リストは一杯だが、払えはする
            Assert.IsFalse(fixture.UseCase.Queue.CanSelectCard(cheap));
            Assert.IsFalse(fixture.UseCase.Queue.CanAfford(expensive));
        }

        [Test]
        public void CanSelectCard_ReturnsFalse_WhenCommandListIsFull()
        {
            var card = NewCard(cost: 0, effects: new CardEffectBase[] { NewAttackEffect(6) });
            var fixture = NewFixture(new[] { card, card }, maxCommandCount: 1);
            fixture.UseCase.Queue.TrySelectCard(card);
            fixture.UseCase.Queue.SelectTarget(fixture.Enemy);

            Assert.IsFalse(fixture.UseCase.Queue.CanSelectCard(card));
        }
    }
}

using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // カード山札（手札・山札・捨て札）共通の変更通知購読・解除処理をまとめた基底クラス
    public abstract class CardPilePresenterBase : PresenterBase
    {
        protected readonly CardPileModel Pile;

        protected CardPilePresenterBase(CardPileModel cardPileModel)
        {
            Pile = cardPileModel;
        }

        protected override void OnInitialize()
        {
            Pile.OnCardAdded += HandleCardChanged;
            Pile.OnCardRemoved += HandleCardChanged;
            Pile.OnCleared += HandleCleared;

            Sync(Pile);
        }

        protected override void OnDispose()
        {
            Pile.OnCardAdded -= HandleCardChanged;
            Pile.OnCardRemoved -= HandleCardChanged;
            Pile.OnCleared -= HandleCleared;
        }

        protected sealed override void OnLocaleChanged()
        {
            Sync(Pile);
        }

        protected abstract void Sync(CardPileModel cardPileModel);

        private void HandleCardChanged(CardData cardData)
        {
            Sync(Pile);
        }

        private void HandleCleared()
        {
            Sync(Pile);
        }
    }
}

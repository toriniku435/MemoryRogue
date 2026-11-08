using Live2DCardRogue.BattleScene.Views;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.BattleScene.Presenters
{
    public sealed class BattleCardDiscardPilePresenter : CardPilePresenterBase
    {
        private readonly CardPileCountView _countView;

        public BattleCardDiscardPilePresenter(CardPileModel cardPileModel, BattleSceneViewContainer viewContainer)
            : base(cardPileModel)
        {
            _countView = viewContainer.DiscardCountView;
        }

        // OnLocaleChangedがSyncを呼ぶ(CardPilePresenterBase)ので、ラベルもここで設定すれば言語切り替えに追従する
        protected override void Sync(CardPileModel cardPileModel)
        {
            _countView.SetLabel(LocalizationHelper.GetUiText(UiTextKey.DiscardPile));
            _countView.SetCount(cardPileModel.Count);
        }
    }
}

using Live2DCardRogue.BattleScene.Views;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // パージ（除外）されたコマンドの数。捨て札と同じ表示部品で、数とラベルだけを出す
    public sealed class BattleCardPurgePilePresenter : CardPilePresenterBase
    {
        private readonly CardPileCountView _countView;

        public BattleCardPurgePilePresenter(CardPileModel cardPileModel, BattleSceneViewContainer viewContainer)
            : base(cardPileModel)
        {
            _countView = viewContainer.PurgeCountView;
        }

        // OnLocaleChangedがSyncを呼ぶ(CardPilePresenterBase)ので、ラベルもここで設定すれば言語切り替えに追従する
        protected override void Sync(CardPileModel cardPileModel)
        {
            if (_countView == null) return;

            _countView.SetLabel(LocalizationHelper.GetUiText(UiTextKey.PurgePile));
            _countView.SetCount(cardPileModel.Count);
        }
    }
}

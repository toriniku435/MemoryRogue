using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // カードの詳細パネル。カーソルを合わせているカードを大きく見せる。
    // どれにも合わせていないときは、対象選択中のカードを見せ続ける
    // （選んだカードから対象の候補一覧へカーソルを動かす間も、効果を読みながら対象を選べるように）。
    // 持ち主がいるのにカードが無いとき（タッチで状態効果の説明を開いた）は閉じる
    public sealed class BattleCardDetailPresenter : PresenterBase
    {
        private readonly CardPreviewModel _previewModel;
        private readonly CardSelectionModel _selectionModel;
        private readonly CardDetailView _view;

        public BattleCardDetailPresenter(CardPreviewModel previewModel, CardSelectionModel selectionModel, CardDetailView view)
        {
            _previewModel = previewModel;
            _selectionModel = selectionModel;
            _view = view;
        }

        protected override void OnInitialize()
        {
            _previewModel.OnChanged += HandleCardChanged;
            _previewModel.OnOwnerChanged += Sync;
            _selectionModel.OnPendingCardChanged += HandleCardChanged;
            Sync();
        }

        protected override void OnDispose()
        {
            _previewModel.OnChanged -= HandleCardChanged;
            _previewModel.OnOwnerChanged -= Sync;
            _selectionModel.OnPendingCardChanged -= HandleCardChanged;
        }

        // カード名・効果はローカライズテーブル由来なので、言語切り替えで引き直す
        protected override void OnLocaleChanged()
        {
            Sync();
        }

        private void HandleCardChanged(CardData _)
        {
            Sync();
        }

        private void Sync()
        {
            var card = _previewModel.Card != null || _previewModel.Owner != null ? _previewModel.Card : _selectionModel.PendingCard;
            if (card == null) _view.Hide();
            else _view.Show(card);
        }
    }
}

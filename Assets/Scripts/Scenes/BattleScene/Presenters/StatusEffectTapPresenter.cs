using System.Collections.Generic;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.Views;
using Live2DCardRogue.Common.Presenters;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // タッチ用（マウスでは作らない）: ユニット1体の状態効果を、タップで説明を開くようにする。
    // 開いておくのは CardPreviewModel の持ち主になっている行だけなので、ほかのコマンドや状態効果をタップすると閉じる
    // （詳細パネルは持ち主のカードが無いので閉じる。説明は一度に1つしか出さない）
    public sealed class StatusEffectTapPresenter : PresenterBase
    {
        private readonly BattleUnitStatusView _view;
        private readonly CardPreviewModel _previewModel;

        // 持ち主の目印。行ごとに1つ（どのユニットのどの行かを区別できるように）
        private readonly List<object> _rowOwners = new List<object>();

        public StatusEffectTapPresenter(BattleUnitStatusView view, CardPreviewModel previewModel)
        {
            _view = view;
            _previewModel = previewModel;
        }

        protected override void OnInitialize()
        {
            _view.SetStatusEffectsExpandOnHover(false);
            _view.OnStatusEffectClicked += HandleClicked;
            _previewModel.OnOwnerChanged += Sync;
            Sync();
        }

        protected override void OnDispose()
        {
            _view.OnStatusEffectClicked -= HandleClicked;
            _previewModel.OnOwnerChanged -= Sync;
        }

        protected override void OnLocaleChanged() { }

        private void HandleClicked(int index)
        {
            while (_rowOwners.Count <= index) _rowOwners.Add(new object());
            _previewModel.Show(_rowOwners[index], null);
        }

        private void Sync()
        {
            _view.SetExpandedStatusEffect(_rowOwners.IndexOf(_previewModel.Owner));
        }
    }
}

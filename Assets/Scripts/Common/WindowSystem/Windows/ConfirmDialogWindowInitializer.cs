using UnityEngine;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Common.WindowSystem
{
    // 共用の確認ウインドウ（ConfirmDialog.prefab）のコンポジションルート
    public sealed class ConfirmDialogWindowInitializer : WindowInitializerBase<ConfirmDialogInitData, ConfirmDialogPresenter>
    {
        [SerializeField] private ConfirmDialogView _view;

        protected override ConfirmDialogPresenter Initialize(ConfirmDialogInitData data)
        {
            return new ConfirmDialogPresenter(_view);
        }
    }
}

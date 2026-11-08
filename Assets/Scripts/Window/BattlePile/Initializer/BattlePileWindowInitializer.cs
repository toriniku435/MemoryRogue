using UnityEngine;
using Live2DCardRogue.Common.WindowSystem;
using Live2DCardRogue.Window.BattlePile.Presenters;
using Live2DCardRogue.Window.BattlePile.Views;

namespace Live2DCardRogue.Window.BattlePile.Initializer
{
    // アーカイブ・パージの中身の一覧（BattlePileWindow.prefab）のコンポジションルート
    public sealed class BattlePileWindowInitializer : WindowInitializerBase<BattlePileInitData, BattlePileWindowPresenter>
    {
        [SerializeField] private BattlePileWindowView _view;

        protected override BattlePileWindowPresenter Initialize(BattlePileInitData data)
        {
            return new BattlePileWindowPresenter(_view, data.Archive, data.Purge);
        }
    }
}

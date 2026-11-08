using UnityEngine;
using Live2DCardRogue.Common.WindowSystem;
using Live2DCardRogue.Window.UnitUpgrade.Presenters;
using Live2DCardRogue.Window.UnitUpgrade.Views;

namespace Live2DCardRogue.Window.UnitUpgrade.Initializer
{
    // ユニット強化（UnitUpgradeWindow.prefab）のコンポジションルート
    public sealed class UnitUpgradeWindowInitializer : WindowInitializerBase<UnitUpgradeInitData, UnitUpgradePresenter>
    {
        [SerializeField] private UnitUpgradeWindowView _view;

        protected override UnitUpgradePresenter Initialize(UnitUpgradeInitData data)
        {
            return new UnitUpgradePresenter(data.Model, _view, data.MapTokenCounter, data.Confirm, data.OnUpgraded);
        }
    }
}

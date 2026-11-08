using UnityEngine;
using Live2DCardRogue.Common.WindowSystem;
using Live2DCardRogue.Window.Menu.Presenters;
using Live2DCardRogue.Window.Menu.Views;

namespace Live2DCardRogue.Window.Menu.Initializer
{
    // ボタンを縦に並べるだけのウインドウ（MenuWindow.prefab）のコンポジションルート
    public sealed class MenuWindowInitializer : WindowInitializerBase<MenuWindowInitData, MenuWindowPresenter>
    {
        [SerializeField] private MenuWindowView _view;

        protected override MenuWindowPresenter Initialize(MenuWindowInitData data)
        {
            return new MenuWindowPresenter(_view, data);
        }
    }
}

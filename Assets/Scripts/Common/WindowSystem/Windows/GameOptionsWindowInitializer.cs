using UnityEngine;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Common.WindowSystem
{
    // オプション（GameOptionsWindow.prefab）のコンポジションルート
    public sealed class GameOptionsWindowInitializer : WindowInitializerBase<GameOptionsInitData, GameOptionsPresenter>
    {
        [SerializeField] private GameOptionsWindowView _view;

        protected override GameOptionsPresenter Initialize(GameOptionsInitData data)
        {
            return new GameOptionsPresenter(_view, data.Confirm, data.OnDeleteUserData);
        }
    }
}

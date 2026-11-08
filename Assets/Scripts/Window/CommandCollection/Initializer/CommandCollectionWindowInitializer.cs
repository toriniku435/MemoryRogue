using UnityEngine;
using Live2DCardRogue.Common.WindowSystem;
using Live2DCardRogue.Window.CommandCollection.Presenters;
using Live2DCardRogue.Window.CommandCollection.Views;

namespace Live2DCardRogue.Window.CommandCollection.Initializer
{
    // コマンドのコレクション（CommandCollectionWindow.prefab）のコンポジションルート
    public sealed class CommandCollectionWindowInitializer : WindowInitializerBase<CommandCollectionInitData, CommandCollectionPresenter>
    {
        [SerializeField] private CommandCollectionWindowView _view;

        protected override CommandCollectionPresenter Initialize(CommandCollectionInitData data)
        {
            return new CommandCollectionPresenter(_view, data.Data, data.GetDiscoveredIds);
        }
    }
}

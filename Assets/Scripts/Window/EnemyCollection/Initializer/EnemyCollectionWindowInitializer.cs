using UnityEngine;
using Live2DCardRogue.Common.Views;
using Live2DCardRogue.Common.WindowSystem;
using Live2DCardRogue.Window.EnemyCollection.Presenters;

namespace Live2DCardRogue.Window.EnemyCollection.Initializer
{
    // エネミーのコレクション（EnemyCollectionWindow.prefab。View は敵詳細と同じ）のコンポジションルート
    public sealed class EnemyCollectionWindowInitializer : WindowInitializerBase<EnemyCollectionInitData, EnemyCollectionPresenter>
    {
        [SerializeField] private EnemyDetailWindowView _view;

        protected override EnemyCollectionPresenter Initialize(EnemyCollectionInitData data)
        {
            return new EnemyCollectionPresenter(_view, data.Data, data.GetDiscoveredIds);
        }
    }
}

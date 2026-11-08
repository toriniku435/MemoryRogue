using UnityEngine;
using Live2DCardRogue.Common.Views;
using Live2DCardRogue.Common.WindowSystem;
using Live2DCardRogue.Window.EnemyDetail.Presenters;

namespace Live2DCardRogue.Window.EnemyDetail.Initializer
{
    // 次に戦う相手の詳細（EnemyDetailWindow.prefab）のコンポジションルート
    public sealed class EnemyDetailWindowInitializer : WindowInitializerBase<EnemyDetailInitData, EnemyDetailPresenter>
    {
        [SerializeField] private EnemyDetailWindowView _view;

        protected override EnemyDetailPresenter Initialize(EnemyDetailInitData data)
        {
            return new EnemyDetailPresenter(_view, data.CurrentStage);
        }
    }
}

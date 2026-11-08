using UnityEngine;
using Live2DCardRogue.Common.WindowSystem;
using Live2DCardRogue.Window.Reward.Presenters;
using Live2DCardRogue.Window.Reward.Views;

namespace Live2DCardRogue.Window.Reward.Initializer
{
    // ステージの報酬（StageRewardWindow.prefab）のコンポジションルート
    public sealed class StageRewardWindowInitializer : WindowInitializerBase<StageRewardInitData, StageRewardPresenter>
    {
        [SerializeField] private StageRewardView _view;

        protected override StageRewardPresenter Initialize(StageRewardInitData data)
        {
            return new StageRewardPresenter(_view);
        }
    }
}

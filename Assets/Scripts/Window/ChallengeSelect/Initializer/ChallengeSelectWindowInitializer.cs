using UnityEngine;
using Live2DCardRogue.Common.WindowSystem;
using Live2DCardRogue.Window.ChallengeSelect.Presenters;
using Live2DCardRogue.Window.ChallengeSelect.Views;

namespace Live2DCardRogue.Window.ChallengeSelect.Initializer
{
    // チャレンジの選択（ChallengeSelectWindow.prefab）のコンポジションルート
    public sealed class ChallengeSelectWindowInitializer : WindowInitializerBase<ChallengeSelectInitData, ChallengeSelectPresenter>
    {
        [SerializeField] private ChallengeSelectWindowView _view;

        protected override ChallengeSelectPresenter Initialize(ChallengeSelectInitData data)
        {
            return new ChallengeSelectPresenter(_view, data.Catalog, data.Confirm, data.OnChallengeChosen, data.IsCleared, data.GetBestLayer);
        }
    }
}

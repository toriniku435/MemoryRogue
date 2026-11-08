using UnityEngine;
using Live2DCardRogue.Common.WindowSystem;
using Live2DCardRogue.Window.EpisodeSelect.Presenters;
using Live2DCardRogue.Window.EpisodeSelect.Views;

namespace Live2DCardRogue.Window.EpisodeSelect.Initializer
{
    // ストーリーのエピソード選択（EpisodeSelectWindow.prefab）のコンポジションルート
    public sealed class EpisodeSelectWindowInitializer : WindowInitializerBase<EpisodeSelectInitData, EpisodeSelectPresenter>
    {
        [SerializeField] private EpisodeSelectWindowView _view;

        protected override EpisodeSelectPresenter Initialize(EpisodeSelectInitData data)
        {
            return new EpisodeSelectPresenter(_view, data.Confirm, data.GetState, data.OnEpisodeChosen, data.GetBestRank);
        }
    }
}

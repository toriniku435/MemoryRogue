using UnityEngine;
using Live2DCardRogue.Common.Views;
using Live2DCardRogue.Common.WindowSystem;
using Live2DCardRogue.Window.FirstBuild.Presenters;

namespace Live2DCardRogue.Window.FirstBuild.Initializer
{
    // ファーストビルドの開封（ChipInstallWindow.prefab。マーケットの中にも同じプレハブがあるが、そちらからは外してある）のコンポジションルート
    public sealed class FirstBuildWindowInitializer : WindowInitializerBase<FirstBuildInitData, FirstBuildPresenter>
    {
        [SerializeField] private ChipInstallWindowView _view;

        protected override FirstBuildPresenter Initialize(FirstBuildInitData data)
        {
            return new FirstBuildPresenter(_view);
        }
    }
}

using System;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.EndingScene.Views;

namespace Live2DCardRogue.EndingScene.Presenters
{
    // 仮のエンディング。画面のどこかが押されたら、次へ進むことを報告するだけ。
    // ルートごとの中身ができたら、ここ（か、ルート別の View）で Route を見て出し分ける
    public sealed class EndingPresenter : PresenterBase
    {
        private readonly EndingSceneViewContainer _viewContainer;
        private readonly Action _onContinue;

        public EndingPresenter(EndingSceneViewContainer viewContainer, Action onContinue)
        {
            _viewContainer = viewContainer;
            _onContinue = onContinue;
        }

        protected override void OnInitialize()
        {
            _viewContainer.TapArea.OnClicked += HandleTapped;
        }

        protected override void OnDispose()
        {
            _viewContainer.TapArea.OnClicked -= HandleTapped;
        }

        // 仮の画面の文字（"Ending"）は飾りの英語で、ローカライズしていない
        protected override void OnLocaleChanged()
        {
        }

        private void HandleTapped()
        {
            _onContinue.Invoke();
        }
    }
}

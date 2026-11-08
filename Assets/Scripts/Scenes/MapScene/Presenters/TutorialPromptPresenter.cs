using System;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.WindowSystem;

namespace Live2DCardRogue.MapScene.Presenters
{
    // 新しいキャンペーンでマップに来た直後の「チュートリアルを見ますか？」（見る / 見ない）。
    // 確認ウインドウはマップのほかの確認（接続開始・ユニット強化など）と共用。
    // 背面のクリックでは閉じない（うっかり触れて「見ない」にならないよう、どちらかのボタンで答えてもらう）
    public sealed class TutorialPromptPresenter : PresenterBase
    {
        private readonly WindowHandler<ConfirmShowData> _confirm;
        private readonly Action<bool> _onAnswered;

        /// <param name="onAnswered">true: チュートリアルを見る</param>
        public TutorialPromptPresenter(WindowHandler<ConfirmShowData> confirm, Action<bool> onAnswered)
        {
            _confirm = confirm;
            _onAnswered = onAnswered;
        }

        protected override void OnInitialize() { }

        protected override void OnDispose() { }

        protected override void OnLocaleChanged() { }

        public void Open()
        {
            _confirm.Show(new ConfirmShowData(
                LocalizationHelper.GetUiText(UiTextKey.TutorialPrompt),
                LocalizationHelper.GetUiText(UiTextKey.TutorialPromptDetail),
                LocalizationHelper.GetUiText(UiTextKey.TutorialWatch),
                LocalizationHelper.GetUiText(UiTextKey.TutorialNoThanks),
                () => _onAnswered.Invoke(true),
                () => _onAnswered.Invoke(false),
                cancelOnBackground: false));
        }
    }
}

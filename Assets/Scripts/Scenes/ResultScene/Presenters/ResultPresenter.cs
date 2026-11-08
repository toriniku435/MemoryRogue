using System;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.ResultScene.Views;

namespace Live2DCardRogue.ResultScene.Presenters
{
    // リザルト画面を受け持つPresenter。ラベルを流し込み、成績を View に渡して演出を始め、
    // 「タイトルへ」が押されたことを報告する
    public sealed class ResultPresenter : PresenterBase
    {
        private readonly ResultSceneViewContainer _viewContainer;
        private readonly CampaignResult _result;
        private readonly Action _onBackToTitle;

        public ResultPresenter(ResultSceneViewContainer viewContainer, CampaignResult result, Action onBackToTitle)
        {
            _viewContainer = viewContainer;
            _result = result;
            _onBackToTitle = onBackToTitle;
        }

        protected override void OnInitialize()
        {
            _viewContainer.BackToTitleButton.onClick.AddListener(HandleBackToTitle);
            ApplyLabels();
            _viewContainer.ResultView.Play(_result, onFinished: null);
        }

        protected override void OnDispose()
        {
            _viewContainer.BackToTitleButton.onClick.RemoveListener(HandleBackToTitle);
        }

        protected override void OnLocaleChanged()
        {
            ApplyLabels();
        }

        private void ApplyLabels()
        {
            _viewContainer.TitleLabel.text = LocalizationHelper.GetUiText(UiTextKey.Result);
            _viewContainer.TokenRowLabel.text = LocalizationHelper.GetUiText(UiTextKey.ResultRemainingTokens);
            _viewContainer.BackupRowLabel.text = LocalizationHelper.GetUiText(UiTextKey.ResultBackupBoosts);
            _viewContainer.ScoreLabel.text = LocalizationHelper.GetUiText(UiTextKey.Score);
            _viewContainer.RankLabel.text = LocalizationHelper.GetUiText(UiTextKey.Rank);
            _viewContainer.BackToTitleLabel.text = LocalizationHelper.GetUiText(UiTextKey.BackToTitle);
        }

        private void HandleBackToTitle()
        {
            _onBackToTitle.Invoke();
        }
    }
}

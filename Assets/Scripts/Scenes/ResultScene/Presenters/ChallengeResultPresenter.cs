using System;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.ResultScene.Views;
using Live2DCardRogue.Common.SceneTransition;

namespace Live2DCardRogue.ResultScene.Presenters
{
    // チャレンジのリザルトを受け持つPresenter。ストーリーと同じ画面・演出で、
    // 内訳を突破した階層と最高記録に、ランクを到達した階層（踏破したなら「踏破」と添えて最後の階層）に置き換えて見せる。
    // 演出のあとは画面のタップでも「タイトルへ」でも、タイトルへ戻ったことを報告する
    public sealed class ChallengeResultPresenter : PresenterBase
    {
        private readonly ResultSceneViewContainer _viewContainer;
        private readonly ChallengeResultTransitionData _result;
        private readonly Action _onBackToTitle;
        private bool _isLeaving;

        public ChallengeResultPresenter(ResultSceneViewContainer viewContainer, ChallengeResultTransitionData result, Action onBackToTitle)
        {
            _viewContainer = viewContainer;
            _result = result;
            _onBackToTitle = onBackToTitle;
        }

        protected override void OnInitialize()
        {
            _viewContainer.BackToTitleButton.onClick.AddListener(HandleBackToTitle);
            _viewContainer.ResultView.OnTapAfterFinished += HandleBackToTitle;
            ApplyLabels();
            // 踏破したなら最後の階層まで突破している（負けたなら、負けた階層の1つ前まで）
            int clearedLayers = _result.IsCleared ? _result.ReachedLayer : _result.ReachedLayer - 1;
            _viewContainer.ResultView.PlayEndless(
                _result.ReachedLayer, clearedLayers, _result.BestLayer, _result.IsNewRecord,
                LocalizationHelper.GetUiText(UiTextKey.ChallengeNewRecord), onFinished: null);
        }

        protected override void OnDispose()
        {
            _viewContainer.BackToTitleButton.onClick.RemoveListener(HandleBackToTitle);
            _viewContainer.ResultView.OnTapAfterFinished -= HandleBackToTitle;
        }

        protected override void OnLocaleChanged()
        {
            ApplyLabels();
        }

        private void ApplyLabels()
        {
            _viewContainer.TitleLabel.text = string.Format(
                LocalizationHelper.GetUiText(UiTextKey.ChallengeResult), LocalizationHelper.GetChallengeName(_result.Challenge));
            _viewContainer.TokenRowLabel.text = LocalizationHelper.GetUiText(UiTextKey.ChallengeClearedLayers);
            _viewContainer.BackupRowLabel.text = LocalizationHelper.GetUiText(UiTextKey.ChallengeBestLayer);
            _viewContainer.RankLabel.text = LocalizationHelper.GetUiText(_result.IsCleared ? UiTextKey.ChallengeCleared : UiTextKey.ChallengeReachedLayer);
            _viewContainer.BackToTitleLabel.text = LocalizationHelper.GetUiText(UiTextKey.BackToTitle);
        }

        // ボタンとタップの両方から来るので、一度だけ報告する
        private void HandleBackToTitle()
        {
            if (_isLeaving) return;
            _isLeaving = true;
            _onBackToTitle.Invoke();
        }
    }
}

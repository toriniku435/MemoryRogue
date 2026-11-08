using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.WindowSystem;
using Live2DCardRogue.Window.Reward.Views;

namespace Live2DCardRogue.Window.Reward.Presenters
{
    // ステージの勝敗の後に出す報酬ウインドウ（勝てばクリア報酬、負ければバックアップブースト）。
    // 受け取ったら閉じて OnClaimed を呼ぶ。その後の数え上げや移動はマップ（呼ぶ側）が行う
    public sealed class StageRewardPresenter : PresenterBase, IWindowPresenter<StageRewardShowData>
    {
        private readonly StageRewardView _view;
        // 見せている報酬（まだ開いていなければ null）
        private StageRewardShowData _request;

        public StageRewardPresenter(StageRewardView view)
        {
            _view = view;
        }

        public bool IsOpen => _view.IsOpen;

        protected override void OnInitialize()
        {
            _view.OnClaimClicked += HandleClaimed;
        }

        protected override void OnDispose()
        {
            _view.OnClaimClicked -= HandleClaimed;
        }

        protected override void OnLocaleChanged()
        {
            if (_request == null) return;
            ApplyTexts();
            _view.RefreshCard();
        }

        public void Open(StageRewardShowData request)
        {
            _request = request;
            ApplyTexts();
            _view.Open(request.Reward, request.Detail, request.TotalAfter);
        }

        public void Close()
        {
            _view.Close(onClosed: null);
        }

        public void HideImmediate()
        {
            _view.HideImmediate();
        }

        // 勝ったときは報酬らしく、負けたときは責めずに立て直しを促す文言にする
        private void ApplyTexts()
        {
            bool victory = _request.Reward.IsVictory;
            _view.SetTexts(
                LocalizationHelper.GetUiText(victory ? UiTextKey.VictoryRewardTitle : UiTextKey.DefeatRewardTitle),
                LocalizationHelper.GetUiText(victory ? UiTextKey.VictoryRewardMessage : UiTextKey.DefeatRewardMessage),
                LocalizationHelper.GetUiText(UiTextKey.Tokens),
                LocalizationHelper.GetUiText(victory ? UiTextKey.RewardClaim : UiTextKey.BackupAccept));
        }

        private void HandleClaimed()
        {
            Close();
            _request?.OnClaimed?.Invoke();
        }
    }
}

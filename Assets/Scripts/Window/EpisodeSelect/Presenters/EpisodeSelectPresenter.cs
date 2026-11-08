using System;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Message;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.Views;
using Live2DCardRogue.Window.EpisodeSelect.Models;
using Live2DCardRogue.Window.EpisodeSelect.Views;
using Live2DCardRogue.Common.WindowSystem;

namespace Live2DCardRogue.Window.EpisodeSelect.Presenters
{
    // ストーリーのエピソード選択ウインドウを受け持つPresenter。
    // 遊べるエピソードは確認ウインドウで確かめてから onEpisodeChosen で伝える。
    // まだ選べないエピソード（押せない見た目）が押されたら、選べるようになる条件を、選べるが中身がまだ無いエピソードは準備中と伝える。
    // クリアしたエピソードには CLEAR と一番良いランクを出す（クリア記録から Initializer が引く）。
    // どのエピソードがどの状態か・選ばれたら何をするかは Initializer が決める
    public sealed class EpisodeSelectPresenter : PresenterBase, IWindowPresenter
    {
        private readonly EpisodeSelectWindowView _view;
        private readonly WindowHandler<ConfirmShowData> _confirm;
        private readonly Func<int, EpisodeState> _getState;
        private readonly Action<int> _onEpisodeChosen;
        private readonly Func<int, CampaignRank?> _getBestRank;

        /// <param name="confirm">始めてよいかを確かめる確認ウインドウ（タイトルで共用）</param>
        /// <param name="getState">そのエピソード（0始まり）の状態（開くたびに聞き直す）</param>
        /// <param name="onEpisodeChosen">エピソードを始めると決めた（0始まり）</param>
        /// <param name="getBestRank">そのエピソード（0始まり）のクリアで取った一番良いランク。クリアしていなければ null</param>
        public EpisodeSelectPresenter(EpisodeSelectWindowView view, WindowHandler<ConfirmShowData> confirm, Func<int, EpisodeState> getState, Action<int> onEpisodeChosen,
            Func<int, CampaignRank?> getBestRank)
        {
            _getBestRank = getBestRank;
            _view = view;
            _confirm = confirm;
            _getState = getState;
            _onEpisodeChosen = onEpisodeChosen;
        }

        protected override void OnInitialize()
        {
            _view.OnEpisodeClicked += HandleEpisodeClicked;
            _view.OnUnavailableEpisodeClicked += HandleUnavailableEpisodeClicked;
            _view.OnCloseRequested += Close;
            ApplyTexts();
        }

        protected override void OnDispose()
        {
            _view.OnEpisodeClicked -= HandleEpisodeClicked;
            _view.OnUnavailableEpisodeClicked -= HandleUnavailableEpisodeClicked;
            _view.OnCloseRequested -= Close;
        }

        protected override void OnLocaleChanged()
        {
            ApplyTexts();
        }

        public bool IsOpen => _view.IsOpen;

        public void HideImmediate()
        {
            _view.HideImmediate();
        }

        public void Open()
        {
            ApplyTexts();
            _view.Open();
        }

        private void ApplyTexts()
        {
            _view.SetTexts(LocalizationHelper.GetUiText(UiTextKey.EpisodeSelect), LocalizationHelper.GetUiText(UiTextKey.Close));
            for (int i = 0; i < _view.EpisodeCount; i++) _view.ShowEpisode(i, EpisodeName(i), _getState.Invoke(i) != EpisodeState.Locked, _getBestRank.Invoke(i));
        }

        private static string EpisodeName(int index)
        {
            return string.Format(LocalizationHelper.GetUiText(UiTextKey.EpisodeName), index + 1);
        }

        // 始めてよいかを確かめてから、ウインドウを閉じて伝える。中身がまだ無ければ準備中と伝える
        private void HandleEpisodeClicked(int index)
        {
            if (_getState.Invoke(index) != EpisodeState.Playable)
            {
                MessageUseCase.ShowMessage($"{EpisodeName(index)}は準備中です");
                return;
            }

            _confirm.Show(new ConfirmShowData(
                string.Format(LocalizationHelper.GetUiText(UiTextKey.EpisodeStartConfirm), EpisodeName(index)), null,
                LocalizationHelper.GetUiText(UiTextKey.EpisodeStartOk), LocalizationHelper.GetUiText(UiTextKey.Back),
                () =>
                {
                    Close();
                    _onEpisodeChosen.Invoke(index);
                }));
        }

        // 押せない見た目のエピソード。選べるようになる条件を伝える
        private void HandleUnavailableEpisodeClicked(int index)
        {
            int required = EpisodeUnlockRule.RequiredEpisode(index);
            MessageUseCase.ShowMessage(required >= 0
                ? $"{EpisodeName(required)}をクリアすると選べるようになります"
                : $"{EpisodeName(index)}はまだ選べません");
        }

        public void Close()
        {
            _view.Close();
        }
    }
}

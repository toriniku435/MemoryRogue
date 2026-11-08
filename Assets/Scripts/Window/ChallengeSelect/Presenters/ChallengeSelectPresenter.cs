using System;
using UnityEngine;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Views;
using Live2DCardRogue.Window.ChallengeSelect.Views;
using Live2DCardRogue.Common.WindowSystem;

namespace Live2DCardRogue.Window.ChallengeSelect.Presenters
{
    // チャレンジの選択ウインドウを受け持つPresenter。ボタンには ChallengeCatalog の並びで名前と階層数を出し
    // （「ノーマル  30階層」、終わりの無いエンドレスは名前だけ）、押されたら確認ウインドウで確かめてから onChallengeChosen で伝える。
    // 踏破したことがあるチャレンジ（階層数のあるものだけ）にはクリアマークを、終わりの無いもの（エンドレス）には最高到達階層を出す。
    // どちらも開くたびに聞き直す
    // 選ばれたら何をするかは Initializer が決める
    public sealed class ChallengeSelectPresenter : PresenterBase, IWindowPresenter
    {
        private readonly ChallengeSelectWindowView _view;
        private readonly ChallengeCatalog _catalog;
        private readonly WindowHandler<ConfirmShowData> _confirm;
        private readonly Action<ChallengeCatalog.Entry> _onChallengeChosen;
        private readonly Func<ChallengeType, bool> _isCleared;
        private readonly Func<ChallengeType, int> _getBestLayer;

        /// <param name="confirm">始めてよいかを確かめる確認ウインドウ（タイトルで共用）</param>
        /// <param name="isCleared">そのチャレンジを踏破したことがあるか（クリア記録を読む）</param>
        /// <param name="getBestLayer">そのチャレンジの最高到達階層（まだ遊んでいなければ 0）</param>
        public ChallengeSelectPresenter(ChallengeSelectWindowView view, ChallengeCatalog catalog, WindowHandler<ConfirmShowData> confirm,
            Action<ChallengeCatalog.Entry> onChallengeChosen, Func<ChallengeType, bool> isCleared, Func<ChallengeType, int> getBestLayer)
        {
            _getBestLayer = getBestLayer;
            _isCleared = isCleared;
            _view = view;
            _catalog = catalog;
            _confirm = confirm;
            _onChallengeChosen = onChallengeChosen;
        }

        protected override void OnInitialize()
        {
            _view.OnChallengeClicked += HandleChallengeClicked;
            _view.OnCloseRequested += Close;
            ApplyTexts();
        }

        protected override void OnDispose()
        {
            _view.OnChallengeClicked -= HandleChallengeClicked;
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
            _view.SetTexts(LocalizationHelper.GetUiText(UiTextKey.ChallengeSelect), LocalizationHelper.GetUiText(UiTextKey.Close));
            var challenges = _catalog != null ? _catalog.Challenges : null;
            if (challenges != null && challenges.Count > _view.ButtonCount)
            {
                Debug.LogError($"チャレンジ（{challenges.Count}）に対してボタン（{_view.ButtonCount}）が足りません");
            }
            for (int i = 0; i < _view.ButtonCount; i++)
            {
                bool exists = challenges != null && i < challenges.Count;
                // 終わりの無いチャレンジ（エンドレス）は踏破が無いので、印は出さない
                bool cleared = exists && challenges[i].LayerCount > 0 && _isCleared.Invoke(challenges[i].Type);
                // 終わりの無いチャレンジは踏破の代わりに、どこまで潜ったかを出す
                int bestLayer = exists && challenges[i].LayerCount == 0 ? _getBestLayer.Invoke(challenges[i].Type) : 0;
                _view.SetChallenge(i, exists ? Label(challenges[i]) : null, cleared, bestLayer);
            }
        }

        // 「ノーマル  30階層」。終わりの無いチャレンジは名前だけ
        private static string Label(ChallengeCatalog.Entry entry)
        {
            string name = LocalizationHelper.GetChallengeName(entry.Type);
            return entry.LayerCount > 0
                ? string.Format(LocalizationHelper.GetUiText(UiTextKey.ChallengeButtonLabel), name, entry.LayerCount)
                : name;
        }

        // 始めてよいかを確かめてから、ウインドウを閉じて伝える
        private void HandleChallengeClicked(int index)
        {
            if (_catalog == null || index < 0 || index >= _catalog.Challenges.Count) return;
            var entry = _catalog.Challenges[index];
            _confirm.Show(new ConfirmShowData(
                string.Format(LocalizationHelper.GetUiText(UiTextKey.ChallengeStartConfirm), LocalizationHelper.GetChallengeName(entry.Type)), Label(entry),
                LocalizationHelper.GetUiText(UiTextKey.EpisodeStartOk), LocalizationHelper.GetUiText(UiTextKey.Back),
                () =>
                {
                    Close();
                    _onChallengeChosen.Invoke(entry);
                }));
        }

        public void Close()
        {
            _view.Close();
        }
    }
}

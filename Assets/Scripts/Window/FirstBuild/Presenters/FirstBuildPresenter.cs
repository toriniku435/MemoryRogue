using System.Threading;
using Cysharp.Threading.Tasks;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Message;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.SceneTransition;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Views;
using Live2DCardRogue.Common.WindowSystem;

namespace Live2DCardRogue.Window.FirstBuild.Presenters
{
    // キャンペーンの最初に開けたチップ（ファーストビルド）を、マップの上で開封演出として見せる。
    // 中身とデッキはもう決まって保存済みで、ここは見せるだけ（途中で閉じても結果は変わらない）。
    // 閉じたら、自動でデッキにセットしたことをトーストで伝える
    public sealed class FirstBuildPresenter : PresenterBase, IWindowPresenter<FirstBuildShowData>
    {
        private readonly ChipInstallWindowView _view;
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

        // 見せている（見せようとしている）ファーストビルド。まだ開いていなければ null
        private FirstBuildShowData _request;

        public FirstBuildPresenter(ChipInstallWindowView view)
        {
            _view = view;
        }

        public bool IsOpen => _view.IsOpen;

        protected override void OnInitialize()
        {
            _view.CloseButton.onClick.AddListener(Close);
        }

        protected override void OnDispose()
        {
            _cancellation.Cancel();
            _cancellation.Dispose();
            _view.CloseButton.onClick.RemoveListener(Close);
        }

        protected override void OnLocaleChanged()
        {
            if (_request == null) return;

            ApplyTexts();
            _view.RefreshTexts();
        }

        /// <summary>開けたチップを見せる。コマンドが無ければ何もしない（再開したときや、ファーストビルドの無い設定）</summary>
        public void Open(FirstBuildShowData request)
        {
            if (request == null || request.Cards == null || request.Cards.Count == 0) return;

            _request = request;
            ApplyTexts();
            ShowWhenReadyAsync(_cancellation.Token).Forget();
        }

        // 開封の演出のまま閉じる。デッキにセットしたことはここで伝える
        public void Close()
        {
            _view.Close();
            if (_request != null) MessageUseCase.ShowMessage(string.Format(LocalizationHelper.GetUiText(UiTextKey.FirstBuildDeckSet), _request.DeckCount));
        }

        public void HideImmediate()
        {
            _view.Hide();
        }

        // 画面の切り替えの覆いが晴れてから開く（覆いの下で演出が進まないように）
        private async UniTaskVoid ShowWhenReadyAsync(CancellationToken token)
        {
            await UniTask.WaitUntil(() => !SceneTransitionEffect.IsTransitioning, cancellationToken: token);
            _view.Show(_request.Cards, HandleCardHovered, HandleCardUnhovered);
        }

        private void ApplyTexts()
        {
            _view.TitleText.text = string.Format(LocalizationHelper.GetUiText(UiTextKey.FirstBuildTitle), _request.ChipCount);
            _view.CloseLabel.text = LocalizationHelper.GetUiText(UiTextKey.FirstBuildConfirm);
        }

        private void HandleCardHovered(CardData card)
        {
            _view.ShowDetail(card);
        }

        private void HandleCardUnhovered(CardData card)
        {
            _view.HideDetail();
        }
    }
}

using Live2DCardRogue.Common.Views;
using Live2DCardRogue.Common.WindowSystem;

namespace Live2DCardRogue.Common.Presenters
{
    // 「OK / キャンセル」の確認ウインドウ。シーンに1つ作り、尋ねたい Presenter で共用する（WindowHandler<ConfirmShowData> 越しに）。
    // 答えは尋ねたときのコールバックにだけ返すので、ほかの Presenter が開いた確認に反応することは無い
    public sealed class ConfirmDialogPresenter : PresenterBase, IWindowPresenter<ConfirmShowData>
    {
        private readonly ConfirmDialogView _view;
        private ConfirmShowData _current;

        public ConfirmDialogPresenter(ConfirmDialogView view)
        {
            _view = view;
        }

        public bool IsOpen => _view.IsOpen;

        protected override void OnInitialize()
        {
            _view.OnOkClicked += HandleOk;
            _view.OnCancelRequested += HandleCancel;
        }

        protected override void OnDispose()
        {
            _view.OnOkClicked -= HandleOk;
            _view.OnCancelRequested -= HandleCancel;
            _current = null;
        }

        // 文言は尋ねる側が渡すので、言語が変わっても引き直すものは無い
        protected override void OnLocaleChanged() { }

        /// <summary>尋ねる。ほかの確認の途中（開いている）なら尋ねない（尋ねられたかは、先に IsOpen で確かめる）</summary>
        public void Open(ConfirmShowData request)
        {
            if (_view.IsOpen) return;
            _current = request;
            _view.Open(request.Message, request.Detail, request.OkLabel, request.CancelLabel, request.CancelOnBackground);
        }

        // 答えずに閉じる（コールバックは呼ばない）
        public void Close()
        {
            _current = null;
            _view.Close();
        }

        public void HideImmediate()
        {
            _current = null;
            _view.HideImmediate();
        }

        private void HandleOk()
        {
            var request = Finish();
            request?.OnOk?.Invoke();
        }

        private void HandleCancel()
        {
            var request = Finish();
            request?.OnCancel?.Invoke();
        }

        // コールバックの中で次の確認を開けるよう、閉じて空にしてから呼ぶ
        private ConfirmShowData Finish()
        {
            var request = _current;
            _current = null;
            _view.Close();
            return request;
        }
    }
}

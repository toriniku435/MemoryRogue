using System;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.WindowSystem;

namespace Live2DCardRogue.Common.Conversation
{
    // 会話パートを流すPresenter。Open で会話を渡すと、ウインドウを開いて1行ずつ出し、読み終えたら onFinished を呼ぶ。
    //
    // 読み終えたときは、先に onFinished（次のウインドウを開く）を呼んでから自分を閉じる。
    // 次のウインドウが開いている間に閉じるので、後ろのぼかしが一度抜けてまたかかる、ということが無い
    // （WindowTransition は最初と最後のウインドウのときだけぼかしの強さを動かす）
    public sealed class ConversationPresenter : PresenterBase, IWindowPresenter<ConversationShowData>
    {
        private readonly ConversationWindowView _view;
        private ConversationData _data;
        private ConversationRunner _runner;
        private Action _onFinished;

        public ConversationPresenter(ConversationWindowView view)
        {
            _view = view;
        }

        public bool IsOpen => _view.IsOpen;

        public void HideImmediate()
        {
            _view.HideImmediate();
        }

        // 途中で閉じる（読み終えたときのコールバックは呼ばない）
        public void Close()
        {
            _runner = null;
            _onFinished = null;
            _view.Close();
        }

        protected override void OnInitialize()
        {
            _view.OnAdvanceRequested += HandleAdvance;
            ApplyTexts();
        }

        protected override void OnDispose()
        {
            _view.OnAdvanceRequested -= HandleAdvance;
        }

        protected override void OnLocaleChanged()
        {
            ApplyTexts();
        }

        private void ApplyTexts()
        {
            _view.SetHint(LocalizationHelper.GetUiText(UiTextKey.TutorialNext));
        }

        /// <summary>会話を流す。流す行が無ければ、すぐに onFinished を呼ぶ</summary>
        public void Open(ConversationShowData request)
        {
            var data = request.Data;
            var onFinished = request.OnFinished;
            if (data == null || !data.HasLines)
            {
                onFinished?.Invoke();
                return;
            }

            _data = data;
            _runner = new ConversationRunner(data.Lines.Count);
            _onFinished = onFinished;
            // 開いてから出す（閉じている間は台詞の文字数を数えられない）
            _view.Open();
            ShowCurrentLine();
        }

        private void HandleAdvance()
        {
            if (_runner == null) return;
            if (_runner.Next())
            {
                ShowCurrentLine();
                return;
            }

            var callback = _onFinished;
            _onFinished = null;
            _runner = null;
            callback?.Invoke();
            _view.Close();
        }

        private void ShowCurrentLine()
        {
            var line = _data.Lines[_runner.Index];
            _view.ShowLine(line?.Speaker, line?.Text);
        }
    }
}

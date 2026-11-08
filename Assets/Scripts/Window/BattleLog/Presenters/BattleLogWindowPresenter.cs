using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.Views;
using UnityEngine;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Window.BattleLog.Views;
using Live2DCardRogue.Common.WindowSystem;

namespace Live2DCardRogue.Window.BattleLog.Presenters
{
    // ログボタンと、それで開く全ログのウインドウを受け持つPresenter。
    // 開いたら一番下（最新）まで送る。開いている間に増えたログは、一番下を見ていたときだけ追いかける
    // （上へ戻って読んでいる途中に、勝手に下へ飛ばないように）
    public sealed class BattleLogWindowPresenter : PresenterBase, IWindowPresenter
    {
        private readonly BattleLogModel _log;
        private readonly BattleLogWindowView _view;
        private readonly BattleLogFormatter _formatter;

        public BattleLogWindowPresenter(BattleLogModel log, BattleLogWindowView view, BattleLogFormatter formatter)
        {
            _log = log;
            _view = view;
            _formatter = formatter;
        }

        public void HideImmediate()
        {
            _view.HideImmediate();
        }

        protected override void OnInitialize()
        {
            _view.List.Initialize(BindRow);
            _view.OnCloseRequested += Close;
            _log.OnAdded += HandleAdded;
            ApplyLabels();
        }

        protected override void OnDispose()
        {
            _view.List.Dispose();
            _view.OnCloseRequested -= Close;
            _log.OnAdded -= HandleAdded;
        }

        protected override void OnLocaleChanged()
        {
            ApplyLabels();
            if (_view.IsOpen) _view.List.Refresh();
        }

        private void ApplyLabels()
        {
            string title = LocalizationHelper.GetUiText(UiTextKey.BattleLog);
            _view.SetTexts(title, LocalizationHelper.GetUiText(UiTextKey.Close));
        }

        public bool IsOpen => _view.IsOpen;

        public void Open()
        {
            if (_view.IsOpen) return;
            _view.Open();
            _view.List.SetItemCount(_log.Entries.Count);
            _view.List.ScrollToEnd();
        }

        public void Close()
        {
            _view.Close();
        }

        private void HandleAdded(BattleLogEntry entry)
        {
            if (!_view.IsOpen) return;

            bool followEnd = _view.List.IsAtEnd;
            _view.List.SetItemCount(_log.Entries.Count);
            if (followEnd) _view.List.ScrollToEnd();
        }

        private void BindRow(int index, RectTransform item)
        {
            item.GetComponent<BattleLogRowView>().Bind(_formatter.Format(_log.Entries[index]));
        }
    }
}

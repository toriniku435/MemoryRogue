using System.Collections.Generic;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.Views;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Window.BattleLog.Presenters;

namespace Live2DCardRogue.BattleScene.Presenters
{
    // 行動中のログ（上部中央）を受け持つPresenter。
    // 実行ボタンが押されてカードが実行され始めたら（PlayerExecution）開き、敵の番を経て
    // プレイヤーの番に戻ったら（Draw / Player）閉じる。開いた時点の直近のターンの区切りから先を見せる
    public sealed class BattleLogFeedPresenter : PresenterBase
    {
        private readonly BattleLogModel _log;
        private readonly BattlePhaseModel _phaseModel;
        private readonly BattleLogFeedView _view;
        private readonly BattleLogFormatter _formatter;
        private readonly List<BattleLogLine> _lines = new List<BattleLogLine>();

        private bool _isShowing;
        // 見せ始める記録の位置
        private int _startIndex;

        public BattleLogFeedPresenter(BattleLogModel log, BattlePhaseModel phaseModel, BattleLogFeedView view, BattleLogFormatter formatter)
        {
            _log = log;
            _phaseModel = phaseModel;
            _view = view;
            _formatter = formatter;
        }

        protected override void OnInitialize()
        {
            _log.OnAdded += HandleAdded;
            _phaseModel.OnPhaseChanged += HandlePhaseChanged;
            _view.HideImmediate();
        }

        protected override void OnDispose()
        {
            _log.OnAdded -= HandleAdded;
            _phaseModel.OnPhaseChanged -= HandlePhaseChanged;
        }

        // ログの文章はローカライズテーブル由来なので、言語切り替えで引き直す
        protected override void OnLocaleChanged()
        {
            if (_isShowing) Refresh(animateNewest: false);
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            if (phase == BattlePhase.PlayerExecution || phase == BattlePhase.Enemy)
            {
                if (_isShowing) return;
                _isShowing = true;
                _startIndex = LastTurnHeaderIndex();
                _view.Open();
                Refresh(animateNewest: false);
                return;
            }

            if (_isShowing)
            {
                _isShowing = false;
                _view.Close();
            }
        }

        private void HandleAdded(BattleLogEntry entry)
        {
            if (_isShowing) Refresh(animateNewest: true);
        }

        private void Refresh(bool animateNewest)
        {
            var entries = _log.Entries;
            int from = System.Math.Max(_startIndex, entries.Count - _view.MaxRows);
            _lines.Clear();
            for (int i = from; i < entries.Count; i++)
            {
                _lines.Add(_formatter.Format(entries[i]));
            }
            _view.SetLines(_lines, animateNewest);
        }

        // 直近のターンの区切り（あなたの番 / 敵の番）の位置。無ければ先頭
        private int LastTurnHeaderIndex()
        {
            var entries = _log.Entries;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].Kind == BattleLogKind.PlayerTurn || entries[i].Kind == BattleLogKind.EnemyTurn) return i;
            }
            return 0;
        }
    }
}

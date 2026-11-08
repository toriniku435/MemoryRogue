using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Window.BattleLog.Views;
using Live2DCardRogue.Common.Presenters;

namespace Live2DCardRogue.BattleScene.Views
{
    // 行動中に上部中央（カードの詳細パネルと同じ場所）に出すログ。
    // 最新の数行だけを下詰めで並べ、新しい行はちらつきながら入ってくる。
    // 開くときは横一線から開き、閉じるときは少し待ってから（最後の行が読めるように）横一線に潰れて消える。
    // 行はスクリプトで並べる（手札と同じく Layout Group は使わない）。時間は実時間で進め、1フレームの上限を付けている
    public sealed class BattleLogFeedView : MonoBehaviour
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private CanvasGroup _panelGroup;
        [SerializeField] private RectTransform _rowParent;
        [SerializeField] private BattleLogRowView _rowPrefab;
        [SerializeField][Min(1)] private int _maxRows = 7;
        [SerializeField] private float _rowHeight = 36f;

        [SerializeField][Min(0.01f)] private float _openDuration = 0.12f;
        [SerializeField][Min(0.01f)] private float _closeDuration = 0.12f;
        // 行動が終わってから閉じ始めるまで
        [SerializeField][Min(0f)] private float _closeDelay = 0.8f;
        [SerializeField][Min(0.01f)] private float _newRowDuration = 0.15f;

        private enum Phase { Hidden, Opening, Open, WaitingToClose, Closing }

        private readonly List<BattleLogRowView> _rows = new List<BattleLogRowView>();
        private Phase _phase = Phase.Hidden;
        private float _time;
        private float _newRowTime = -1f;
        private BattleLogRowView _newestRow;

        public int MaxRows => _maxRows;

        /// <summary>開く（開いている・閉じかけているなら、そのまま開いた状態に戻す）</summary>
        public void Open()
        {
            gameObject.SetActive(true);
            if (_phase == Phase.Open || _phase == Phase.Opening) return;
            if (_phase == Phase.WaitingToClose)
            {
                _phase = Phase.Open;
                return;
            }
            _phase = Phase.Opening;
            _time = 0f;
            Apply(0f);
        }

        /// <summary>少し待ってから閉じる</summary>
        public void Close()
        {
            if (_phase == Phase.Hidden || _phase == Phase.Closing || _phase == Phase.WaitingToClose) return;
            _phase = Phase.WaitingToClose;
            _time = 0f;
        }

        public void HideImmediate()
        {
            _phase = Phase.Hidden;
            gameObject.SetActive(false);
        }

        /// <summary>並べる行（古い順）。最新の行だけちらつかせたいときは animateNewest</summary>
        public void SetLines(IReadOnlyList<BattleLogLine> lines, bool animateNewest)
        {
            int start = Mathf.Max(0, lines.Count - _maxRows);
            int count = lines.Count - start;

            while (_rows.Count < count) _rows.Add(Instantiate(_rowPrefab, _rowParent));

            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                bool active = i < count;
                row.gameObject.SetActive(active);
                if (!active) continue;

                row.Bind(lines[start + i]);
                // 下詰め: 最新の行が一番下
                row.RectTransform.anchoredPosition = new Vector2(0f, (count - 1 - i) * _rowHeight);
                SetRowAlpha(row, 1f);
            }

            _newestRow = count > 0 && animateNewest ? _rows[count - 1] : null;
            _newRowTime = _newestRow != null ? 0f : -1f;
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 15f);
            _time += dt;

            switch (_phase)
            {
                case Phase.Opening:
                    Apply(Mathf.Clamp01(_time / _openDuration));
                    if (_time >= _openDuration) _phase = Phase.Open;
                    break;
                case Phase.WaitingToClose:
                    if (_time >= _closeDelay)
                    {
                        _phase = Phase.Closing;
                        _time = 0f;
                    }
                    break;
                case Phase.Closing:
                    Apply(1f - Mathf.Clamp01(_time / _closeDuration));
                    if (_time >= _closeDuration) HideImmediate();
                    break;
            }

            // 新しい行はちらつきながら入る
            if (_newRowTime >= 0f && _newestRow != null)
            {
                _newRowTime += dt;
                float t = Mathf.Clamp01(_newRowTime / _newRowDuration);
                bool flicker = t < 1f && Mathf.FloorToInt(_newRowTime * 40f) % 2 == 0;
                SetRowAlpha(_newestRow, flicker ? 0.3f : t);
                if (t >= 1f) _newRowTime = -1f;
            }
        }

        // 横一線から開く（0:線 〜 1:開き切った）
        private void Apply(float open)
        {
            float eased = 1f - (1f - open) * (1f - open);
            _panel.localScale = new Vector3(1f, Mathf.Lerp(0.03f, 1f, eased), 1f);
            _panelGroup.alpha = open <= 0f ? 0f : 1f;
        }

        private static void SetRowAlpha(BattleLogRowView row, float alpha)
        {
            var group = row.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = alpha;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Live2DCardRogue.Common.Views
{
    // 縦スクロールのグリッド。表示範囲に入っている分だけ要素を生成し、
    // 画面外に出たものは使い回す（件数が増えてもGameObjectは見えている数＋数個しか作らない）。
    //
    // 中身の型は知らない。並べる枠だけを用意して、その枠に何を描くかは Initialize で渡す
    // コールバックに任せる（呼び出し側が自分のViewコンポーネントを GetComponent で取り出す）。
    //
    // 想定しているのは「全要素が同じ大きさ」のリスト。可変高さには対応していない
    [RequireComponent(typeof(ScrollRect))]
    public sealed class CommonScrollList : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private RectTransform _content;
        [SerializeField] private RectTransform _itemPrefab;

        [SerializeField] private Vector2 _cellSize = new Vector2(200f, 280f);
        [SerializeField] private Vector2 _spacing = new Vector2(16f, 16f);
        [SerializeField][Min(1)] private int _columns = 3;

        // 端で見切れている行の分だけ余分に出しておく（スクロール中に欠けて見えないように）
        private const int ExtraRows = 1;

        private readonly List<RectTransform> _pool = new List<RectTransform>();
        private readonly Dictionary<int, RectTransform> _activeItems = new Dictionary<int, RectTransform>();

        private Action<int, RectTransform> _onBindItem;
        private int _itemCount;

        /// <param name="onBindItem">枠(第2引数)に、何番目の要素(第1引数)を描くかを実装する</param>
        public void Initialize(Action<int, RectTransform> onBindItem)
        {
            _onBindItem = onBindItem;
            _scrollRect.onValueChanged.AddListener(HandleScrolled);
        }

        public void Dispose()
        {
            _scrollRect.onValueChanged.RemoveListener(HandleScrolled);
        }

        /// <summary>件数を伝え直して作り直す。中身が変わったときは毎回これを呼ぶ</summary>
        public void SetItemCount(int itemCount)
        {
            _itemCount = Mathf.Max(0, itemCount);

            int rowCount = Mathf.CeilToInt(_itemCount / (float)_columns);
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, Mathf.Max(0f, rowCount * RowHeight - _spacing.y));

            // 件数が減って表示位置が行き過ぎている場合に備えて、先頭側へ寄せ直す
            if (_content.anchoredPosition.y > MaxScrollY)
            {
                _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, MaxScrollY);
            }

            Refresh();
        }

        /// <summary>
        /// 表示枠の幅に収まるだけの列数にし直す（パネルの幅を変えたときに呼ぶ）。列数が変わったら並べ直す
        /// </summary>
        public void FitColumnsToWidth()
        {
            int fit = Mathf.Max(1, Mathf.FloorToInt((_viewport.rect.width + _spacing.x) / (_cellSize.x + _spacing.x)));
            if (fit == _columns) return;
            _columns = fit;
            SetItemCount(_itemCount);
        }

        /// <summary>一番上（最初の要素）までスクロールする。開き直したウインドウを先頭から見せるとき用</summary>
        public void ScrollToStart()
        {
            _scrollRect.StopMovement();
            _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, 0f);
            Refresh();
        }

        /// <summary>一番下（最後の要素）までスクロールする。ログのように新しいものが下に積まれるリスト用</summary>
        public void ScrollToEnd()
        {
            _scrollRect.StopMovement();
            _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, MaxScrollY);
            Refresh();
        }

        /// <summary>今一番下まで表示しているか（新しい要素が来たときに、下へ追いかけるかどうかの判断に使う）</summary>
        public bool IsAtEnd => _content.anchoredPosition.y >= MaxScrollY - 1f;

        /// <summary>件数は変えずに、表示中の要素だけ描き直す</summary>
        public void Refresh()
        {
            int firstRow = Mathf.Max(0, Mathf.FloorToInt(_content.anchoredPosition.y / RowHeight));
            int visibleRows = Mathf.CeilToInt(_viewport.rect.height / RowHeight) + ExtraRows;

            int firstIndex = firstRow * _columns;
            int lastIndex = Mathf.Min(_itemCount - 1, (firstRow + visibleRows) * _columns - 1);

            // 範囲から外れたものはプールへ返す
            var recycled = new List<int>();
            foreach (var pair in _activeItems)
            {
                if (pair.Key < firstIndex || pair.Key > lastIndex) recycled.Add(pair.Key);
            }

            foreach (var index in recycled)
            {
                var item = _activeItems[index];
                item.gameObject.SetActive(false);
                _pool.Add(item);
                _activeItems.Remove(index);
            }

            for (int index = firstIndex; index <= lastIndex; index++)
            {
                if (!_activeItems.TryGetValue(index, out var item))
                {
                    item = TakeFromPool();
                    _activeItems.Add(index, item);
                }

                item.anchoredPosition = PositionOf(index);
                _onBindItem?.Invoke(index, item);
            }
        }

        private float RowHeight => _cellSize.y + _spacing.y;

        // 中身が表示枠より短い場合は動かさない
        private float MaxScrollY => Mathf.Max(0f, _content.sizeDelta.y - _viewport.rect.height);

        private Vector2 PositionOf(int index)
        {
            int row = index / _columns;
            int column = index % _columns;

            // Contentは左上基準。要素は中心基準で置く
            return new Vector2(
                column * (_cellSize.x + _spacing.x) + _cellSize.x / 2f,
                -(row * RowHeight) - _cellSize.y / 2f);
        }

        private RectTransform TakeFromPool()
        {
            if (_pool.Count > 0)
            {
                var pooled = _pool[_pool.Count - 1];
                _pool.RemoveAt(_pool.Count - 1);
                pooled.gameObject.SetActive(true);
                return pooled;
            }

            var created = Instantiate(_itemPrefab, _content);
            created.anchorMin = new Vector2(0f, 1f);
            created.anchorMax = new Vector2(0f, 1f);
            created.pivot = new Vector2(0.5f, 0.5f);
            created.sizeDelta = _cellSize;
            return created;
        }

        private void HandleScrolled(Vector2 _)
        {
            Refresh();
        }
    }
}

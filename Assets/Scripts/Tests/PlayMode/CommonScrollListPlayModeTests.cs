using System.Collections;
using System.Linq;
using Live2DCardRogue.Common.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Live2DCardRogue.Tests.PlayMode
{
    // CommonScrollList の肝は「件数が増えてもGameObjectは見えている分しか作らない」こと。
    // RectTransform のレイアウトが要るので PlayMode 側に置いている
    // (Edit Mode では rect.height が 0 のままで、表示範囲の計算が成立しない)。
    public sealed class CommonScrollListPlayModeTests
    {
        private const int Columns = 3;
        private static readonly Vector2 CellSize = new Vector2(100f, 100f);
        private static readonly Vector2 Spacing = new Vector2(10f, 10f);
        private const float ViewportHeight = 330f; // 3行分ぶん

        private GameObject _canvasGo;
        private CommonScrollList _list;
        private RectTransform _content;

        [SetUp]
        public void CreateList()
        {
            _canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var listGo = new GameObject("List", typeof(RectTransform), typeof(ScrollRect), typeof(CommonScrollList));
            listGo.transform.SetParent(_canvasGo.transform, false);

            var viewport = new GameObject("Viewport", typeof(RectTransform)).GetComponent<RectTransform>();
            viewport.SetParent(listGo.transform, false);
            viewport.sizeDelta = new Vector2(Columns * (CellSize.x + Spacing.x), ViewportHeight);

            _content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _content.SetParent(viewport, false);
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0f, 1f);

            var itemPrefab = new GameObject("Item", typeof(RectTransform)).GetComponent<RectTransform>();

            var scrollRect = listGo.GetComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = _content;

            _list = listGo.GetComponent<CommonScrollList>();
            SetPrivateField(_list, "_scrollRect", scrollRect);
            SetPrivateField(_list, "_viewport", viewport);
            SetPrivateField(_list, "_content", _content);
            SetPrivateField(_list, "_itemPrefab", itemPrefab);
            SetPrivateField(_list, "_cellSize", CellSize);
            SetPrivateField(_list, "_spacing", Spacing);
            SetPrivateField(_list, "_columns", Columns);
        }

        [TearDown]
        public void DestroyList()
        {
            Object.DestroyImmediate(_canvasGo);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private int CreatedItemCount => _content.childCount;
        private int ActiveItemCount => _content.Cast<Transform>().Count(t => t.gameObject.activeSelf);

        [UnityTest]
        public IEnumerator SetItemCount_CreatesOnlyTheItemsThatFitOnScreen()
        {
            int bound = 0;
            _list.Initialize((index, item) => bound++);
            yield return null;

            _list.SetItemCount(300);

            // 3列 x (見えている3行 + 予備1行) = 12個前後。件数に比例して増えないことが大事
            Assert.LessOrEqual(CreatedItemCount, Columns * 5, "件数に対してGameObjectを作りすぎている");
            Assert.Greater(CreatedItemCount, 0);
            Assert.AreEqual(CreatedItemCount, bound, "生成した枠の数だけ描画が呼ばれるはず");
        }

        // スクロールしても新規生成は増えず、同じGameObjectが使い回される
        [UnityTest]
        public IEnumerator Scrolling_ReusesTheSameObjects()
        {
            _list.Initialize((index, item) => { });
            yield return null;

            _list.SetItemCount(300);
            int createdAtStart = CreatedItemCount;

            for (int row = 1; row <= 20; row++)
            {
                _content.anchoredPosition = new Vector2(0f, row * (CellSize.y + Spacing.y));
                _list.Refresh();
            }

            Assert.AreEqual(createdAtStart, CreatedItemCount, "スクロール中に新しいGameObjectを作っている");
            Assert.Greater(ActiveItemCount, 0);
        }

        // 件数が減ったら、余った枠は非アクティブにして残す（破棄せず使い回す）
        [UnityTest]
        public IEnumerator SetItemCount_Shrinking_DeactivatesTheExtraItems()
        {
            _list.Initialize((index, item) => { });
            yield return null;

            _list.SetItemCount(300);
            int createdWhenFull = CreatedItemCount;

            _list.SetItemCount(2);

            Assert.AreEqual(2, ActiveItemCount);
            Assert.AreEqual(createdWhenFull, CreatedItemCount, "使わなくなった枠を破棄している");
        }

        [UnityTest]
        public IEnumerator SetItemCount_Zero_ShowsNothing()
        {
            _list.Initialize((index, item) => { });
            yield return null;

            _list.SetItemCount(0);

            Assert.AreEqual(0, ActiveItemCount);
        }

        // 表示枠を広げてから幅に合わせると、収まるだけ列が増え、2行目の先頭が右へ詰まる
        [UnityTest]
        public IEnumerator FitColumnsToWidth_UsesEveryColumnThatFits()
        {
            RectTransform secondRowFirst = null;
            _list.Initialize((index, item) => { if (index == Columns) secondRowFirst = item; });
            yield return null;
            _list.SetItemCount(12);
            Assert.IsNotNull(secondRowFirst);
            Assert.Less(secondRowFirst.anchoredPosition.y, -CellSize.y / 2f, "3列のときは4つ目が2行目");

            var viewport = (RectTransform)_content.parent;
            viewport.sizeDelta = new Vector2(6 * (CellSize.x + Spacing.x), ViewportHeight);
            yield return null;
            _list.FitColumnsToWidth();

            int columns = (int)typeof(CommonScrollList).GetField("_columns", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(_list);
            Assert.AreEqual(6, columns);
            Assert.AreEqual(-CellSize.y / 2f, secondRowFirst.anchoredPosition.y, 0.01f, "6列のときは4つ目も1行目");
        }
    }
}

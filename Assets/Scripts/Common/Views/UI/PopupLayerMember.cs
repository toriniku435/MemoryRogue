using UnityEngine;

namespace Live2DCardRogue.Common.Views
{
    // ウインドウのプレハブのルートに付ける、そのウインドウの描く段の切り替え口。
    // 入れ子のCanvasを持ち、PopupStack から「一番手前か」と「奥からの順番」を受けて、
    //   - 一番手前: レイヤー UI_PopupTop（Popup Camera の描画の最後、ぼかしの後に描かれる）
    //   - それ以外: レイヤー UI_Popup（ぼかしの前に描かれ、ぼかしの下になる）
    // に自分以下ごと移し、Canvasの並び順を重なり順にそろえる（同じカメラなので、クリックの優先もこの順で決まる）。
    // 描き分けに効くのは Canvas とパーティクルのレイヤーだけなので、後から生成される一覧の行などを追う必要はない
    [RequireComponent(typeof(Canvas))]
    public sealed class PopupLayerMember : MonoBehaviour
    {
        public const string BackLayerName = "UI_Popup";
        public const string FrontLayerName = "UI_PopupTop";
        private const string SortingLayerName = "UI_Popup";

        // 重なり順の並び順の始まり。トーストとチュートリアル（100）より下に収める
        private const int BaseSortingOrder = 10;

        private Canvas _canvas;

        /// <param name="isFront">一番手前か</param>
        /// <param name="order">奥からの順番（0 が一番奥）</param>
        public void Apply(bool isFront, int order)
        {
            if (_canvas == null) _canvas = GetComponent<Canvas>();
            // 有効になったときに外れることがあるので、毎回入れ直す
            _canvas.overrideSorting = true;
            _canvas.sortingLayerName = SortingLayerName;
            _canvas.sortingOrder = BaseSortingOrder + order;

            int layer = LayerMask.NameToLayer(isFront ? FrontLayerName : BackLayerName);
            if (layer >= 0) SetLayerRecursively(transform, layer);
        }

        private static void SetLayerRecursively(Transform target, int layer)
        {
            target.gameObject.layer = layer;
            foreach (Transform child in target) SetLayerRecursively(child, layer);
        }
    }
}

using UnityEngine;

namespace Live2DCardRogue.Common.Views
{
    // 画面いっぱいの RectTransform を、端末のセーフエリア（ノッチ・角の丸み・ジェスチャーバーを避けた範囲）に縮める。
    // 画面の端や隅に寄せる HUD をこの下に置くと、ノッチに隠れない。
    //
    // 置き場所の約束: 親が画面全体を覆っていること（Canvas の直下、または画面いっぱいに広げた物の下）。
    // アンカーを画面に対する割合で決めるので、親が画面より小さいとずれる。
    // 背景や戦場のユニットなど「世界」に属する物、全画面の当たり判定（キャンセル用の板など）は、この下に入れない。
    //
    // セーフエリアが画面全体（PC や、ノッチの無い端末）ならアンカーは 0〜1 のままで、置いた物の位置は何も変わらない。
    // 書き換えるのは再生中だけで、シーンに保存される値（0〜1）は触らない。
    //
    // UnityEngine.Screen ではなく UnityEngine.Device.Screen を読む: Device Simulator が端末の値に差し替えるのは Device 側だけ
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _appliedSafeArea;
        private Vector2Int _appliedScreenSize;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
        }

        private void OnEnable()
        {
            Apply();
        }

        // セーフエリアは起動中にも変わるので、毎フレーム確かめる（変わったときだけ付け直す）。
        // 画面は横持ちだけだが左右どちらにも回るので、180度回すとノッチが反対側へ移る。そのとき画面の大きさは変わらないので、
        // 大きさの変化（OnRectTransformDimensionsChange）だけを見ていると取りこぼす。分割画面やウインドウの大きさの変更でも変わる
        private void Update()
        {
            var screenSize = new Vector2Int(UnityEngine.Device.Screen.width, UnityEngine.Device.Screen.height);
            if (UnityEngine.Device.Screen.safeArea == _appliedSafeArea && screenSize == _appliedScreenSize) return;
            Apply();
        }

        private void Apply()
        {
            _appliedSafeArea = UnityEngine.Device.Screen.safeArea;
            _appliedScreenSize = new Vector2Int(UnityEngine.Device.Screen.width, UnityEngine.Device.Screen.height);

            CalculateAnchors(_appliedSafeArea, _appliedScreenSize, out var anchorMin, out var anchorMax);
            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>セーフエリア（ピクセル、左下が原点）を、画面に対するアンカー（0〜1）にする。画面の大きさが 0 なら画面全体</summary>
        public static void CalculateAnchors(Rect safeArea, Vector2Int screenSize, out Vector2 anchorMin, out Vector2 anchorMax)
        {
            if (screenSize.x <= 0 || screenSize.y <= 0)
            {
                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
                return;
            }

            anchorMin = new Vector2(Mathf.Clamp01(safeArea.xMin / screenSize.x), Mathf.Clamp01(safeArea.yMin / screenSize.y));
            anchorMax = new Vector2(Mathf.Clamp01(safeArea.xMax / screenSize.x), Mathf.Clamp01(safeArea.yMax / screenSize.y));
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace Live2DCardRogue.MapScene.Views
{
    // 部屋と部屋をつなぐデータバスの配線1本。通った区間だけ光らせる
    public sealed class MapLinkView : MonoBehaviour
    {
        [SerializeField] private Graphic _line;
        [SerializeField] private Color _litColor = new Color(0f, 0.94f, 1f, 1f);
        [SerializeField] private Color _unlitColor = new Color(0f, 0.94f, 1f, 0.18f);

        // 2点の間に線を張る（中点に置き、長さと向きを合わせる）
        public void Connect(Vector2 from, Vector2 to)
        {
            var rt = (RectTransform)transform;
            var delta = to - from;
            rt.anchoredPosition = (from + to) * 0.5f;
            rt.sizeDelta = new Vector2(delta.magnitude, rt.sizeDelta.y);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        public void SetLit(bool isLit)
        {
            _line.color = isLit ? _litColor : _unlitColor;
        }
    }
}

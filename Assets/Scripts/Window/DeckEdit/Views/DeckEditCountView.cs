using UnityEngine;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Window.DeckEdit.Views
{
    // デッキの枚数表示。上限までのコマを並べたゲージと「12/20」の数字で、あと何枚かを見せる。
    // ちょうど上限でない（足りない・超えている）うちは数字を警告色にして、保存できない理由が押す前から分かるようにする。
    // 超えているときもゲージは満杯のまま（コマは上限の数だけ）で、超えた分は数字で見せる
    public sealed class DeckEditCountView : MonoBehaviour
    {
        [SerializeField] private CommonText _countText;
        [SerializeField] private SegmentGaugeView _gauge;
        [SerializeField] private Color _incompleteColor = new Color(1f, 0.18f, 0.77f, 1f);
        [SerializeField] private Color _completeColor = new Color(0.49f, 1f, 0.31f, 1f);

        public void SetCount(int count, int max)
        {
            _countText.text = $"{count}/{max}";
            _countText.color = count == max ? _completeColor : _incompleteColor;
            if (_gauge != null) _gauge.SetValue(count, max);
        }
    }
}

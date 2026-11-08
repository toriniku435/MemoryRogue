using UnityEngine;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.MapScene.Views
{
    // マップの部屋1つ分（サイバー空間の階層の1つ）。何層目か・アドレスの表示と、
    // 踏破済み / 現在地 / 未到達 の見た目だけを持つ。現在地の部屋は縁を脈打たせる
    public sealed class MapCellView : MonoBehaviour
    {
        [SerializeField] private CommonText _numberText;
        // 飾りのアドレス表示（未設定でもよい）
        [SerializeField] private CommonText _addressText;
        // 状態の色を乗せる縁
        [SerializeField] private UnityEngine.UI.Image _background;
        [SerializeField] private Color _defaultColor = new Color(0f, 0.94f, 1f, 0.3f);
        [SerializeField] private Color _currentColor = new Color(1f, 0.9f, 0f, 1f);
        [SerializeField] private Color _clearedColor = new Color(0.49f, 1f, 0.31f, 1f);
        [SerializeField][Min(0f)] private float _pulseSpeed = 5f;

        private bool _isCurrent;

        public RectTransform RectTransform => (RectTransform)transform;

        public void SetNumber(int number)
        {
            _numberText.text = number.ToString("00");
            // アドレスは飾り。層が深くなるほど番地も進んで見えるように
            if (_addressText != null) _addressText.text = FormatAddress(number);
        }

        /// <summary>
        /// 飾りのアドレス（0x0100, 0x0200 …）。下4桁だけを出すので、256階層ごとに 0x0000 へ戻る。
        /// 番号 × 0x100 をそのまま int で掛けると、とても深い階層であふれてマイナスになるため
        /// </summary>
        public static string FormatAddress(int number)
        {
            return $"0x{(number & 0xFF) << 8:X4}";
        }

        // 踏破済み / 現在地 / 未到達 の3状態を色で出す
        public void SetState(bool isCleared, bool isCurrent)
        {
            _isCurrent = isCurrent;
            _background.color = isCurrent ? _currentColor : (isCleared ? _clearedColor : _defaultColor);
            _numberText.color = isCurrent || isCleared ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        }

        private void Update()
        {
            if (!_isCurrent || _pulseSpeed <= 0f) return;
            var color = _currentColor;
            color.a *= Mathf.Lerp(0.55f, 1f, (Mathf.Sin(Time.unscaledTime * _pulseSpeed) + 1f) * 0.5f);
            _background.color = color;
        }
    }
}

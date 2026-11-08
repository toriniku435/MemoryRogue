using System;
using UnityEngine;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Window.UnitUpgrade.Views
{
    // ユニット強化の1項目（HP / メモリー上限）。今の値 → 次の値、段のゲージ、値段、強化ボタン。
    // 値段が払えないときは値段を警告色にする（トンマナ: 使えないものは押す前から見える）。
    // 上限まで上げたら値段の代わりに MAX と出し、ボタンは押せない。押されたことを伝えるだけで、上げるのは Presenter
    public sealed class UnitUpgradeRowView : MonoBehaviour
    {
        [SerializeField] private CommonText _label;
        [SerializeField] private CommonText _valueText;
        [SerializeField] private SegmentGaugeView _gauge;
        [SerializeField] private CommonText _costText;
        [SerializeField] private CommonButton _button;
        [SerializeField] private CommonText _buttonLabel;
        // 上げた瞬間に弾ませるもの（値の文字）
        [SerializeField] private RectTransform _pulseTarget;

        [SerializeField] private Color _affordableColor = new Color(1f, 0.9f, 0f, 1f);
        [SerializeField] private Color _unaffordableColor = new Color(1f, 0.18f, 0.77f, 1f);
        [SerializeField] private Color _maxedColor = new Color(0.49f, 1f, 0.31f, 1f);
        [SerializeField][Min(0.01f)] private float _pulseDuration = 0.25f;
        [SerializeField] private float _pulseScale = 1.3f;

        private float _pulseTime = -1f;

        /// <summary>強化ボタンが押された</summary>
        public event Action OnUpgradeClicked;

        private void Awake()
        {
            _button.onClick.AddListener(HandleClicked);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClicked);
        }

        public void SetTexts(string label, string buttonLabel)
        {
            _label.text = label;
            _buttonLabel.text = buttonLabel;
        }

        public void Show(int current, int next, bool isMaxed, int level, int maxLevel, int cost, bool canAfford)
        {
            _valueText.text = isMaxed ? $"{current}" : $"{current} → {next}";
            _gauge.SetValue(level, maxLevel);
            _costText.text = isMaxed ? "MAX" : $"{cost} TKN";
            _costText.color = isMaxed ? _maxedColor : canAfford ? _affordableColor : _unaffordableColor;
            // 上限まで上げたとき・払えないときは押せない（押せないボタンを押してもエラー音で応える。CommonButton）
            _button.interactable = !isMaxed && canAfford;
        }

        /// <summary>上がった瞬間の手応え（値の文字が弾む）</summary>
        public void PlayUpgraded()
        {
            _pulseTime = 0f;
        }

        private void Update()
        {
            if (_pulseTime < 0f || _pulseTarget == null) return;
            _pulseTime += Mathf.Min(Time.unscaledDeltaTime, 1f / 15f);
            float t = Mathf.Clamp01(_pulseTime / _pulseDuration);
            float scale = Mathf.Lerp(_pulseScale, 1f, 1f - (1f - t) * (1f - t));
            _pulseTarget.localScale = new Vector3(scale, scale, 1f);
            if (t >= 1f) _pulseTime = -1f;
        }

        private void HandleClicked()
        {
            OnUpgradeClicked?.Invoke();
        }
    }
}

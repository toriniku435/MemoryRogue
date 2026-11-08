using System;
using UnityEngine;
using UnityEngine.UI;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Window.DeckEdit.Views
{
    // デッキ編集の上に並ぶデッキの番号のタブ（01〜10）1つ分。
    // 選んでいるタブは明るく、アクティブな（戦闘で使う）デッキには黄色の印を付ける。押されたことを伝えるだけ
    public sealed class DeckSlotTabView : MonoBehaviour
    {
        [SerializeField] private CommonButton _button;
        [SerializeField] private Image _background;
        [SerializeField] private CommonText _numberText;
        // アクティブなデッキの印
        [SerializeField] private GameObject _activeMark;

        // 地の画像は暗い板に枠なので、色は枠に乗る。文字は暗い地の上に出るので明るい色にする
        [SerializeField] private Color _selectedColor = new Color(0.2f, 0.95f, 1f, 1f);
        [SerializeField] private Color _normalColor = new Color(0.2f, 0.95f, 1f, 0.3f);
        [SerializeField] private Color _selectedTextColor = new Color(0.91f, 0.98f, 1f, 1f);
        [SerializeField] private Color _normalTextColor = new Color(0.55f, 0.62f, 0.72f, 1f);
        [SerializeField] private Color _activeTextColor = new Color(1f, 0.9f, 0f, 1f);

        private int _index;

        /// <summary>押された（デッキの番号）</summary>
        public event Action<int> OnClicked;

        private void Awake()
        {
            _button.onClick.AddListener(HandleClicked);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClicked);
        }

        /// <param name="index">デッキの番号（0始まり。表示は1始まり）</param>
        public void Show(int index, bool isSelected, bool isActive)
        {
            _index = index;
            _numberText.text = (index + 1).ToString("00");
            _background.color = isSelected ? _selectedColor : _normalColor;
            _numberText.color = isActive ? _activeTextColor : isSelected ? _selectedTextColor : _normalTextColor;
            if (_activeMark != null) _activeMark.SetActive(isActive);
        }

        private void HandleClicked()
        {
            OnClicked?.Invoke(_index);
        }
    }
}

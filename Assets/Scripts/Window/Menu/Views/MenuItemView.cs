using System;
using Live2DCardRogue.Common.Views;
using UnityEngine;

namespace Live2DCardRogue.Window.Menu.Views
{
    // メニューのボタン1つ（MenuWindow.prefab の ItemTemplate を MenuWindowView が複製する）。
    // 押されたことを伝えるだけで、何をするかは Presenter が決める
    public sealed class MenuItemView : MonoBehaviour
    {
        [SerializeField] private CommonButton _button;
        [SerializeField] private CommonText _label;
        [SerializeField] private Color _normalColor = new Color32(0xE8, 0xFA, 0xFF, 0xFF);
        // 取り消せない操作（MenuItemStyle.Danger）のラベルの色
        [SerializeField] private Color _dangerColor = new Color32(0xFF, 0x2E, 0xC4, 0xFF);

        /// <summary>押された</summary>
        public event Action OnClicked;

        public RectTransform RectTransform => (RectTransform)transform;

        private void Awake()
        {
            _button.onClick.AddListener(HandleClicked);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClicked);
        }

        public void SetLabel(string text)
        {
            _label.text = text;
        }

        public void SetDanger(bool isDanger)
        {
            _label.color = isDanger ? _dangerColor : _normalColor;
        }

        public void SetInteractable(bool interactable)
        {
            _button.interactable = interactable;
        }

        private void HandleClicked()
        {
            OnClicked?.Invoke();
        }
    }
}

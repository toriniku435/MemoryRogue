using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.BattleScene.Views
{
    // 対象の候補一覧の1件。立ち絵のアイコンとHPを見せ、クリックとカーソルの出入りを流すだけ
    public sealed class BattleTargetOptionView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Image _portrait;
        [SerializeField] private CommonText _hpText;
        // 枠（クリック判定もこれで受ける）
        [SerializeField] private Graphic _frame;
        [SerializeField] private Color _normalColor = new Color(0f, 0.94f, 1f, 1f);
        [SerializeField] private Color _hoverColor = new Color(1f, 0.9f, 0f, 1f);
        [SerializeField] private float _hoverScale = 1.1f;

        private Action _onClicked;
        private Action<bool> _onHoverChanged;
        private bool _isHovered;

        public RectTransform RectTransform => (RectTransform)transform;

        public void Bind(Sprite portrait, string hpText, Action onClicked, Action<bool> onHoverChanged)
        {
            _portrait.sprite = portrait;
            _portrait.enabled = portrait != null;
            _hpText.text = hpText;
            _onClicked = onClicked;
            _onHoverChanged = onHoverChanged;
            SetHovered(false, notify: false);
        }

        public void SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetHovered(true, notify: true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHovered(false, notify: true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _onClicked?.Invoke();
        }

        // 閉じられたときは OnPointerExit が飛ばないので、ここで指していた状態を戻す
        private void OnDisable()
        {
            if (_isHovered) SetHovered(false, notify: true);
        }

        private void SetHovered(bool isHovered, bool notify)
        {
            _isHovered = isHovered;
            if (_frame != null) _frame.color = isHovered ? _hoverColor : _normalColor;
            transform.localScale = Vector3.one * (isHovered ? _hoverScale : 1f);
            if (notify) _onHoverChanged?.Invoke(isHovered);
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Live2DCardRogue.Common.Audio;

namespace Live2DCardRogue.Common.Views
{
    // プロジェクト共通のボタン。全ボタンに共通の振る舞いはここに足す。
    // 今は操作音：カーソルが乗ったら _hoverSe、押したら _clickSe、押せない状態で押したら Error。
    // 確定・取り消しなど役割のあるボタンは、Inspector で _clickSe を変える（Confirm / Cancel など）
    public class CommonButton : Button
    {
        [SerializeField] private SeId _clickSe = SeId.Click;
        [SerializeField] private SeId _hoverSe = SeId.Hover;

        /// <summary>押せない状態で押された（onClick は呼ばれない）。押せない理由を伝えたいときに使う</summary>
        public event System.Action OnDisabledClicked;

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            if (IsInteractable()) AudioUseCase.PlaySe(_hoverSe);
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                AudioUseCase.PlaySe(IsInteractable() ? _clickSe : SeId.Error);
                if (!IsInteractable()) OnDisabledClicked?.Invoke();
            }
            base.OnPointerClick(eventData);
        }
    }
}

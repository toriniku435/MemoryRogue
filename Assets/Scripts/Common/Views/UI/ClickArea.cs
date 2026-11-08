using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Live2DCardRogue.Common.Views
{
    // クリックされたことを知らせるだけの領域（ウインドウの背面など）。
    // 受け取るには同じオブジェクトに raycastTarget の Graphic が要る
    public sealed class ClickArea : MonoBehaviour, IPointerClickHandler
    {
        public event Action OnClicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClicked?.Invoke();
        }
    }
}

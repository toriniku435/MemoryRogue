using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.BattleScene.Views
{
    // 敵の予告スロット1つ。何番目に何をするかを見せ、カーソルが乗ったこととタップされたことだけを伝える
    public sealed class BattleEnemyIntentSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private CommonText _nameText;
        [SerializeField] private Image _illustration;
        // 停止などで行わない行動に被せる
        [SerializeField] private GameObject _skippedOverlay;

        public event Action OnHovered;
        public event Action OnUnhovered;
        public event Action OnClicked;

        public void Set(string cardName, Sprite illustration, bool willAct)
        {
            if (_nameText != null) _nameText.text = cardName;
            if (_illustration != null)
            {
                _illustration.sprite = illustration;
                _illustration.enabled = illustration != null;
            }
            if (_skippedOverlay != null) _skippedOverlay.SetActive(!willAct);
        }

        public void OnPointerEnter(PointerEventData eventData) => OnHovered?.Invoke();

        public void OnPointerExit(PointerEventData eventData) => OnUnhovered?.Invoke();

        public void OnPointerClick(PointerEventData eventData) => OnClicked?.Invoke();

        // 隠れるときに、乗っていたカーソルの分を取り消す（詳細パネルが出たまま残らないように）
        private void OnDisable() => OnUnhovered?.Invoke();
    }
}

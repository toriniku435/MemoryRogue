using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Views;
using Live2DCardRogue.Common.Audio;

namespace Live2DCardRogue.Window.DeckEdit.Views
{
    // 所持リスト・デッキリストに並ぶカード1枚分。枚数バッジ付きのCardView。
    // スクロールで使い回されるので、押されたときに「どのカードか」はクリック時点の値を渡す。
    //
    // ドラッグはウインドウ（DeckEditWindowView）に伝えるだけで、どちらのリストへ動かすかはウインドウが決める。
    // 横向きに引き始めたらカードのドラッグ、縦向きならリストのスクロールとして親の ScrollRect に渡す
    // （カードがドラッグを受けると、そのままでは ScrollRect にドラッグが届かなくなるため）
    public sealed class DeckEditCardView : CardView, IPointerClickHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private CommonText _countText;
        // サブビューに出しているカードに付ける印（光る縁など）。未設定でもよい
        [SerializeField] private GameObject _selectedMark;

        private Action<CardData> _onClicked;
        private DeckEditWindowView _window;

        private enum DragMode { None, Card, Scroll }
        private DragMode _dragMode;

        public void Bind(CardData cardData, int count, bool isSelected, Action<CardData> onClicked)
        {
            _onClicked = onClicked;
            SetCardData(cardData);
            _countText.text = "x" + count;
            if (_selectedMark != null) _selectedMark.SetActive(isSelected);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            AudioUseCase.PlaySe(SeId.CardSelect);
            _onClicked?.Invoke(CardData);
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            ForwardToParent(eventData, ExecuteEvents.initializePotentialDrag);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_window == null) _window = GetComponentInParent<DeckEditWindowView>();
            bool horizontal = Mathf.Abs(eventData.delta.x) >= Mathf.Abs(eventData.delta.y);

            if (horizontal && _window != null && _window.TryBeginCardDrag(this, eventData))
            {
                _dragMode = DragMode.Card;
                return;
            }

            _dragMode = DragMode.Scroll;
            ForwardToParent(eventData, ExecuteEvents.beginDragHandler);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragMode == DragMode.Card) _window.DragCard(eventData);
            else if (_dragMode == DragMode.Scroll) ForwardToParent(eventData, ExecuteEvents.dragHandler);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            var mode = _dragMode;
            _dragMode = DragMode.None;
            if (mode == DragMode.Card) _window.EndCardDrag(eventData);
            else if (mode == DragMode.Scroll) ForwardToParent(eventData, ExecuteEvents.endDragHandler);
        }

        // ドラッグ中にスクロールで外れて使い回し用に隠されると OnEndDrag が届かないので、ここで取りやめる
        private void OnDisable()
        {
            if (_dragMode == DragMode.Card && _window != null) _window.CancelCardDrag();
            _dragMode = DragMode.None;
        }

        private void ForwardToParent<T>(PointerEventData eventData, ExecuteEvents.EventFunction<T> handler)
            where T : IEventSystemHandler
        {
            if (transform.parent != null) ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, handler);
        }
    }
}

using System;
using UnityEngine;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Window.DeckEdit.Views
{
    // 選んだカードを拡大表示するサブビュー。
    // 増やすボタンはどちらのリストから開いても出し、減らすボタンはデッキ側から開いたときだけ出す
    public sealed class DeckEditSubView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private CardView _cardView;
        [SerializeField] private CommonButton _addButton;
        [SerializeField] private CommonButton _removeButton;
        [SerializeField] private CommonText _statusText;
        // 何も選んでいないときに出す案内（_root の外に置く）。未設定でもよい
        [SerializeField] private GameObject _emptyHint;

        public CommonButton AddButton => _addButton;
        public CommonButton RemoveButton => _removeButton;

        public void Hide()
        {
            _root.SetActive(false);
            if (_emptyHint != null) _emptyHint.SetActive(true);
        }

        /// <param name="canAdd">増やせるか（所持の余りとデッキ上限の両方を満たすか）</param>
        /// <param name="showRemove">減らすボタンを出すか（デッキリストから開いたか）</param>
        /// <param name="canRemove">減らせるか</param>
        public void Show(CardData cardData, string status, bool canAdd, bool showRemove, bool canRemove)
        {
            _root.SetActive(true);
            if (_emptyHint != null) _emptyHint.SetActive(false);
            _cardView.SetCardData(cardData);
            _statusText.text = status;

            _addButton.interactable = canAdd;
            _removeButton.gameObject.SetActive(showRemove);
            _removeButton.interactable = canRemove;
        }
    }
}

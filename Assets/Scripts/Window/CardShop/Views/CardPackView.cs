using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Views;
using Live2DCardRogue.Common.Audio;

namespace Live2DCardRogue.Window.CardShop.Views
{
    // 一覧に並ぶパック1つ分。スクロールで使い回されるので、押されたときのパックは
    // クリック時点で渡された値を返す
    public sealed class CardPackView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private CommonText _nameText;
        [SerializeField] private CommonText _priceText;
        // 選択状態で色を変える枠（白い画像に色を乗せる）
        [SerializeField] private Image _background;
        [SerializeField] private Color _defaultColor = new Color(0f, 0.59f, 0.78f, 0.6f);
        [SerializeField] private Color _selectedColor = new Color(0f, 0.94f, 1f, 1f);
        // 選択中だけ出す飾り（光る縁など）。未設定でもよい
        [SerializeField] private GameObject _selectedMark;
        // パックの絵。未設定でもよい
        [SerializeField] private Image _illustrationImage;
        // 1パックも買えないときの価格の色（押す前から買えないと分かるように）
        [SerializeField] private Color _priceColor = new Color(0.91f, 0.98f, 1f, 1f);
        [SerializeField] private Color _unaffordablePriceColor = new Color(1f, 0.18f, 0.77f, 1f);

        private CardPackData _pack;
        private Action<CardPackData> _onClicked;

        public void Bind(CardPackData pack, string displayName, bool isSelected, int price, bool isAffordable, Action<CardPackData> onClicked)
        {
            _pack = pack;
            _onClicked = onClicked;
            _nameText.text = displayName;
            _priceText.text = $"{price} TKN / {pack.CardCount}個";
            _priceText.color = isAffordable ? _priceColor : _unaffordablePriceColor;
            _background.color = isSelected ? _selectedColor : _defaultColor;
            if (_selectedMark != null) _selectedMark.SetActive(isSelected);

            if (_illustrationImage != null)
            {
                _illustrationImage.sprite = pack.Illustration;
                _illustrationImage.enabled = pack.Illustration != null;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            AudioUseCase.PlaySe(SeId.CardSelect);
            _onClicked?.Invoke(_pack);
        }
    }
}

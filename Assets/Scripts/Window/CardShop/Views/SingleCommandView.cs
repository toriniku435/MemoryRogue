using System;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Views;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Live2DCardRogue.Window.CardShop.Views
{
    // シングル購入の一覧の1行（コマンド1種類）。絵・名前・レアリティの色・値段・残り数。
    // スクロールで使い回されるので、押されたときのコマンドはクリック時点で渡された値を返す
    public sealed class SingleCommandView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private CommonText _nameText;
        [SerializeField] private CommonText _priceText;
        [SerializeField] private CommonText _stockText;
        [SerializeField] private Image _illustrationImage;
        // 選択状態で色を変える枠（白い画像に色を乗せる）
        [SerializeField] private Image _background;
        [SerializeField] private GameObject _selectedMark;
        // レアリティの色を乗せる縁
        [SerializeField] private Image _rarityFrame;
        // 売り切れのときに行を暗くする
        [SerializeField] private CanvasGroup _canvasGroup;
        // デッキに入れられる枚数に上限があるコマンドの、絵の右上の丸い数字
        [SerializeField] private GameObject _deckLimitBadge;
        [SerializeField] private CommonText _deckLimitText;

        [SerializeField] private Color _defaultColor = new Color(0f, 0.59f, 0.78f, 0.6f);
        [SerializeField] private Color _selectedColor = new Color(0f, 0.94f, 1f, 1f);
        [SerializeField] private Color _priceColor = new Color(0.91f, 0.98f, 1f, 1f);
        [SerializeField] private Color _unaffordablePriceColor = new Color(1f, 0.18f, 0.77f, 1f);
        // レアリティの色はゲーム共通（ノーマル シアン、レア マゼンタ、エピック 金）
        [SerializeField] private Color[] _rarityColors =
        {
            new Color(0f, 0.94f, 1f, 1f),
            new Color(1f, 0.18f, 0.77f, 1f),
            new Color(1f, 0.9f, 0f, 1f),
        };

        private CardData _card;
        private Action<CardData> _onClicked;

        public void Bind(CardData card, string displayName, bool isSelected, string price, bool isAffordable, string stock, bool isSoldOut,
            Action<CardData> onClicked)
        {
            _card = card;
            _onClicked = onClicked;
            _nameText.text = displayName;
            _priceText.text = price;
            _priceText.color = isAffordable && !isSoldOut ? _priceColor : _unaffordablePriceColor;
            _stockText.text = stock;
            _background.color = isSelected ? _selectedColor : _defaultColor;
            if (_selectedMark != null) _selectedMark.SetActive(isSelected);
            if (_rarityFrame != null) _rarityFrame.color = _rarityColors[Mathf.Clamp((int)card.Rarity, 0, _rarityColors.Length - 1)];
            // 売り切れても選んで中身は見られるように、押せなくはしない（暗くするだけ）
            if (_canvasGroup != null) _canvasGroup.alpha = isSoldOut ? 0.45f : 1f;

            _illustrationImage.sprite = card.Illustration;
            _illustrationImage.enabled = card.Illustration != null;

            if (_deckLimitBadge != null)
            {
                bool limited = card.MaxCopiesInDeck > 0;
                _deckLimitBadge.SetActive(limited);
                if (limited) _deckLimitText.text = card.MaxCopiesInDeck.ToString();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            AudioUseCase.PlaySe(SeId.CardSelect);
            _onClicked?.Invoke(_card);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Window.CardShop.Views
{
    // 選んだパック（画面上はチップ）かコマンド（シングル購入）の購入パネル。個数の増減と購入ボタンを持つ。
    // チップなら役割の1行（コマンドを獲得してデッキを強化できる）とチップごとの説明、
    // シングルならレアリティ・コスト・残り数の1行とコマンドの効果の説明を、同じ欄に出す
    public sealed class CardShopPurchaseView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private CommonText _packNameText;
        // チップの役割（何個のコマンドが手に入るか）と、チップごとの説明
        [SerializeField] private CommonText _installInfoText;
        [SerializeField] private CommonText _descriptionText;
        [SerializeField] private CommonText _quantityText;
        [SerializeField] private CommonText _totalPriceText;
        [SerializeField] private CommonButton _increaseButton;
        [SerializeField] private CommonButton _decreaseButton;
        [SerializeField] private CommonButton _buyButton;
        [SerializeField] private CommonText _buyLabel;
        // パックの絵と、何も選んでいないときに出す案内（_root の外に置く）。どちらも未設定でもよい
        [SerializeField] private Image _illustrationImage;
        [SerializeField] private GameObject _emptyHint;
        // コマンドを選んでいるとき、デッキに入れられる枚数に上限があれば絵の右上に丸い数字を出す（未設定でもよい）
        [SerializeField] private GameObject _deckLimitBadge;
        [SerializeField] private CommonText _deckLimitText;
        // 同じことを言葉で書く札（「デッキに1個まで」）。絵の下端に重ねる
        [SerializeField] private GameObject _deckLimitLabel;
        [SerializeField] private CommonText _deckLimitLabelText;

        public CommonButton IncreaseButton => _increaseButton;
        public CommonButton DecreaseButton => _decreaseButton;
        public CommonButton BuyButton => _buyButton;
        public CommonText BuyLabel => _buyLabel;

        /// <summary>何も選んでいないときの案内の文（// SELECT A CHIP など。飾りの英語）</summary>
        public void SetEmptyHint(string text)
        {
            if (_emptyHint == null) return;
            var hint = _emptyHint.GetComponentInChildren<CommonText>(true);
            if (hint != null) hint.text = text;
        }

        public void Hide()
        {
            _root.SetActive(false);
            if (_emptyHint != null) _emptyHint.SetActive(true);
        }

        /// <param name="deckLimit">デッキに入れられる枚数の上限（0 なら上限なし。バッジを出さない）</param>
        /// <param name="deckLimitLabel">上限を言葉で書いた札の文（null なら出さない）</param>
        public void Show(string packName, string installInfo, string description, Sprite illustration, int quantity, int totalPrice, bool canIncrease, bool canDecrease, bool canBuy,
            int deckLimit = 0, string deckLimitLabel = null)
        {
            if (_deckLimitBadge != null)
            {
                _deckLimitBadge.SetActive(deckLimit > 0);
                if (deckLimit > 0) _deckLimitText.text = deckLimit.ToString();
            }
            if (_deckLimitLabel != null)
            {
                _deckLimitLabel.SetActive(!string.IsNullOrEmpty(deckLimitLabel));
                if (!string.IsNullOrEmpty(deckLimitLabel)) _deckLimitLabelText.text = deckLimitLabel;
            }
            _root.SetActive(true);
            if (_emptyHint != null) _emptyHint.SetActive(false);
            _packNameText.text = packName;
            _installInfoText.text = installInfo;
            _descriptionText.text = description;
            if (_illustrationImage != null)
            {
                _illustrationImage.sprite = illustration;
                _illustrationImage.enabled = illustration != null;
            }
            _quantityText.text = quantity.ToString();
            _totalPriceText.text = $"{totalPrice} TKN";

            _increaseButton.interactable = canIncrease;
            _decreaseButton.interactable = canDecrease;
            _buyButton.interactable = canBuy; // トークンが足りなければ押せない
        }
    }
}

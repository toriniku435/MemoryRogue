using UnityEngine;
using UnityEngine.UI;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Views
{
    // カードの情報を表示するだけの汎用View。ドラッグ操作などシーン固有の振る舞いはサブクラスで実装する
    public class CardView : MonoBehaviour
    {
        [SerializeField] private CommonText _nameText;
        [SerializeField] private CommonText _costText;
        [SerializeField] private CommonText _descriptionText;

        // 見た目の差し替え口。どれも未設定のままでよい（その場合は文字だけのカードになる）
        [SerializeField] private Image _frameImage;
        [SerializeField] private CardFrameSet _frameSet;
        [SerializeField] private Image _illustrationImage;

        // デッキに入れられる枚数に上限があるコマンド（エクスプロイトチェーンの3つは1枚）の、右上の丸い数字。未設定でもよい。
        // バトルでは付けない（デッキを組むときの情報なので。手札・コマンドメモリー・詳細パネルは未設定のまま）
        [SerializeField] private GameObject _deckLimitBadge;
        [SerializeField] private CommonText _deckLimitText;
        // 同じことを言葉で書く札（「デッキに1個まで」）。大きく見せるカード（デッキ編集のサブビュー、購入結果の詳細、報酬）だけに付ける
        [SerializeField] private GameObject _deckLimitLabel;
        [SerializeField] private CommonText _deckLimitLabelText;

        private CardData _cardData;

        public CardData CardData => _cardData;
        public RectTransform RectTransform => (RectTransform)transform;

        // サブクラスがコストの見せ方（色など）を変えるためのもの
        protected CommonText CostText => _costText;

        public void SetCardData(CardData cardData)
        {
            _cardData = cardData;
            _nameText.text = LocalizationHelper.GetCardName(cardData);
            _costText.text = cardData.Cost.ToString();
            _descriptionText.text = LocalizationHelper.GetCardDescription(cardData);

            if (_frameImage != null && _frameSet != null)
            {
                _frameImage.sprite = _frameSet.GetFrame(cardData.Rarity);
            }

            bool limited = cardData.MaxCopiesInDeck > 0;
            if (_deckLimitBadge != null)
            {
                _deckLimitBadge.SetActive(limited);
                if (limited && _deckLimitText != null) _deckLimitText.text = cardData.MaxCopiesInDeck.ToString();
            }
            if (_deckLimitLabel != null)
            {
                _deckLimitLabel.SetActive(limited);
                if (limited && _deckLimitLabelText != null)
                {
                    _deckLimitLabelText.text = string.Format(LocalizationHelper.GetUiText(UiTextKey.DeckLimit), cardData.MaxCopiesInDeck);
                }
            }

            if (_illustrationImage != null)
            {
                // 絵の無いカードは窓を空けておく（白い四角が出るよりよい）
                _illustrationImage.sprite = cardData.Illustration;
                _illustrationImage.enabled = cardData.Illustration != null;
            }
        }

        public void SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }
    }
}

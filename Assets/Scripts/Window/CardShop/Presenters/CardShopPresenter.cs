using System;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Views;
using UnityEngine;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Window.CardShop.Views;
using Live2DCardRogue.Common.WindowSystem;

namespace Live2DCardRogue.Window.CardShop.Presenters
{
    // マップの「マーケット」ボタンと、それで開くウインドウ（一覧のチップ / シングルのタブと購入パネル）を受け持つPresenter。
    // 「買えるか」「個数を増やせるか」「あといくつ買えるか」はすべてモデルに聞くので、
    // 表示上の活性と実際にできることがズレない。
    //
    // 開くたびに createModel でモデルを作り直す（その時点のトークン・所持コマンド・シングルの購入済み数で始める）。
    // 買うたびに onPurchased で結果（手に入れたコマンド・そのうちシングルで買ったもの・残りトークン）を伝え、
    // 保存先への書き戻しは Initializer が行う（この Presenter はリポジトリを知らない）
    public sealed class CardShopPresenter : PresenterBase, IWindowPresenter
    {
        // 選んでいるタブの文字の色（選んでいない方は暗く）
        private static readonly Color ActiveTabColor = new Color(1f, 0.9f, 0f, 1f);
        private static readonly Color InactiveTabColor = new Color(0.55f, 0.62f, 0.72f, 1f);

        private readonly CardShopWindowView _viewContainer;
        private readonly TokenCounterView _mapTokenCounter;
        private readonly Func<CardShopModel> _createModel;
        private readonly Action<CardData[], CardData[], int> _onPurchased;

        // 買い物の状態。開くたびに作り直す（まだ一度も開いていなければ null）
        private CardShopModel _model;

        /// <param name="mapTokenCounter">マップのトークン表示。買って減った分を数え下げる</param>
        /// <param name="createModel">開いたときの状態でモデルを作る</param>
        /// <param name="onPurchased">買った（手に入れたコマンド, そのうちシングルで買ったもの, 残りトークン）</param>
        public CardShopPresenter(
            CardShopWindowView window,
            TokenCounterView mapTokenCounter,
            Func<CardShopModel> createModel,
            Action<CardData[], CardData[], int> onPurchased)
        {
            _viewContainer = window;
            _mapTokenCounter = mapTokenCounter;
            _createModel = createModel;
            _onPurchased = onPurchased;
        }

        public bool IsOpen => _viewContainer.IsOpen;

        public void HideImmediate()
        {
            _viewContainer.HideImmediate();
        }

        protected override void OnInitialize()
        {
            _viewContainer.OnCloseClicked += Close;

            _viewContainer.PackList.Initialize(BindPackItem);
            _viewContainer.SingleList.Initialize(BindSingleItem);
            _viewContainer.PurchaseView.IncreaseButton.onClick.AddListener(HandleIncrease);
            _viewContainer.PurchaseView.DecreaseButton.onClick.AddListener(HandleDecrease);
            _viewContainer.PurchaseView.BuyButton.onClick.AddListener(HandleBuy);
            _viewContainer.ResultView.CloseButton.onClick.AddListener(HandleResultClose);
            _viewContainer.ChipTabButton.onClick.AddListener(HandleChipTab);
            _viewContainer.SingleTabButton.onClick.AddListener(HandleSingleTab);

            _viewContainer.PurchaseView.Hide();
            _viewContainer.ResultView.Hide();
            ApplyLabels();
        }

        protected override void OnDispose()
        {
            _viewContainer.OnCloseClicked -= Close;
            ReleaseModel();

            _viewContainer.PackList.Dispose();
            _viewContainer.SingleList.Dispose();
            _viewContainer.PurchaseView.IncreaseButton.onClick.RemoveListener(HandleIncrease);
            _viewContainer.PurchaseView.DecreaseButton.onClick.RemoveListener(HandleDecrease);
            _viewContainer.PurchaseView.BuyButton.onClick.RemoveListener(HandleBuy);
            _viewContainer.ResultView.CloseButton.onClick.RemoveListener(HandleResultClose);
            _viewContainer.ChipTabButton.onClick.RemoveListener(HandleChipTab);
            _viewContainer.SingleTabButton.onClick.RemoveListener(HandleSingleTab);
        }

        // チップ名もコマンド名もボタンのラベルもローカライズテーブル由来なので、言語切り替えで引き直す
        protected override void OnLocaleChanged()
        {
            ApplyLabels();
            if (_model != null) Sync();
        }

        private void ApplyLabels()
        {
            string title = LocalizationHelper.GetUiText(UiTextKey.CardShop);
            _viewContainer.TitleText.text = title;
            _viewContainer.CloseLabel.text = LocalizationHelper.GetUiText(UiTextKey.Close);
            _viewContainer.PurchaseView.BuyLabel.text = LocalizationHelper.GetUiText(UiTextKey.Buy);
            _viewContainer.ResultView.TitleText.text = LocalizationHelper.GetUiText(UiTextKey.AcquiredCards);
            _viewContainer.ResultView.CloseLabel.text = LocalizationHelper.GetUiText(UiTextKey.Close);
            _viewContainer.ResultView.RefreshTexts();
        }

        // 開くたびに、その時点のトークンと持ち物で買い物を始め直す（タブはチップ、何も選んでいない状態）
        public void Open()
        {
            if (_viewContainer.IsOpen) return;
            ReleaseModel();
            _model = _createModel.Invoke();
            _model.OnChanged += HandleModelChanged;

            _viewContainer.ResultView.Hide();
            _viewContainer.Open();
            _viewContainer.PackList.SetItemCount(_model.Packs.Count);
            _viewContainer.SingleList.SetItemCount(_model.Singles.Count);
            _viewContainer.PackList.ScrollToStart();
            _viewContainer.SingleList.ScrollToStart();
            Sync();
        }

        // 買ったものはその都度伝えてあるので、閉じるだけでよい
        public void Close()
        {
            _viewContainer.Close();
        }

        private void ReleaseModel()
        {
            if (_model == null) return;
            _model.OnChanged -= HandleModelChanged;
            _model = null;
        }

        private void Sync()
        {
            _viewContainer.TokensText.text = $"{LocalizationHelper.GetUiText(UiTextKey.Tokens)}  {_model.Tokens}";

            SyncTabs();
            // 選択中の色付けや残り数が変わるので、一覧も描き直す
            _viewContainer.PackList.Refresh();
            _viewContainer.SingleList.Refresh();
            SyncPurchaseView();
        }

        private void SyncTabs()
        {
            bool chips = _model.Tab == ShopTab.Chips;
            _viewContainer.ChipTabLabel.text = LocalizationHelper.GetUiText(UiTextKey.ChipTab);
            _viewContainer.SingleTabLabel.text = LocalizationHelper.GetUiText(UiTextKey.SingleTab);
            _viewContainer.ChipTabLabel.color = chips ? ActiveTabColor : InactiveTabColor;
            _viewContainer.SingleTabLabel.color = chips ? InactiveTabColor : ActiveTabColor;
            // 見出しの飾りは英語のまま（ローカライズしない）
            _viewContainer.ListDeco.text = chips ? "// CHIPS" : "// SINGLES";
            _viewContainer.PurchaseView.SetEmptyHint(chips ? "// SELECT A CHIP" : "// SELECT A COMMAND");
            _viewContainer.PackList.gameObject.SetActive(chips);
            _viewContainer.SingleList.gameObject.SetActive(!chips);
        }

        private void SyncPurchaseView()
        {
            if (_model.SelectedPack != null)
            {
                var pack = _model.SelectedPack;
                _viewContainer.PurchaseView.Show(
                    LocalizationHelper.GetCardPackName(pack),
                    string.Format(LocalizationHelper.GetUiText(UiTextKey.ChipInstallInfo), pack.CardCount),
                    LocalizationHelper.GetCardPackDescription(pack),
                    pack.Illustration,
                    _model.Quantity,
                    _model.TotalPrice,
                    canIncrease: _model.CanIncreaseQuantity,
                    canDecrease: _model.CanDecreaseQuantity,
                    canBuy: _model.CanBuy);
                return;
            }

            if (_model.SelectedSingle != null)
            {
                var card = _model.SelectedSingle;
                // 例: 「レア  コスト3  残り 2/3  所持 4」
                string info = string.Format(LocalizationHelper.GetUiText(UiTextKey.SingleInfo),
                    RarityName(card.Rarity), card.Cost, _model.RemainingOf(card), _model.SingleLimit, _model.OwnedCountOf(card));
                _viewContainer.PurchaseView.Show(
                    LocalizationHelper.GetCardName(card),
                    info,
                    LocalizationHelper.GetCardDescription(card),
                    card.Illustration,
                    _model.Quantity,
                    _model.TotalPrice,
                    canIncrease: _model.CanIncreaseQuantity,
                    canDecrease: _model.CanDecreaseQuantity,
                    canBuy: _model.CanBuy,
                    deckLimit: card.MaxCopiesInDeck,
                    deckLimitLabel: card.MaxCopiesInDeck > 0 ? string.Format(LocalizationHelper.GetUiText(UiTextKey.DeckLimit), card.MaxCopiesInDeck) : null);
                return;
            }

            _viewContainer.PurchaseView.Hide();
        }

        private static string RarityName(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Rare: return LocalizationHelper.GetUiText(UiTextKey.RarityRare);
                case CardRarity.Epic: return LocalizationHelper.GetUiText(UiTextKey.RarityEpic);
                default: return LocalizationHelper.GetUiText(UiTextKey.RarityNormal);
            }
        }

        private void BindPackItem(int index, RectTransform item)
        {
            var pack = _model.Packs[index];
            item.GetComponent<CardPackView>().Bind(
                pack,
                LocalizationHelper.GetCardPackName(pack),
                pack == _model.SelectedPack,
                _model.PriceOf(pack),
                _model.PriceOf(pack) <= _model.Tokens,
                HandlePackClicked);
        }

        private void BindSingleItem(int index, RectTransform item)
        {
            var card = _model.Singles[index];
            int remaining = _model.RemainingOf(card);
            bool soldOut = remaining <= 0;
            string stock = soldOut
                ? LocalizationHelper.GetUiText(UiTextKey.SoldOut)
                : string.Format(LocalizationHelper.GetUiText(UiTextKey.SingleStock), remaining, _model.SingleLimit);
            // 例: 「残り 2/3   所持 4」
            stock += "   " + string.Format(LocalizationHelper.GetUiText(UiTextKey.SingleOwned), _model.OwnedCountOf(card));
            item.GetComponent<SingleCommandView>().Bind(
                card,
                LocalizationHelper.GetCardName(card),
                card == _model.SelectedSingle,
                $"{_model.PriceOfSingle(card)} TKN",
                _model.PriceOfSingle(card) <= _model.Tokens,
                stock,
                soldOut,
                HandleSingleClicked);
        }

        private void HandleChipTab()
        {
            if (_model == null) return;
            _model.SetTab(ShopTab.Chips);
        }

        private void HandleSingleTab()
        {
            if (_model == null) return;
            _model.SetTab(ShopTab.Singles);
        }

        private void HandlePackClicked(CardPackData pack)
        {
            if (_model == null) return;
            _model.SelectPack(pack);
        }

        private void HandleSingleClicked(CardData card)
        {
            if (_model == null) return;
            _model.SelectSingle(card);
        }

        private void HandleIncrease()
        {
            if (_model == null) return;
            _model.IncreaseQuantity();
        }

        private void HandleDecrease()
        {
            if (_model == null) return;
            _model.DecreaseQuantity();
        }

        // チップもシングルも、手に入ったものは同じインストールのウインドウで見せる
        private void HandleBuy()
        {
            if (_model == null || !_viewContainer.IsOpen) return;

            bool isSingle = _model.SelectedPack == null;
            var bought = _model.Buy();
            if (bought.Length == 0) return;

            _viewContainer.ResultView.Show(bought, HandleResultCardHovered, HandleResultCardUnhovered);
            _mapTokenCounter.CountTo(_model.Tokens);
            _onPurchased.Invoke(bought, isSingle ? bought : new CardData[0], _model.Tokens);
        }

        private void HandleResultCardHovered(CardData card)
        {
            _viewContainer.ResultView.ShowDetail(card);
        }

        private void HandleResultCardUnhovered(CardData card)
        {
            _viewContainer.ResultView.HideDetail();
        }

        private void HandleResultClose()
        {
            _viewContainer.ResultView.Close();
        }

        private void HandleModelChanged()
        {
            Sync();
        }
    }
}

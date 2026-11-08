using System;
using UnityEngine;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Window.CardShop.Views
{
    // マーケット（チップ購入）のウインドウ（Assets/Prefabs/CardShopWindow.prefab）。
    // 左に一覧（チップ / シングルのタブ）、右に購入パネル、右上に持っているトークン。
    // 買ったものを見せるインストールのウインドウ（_resultView）は同じプレハブの中に置いてある。
    // 開くと PopupStack が一番手前にするので、このウインドウはマップと一緒にぼけ、その上に出る。
    // 開閉の動きと背後のぼかしは、ゲーム共通の WindowTransition（_dialog に付いている）に任せる。
    // 背面のクリックでは閉じない。閉じるボタンで閉じたいことを伝えるだけで、閉じるのは Presenter
    public sealed class CardShopWindowView : MonoBehaviour
    {
        [SerializeField] private WindowTransition _dialog;

        [SerializeField] private CommonScrollList _packList;
        // シングル購入の一覧（チップの一覧と同じ場所に、タブで切り替えて出す）
        [SerializeField] private CommonScrollList _singleList;
        [SerializeField] private CardShopPurchaseView _purchaseView;
        [SerializeField] private ChipInstallWindowView _resultView;

        [SerializeField] private CommonText _tokensText;
        [SerializeField] private CommonText _titleText;
        [SerializeField] private CommonButton _closeButton;
        [SerializeField] private CommonText _closeLabel;

        // 左のパネルのタブ（チップ / シングル）と、パネルの見出しの飾り（// CHIPS / // SINGLES）
        [SerializeField] private CommonButton _chipTabButton;
        [SerializeField] private CommonText _chipTabLabel;
        [SerializeField] private CommonButton _singleTabButton;
        [SerializeField] private CommonText _singleTabLabel;
        [SerializeField] private CommonText _listDeco;

        /// <summary>閉じるボタンが押された</summary>
        public event Action OnCloseClicked;

        public CommonScrollList PackList => _packList;
        public CommonScrollList SingleList => _singleList;
        public CardShopPurchaseView PurchaseView => _purchaseView;
        public ChipInstallWindowView ResultView => _resultView;
        public CommonText TokensText => _tokensText;
        public CommonText TitleText => _titleText;
        public CommonText CloseLabel => _closeLabel;
        public CommonButton ChipTabButton => _chipTabButton;
        public CommonText ChipTabLabel => _chipTabLabel;
        public CommonButton SingleTabButton => _singleTabButton;
        public CommonText SingleTabLabel => _singleTabLabel;
        public CommonText ListDeco => _listDeco;

        public bool IsOpen => _dialog.gameObject.activeSelf && !_dialog.IsClosing;

        private void Awake()
        {
            _closeButton.onClick.AddListener(HandleClose);
        }

        private void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(HandleClose);
        }

        public void Open()
        {
            _dialog.PlayOpen();
        }

        public void Close()
        {
            _dialog.PlayClose(null);
        }

        /// <summary>動きなしで閉じる（画面の初期化用）</summary>
        public void HideImmediate()
        {
            _dialog.HideImmediate();
        }

        private void HandleClose()
        {
            if (IsOpen) OnCloseClicked?.Invoke();
        }
    }
}

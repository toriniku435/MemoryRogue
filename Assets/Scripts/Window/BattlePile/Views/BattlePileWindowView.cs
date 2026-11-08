using System;
using UnityEngine;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Window.BattlePile.Views
{
    // アーカイブ・パージの中身の一覧ウインドウ（Assets/Prefabs/BattlePileWindow.prefab）。どちらの山かは Presenter が決めて文字を渡す。
    // 左にコマンドの一覧（_list）、右にカーソルを合わせたコマンドの拡大表示（_detail）。
    // バトルログと同じく、開いている間も戦闘は止めない。閉じるボタンか背面のクリックで閉じたいことを伝える
    public sealed class BattlePileWindowView : MonoBehaviour
    {
        [SerializeField] private WindowTransition _dialog;
        [SerializeField] private ClickArea _backgroundArea;
        // 飾りの見出し（// ARCHIVE など）と題
        [SerializeField] private CommonText _decoText;
        [SerializeField] private CommonText _titleText;
        [SerializeField] private CommonText _headerText;
        // 山の数（12個）
        [SerializeField] private CommonText _countText;
        [SerializeField] private CommonScrollList _list;
        [SerializeField] private CardListDetailView _detail;
        [SerializeField] private CommonButton _closeButton;
        [SerializeField] private CommonText _closeLabel;

        /// <summary>閉じるボタンが押された、または背面がクリックされた</summary>
        public event Action OnCloseRequested;

        public CommonScrollList List => _list;
        public CardListDetailView Detail => _detail;

        public bool IsOpen => _dialog.gameObject.activeSelf && !_dialog.IsClosing;

        private void Awake()
        {
            _closeButton.onClick.AddListener(HandleClose);
            _backgroundArea.OnClicked += HandleBackgroundClicked;
        }

        private void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(HandleClose);
            _backgroundArea.OnClicked -= HandleBackgroundClicked;
        }

        public void SetTexts(string deco, string title, string header, string close)
        {
            _decoText.text = deco;
            _titleText.text = title;
            _headerText.text = header;
            _closeLabel.text = close;
        }

        public void SetCount(string count)
        {
            _countText.text = count;
        }

        public void Open()
        {
            _dialog.PlayOpen();
            _list.FitColumnsToWidth();
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
            if (IsOpen) OnCloseRequested?.Invoke();
        }

        // 背面のクリックはボタンと違って自分では鳴らないので、取り消しの音をここで鳴らす
        private void HandleBackgroundClicked()
        {
            if (!IsOpen) return;
            AudioUseCase.PlaySe(SeId.Cancel);
            OnCloseRequested?.Invoke();
        }
    }
}

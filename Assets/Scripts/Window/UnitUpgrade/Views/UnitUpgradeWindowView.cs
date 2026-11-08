using System;
using UnityEngine;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Window.UnitUpgrade.Views
{
    // ユニット強化のウインドウ（Assets/Prefabs/UnitUpgradeWindow.prefab）。持っているトークンと、HP / メモリー上限の2項目、
    // 下にキャンセルと反映。開閉の動きと背後のぼかしは、ゲーム共通の WindowTransition（_dialog に付いている）に任せる。
    // キャンセル（_closeButton）か背面のクリックで閉じたいこと、反映で確定したいことを伝えるだけで、
    // 確定も、変更を捨ててよいかの確認も、閉じるのも Presenter
    public sealed class UnitUpgradeWindowView : MonoBehaviour
    {
        [SerializeField] private WindowTransition _dialog;
        [SerializeField] private ClickArea _backgroundArea;
        [SerializeField] private CommonText _titleText;
        [SerializeField] private CommonText _tokenLabel;
        [SerializeField] private TokenCounterView _tokenCounter;
        [SerializeField] private UnitUpgradeRowView _hpRow;
        [SerializeField] private UnitUpgradeRowView _memoryRow;
        [SerializeField] private CommonButton _closeButton;
        [SerializeField] private CommonText _closeLabel;
        [SerializeField] private CommonButton _applyButton;
        [SerializeField] private CommonText _applyLabel;

        /// <summary>キャンセルが押された、または背面がクリックされた</summary>
        public event Action OnCloseRequested;

        /// <summary>反映が押された</summary>
        public event Action OnApplyClicked;

        public UnitUpgradeRowView HpRow => _hpRow;
        public UnitUpgradeRowView MemoryRow => _memoryRow;
        public TokenCounterView TokenCounter => _tokenCounter;

        public bool IsOpen => _dialog.gameObject.activeSelf && !_dialog.IsClosing;

        private void Awake()
        {
            _closeButton.onClick.AddListener(HandleClose);
            _applyButton.onClick.AddListener(HandleApply);
            _backgroundArea.OnClicked += HandleBackgroundClicked;
        }

        private void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(HandleClose);
            _applyButton.onClick.RemoveListener(HandleApply);
            _backgroundArea.OnClicked -= HandleBackgroundClicked;
        }

        public void SetTexts(string title, string tokenLabel, string cancelLabel, string applyLabel)
        {
            _titleText.text = title;
            _tokenLabel.text = tokenLabel;
            _closeLabel.text = cancelLabel;
            _applyLabel.text = applyLabel;
        }

        /// <summary>反映を押せるか（確定していない強化があるときだけ）</summary>
        public void SetApplyInteractable(bool interactable)
        {
            _applyButton.interactable = interactable;
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
            if (IsOpen) OnCloseRequested?.Invoke();
        }

        private void HandleApply()
        {
            if (IsOpen) OnApplyClicked?.Invoke();
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

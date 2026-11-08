using System;
using UnityEngine;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.Window.CampaignResume.Views
{
    // 中断したキャンペーンがあるときに出す3択のウインドウ（Assets/Prefabs/CampaignResumeDialog.prefab）。
    // 再開 / 破棄 / キャンセル。背面のクリックはキャンセルと同じ扱い。
    // 押されたことを伝えるだけで、何をするかは Presenter と Initializer が決める。
    // 開閉の動きと背後のぼかしは、ゲーム共通の WindowTransition（_dialog に付いている）に任せる
    public sealed class CampaignResumeDialogView : MonoBehaviour
    {
        [SerializeField] private WindowTransition _dialog;
        [SerializeField] private ClickArea _backgroundArea;
        [SerializeField] private CommonText _messageText;
        // 中断したところ（LAYER 04 / 10 など）
        [SerializeField] private CommonText _detailText;
        [SerializeField] private CommonButton _resumeButton;
        [SerializeField] private CommonText _resumeLabel;
        [SerializeField] private CommonButton _discardButton;
        [SerializeField] private CommonText _discardLabel;
        [SerializeField] private CommonButton _cancelButton;
        [SerializeField] private CommonText _cancelLabel;

        public event Action OnResumeClicked;
        public event Action OnDiscardClicked;

        /// <summary>キャンセルが押された、または背面がクリックされた</summary>
        public event Action OnCancelRequested;

        public bool IsOpen => _dialog.gameObject.activeSelf && !_dialog.IsClosing;

        private void Awake()
        {
            _resumeButton.onClick.AddListener(HandleResume);
            _discardButton.onClick.AddListener(HandleDiscard);
            _cancelButton.onClick.AddListener(HandleCancel);
            _backgroundArea.OnClicked += HandleBackgroundClicked;
        }

        private void OnDestroy()
        {
            _resumeButton.onClick.RemoveListener(HandleResume);
            _discardButton.onClick.RemoveListener(HandleDiscard);
            _cancelButton.onClick.RemoveListener(HandleCancel);
            _backgroundArea.OnClicked -= HandleBackgroundClicked;
        }

        public void SetTexts(string message, string resume, string discard, string cancel)
        {
            _messageText.text = message;
            _resumeLabel.text = resume;
            _discardLabel.text = discard;
            _cancelLabel.text = cancel;
        }

        public void Open(string detail)
        {
            _detailText.text = detail;
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

        // 閉じる途中の二度押しは伝えない
        private void HandleResume()
        {
            if (IsOpen) OnResumeClicked?.Invoke();
        }

        private void HandleDiscard()
        {
            if (IsOpen) OnDiscardClicked?.Invoke();
        }

        private void HandleCancel()
        {
            if (IsOpen) OnCancelRequested?.Invoke();
        }

        // 背面のクリックはボタンと違って自分では鳴らないので、取り消しの音をここで鳴らす
        private void HandleBackgroundClicked()
        {
            if (IsOpen) AudioUseCase.PlaySe(SeId.Cancel);
            HandleCancel();
        }
    }
}

using System;
using UnityEngine;
using Live2DCardRogue.Common.Audio;

namespace Live2DCardRogue.Common.Views
{
    // 「OK / キャンセル」の2択で確かめるウインドウ（Assets/Prefabs/ConfirmDialog.prefab）。
    // 押されたことを伝えるだけで、どちらを押したら何をするかは使う側（Presenter）が決める。
    // ウインドウの背面のクリックは、ふつうはキャンセルと同じ扱い（尋ねる側が切れる。Open の cancelOnBackground）。
    //
    // 開閉の動きと背後のぼかしは、ゲーム共通の WindowTransition（_dialog に付いている）に任せる。
    // このコンポーネントは常に表示されている親に付け、開閉するのは子の _dialog だけにしてある
    // （閉じたときに散る粒を _dialog の外に置くため。WindowTransition 参照）
    public sealed class ConfirmDialogView : MonoBehaviour
    {
        [SerializeField] private WindowTransition _dialog;
        [SerializeField] private ClickArea _backgroundArea;
        [SerializeField] private CommonText _messageText;
        // 補足（どのステージか、など）。空なら出さない
        [SerializeField] private CommonText _detailText;
        [SerializeField] private CommonButton _okButton;
        [SerializeField] private CommonText _okLabel;
        [SerializeField] private CommonButton _cancelButton;
        [SerializeField] private CommonText _cancelLabel;

        /// <summary>OK が押された</summary>
        public event Action OnOkClicked;

        /// <summary>キャンセルが押された、または背面がクリックされた</summary>
        public event Action OnCancelRequested;

        /// <summary>開いていて、閉じる途中でもない</summary>
        public bool IsOpen => _dialog.gameObject.activeSelf && !_dialog.IsClosing;

        private bool _cancelOnBackground = true;

        private void Awake()
        {
            _okButton.onClick.AddListener(HandleOk);
            _cancelButton.onClick.AddListener(HandleCancel);
            _backgroundArea.OnClicked += HandleBackgroundClicked;
        }

        private void OnDestroy()
        {
            _okButton.onClick.RemoveListener(HandleOk);
            _cancelButton.onClick.RemoveListener(HandleCancel);
            _backgroundArea.OnClicked -= HandleBackgroundClicked;
        }

        /// <param name="cancelOnBackground">false なら背面のクリックでは閉じない（音も鳴らさない）</param>
        public void Open(string message, string detail, string okLabel, string cancelLabel, bool cancelOnBackground = true)
        {
            _cancelOnBackground = cancelOnBackground;
            _messageText.text = message;
            _detailText.text = detail;
            _detailText.gameObject.SetActive(!string.IsNullOrEmpty(detail));
            _okLabel.text = okLabel;
            _cancelLabel.text = cancelLabel;
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
        private void HandleOk()
        {
            if (IsOpen) OnOkClicked?.Invoke();
        }

        // 背面のクリックはボタンと違って自分では鳴らないので、取り消しの音をここで鳴らす
        private void HandleBackgroundClicked()
        {
            if (!_cancelOnBackground) return;
            if (IsOpen) AudioUseCase.PlaySe(SeId.Cancel);
            HandleCancel();
        }

        private void HandleCancel()
        {
            if (IsOpen) OnCancelRequested?.Invoke();
        }
    }
}

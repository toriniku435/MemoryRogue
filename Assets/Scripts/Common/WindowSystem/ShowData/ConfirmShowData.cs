using System;

namespace Live2DCardRogue.Common.WindowSystem
{
    // 確認ウインドウで1つ尋ねる内容と、答えを受け取るコールバック
    public sealed class ConfirmShowData : WindowShowData
    {
        public string Message { get; }
        /// <summary>補足（どのステージか、など）。null か空なら出さない</summary>
        public string Detail { get; }
        public string OkLabel { get; }
        public string CancelLabel { get; }
        public Action OnOk { get; }
        /// <summary>キャンセルか背面のクリック</summary>
        public Action OnCancel { get; }
        /// <summary>背面のクリックをキャンセルとして扱うか。false なら背面のクリックは何もしない（どちらかのボタンで答えさせる）</summary>
        public bool CancelOnBackground { get; }

        public ConfirmShowData(string message, string detail, string okLabel, string cancelLabel, Action onOk, Action onCancel = null,
            bool cancelOnBackground = true)
        {
            CancelOnBackground = cancelOnBackground;
            Message = message;
            Detail = detail;
            OkLabel = okLabel;
            CancelLabel = cancelLabel;
            OnOk = onOk;
            OnCancel = onCancel;
        }
    }
}

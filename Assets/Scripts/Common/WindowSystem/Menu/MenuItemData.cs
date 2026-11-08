using System;
using Live2DCardRogue.Common.Localization;

namespace Live2DCardRogue.Common.WindowSystem
{
    // メニューのボタン1つ分。文言と、押したときにすること。
    //   canPress : 押せるか（null なら常に押せる）。開くたびと押したときに確かめ、押せなければボタンを暗くする
    //   confirm  : 実行する前に確認ウインドウで尋ねる（null なら尋ねない）。OK ならメニューを閉じてから onClicked を呼ぶ
    // 確認しない項目は、メニューを開いたまま onClicked を呼ぶ（オプションなど、ウインドウを上に重ねて開くもの）
    public sealed class MenuItemData
    {
        public UiTextKey Label { get; }
        public Action OnClicked { get; }
        public MenuItemStyle Style { get; }
        public MenuConfirmData Confirm { get; }

        private readonly Func<bool> _canPress;

        public MenuItemData(UiTextKey label, Action onClicked, MenuItemStyle style = MenuItemStyle.Normal, Func<bool> canPress = null, MenuConfirmData confirm = null)
        {
            Label = label;
            OnClicked = onClicked;
            Style = style;
            _canPress = canPress;
            Confirm = confirm;
        }

        public bool CanPress()
        {
            return _canPress == null || _canPress.Invoke();
        }
    }
}

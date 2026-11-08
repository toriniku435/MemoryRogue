using Live2DCardRogue.Common.Localization;

namespace Live2DCardRogue.Common.WindowSystem
{
    // 項目を押したときに、実行する前に出す確認ウインドウの文言（共用の ConfirmDialog で尋ねる）
    public sealed class MenuConfirmData
    {
        public UiTextKey Message { get; }
        public UiTextKey Detail { get; }
        public UiTextKey Ok { get; }
        public UiTextKey Cancel { get; }

        public MenuConfirmData(UiTextKey message, UiTextKey detail, UiTextKey ok, UiTextKey cancel)
        {
            Message = message;
            Detail = detail;
            Ok = ok;
            Cancel = cancel;
        }
    }
}

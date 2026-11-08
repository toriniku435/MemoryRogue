using System.Collections.Generic;
using Live2DCardRogue.Common.Localization;

namespace Live2DCardRogue.Common.WindowSystem
{
    // ボタンを縦に並べるだけのウインドウ（マップ・バトルのメニュー、タイトルのコレクション）。
    // 中身は、タイトルと上から並べる項目
    public sealed class MenuWindowInitData : WindowInitData
    {
        public override WindowId Id => WindowId.Menu;

        public UiTextKey Title { get; }
        public IReadOnlyList<MenuItemData> Items { get; }

        /// <summary>確認のある項目があるときに使う、シーンで共用の確認ウインドウ（無ければ null）</summary>
        public WindowHandler<ConfirmShowData> Confirm { get; }

        public MenuWindowInitData(UiTextKey title, WindowHandler<ConfirmShowData> confirm, params MenuItemData[] items)
        {
            Title = title;
            Confirm = confirm;
            Items = items;
        }
    }
}

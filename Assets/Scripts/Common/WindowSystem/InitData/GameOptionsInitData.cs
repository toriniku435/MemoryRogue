using System;

namespace Live2DCardRogue.Common.WindowSystem
{
    // オプション（タイトル・マップ・バトルで共通）
    public sealed class GameOptionsInitData : WindowInitData
    {
        public override WindowId Id => WindowId.GameOptions;

        /// <summary>変更を捨てる・ユーザーデータを消すときに尋ねる、シーンで共用の確認ウインドウ</summary>
        public WindowHandler<ConfirmShowData> Confirm { get; }

        /// <summary>ユーザーデータを消す。null なら削除の行を出さない（タイトルだけが渡す）</summary>
        public Action OnDeleteUserData { get; }

        public GameOptionsInitData(WindowHandler<ConfirmShowData> confirm, Action onDeleteUserData = null)
        {
            Confirm = confirm;
            OnDeleteUserData = onDeleteUserData;
        }
    }
}

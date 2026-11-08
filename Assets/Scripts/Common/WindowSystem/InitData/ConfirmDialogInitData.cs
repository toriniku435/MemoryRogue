namespace Live2DCardRogue.Common.WindowSystem
{
    // 共用の確認ウインドウ。生成に要るものは無く、尋ねる内容は開くたびに ConfirmShowData で渡す
    public sealed class ConfirmDialogInitData : WindowInitData<ConfirmShowData>
    {
        public override WindowId Id => WindowId.ConfirmDialog;
    }
}

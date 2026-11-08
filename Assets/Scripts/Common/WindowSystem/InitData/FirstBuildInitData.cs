namespace Live2DCardRogue.Common.WindowSystem
{
    // ファーストビルド（開けたチップの開封）。見せるコマンドは開くときに FirstBuildShowData で渡す
    public sealed class FirstBuildInitData : WindowInitData<FirstBuildShowData>
    {
        public override WindowId Id => WindowId.FirstBuild;
    }
}

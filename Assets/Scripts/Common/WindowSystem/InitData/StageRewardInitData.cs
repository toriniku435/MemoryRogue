namespace Live2DCardRogue.Common.WindowSystem
{
    // ステージの勝敗の後に出す報酬。見せる報酬は開くときに StageRewardShowData で渡す
    public sealed class StageRewardInitData : WindowInitData<StageRewardShowData>
    {
        public override WindowId Id => WindowId.StageReward;
    }
}

using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.SceneTransition
{
    // リザルトの開始条件。成績はマップで確定済みで、この画面は見せるだけ
    public sealed class ResultSceneTransitionData : ResultSceneTransitionDataBase
    {
        public CampaignResult Result { get; }

        public ResultSceneTransitionData(CampaignResult result)
        {
            Result = result;
        }
    }
}

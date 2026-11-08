using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.SceneTransition
{
    // エンディングの開始条件。成績はマップで確定済みで、ルートの分岐はこれの Route を見るだけ。
    // 読み終えたら同じ結果をリザルトへ渡す
    public sealed class EndingSceneTransitionData : SceneTransitionData
    {
        public CampaignResult Result { get; }

        public EndingSceneTransitionData(CampaignResult result)
            : base(SceneName.Ending)
        {
            Result = result;
        }
    }
}

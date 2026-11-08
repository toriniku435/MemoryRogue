using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.SceneTransition
{
    // マップを開く。進み具合はリポジトリから読むので、持つのは戦闘から戻ってきたときの結果だけ
    // （タイトルから入る・マップを開き直すときは null）
    public sealed class MapSceneTransitionData : SceneTransitionData
    {
        public BattleResult BattleResult { get; }

        public MapSceneTransitionData(BattleResult battleResult = null)
            : base(SceneName.Map)
        {
            BattleResult = battleResult;
        }
    }
}

using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.SceneTransition;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    // 遷移のデータは行き先を自分で持つ: どの型も決まった1つのシーンへ行く（行き先が複数あるなら型を分ける）
    public sealed class SceneTransitionDataTests
    {
        [Test]
        public void SceneTransitionData_GoesToItsOwnScene()
        {
            var result = new CampaignResult(0, 0, 0, 0, 0, CampaignRank.E, EndingRoute.Normal);

            Assert.AreEqual(SceneName.Title, new TitleSceneTransitionData().Destination);
            Assert.AreEqual(SceneName.Map, new MapSceneTransitionData().Destination);
            Assert.AreEqual(SceneName.Battle, new BattleSceneTransitionData(null, null, null).Destination);
            Assert.AreEqual(SceneName.Ending, new EndingSceneTransitionData(result).Destination);
            Assert.AreEqual(SceneName.Result, new ResultSceneTransitionData(result).Destination);
            Assert.AreEqual(SceneName.Result, new ChallengeResultTransitionData(ChallengeType.Normal, 30, true, 0).Destination);
        }

        [Test]
        public void MapSceneTransitionData_CarriesTheBattleResultOnlyWhenGiven()
        {
            Assert.IsNull(new MapSceneTransitionData().BattleResult);

            var result = new BattleResult(true, tokenBonusPercent: 50, tokensSpent: 100);
            Assert.AreSame(result, new MapSceneTransitionData(result).BattleResult);
        }

        [Test]
        public void BattleSceneTransitionData_StaysWithoutAReturn()
        {
            Assert.IsNull(new BattleSceneTransitionData(null, null, null).CreateReturnData);
        }

        [Test]
        public void BattleSceneTransitionData_PlaysTheBattleThemeUnlessToldOtherwise()
        {
            Assert.AreEqual(BgmId.Battle, new BattleSceneTransitionData(null, null, null).Bgm);
            Assert.AreEqual(BgmId.BossBattle, new BattleSceneTransitionData(null, null, null, bgm: BgmId.BossBattle).Bgm);
        }
    }
}

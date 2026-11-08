using Live2DCardRogue.Common.Repositories;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    // シーンをまたいで生き残るstaticな状態なので、テストの実行順に影響されないよう毎回戻してから始める
    public sealed class CampaignProgressRepositoryTests
    {
        [SetUp]
        [TearDown]
        public void ResetProgress()
        {
            CampaignProgressRepository.Reset();
        }

        [Test]
        public void Reset_StartsAtTheFirstStage()
        {
            CampaignProgressRepository.AdvanceStage();
            CampaignProgressRepository.Reset();

            Assert.AreEqual(0, CampaignProgressRepository.CurrentStageIndex);
        }

        // バックアップブーストの回数はリザルトの減点に使う。キャンペーンを始め直せば0に戻る
        [Test]
        public void RecordBackupBoost_CountsUp_AndResetClearsIt()
        {
            CampaignProgressRepository.RecordBackupBoost();
            CampaignProgressRepository.RecordBackupBoost();
            Assert.AreEqual(2, CampaignProgressRepository.BackupBoostCount);

            CampaignProgressRepository.Reset();
            Assert.AreEqual(0, CampaignProgressRepository.BackupBoostCount);
        }

        [Test]
        public void AdvanceStage_MovesOneCellForward()
        {
            CampaignProgressRepository.AdvanceStage();
            Assert.AreEqual(1, CampaignProgressRepository.CurrentStageIndex);

            CampaignProgressRepository.AdvanceStage();
            Assert.AreEqual(2, CampaignProgressRepository.CurrentStageIndex);
        }

        // 最後のマスをクリアした時点で踏破。マス数はキャンペーンのアセット次第なので引数で渡す
        [Test]
        public void IsCompleted_IsTrueOnlyAfterClearingTheLastStage()
        {
            Assert.IsFalse(CampaignProgressRepository.IsCompleted(stageCount: 2));

            CampaignProgressRepository.AdvanceStage();
            Assert.IsFalse(CampaignProgressRepository.IsCompleted(stageCount: 2));

            CampaignProgressRepository.AdvanceStage();
            Assert.IsTrue(CampaignProgressRepository.IsCompleted(stageCount: 2));
        }

        // ステージが0個のキャンペーンは、進みようがないので最初から踏破扱い
        [Test]
        public void IsCompleted_WithNoStages_IsTrue()
        {
            Assert.IsTrue(CampaignProgressRepository.IsCompleted(stageCount: 0));
        }
    }
}

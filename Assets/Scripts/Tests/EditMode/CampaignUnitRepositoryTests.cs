using Live2DCardRogue.Common.Repositories;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class CampaignUnitRepositoryTests
    {
        [SetUp]
        [TearDown]
        public void ResetRepository()
        {
            CampaignUnitRepository.Clear();
        }

        [Test]
        public void Initialize_ThenSet_KeepsTheUpgradedValues()
        {
            CampaignUnitRepository.Initialize(100, 10);
            CampaignUnitRepository.Set(160, 12);

            Assert.IsTrue(CampaignUnitRepository.IsInitialized);
            Assert.AreEqual(160, CampaignUnitRepository.MaxHp);
            Assert.AreEqual(12, CampaignUnitRepository.MaxMemory);
        }

        // キャンペーンを始め直すと、マップが初期値を配り直せるように未初期化へ戻る
        [Test]
        public void Clear_MakesTheNextCampaignStartUninitialized()
        {
            CampaignUnitRepository.Initialize(100, 10);

            CampaignUnitRepository.Clear();

            Assert.IsFalse(CampaignUnitRepository.IsInitialized);
        }
    }
}

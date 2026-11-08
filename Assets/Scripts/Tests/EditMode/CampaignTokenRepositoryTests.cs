using Live2DCardRogue.Common.Repositories;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class CampaignTokenRepositoryTests
    {
        // static なので、前後のテストに持ち越さない
        [SetUp]
        [TearDown]
        public void ResetRepository()
        {
            CampaignTokenRepository.Clear();
        }

        [Test]
        public void Clear_MakesTheNextCampaignStartUninitialized()
        {
            CampaignTokenRepository.Initialize(500);

            CampaignTokenRepository.Clear();

            Assert.IsFalse(CampaignTokenRepository.IsInitialized);
            Assert.AreEqual(0, CampaignTokenRepository.Tokens);
        }

        [Test]
        public void Initialize_SetsTokensAndMarksInitialized()
        {
            CampaignTokenRepository.Initialize(500);

            Assert.IsTrue(CampaignTokenRepository.IsInitialized);
            Assert.AreEqual(500, CampaignTokenRepository.Tokens);
        }

        [Test]
        public void Add_AccumulatesRewards()
        {
            CampaignTokenRepository.Initialize(500);

            CampaignTokenRepository.Add(100);
            CampaignTokenRepository.Add(200);

            Assert.AreEqual(800, CampaignTokenRepository.Tokens);
        }

        [Test]
        public void Add_IgnoresZeroAndNegativeAmounts()
        {
            CampaignTokenRepository.Initialize(500);

            CampaignTokenRepository.Add(0);
            CampaignTokenRepository.Add(-100);

            Assert.AreEqual(500, CampaignTokenRepository.Tokens);
        }

        [Test]
        public void Add_ClampsInsteadOfOverflowing()
        {
            CampaignTokenRepository.Initialize(int.MaxValue - 1);

            CampaignTokenRepository.Add(100);

            Assert.AreEqual(int.MaxValue, CampaignTokenRepository.Tokens);
        }

        // 購入画面から戻った残りで置き換える
        [Test]
        public void Set_ReplacesWithRemainingTokens()
        {
            CampaignTokenRepository.Initialize(500);

            CampaignTokenRepository.Set(200);

            Assert.AreEqual(200, CampaignTokenRepository.Tokens);
        }

        [Test]
        public void Set_NeverGoesBelowZero()
        {
            CampaignTokenRepository.Initialize(500);

            CampaignTokenRepository.Set(-1);

            Assert.AreEqual(0, CampaignTokenRepository.Tokens);
        }
    }
}

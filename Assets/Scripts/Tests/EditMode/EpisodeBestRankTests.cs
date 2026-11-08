using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Save;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    // エピソード選択に出す、エピソードごとの一番良いランク（CampaignClearData.BestRank）
    public sealed class EpisodeBestRankTests
    {
        [Test]
        public void BestRank_IsNullUntilCleared()
        {
            Assert.IsNull(new CampaignClearData().BestRank(0));
        }

        // 同じエピソードを何度クリアしても、一番良いランクを返す。ほかのエピソードの記録は混ぜない
        [Test]
        public void BestRank_IsTheBestOfThatEpisode()
        {
            var data = new CampaignClearData();
            data.clears.Add(new CampaignClearRecord { episodeIndex = 0, rank = "B" });
            data.clears.Add(new CampaignClearRecord { episodeIndex = 0, rank = "S" });
            data.clears.Add(new CampaignClearRecord { episodeIndex = 0, rank = "A" });
            data.clears.Add(new CampaignClearRecord { episodeIndex = 1, rank = "SSS" });

            Assert.AreEqual(CampaignRank.S, data.BestRank(0));
            Assert.AreEqual(CampaignRank.SSS, data.BestRank(1));
            Assert.IsNull(data.BestRank(2));
        }

        // 読めないランクの名前は飛ばす（列挙の名前を変えた古い記録など）
        [Test]
        public void BestRank_SkipsUnreadableRanks()
        {
            var data = new CampaignClearData();
            data.clears.Add(new CampaignClearRecord { episodeIndex = 0, rank = "Z" });

            Assert.IsNull(data.BestRank(0));
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Window.EpisodeSelect.Models;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class EpisodeUnlockRuleTests
    {
        private static EpisodeState State(int index, ICollection<int> cleared, ICollection<int> playable)
        {
            return EpisodeUnlockRule.GetState(index, cleared.Contains, playable.Contains);
        }

        // エピソード1は、何もクリアしていなくても遊べる
        [Test]
        public void Episode1_IsAlwaysOpen()
        {
            Assert.AreEqual(EpisodeState.Playable, State(0, new int[0], new[] { 0 }));
        }

        // エピソード2はエピソード1を、エピソード3はエピソード2をクリアするまで選べない
        [Test]
        public void LaterEpisodes_NeedThePreviousOneCleared()
        {
            var playable = new[] { 0, 1, 2 };
            Assert.AreEqual(EpisodeState.Locked, State(1, new int[0], playable));
            Assert.AreEqual(EpisodeState.Playable, State(1, new[] { 0 }, playable));

            Assert.AreEqual(EpisodeState.Locked, State(2, new[] { 0 }, playable), "エピソード1だけでは3は選べない");
            Assert.AreEqual(EpisodeState.Playable, State(2, new[] { 0, 1 }, playable));
            Assert.AreEqual(EpisodeState.Playable, State(2, new[] { 1 }, playable), "条件はひとつ前のエピソードだけ");
        }

        // 選べるようになっても、中身が無ければ準備中
        [Test]
        public void UnlockedEpisode_WithoutContent_IsNotReady()
        {
            Assert.AreEqual(EpisodeState.NotReady, State(1, new[] { 0 }, new[] { 0 }));
            Assert.AreEqual(EpisodeState.Locked, State(1, new int[0], new[] { 0 }), "選べないことが先");
        }

        [Test]
        public void RequiredEpisode_IsThePreviousOne()
        {
            Assert.AreEqual(-1, EpisodeUnlockRule.RequiredEpisode(0));
            Assert.AreEqual(0, EpisodeUnlockRule.RequiredEpisode(1));
            Assert.AreEqual(1, EpisodeUnlockRule.RequiredEpisode(2));
        }
    }
}

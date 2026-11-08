using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.Tutorial;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    // チュートリアルの手順は、始めてから終えるまで CampaignTutorialRepository が「解放しない」印を付けて持つ。
    // 印が無いと、ビルドでは模擬戦へ切り替えるときに手順が解放され、チュートリアルが流れない（エディタでは再現しない）
    public sealed class CampaignTutorialRepositoryTests
    {
        private TutorialSequence _map;
        private TutorialSequence _battle;

        [SetUp]
        public void SetUp()
        {
            _map = ScriptableObject.CreateInstance<TutorialSequence>();
            _battle = ScriptableObject.CreateInstance<TutorialSequence>();
        }

        [TearDown]
        public void TearDown()
        {
            CampaignTutorialRepository.Finish();
            Object.DestroyImmediate(_map);
            Object.DestroyImmediate(_battle);
        }

        [Test]
        public void StartFromMap_HoldsBothSequencesUntilFinish()
        {
            CampaignTutorialRepository.StartFromMap(_map, _battle);

            Assert.AreSame(_map, CampaignTutorialRepository.MapSequence);
            Assert.AreSame(_battle, CampaignTutorialRepository.BattleSequence);
            Assert.IsTrue(IsHeld(_map), "マップの手順に解放しない印が無い");
            Assert.IsTrue(IsHeld(_battle), "戦闘の手順に解放しない印が無い");

            // 戦闘へ移っても持ったまま
            CampaignTutorialRepository.MoveToBattle();
            Assert.AreSame(_battle, CampaignTutorialRepository.BattleSequence);
            Assert.IsTrue(IsHeld(_battle));

            CampaignTutorialRepository.Finish();
            Assert.IsNull(CampaignTutorialRepository.MapSequence);
            Assert.IsNull(CampaignTutorialRepository.BattleSequence);
            Assert.IsFalse(IsHeld(_map), "終えたのに印が残っている");
            Assert.IsFalse(IsHeld(_battle), "終えたのに印が残っている");
        }

        // 見直しで始め直したときも、前の手順を手放してから持ち直す
        [Test]
        public void StartFromMap_Again_ReleasesThePreviousSequences()
        {
            var other = ScriptableObject.CreateInstance<TutorialSequence>();
            try
            {
                CampaignTutorialRepository.StartFromMap(_map, _battle);
                CampaignTutorialRepository.StartFromMap(other, _battle);

                Assert.IsFalse(IsHeld(_map));
                Assert.IsTrue(IsHeld(other));
                Assert.IsTrue(IsHeld(_battle));
            }
            finally
            {
                CampaignTutorialRepository.Finish();
                Object.DestroyImmediate(other);
            }
        }

        private static bool IsHeld(Object asset)
        {
            return (asset.hideFlags & HideFlags.DontUnloadUnusedAsset) != 0;
        }
    }
}

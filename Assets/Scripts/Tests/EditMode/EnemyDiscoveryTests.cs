using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Live2DCardRogue.Common.Save;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.TitleScene.Models;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    // エネミーコレクションの「出会ったことがある敵」: 戦った敵の記録と、記録を始める前のデータからの割り出し
    public sealed class EnemyDiscoveryTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "Live2DCardRogueEnemyTests_" + System.Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static EnemyData NewEnemy(string id)
        {
            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            SetPrivateField(enemy, "_id", id);
            return enemy;
        }

        private static StageData NewStage(params EnemyData[] enemies)
        {
            var stage = ScriptableObject.CreateInstance<StageData>();
            SetPrivateField(stage, "_enemies", enemies);
            return stage;
        }

        // エピソード1: ステージ A, B, C（敵も A, B, C）
        private static CampaignBalance NewBalance()
        {
            var episode = ScriptableObject.CreateInstance<CampaignData>();
            SetPrivateField(episode, "_stages", new[] { NewStage(NewEnemy("A")), NewStage(NewEnemy("B")), NewStage(NewEnemy("C")) });
            var balance = ScriptableObject.CreateInstance<CampaignBalance>();
            SetPrivateField(balance, "_episodes", new[] { episode });
            return balance;
        }

        // 戦った敵は記録に残り、同じ敵は一度だけ
        [Test]
        public void RecordDiscoveredEnemies_AddsEachEnemyOnce()
        {
            var service = new CampaignSaveService(new SaveFileStorage(_directory));
            var trojan = NewEnemy("Trojan");

            service.RecordDiscoveredEnemies(new[] { trojan, trojan });
            service.RecordDiscoveredEnemies(new[] { NewEnemy("Botnet") });

            CollectionAssert.AreEqual(new[] { "Trojan", "Botnet" }, service.LoadClearData().discoveredEnemies);
        }

        // 記録だけ
        [Test]
        public void Resolve_UsesTheRecord()
        {
            var clear = new CampaignClearData { discoveredEnemies = new List<string> { "X" } };

            var ids = EnemyDiscoveryResolver.Resolve(clear, null, null, NewBalance(), null);

            CollectionAssert.AreEquivalent(new[] { "X" }, ids);
        }

        // クリアしたエピソードは全ステージの敵に出会っている
        [Test]
        public void Resolve_AClearedEpisodeCountsEveryStage()
        {
            var clear = new CampaignClearData();
            clear.clears.Add(new CampaignClearRecord { episodeIndex = 0 });

            var ids = EnemyDiscoveryResolver.Resolve(clear, null, null, NewBalance(), null);

            CollectionAssert.AreEquivalent(new[] { "A", "B", "C" }, ids);
        }

        // 中断しているストーリーは、今いるステージより前の敵だけ（今のステージはまだ戦っていないかもしれない）
        [Test]
        public void Resolve_ASuspendedCampaignCountsClearedStagesOnly()
        {
            var campaign = new CampaignSaveData { episodeIndex = 0, stageIndex = 2 };

            var ids = EnemyDiscoveryResolver.Resolve(null, campaign, null, NewBalance(), null);

            CollectionAssert.AreEquivalent(new[] { "A", "B" }, ids);
        }

        // 中断しているエンドレスは、突破した階層の敵をシードから組み直す
        [Test]
        public void Resolve_ASuspendedEndlessRunCountsClearedLayers()
        {
            var a = NewEnemy("A");
            var entry = new EndlessBalance.EnemyEntry();
            SetPrivateField(entry, "_enemy", a);
            SetPrivateField(entry, "_fromLayer", 1);
            SetPrivateField(entry, "_canAppearAlone", true);
            var endlessBalance = ScriptableObject.CreateInstance<EndlessBalance>();
            SetPrivateField(endlessBalance, "_enemies", new[] { entry });

            Assert.IsEmpty(EnemyDiscoveryResolver.Resolve(null, null, new CampaignSaveData { isEndless = true, stageIndex = 0 }, NewBalance(), endlessBalance));
            CollectionAssert.AreEquivalent(new[] { "A" },
                EnemyDiscoveryResolver.Resolve(null, null, new CampaignSaveData { isEndless = true, stageIndex = 3, endlessSeed = 5 }, NewBalance(), endlessBalance));
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEditor;

namespace Live2DCardRogue.Tests.EditMode
{
    // コレクション（エネミー）に並べる敵: 遊ぶ中で出会う敵（ストーリーのステージ・エンドレスの候補）がすべて並ぶ
    public sealed class EnemyCollectionTests
    {
        private const string DataPath = "Assets/ScriptableObjects/EnemyCollection.asset";

        [Test]
        public void Asset_ContainsEveryEnemyMetInPlay()
        {
            var data = AssetDatabase.LoadAssetAtPath<EnemyCollectionData>(DataPath);
            Assert.IsNotNull(data, DataPath);
            var listed = new HashSet<EnemyData>(data.Enemies);

            var met = new HashSet<EnemyData>();
            foreach (var guid in AssetDatabase.FindAssets("t:StageData"))
            {
                foreach (var enemy in AssetDatabase.LoadAssetAtPath<StageData>(AssetDatabase.GUIDToAssetPath(guid)).CreateEnemyLineup()) met.Add(enemy);
            }
            foreach (var guid in AssetDatabase.FindAssets("t:EndlessBalance"))
            {
                var balance = AssetDatabase.LoadAssetAtPath<EndlessBalance>(AssetDatabase.GUIDToAssetPath(guid));
                for (int index = 0; index < 60; index++)
                {
                    foreach (var enemy in balance.CreateStage(index * 7919, index).Enemies) met.Add(enemy);
                }
            }

            var missing = met.Where(enemy => !listed.Contains(enemy)).Select(enemy => enemy.Id).ToList();
            Assert.IsEmpty(missing, "エネミーコレクションに無い敵: " + string.Join(", ", missing));
        }

        // 並べるのはデータの順のまま、重複と空欄は除く
        [Test]
        public void Asset_HasNoDuplicates()
        {
            var data = AssetDatabase.LoadAssetAtPath<EnemyCollectionData>(DataPath);
            Assert.AreEqual(data.Enemies.Count, data.Enemies.Distinct().Count());
            Assert.IsTrue(data.Enemies.All(enemy => enemy != null));
        }
    }
}

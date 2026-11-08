using System.Collections.Generic;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Localization;

namespace Live2DCardRogue.Tests.EditMode
{
    // 敵の名前と特徴は EnemyData.Id をキーに EnemyName / EnemyDescription テーブルを引く
    // （名前は戦闘画面で立ち絵の上に、特徴はマップで次の相手として出る）。
    // 敵を足してテーブルへの登録を忘れると画面に "No translation found ..." が出るので、アセット側から突き合わせる
    public sealed class EnemyLocalizationTests
    {
        private static IEnumerable<EnemyData> AllEnemies()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:EnemyData"))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(guid));
                if (enemy != null) yield return enemy;
            }
        }

        [Test]
        public void EveryEnemyId_HasNameInEveryLocale()
        {
            AssertEveryEnemyHasEntry(LocalizationHelper.EnemyNameTable);
        }

        [Test]
        public void EveryEnemyId_HasDescriptionInEveryLocale()
        {
            AssertEveryEnemyHasEntry(LocalizationHelper.EnemyDescriptionTable);
        }

        private static void AssertEveryEnemyHasEntry(string tableName)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
            Assert.IsNotNull(collection, tableName + " テーブルが見つからない");

            foreach (var enemy in AllEnemies())
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(enemy.Id), AssetDatabase.GetAssetPath(enemy) + " の _id が空");
                Assert.IsTrue(collection.SharedData.Contains(enemy.Id), tableName + " にキーが無い: " + enemy.Id);

                foreach (var table in collection.StringTables)
                {
                    var entry = table.GetEntry(enemy.Id);
                    Assert.IsNotNull(entry, table.LocaleIdentifier.Code + " に " + enemy.Id + " のエントリが無い");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Value), table.LocaleIdentifier.Code + " の " + enemy.Id + " が空");
                }
            }
        }
    }
}

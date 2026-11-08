using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Localization;

namespace Live2DCardRogue.Tests.EditMode
{
    // パック（画面上はチップ）を足してテーブルへの登録を忘れると、購入画面に "No translation found ..." が出るまで気付けない。
    // CardPackData のアセットを全部拾って、名前と説明の両方のテーブルと突き合わせる
    public sealed class CardPackLocalizationTests
    {
        [TestCase(LocalizationHelper.CardPackNameTable)]
        [TestCase(LocalizationHelper.CardPackDescriptionTable)]
        public void EveryCardPack_HasTranslationInEveryLocale(string tableName)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
            Assert.IsNotNull(collection, tableName + " テーブルが見つからない");

            var guids = AssetDatabase.FindAssets("t:CardPackData");
            Assert.IsNotEmpty(guids);

            foreach (var guid in guids)
            {
                var pack = AssetDatabase.LoadAssetAtPath<CardPackData>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.IsTrue(collection.SharedData.Contains(pack.Id), tableName + " にキーが無い: " + pack.Id);

                foreach (var table in collection.StringTables)
                {
                    var entry = table.GetEntry(pack.Id);
                    Assert.IsNotNull(entry, tableName + "/" + table.LocaleIdentifier.Code + " に " + pack.Id + " のエントリが無い");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Value), tableName + "/" + table.LocaleIdentifier.Code + " の " + pack.Id + " が空");
                }
            }
        }
    }
}

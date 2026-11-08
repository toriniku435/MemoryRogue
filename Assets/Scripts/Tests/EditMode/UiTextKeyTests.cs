using System;
using Live2DCardRogue.Common.Localization;
using NUnit.Framework;
using UnityEditor.Localization;

namespace Live2DCardRogue.Tests.EditMode
{
    // UiTextKeyはテーブルのキーそのものなので、enumとテーブルが食い違うと
    // 画面に "No translation found ..." が出るまで誰も気付けない。そこをEditModeで潰しておく。
    // (テーブルアセットの検査はEditor APIなのでEditMode側。実際の取得はPlayModeテストで見ている)
    public sealed class UiTextKeyTests
    {
        private static StringTableCollection GetCollection()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(LocalizationHelper.UiTextTable);
            Assert.IsNotNull(collection, LocalizationHelper.UiTextTable + " テーブルが見つからない");
            return collection;
        }

        [Test]
        public void EveryKey_ExistsInSharedTableData()
        {
            var shared = GetCollection().SharedData;

            foreach (UiTextKey key in Enum.GetValues(typeof(UiTextKey)))
            {
                Assert.IsTrue(shared.Contains(key.ToString()), "テーブルにキーが無い: " + key);
            }
        }

        [Test]
        public void EveryKey_HasTranslationInEveryLocale()
        {
            var collection = GetCollection();

            foreach (var table in collection.StringTables)
            {
                foreach (UiTextKey key in Enum.GetValues(typeof(UiTextKey)))
                {
                    var entry = table.GetEntry(key.ToString());
                    Assert.IsNotNull(entry, table.LocaleIdentifier.Code + " に " + key + " のエントリが無い");
                    Assert.IsFalse(
                        string.IsNullOrWhiteSpace(entry.Value),
                        table.LocaleIdentifier.Code + " の " + key + " が空");
                }
            }
        }
    }
}

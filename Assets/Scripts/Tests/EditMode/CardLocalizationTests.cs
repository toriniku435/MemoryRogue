using System.Collections.Generic;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Localization;

namespace Live2DCardRogue.Tests.EditMode
{
    // カード名・説明文はどちらもCardData.Idをキーにテーブルを引く。
    // 「カードを増やしてテーブルへの登録を忘れる」のが一番ありがちな漏れで、放っておくと
    // 画面に "No translation found ..." が出るまで気付けないので、アセット側から突き合わせておく。
    // (テーブルアセットの検査はEditor APIなのでEditMode側。実際の取得はPlayModeテストで見ている)
    public sealed class CardLocalizationTests
    {
        private static IEnumerable<CardData> AllCards()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:CardData"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card != null)
                {
                    yield return card;
                }
            }
        }

        [Test]
        public void EveryCard_HasNonEmptyId()
        {
            foreach (var card in AllCards())
            {
                Assert.IsFalse(
                    string.IsNullOrWhiteSpace(card.Id),
                    AssetDatabase.GetAssetPath(card) + " の _id が空(テーブルのキーになるので必須)");
            }
        }

        [TestCase(LocalizationHelper.CardNameTable)]
        [TestCase(LocalizationHelper.CardDescriptionTable)]
        public void EveryCardId_HasTranslationInEveryLocale(string tableName)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
            Assert.IsNotNull(collection, tableName + " テーブルが見つからない");

            foreach (var card in AllCards())
            {
                Assert.IsTrue(
                    collection.SharedData.Contains(card.Id),
                    tableName + " にキーが無い: " + card.Id);

                foreach (var table in collection.StringTables)
                {
                    var entry = table.GetEntry(card.Id);
                    Assert.IsNotNull(entry, tableName + "/" + table.LocaleIdentifier.Code + " に " + card.Id + " のエントリが無い");
                    Assert.IsFalse(
                        string.IsNullOrWhiteSpace(entry.Value),
                        tableName + "/" + table.LocaleIdentifier.Code + " の " + card.Id + " が空");
                }
            }
        }
    }
}

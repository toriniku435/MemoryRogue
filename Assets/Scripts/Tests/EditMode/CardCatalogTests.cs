using System.Linq;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEditor;

namespace Live2DCardRogue.Tests.EditMode
{
    // セーブデータはコマンドを ID で持ち、読み込むときに CardCatalog から引き直す。
    // 一覧に載せ忘れたコマンドは、再開したときに黙って消えてしまうので、全部載っているかを確かめる
    public sealed class CardCatalogTests
    {
        [Test]
        public void EveryCardData_IsInTheCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>("Assets/ScriptableObjects/CardCatalog.asset");
            Assert.IsNotNull(catalog, "CardCatalog.asset が無い");

            foreach (var guid in AssetDatabase.FindAssets("t:CardData"))
            {
                var card = AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.AreSame(card, catalog.Find(card.Id), card.Id + " が CardCatalog に載っていない");
            }
        }

        [Test]
        public void CardIds_AreUnique()
        {
            var ids = AssetDatabase.FindAssets("t:CardData")
                .Select(g => AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(g)).Id)
                .ToList();
            CollectionAssert.AllItemsAreUnique(ids, "同じ ID のコマンドがある（セーブから引き直すときに取り違える）");
        }
    }
}

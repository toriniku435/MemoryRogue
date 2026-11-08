using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Live2DCardRogue.Window.CommandCollection.Models;

namespace Live2DCardRogue.Tests.EditMode
{
    // コレクション（コマンド）: 絞り込みと「？」の判定（モデル）、並べるコマンドの一覧（アセット）
    public sealed class CommandCollectionTests
    {
        private const string DataPath = "Assets/ScriptableObjects/CommandCollection.asset";

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static CardData NewCard(string id, CardRarity rarity = CardRarity.Normal)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(card, "_id", id);
            SetPrivateField(card, "_rarity", rarity);
            return card;
        }

        // ---------------- モデル ----------------

        // 最初は「すべて」。手に入れたことがあるかは ID で決まる
        [Test]
        public void Model_StartsWithAll_AndMarksDiscoveredById()
        {
            var a = NewCard("A");
            var b = NewCard("B");
            var model = new CommandCollectionModel(new[] { a, b }, null, new HashSet<string> { "B" });

            Assert.AreEqual(0, model.SelectedFilter);
            Assert.AreEqual(2, model.Entries.Count);
            Assert.IsFalse(model.Entries[0].IsDiscovered);
            Assert.IsTrue(model.Entries[1].IsDiscovered);
            Assert.AreEqual(1, model.DiscoveredCount);
            Assert.AreEqual(2, model.TotalCount);
        }

        // チップの絞り込みを選ぶとそのチップのコマンドだけが並び、知らせが来る。手に入れた数は「すべて」で数えたまま
        [Test]
        public void Model_SelectFilter_ShowsThatPackOnly()
        {
            var a = NewCard("A");
            var b = NewCard("B");
            var c = NewCard("C");
            var model = new CommandCollectionModel(new[] { a, b, c }, new IReadOnlyList<CardData>[] { new[] { c } }, new HashSet<string> { "A", "C" });
            int changed = 0;
            model.OnChanged += () => changed++;

            model.SelectFilter(1);

            Assert.AreEqual(1, changed);
            Assert.AreEqual(1, model.Entries.Count);
            Assert.AreSame(c, model.Entries[0].Card);
            Assert.AreEqual(2, model.DiscoveredCount);
            Assert.AreEqual(2, model.FilterCount);
        }

        // 範囲外や今と同じ絞り込みは何もしない
        [Test]
        public void Model_SelectFilter_IgnoresOutOfRangeAndSame()
        {
            var model = new CommandCollectionModel(new[] { NewCard("A") }, null, null);
            int changed = 0;
            model.OnChanged += () => changed++;

            model.SelectFilter(0);
            model.SelectFilter(5);
            model.SelectFilter(-1);

            Assert.AreEqual(0, changed);
            Assert.AreEqual(0, model.SelectedFilter);
        }

        // ---------------- 並べるコマンド ----------------

        // 「すべて」はレアリティの低い順、同じレアリティなら最初に出てきた順。重複はしない
        [Test]
        public void Data_AllCards_AreDistinctAndSortedByRarity()
        {
            var epic = NewCard("E", CardRarity.Epic);
            var normal1 = NewCard("N1");
            var rare = NewCard("R", CardRarity.Rare);
            var normal2 = NewCard("N2");
            var pack = ScriptableObject.CreateInstance<CardPackData>();
            SetPrivateField(pack, "_cards", new[] { epic, normal1, rare, normal1 });
            var data = ScriptableObject.CreateInstance<CommandCollectionData>();
            SetPrivateField(data, "_packs", new[] { pack });
            SetPrivateField(data, "_otherCards", new[] { normal2, epic });

            CollectionAssert.AreEqual(new[] { normal1, normal2, rare, epic }, data.CreateAllCards());
            CollectionAssert.AreEqual(new[] { epic, normal1, rare }, CommandCollectionData.CardsOf(pack));
        }

        // 実際のアセット: プレイヤーが手に入れられるコマンド（チップ・ステージの報酬・最初から持っている分）がすべて並ぶ
        [Test]
        public void Asset_ContainsEveryObtainableCommand()
        {
            var data = AssetDatabase.LoadAssetAtPath<CommandCollectionData>(DataPath);
            Assert.IsNotNull(data, DataPath);
            var all = new HashSet<CardData>(data.CreateAllCards());

            var missing = ObtainableCards().Where(card => !all.Contains(card)).Select(card => card.Id).Distinct().ToList();
            Assert.IsEmpty(missing, "コレクションに無いコマンド: " + string.Join(", ", missing));
        }

        // 実際のアセット: 敵だけが使うコマンドは並べない
        [Test]
        public void Asset_ContainsNoEnemyOnlyCommand()
        {
            var data = AssetDatabase.LoadAssetAtPath<CommandCollectionData>(DataPath);
            var all = new HashSet<CardData>(data.CreateAllCards());
            var obtainable = new HashSet<CardData>(ObtainableCards());

            var enemyOnly = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:EnemyData"))
            {
                foreach (var card in AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(guid)).Cards ?? new CardData[0])
                {
                    if (card != null && !obtainable.Contains(card) && all.Contains(card)) enemyOnly.Add(card.Id);
                }
            }
            Assert.IsEmpty(enemyOnly, "敵専用なのにコレクションに並ぶコマンド: " + string.Join(", ", enemyOnly));
        }

        // プレイヤーが手に入れられるコマンド: どのチップの中身、ステージの勝利報酬、開始時に持っているコマンド
        private static List<CardData> ObtainableCards()
        {
            var cards = new List<CardData>();
            foreach (var guid in AssetDatabase.FindAssets("t:CardPackData"))
            {
                cards.AddRange(AssetDatabase.LoadAssetAtPath<CardPackData>(AssetDatabase.GUIDToAssetPath(guid)).Cards);
            }
            foreach (var guid in AssetDatabase.FindAssets("t:StageData"))
            {
                cards.Add(AssetDatabase.LoadAssetAtPath<StageData>(AssetDatabase.GUIDToAssetPath(guid)).CreateReward(true).Card);
            }
            foreach (var guid in AssetDatabase.FindAssets("t:CampaignBalance"))
            {
                cards.AddRange(AssetDatabase.LoadAssetAtPath<CampaignBalance>(AssetDatabase.GUIDToAssetPath(guid)).StartOwnedCards);
            }
            return cards.Where(card => card != null).ToList();
        }
    }
}

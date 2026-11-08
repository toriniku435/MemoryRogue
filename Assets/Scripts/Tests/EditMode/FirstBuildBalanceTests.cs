using System.Collections.Generic;
using System.Linq;
using Live2DCardRogue.Common.Models.Cards;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEditor;
using Live2DCardRogue.Window.DeckEdit.Constants;

namespace Live2DCardRogue.Tests.EditMode
{
    // バランスのアセット（難易度ごとに1つ）それぞれの、ファーストビルドの設定が成り立っているかを確かめる。
    // 自動で組んだデッキがデッキ編集の規定枚数と違うと、保存できないデッキでキャンペーンが始まってしまう
    public sealed class FirstBuildBalanceTests
    {
        private static IEnumerable<CampaignBalance> AllBalances()
        {
            return AssetDatabase.FindAssets("t:CampaignBalance")
                .Select(guid => AssetDatabase.LoadAssetAtPath<CampaignBalance>(AssetDatabase.GUIDToAssetPath(guid)));
        }

        [Test]
        public void AutoDeckCounts_AddUpToTheDeckSize()
        {
            foreach (var balance in AllBalances())
            {
                Assert.AreEqual(DeckEditConstants.MaxDeckSize, balance.CreateAutoDeckRule().DeckSize,
                    balance.name + ": 自動デッキの役割ごとの枚数の合計が、デッキの規定枚数と違う");
            }
        }

        [Test]
        public void FirstBuild_OpensChipCountTimesCardCount_AndKeepsTheGuaranteed()
        {
            foreach (var balance in AllBalances())
            {
                Assert.IsNotNull(balance.FirstBuildChip, balance.name + " にファーストビルドのチップが無い");

                var build = balance.OpenFirstBuild(new System.Random(1));
                Assert.AreEqual(balance.FirstBuildChip.CardCount * balance.FirstBuildChipCount, build.Length, balance.name);
                CollectionAssert.AllItemsAreNotNull(build, balance.name);

                // 確定枠の分は、どの引きでも必ず入っている
                var guaranteed = new SerializedObject(balance).FindProperty("_firstBuildGuaranteed");
                var expected = Enumerable.Range(0, guaranteed.arraySize)
                    .Select(i => guaranteed.GetArrayElementAtIndex(i).objectReferenceValue as CardData)
                    .Where(card => card != null)
                    .GroupBy(card => card);
                foreach (var group in expected)
                {
                    Assert.GreaterOrEqual(build.Count(card => card == group.Key), group.Count(), balance.name + ": 確定枠の " + group.Key.Id + " が足りない");
                }
            }
        }

        // どんな引きでも、最初の所持からデッキの規定枚数が組める（組めないと保存できないデッキで始まる）
        [Test]
        public void FirstBuild_AlwaysYieldsAFullDeck()
        {
            foreach (var balance in AllBalances())
            {
                var random = new System.Random(7);
                for (int i = 0; i < 50; i++)
                {
                    var owned = balance.StartOwnedCards.Where(card => card != null).Concat(balance.OpenFirstBuild(random)).ToArray();
                    var deck = AutoDeckBuilder.Build(owned, balance.CreateAutoDeckRule());
                    Assert.AreEqual(DeckEditConstants.MaxDeckSize, deck.Length, balance.name);
                }
            }
        }
    }
}

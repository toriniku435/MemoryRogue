using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Live2DCardRogue.Common.Models.Cards;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class CardPackOpenerTests
    {
        private static CardData NewCard(string id, CardRarity rarity)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            SetPrivateField(card, "_id", id);
            SetPrivateField(card, "_rarity", rarity);
            card.name = id;
            return card;
        }

        private static RarityWeight NewWeight(CardRarity rarity, int weight)
        {
            var value = new RarityWeight();
            SetPrivateField(value, "_rarity", rarity);
            SetPrivateField(value, "_weight", weight);
            return value;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        [Test]
        public void Open_DrawsTheRequestedNumberOfCards()
        {
            var pool = new[] { NewCard("A", CardRarity.Normal) };
            var weights = new[] { NewWeight(CardRarity.Normal, 1) };

            var drawn = CardPackOpener.Open(pool, weights, 5, new System.Random(1));

            Assert.AreEqual(5, drawn.Length);
        }

        // 重み0のレアリティは出ない
        [Test]
        public void Open_NeverDrawsARarityWithZeroWeight()
        {
            var pool = new[] { NewCard("N", CardRarity.Normal), NewCard("E", CardRarity.Epic) };
            var weights = new[] { NewWeight(CardRarity.Normal, 1), NewWeight(CardRarity.Epic, 0) };

            var drawn = CardPackOpener.Open(pool, weights, 200, new System.Random(2));

            CollectionAssert.AreEquivalent(new[] { "N" }, drawn.Select(c => c.Id).Distinct());
        }

        // 封入されていないレアリティに重みを付けても出ない（重みだけ設定して入れ忘れた場合）
        [Test]
        public void Open_IgnoresWeightsForRaritiesThatAreNotInThePool()
        {
            var pool = new[] { NewCard("N", CardRarity.Normal) };
            var weights = new[] { NewWeight(CardRarity.Normal, 1), NewWeight(CardRarity.Epic, 99) };

            var drawn = CardPackOpener.Open(pool, weights, 50, new System.Random(3));

            Assert.AreEqual(50, drawn.Length);
            CollectionAssert.AreEquivalent(new[] { "N" }, drawn.Select(c => c.Id).Distinct());
        }

        // 重みが大きいレアリティほど多く出る
        [Test]
        public void Open_DrawsHeavierRaritiesMoreOften()
        {
            var pool = new[] { NewCard("N", CardRarity.Normal), NewCard("E", CardRarity.Epic) };
            var weights = new[] { NewWeight(CardRarity.Normal, 9), NewWeight(CardRarity.Epic, 1) };

            var drawn = CardPackOpener.Open(pool, weights, 1000, new System.Random(4));

            int normal = drawn.Count(c => c.Rarity == CardRarity.Normal);
            int epic = drawn.Count(c => c.Rarity == CardRarity.Epic);
            Assert.Greater(normal, epic * 3, $"重みの効きが弱すぎる (normal={normal}, epic={epic})");
            Assert.Greater(epic, 0, "重みがあるのに一度も出ていない");
        }

        // レアリティを先に決めるので、封入枚数の偏りは当たりやすさに影響しない
        [Test]
        public void Open_RarityOddsDoNotDependOnHowManyCardsOfThatRarityAreIncluded()
        {
            var pool = new List<CardData> { NewCard("E", CardRarity.Epic) };
            for (int i = 0; i < 20; i++) pool.Add(NewCard("N" + i, CardRarity.Normal));
            var weights = new[] { NewWeight(CardRarity.Normal, 1), NewWeight(CardRarity.Epic, 1) };

            var drawn = CardPackOpener.Open(pool, weights, 1000, new System.Random(5));

            int epic = drawn.Count(c => c.Rarity == CardRarity.Epic);
            Assert.Greater(epic, 300, $"枚数の少ないレアリティが出にくくなっている (epic={epic})");
            Assert.Less(epic, 700, $"枚数の多いレアリティが出にくくなっている (epic={epic})");
        }

        [Test]
        public void Open_WithNothingDrawable_ReturnsEmpty()
        {
            var pool = new[] { NewCard("N", CardRarity.Normal) };

            Assert.IsEmpty(CardPackOpener.Open(pool, new RarityWeight[0], 3, new System.Random(6)));
            Assert.IsEmpty(CardPackOpener.Open(new CardData[0], new[] { NewWeight(CardRarity.Normal, 1) }, 3, new System.Random(7)));
            Assert.IsEmpty(CardPackOpener.Open(pool, new[] { NewWeight(CardRarity.Normal, 1) }, 0, new System.Random(8)));
        }

        // Inspectorで空欄のまま残された要素があっても落ちない
        [Test]
        public void Open_IgnoresNullEntries()
        {
            var pool = new[] { null, NewCard("N", CardRarity.Normal), null };
            var weights = new[] { null, NewWeight(CardRarity.Normal, 1) };

            var drawn = CardPackOpener.Open(pool, weights, 4, new System.Random(9));

            Assert.AreEqual(4, drawn.Length);
            Assert.IsTrue(drawn.All(c => c != null));
        }
    }
}

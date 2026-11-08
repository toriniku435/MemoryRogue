using System.Collections.Generic;
using System.Reflection;
using Live2DCardRogue.Common.Models.EnemyCardPickers;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class RandomEnemyCardPickerTests
    {
        private static CardData NewCard(string id)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            typeof(CardData)
                .GetField("_id", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(card, id);
            return card;
        }

        private static CardData[] ThreeCards()
        {
            return new[] { NewCard("Attack"), NewCard("Block"), NewCard("StrongAttack") };
        }

        // Directorはこの戻り値をそのままforeachするので、nullを返してはいけない
        [Test]
        public void Pick_NoCandidates_ReturnsEmptyNotNull()
        {
            var picker = new RandomEnemyCardPicker();

            Assert.IsNotNull(picker.Pick(null, 1, 2, default));
            Assert.IsEmpty(picker.Pick(null, 1, 2, default));
            Assert.IsEmpty(picker.Pick(new CardData[0], 1, 2, default));
        }

        [Test]
        public void Pick_ZeroMaxActionCount_ReturnsEmpty()
        {
            Assert.IsEmpty(new RandomEnemyCardPicker().Pick(ThreeCards(), 0, 0, default));
        }

        [Test]
        public void Pick_MinEqualsMax_ReturnsExactlyThatManyCards()
        {
            var picked = new RandomEnemyCardPicker().Pick(ThreeCards(), 2, 2, default);

            Assert.AreEqual(2, picked.Count);
        }

        // 回数は[min, max]から毎回引き直されるので、範囲だけを押さえる
        [Test]
        public void Pick_CountAlwaysWithinMinMax()
        {
            var cards = ThreeCards();
            var picker = new RandomEnemyCardPicker();

            for (int i = 0; i < 50; i++)
            {
                int count = picker.Pick(cards, 1, 3, default).Count;
                Assert.GreaterOrEqual(count, 1);
                Assert.LessOrEqual(count, 3);
            }
        }

        [Test]
        public void Pick_AlwaysReturnsCardsFromTheCandidates()
        {
            var cards = new List<CardData>(ThreeCards());
            var picker = new RandomEnemyCardPicker();

            for (int i = 0; i < 50; i++)
            {
                foreach (var card in picker.Pick(cards, 1, 3, default))
                {
                    Assert.Contains(card, cards);
                }
            }
        }
    }
}

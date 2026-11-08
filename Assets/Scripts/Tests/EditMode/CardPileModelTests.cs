using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEngine;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class CardPileModelTests
    {
        private static CardData NewCard()
        {
            return ScriptableObject.CreateInstance<CardData>();
        }

        [Test]
        public void AddCard_RaisesOnCardAdded_AndIncreasesCount()
        {
            var pile = new CardPileModel(new CardData[0]);
            var card = NewCard();
            CardData raised = null;
            pile.OnCardAdded += c => raised = c;

            pile.AddCard(card);

            Assert.AreEqual(1, pile.Count);
            Assert.AreSame(card, raised);
            Assert.IsFalse(pile.IsEmpty);
        }

        [Test]
        public void RemoveCard_WhenPresent_RaisesOnCardRemoved_AndDecreasesCount()
        {
            var card = NewCard();
            var pile = new CardPileModel(new[] { card });
            CardData raised = null;
            pile.OnCardRemoved += c => raised = c;

            pile.RemoveCard(card);

            Assert.AreEqual(0, pile.Count);
            Assert.AreSame(card, raised);
            Assert.IsTrue(pile.IsEmpty);
        }

        [Test]
        public void RemoveCard_WhenNotPresent_DoesNotRaiseOnCardRemoved()
        {
            var pile = new CardPileModel(new CardData[0]);
            bool raised = false;
            pile.OnCardRemoved += _ => raised = true;

            pile.RemoveCard(NewCard());

            Assert.IsFalse(raised);
        }

        [Test]
        public void Clear_RaisesOnCleared_AndEmptiesPile()
        {
            var pile = new CardPileModel(new[] { NewCard(), NewCard() });
            bool raised = false;
            pile.OnCleared += () => raised = true;

            pile.Clear();

            Assert.IsTrue(raised);
            Assert.AreEqual(0, pile.Count);
        }

        [Test]
        public void MoveAllTo_FiresOnCardRemoved_ForEveryCard_AndFillsTargetPile()
        {
            var cardA = NewCard();
            var cardB = NewCard();
            var source = new CardPileModel(new[] { cardA, cardB });
            var target = new CardPileModel(new CardData[0]);
            int removedCount = 0;
            source.OnCardRemoved += _ => removedCount++;

            source.MoveAllTo(target);

            Assert.AreEqual(0, source.Count);
            Assert.AreEqual(2, target.Count);
            Assert.AreEqual(2, removedCount);
            Assert.Contains(cardA, (System.Collections.ICollection)target.Cards);
            Assert.Contains(cardB, (System.Collections.ICollection)target.Cards);
        }

        [Test]
        public void Shuffle_KeepsSameCards_JustReordersOrStays()
        {
            var cardA = NewCard();
            var cardB = NewCard();
            var cardC = NewCard();
            var pile = new CardPileModel(new[] { cardA, cardB, cardC });

            pile.Shuffle();

            Assert.AreEqual(3, pile.Count);
            CollectionAssert.AreEquivalent(new[] { cardA, cardB, cardC }, pile.Cards);
        }

        [Test]
        public void GetCardAt_ReturnsCardAtIndex()
        {
            var cardA = NewCard();
            var cardB = NewCard();
            var pile = new CardPileModel(new[] { cardA, cardB });

            Assert.AreSame(cardA, pile.GetCardAt(0));
            Assert.AreSame(cardB, pile.GetCardAt(1));
        }
    }
}

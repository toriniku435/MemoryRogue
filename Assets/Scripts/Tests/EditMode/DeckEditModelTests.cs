using System.Collections.Generic;
using System.Linq;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEngine;
using Live2DCardRogue.Window.DeckEdit.Models;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class DeckEditModelTests
    {
        private static CardData NewCard(string id)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = id;
            return card;
        }

        private static CardData[] Repeat(CardData card, int count)
        {
            return Enumerable.Repeat(card, count).ToArray();
        }

        private static DeckEditModel NewModel(IReadOnlyList<CardData> owned, IReadOnlyList<CardData> deck, int maxDeckSize = 20)
        {
            return new DeckEditModel(owned, deck, maxDeckSize);
        }

        [Test]
        public void AvailableCount_IsOwnedMinusWhatIsInTheDeck()
        {
            var card = NewCard("Attack");
            var model = NewModel(Repeat(card, 10), Repeat(card, 3));

            Assert.AreEqual(10, model.GetOwnedCount(card));
            Assert.AreEqual(3, model.GetDeckCount(card));
            Assert.AreEqual(7, model.GetAvailableCount(card)); // 所持リストに出る枚数
        }

        // デッキに入れた分だけ所持リストから減り、戻せば増える
        [Test]
        public void AddAndRemove_KeepTheTwoListsConsistent()
        {
            var card = NewCard("Attack");
            var model = NewModel(Repeat(card, 2), new CardData[0]);

            Assert.IsTrue(model.TryAddToDeck(card));
            Assert.AreEqual(1, model.GetDeckCount(card));
            Assert.AreEqual(1, model.GetAvailableCount(card));

            Assert.IsTrue(model.TryAddToDeck(card));
            Assert.AreEqual(2, model.GetDeckCount(card));
            Assert.AreEqual(0, model.GetAvailableCount(card));

            Assert.IsTrue(model.TryRemoveFromDeck(card));
            Assert.AreEqual(1, model.GetDeckCount(card));
            Assert.AreEqual(1, model.GetAvailableCount(card));
        }

        // 所持している分を全部入れたら、それ以上は増やせない
        [Test]
        public void CanAddToDeck_IsFalse_WhenNoCopiesAreLeft()
        {
            var card = NewCard("Attack");
            var model = NewModel(Repeat(card, 1), new CardData[0]);
            model.TryAddToDeck(card);

            Assert.IsFalse(model.CanAddToDeck(card));
            Assert.IsFalse(model.TryAddToDeck(card));
            Assert.AreEqual(1, model.GetDeckCount(card));
        }

        // 編集中は上限を超えても入れられる（判定は保存のときだけ）
        [Test]
        public void CanAddToDeck_IsTrue_EvenBeyondTheLimit()
        {
            var card = NewCard("Attack");
            var model = NewModel(Repeat(card, 10), Repeat(card, 2), maxDeckSize: 2);

            Assert.IsTrue(model.CanAddToDeck(card));
            Assert.IsTrue(model.TryAddToDeck(card));
            Assert.AreEqual(3, model.DeckCount);
        }

        // 保存できるのはちょうど上限の枚数のときだけ（足りなくても、超えていてもだめ）
        [Test]
        public void IsDeckValid_OnlyAtExactlyTheLimit()
        {
            var card = NewCard("Attack");
            var model = NewModel(Repeat(card, 10), Repeat(card, 1), maxDeckSize: 2);
            Assert.IsFalse(model.IsDeckValid);

            model.TryAddToDeck(card);
            Assert.IsTrue(model.IsDeckValid);

            model.TryAddToDeck(card);
            Assert.IsFalse(model.IsDeckValid);
        }

        [Test]
        public void CanRemoveFromDeck_IsFalse_WhenTheCardIsNotInTheDeck()
        {
            var card = NewCard("Attack");
            var model = NewModel(Repeat(card, 3), new CardData[0]);

            Assert.IsFalse(model.CanRemoveFromDeck(card));
            Assert.IsFalse(model.TryRemoveFromDeck(card));
        }

        // 所持リストに出るのは「まだ入れていない分が残っているカード」だけ
        [Test]
        public void CreateAvailableCards_SkipsCardsFullyMovedIntoTheDeck()
        {
            var attack = NewCard("Attack");
            var block = NewCard("Block");
            var owned = Repeat(attack, 1).Concat(Repeat(block, 2)).ToArray();
            var model = NewModel(owned, Repeat(attack, 1));

            CollectionAssert.AreEqual(new[] { block }, model.CreateAvailableCards());
            CollectionAssert.AreEqual(new[] { attack }, model.CreateDeckCards());
        }

        // 戦闘へ渡すときは1枚ずつ並べた形に戻す
        [Test]
        public void CreateDeck_ReturnsOneEntryPerCopy()
        {
            var attack = NewCard("Attack");
            var block = NewCard("Block");
            var model = NewModel(Repeat(attack, 5).Concat(Repeat(block, 5)).ToArray(), new CardData[0]);
            model.TryAddToDeck(attack);
            model.TryAddToDeck(attack);
            model.TryAddToDeck(block);

            var deck = model.CreateDeck();

            Assert.AreEqual(3, deck.Length);
            Assert.AreEqual(2, deck.Count(c => c == attack));
            Assert.AreEqual(1, deck.Count(c => c == block));
        }

        // 所持数より多くデッキに入っているデータを渡された場合は、所持数の方を引き上げて辻褄を合わせる
        [Test]
        public void Constructor_TreatsDeckCardsAsOwned_EvenIfTheOwnedListIsShort()
        {
            var card = NewCard("Attack");
            var model = NewModel(new CardData[0], Repeat(card, 3));

            Assert.AreEqual(3, model.GetOwnedCount(card));
            Assert.AreEqual(3, model.GetDeckCount(card));
            Assert.AreEqual(0, model.GetAvailableCount(card));
        }

        [Test]
        public void OnChanged_FiresOnlyWhenSomethingActuallyChanged()
        {
            var card = NewCard("Attack");
            var model = NewModel(Repeat(card, 1), new CardData[0]);
            int changes = 0;
            model.OnChanged += () => changes++;

            model.TryAddToDeck(card);
            Assert.AreEqual(1, changes);

            model.TryAddToDeck(card); // 所持が尽きているので失敗
            Assert.AreEqual(1, changes);
        }

        // おまかせ：所持の中から組み直したデッキでまるごと置き換える
        [Test]
        public void ReplaceDeck_ReplacesTheWholeDeck_AndNotifiesOnce()
        {
            var attack = NewCard("Attack");
            var block = NewCard("Block");
            var model = NewModel(Repeat(attack, 5).Concat(Repeat(block, 5)).ToArray(), Repeat(attack, 3));
            int changes = 0;
            model.OnChanged += () => changes++;

            model.ReplaceDeck(Repeat(block, 4));

            Assert.AreEqual(0, model.GetDeckCount(attack));
            Assert.AreEqual(4, model.GetDeckCount(block));
            Assert.AreEqual(1, changes);
        }

        [Test]
        public void ReplaceDeck_SkipsCopiesBeyondWhatIsOwned()
        {
            var attack = NewCard("Attack");
            var model = NewModel(Repeat(attack, 2), new CardData[0]);

            model.ReplaceDeck(Repeat(attack, 5));

            Assert.AreEqual(2, model.GetDeckCount(attack));
        }

        [Test]
        public void CreateOwnedCards_IncludesTheCopiesInTheDeck()
        {
            var attack = NewCard("Attack");
            var model = NewModel(Repeat(attack, 4), Repeat(attack, 3));

            Assert.AreEqual(4, model.CreateOwnedCards().Count(c => c == attack));
        }
    }
}

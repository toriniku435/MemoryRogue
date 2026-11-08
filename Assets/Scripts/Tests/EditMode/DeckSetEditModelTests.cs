using System.Collections.Generic;
using System.Linq;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEngine;
using Live2DCardRogue.Window.DeckEdit.Models;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class DeckSetEditModelTests
    {
        private const int SlotCount = 10;
        private const int MaxDeckSize = 3;

        private readonly List<CardData> _created = new List<CardData>();

        [TearDown]
        public void TearDown()
        {
            foreach (var card in _created) Object.DestroyImmediate(card);
            _created.Clear();
        }

        private CardData NewCard()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            _created.Add(card);
            return card;
        }

        private DeckSetEditModel NewModel(CardData[] owned, CardData[][] decks, int activeIndex = 0)
        {
            return new DeckSetEditModel(owned, decks, activeIndex, SlotCount, MaxDeckSize);
        }

        [Test]
        public void StartsWithTheActiveDeckSelected_AndFillsMissingSlotsWithEmptyDecks()
        {
            var a = NewCard();
            var model = NewModel(new[] { a, a, a }, new[] { new CardData[0], new[] { a, a, a } }, activeIndex: 1);

            Assert.AreEqual(SlotCount, model.SlotCount);
            Assert.AreEqual(1, model.SelectedIndex);
            Assert.AreEqual(3, model.Selected.DeckCount);
            Assert.AreEqual(0, model.GetDeck(9).DeckCount);
        }

        // デッキ同士で所持カードを取り合わない: 同じカードをどのデッキにも所持数まで入れられる
        [Test]
        public void Decks_ShareTheOwnedCards()
        {
            var a = NewCard();
            var model = NewModel(new[] { a, a }, new[] { new[] { a, a } });

            model.Select(1);

            Assert.AreEqual(2, model.Selected.GetAvailableCount(a));
            Assert.IsTrue(model.Selected.TryAddToDeck(a));
            Assert.AreEqual(2, model.GetDeck(0).DeckCount);
        }

        [Test]
        public void Select_RaisesSelectionChanged_OnlyWhenItChanges()
        {
            var model = NewModel(new CardData[0], new CardData[0][]);
            int changed = 0;
            model.OnSelectionChanged += () => changed++;

            model.Select(0);
            model.Select(4);
            model.Select(SlotCount);

            Assert.AreEqual(1, changed);
            Assert.AreEqual(4, model.SelectedIndex);
        }

        // アクティブにするのは個数を問わない。ただし規定の個数でないアクティブなデッキは保存できない
        [Test]
        public void Activate_AnyCount_ButSaveRequiresAValidActiveDeck()
        {
            var a = NewCard();
            var model = NewModel(new[] { a, a, a }, new[] { new[] { a, a, a } });
            model.Select(1);
            model.Selected.TryAddToDeck(a);

            Assert.IsTrue(model.TryActivateSelected());
            Assert.AreEqual(1, model.ActiveIndex);
            Assert.IsTrue(model.IsSelectedActive);
            Assert.IsFalse(model.IsSaveValid);

            model.Selected.TryAddToDeck(a);
            model.Selected.TryAddToDeck(a);
            Assert.IsTrue(model.IsSaveValid);
        }

        [Test]
        public void Activate_TheActiveDeckAgain_ReturnsFalse()
        {
            var model = NewModel(new CardData[0], new CardData[0][]);
            Assert.IsFalse(model.TryActivateSelected());
        }

        // アクティブでないデッキは過不足があっても保存できる。アクティブなデッキが規定の個数でなければ保存できない
        [Test]
        public void SaveValidity_OnlyLooksAtTheActiveDeck()
        {
            var a = NewCard();
            var model = NewModel(new[] { a, a, a }, new[] { new[] { a, a, a } });
            model.Select(2);
            model.Selected.TryAddToDeck(a);
            Assert.IsTrue(model.IsSaveValid);

            model.Select(0);
            model.Selected.TryRemoveFromDeck(a);
            Assert.IsFalse(model.IsSaveValid);
        }

        // 選び替えただけでは変更にならない。中身かアクティブが変わったら変更あり、元に戻せば変更なし
        [Test]
        public void IsDirty_FollowsTheContentsAndTheActiveDeck()
        {
            var a = NewCard();
            var model = NewModel(new[] { a, a, a }, new[] { new[] { a, a, a } });
            Assert.IsFalse(model.IsDirty);

            model.Select(1);
            Assert.IsFalse(model.IsDirty);

            model.Selected.TryAddToDeck(a);
            Assert.IsTrue(model.IsDirty);
            model.Selected.TryRemoveFromDeck(a);
            Assert.IsFalse(model.IsDirty);

            model.Selected.TryAddToDeck(a);
            model.Selected.TryAddToDeck(a);
            model.Selected.TryAddToDeck(a);
            model.TryActivateSelected();
            model.Selected.TryRemoveFromDeck(a);
            model.Selected.TryRemoveFromDeck(a);
            model.Selected.TryRemoveFromDeck(a);
            Assert.IsTrue(model.IsDirty, "アクティブなデッキが変わっている");
        }

        [Test]
        public void CreateDecks_ReturnsEverySlot()
        {
            var a = NewCard();
            var model = NewModel(new[] { a, a, a }, new[] { new[] { a, a, a } });
            model.Select(3);
            model.Selected.TryAddToDeck(a);

            var decks = model.CreateDecks();

            Assert.AreEqual(SlotCount, decks.Length);
            Assert.AreEqual(3, decks[0].Length);
            Assert.AreEqual(1, decks[3].Length);
            Assert.IsTrue(decks.Where((d, i) => i != 0 && i != 3).All(d => d.Length == 0));
        }
    }
}

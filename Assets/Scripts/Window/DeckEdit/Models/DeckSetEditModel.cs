using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Window.DeckEdit.Models
{
    // 複数のデッキをまとめて編集する状態。デッキごとに DeckEditModel を1つ持ち、どれも同じ所持カードから組む
    // （デッキ同士で同じカードを取り合わない。所持リストの残りはデッキごとに数える）。
    //
    // 今編集しているデッキ（Selected）と、戦闘で使うアクティブなデッキ（ActiveIndex）は別のもの。
    // アクティブにするのは個数を問わない（編集の途中で切り替えても挙動がばらつかないように）。
    // 個数を確かめるのは保存のとき: アクティブなデッキがちょうど規定の個数でなければ保存できない（IsSaveValid）。
    // アクティブでないデッキは、個数が過不足でも保存できる
    public sealed class DeckSetEditModel
    {
        private readonly List<DeckEditModel> _decks = new List<DeckEditModel>();

        /// <summary>選んでいるデッキが変わった</summary>
        public event Action OnSelectionChanged;

        /// <summary>アクティブなデッキが変わった</summary>
        public event Action OnActiveChanged;

        /// <summary>どれかのデッキの中身が変わった</summary>
        public event Action OnDeckChanged;

        /// <param name="ownedCards">所持カード（同じものは個数分並べる）</param>
        /// <param name="decks">デッキの一覧。slotCount に足りない分は空のデッキにする</param>
        /// <param name="activeIndex">アクティブなデッキの番号</param>
        public DeckSetEditModel(IReadOnlyList<CardData> ownedCards, IReadOnlyList<IReadOnlyList<CardData>> decks, int activeIndex, int slotCount, int maxDeckSize)
        {
            for (int i = 0; i < slotCount; i++)
            {
                IReadOnlyList<CardData> deck = decks != null && i < decks.Count && decks[i] != null ? decks[i] : new CardData[0];
                var model = new DeckEditModel(ownedCards, deck, maxDeckSize);
                model.OnChanged += HandleDeckChanged;
                _decks.Add(model);
            }

            ActiveIndex = activeIndex >= 0 && activeIndex < slotCount ? activeIndex : 0;
            _initialActiveIndex = ActiveIndex;
            SelectedIndex = ActiveIndex;
        }

        private readonly int _initialActiveIndex;

        /// <summary>編集を始めたときから、どれかのデッキの中身かアクティブなデッキが変わっているか（選んでいるデッキは問わない）</summary>
        public bool IsDirty
        {
            get
            {
                if (ActiveIndex != _initialActiveIndex) return true;
                foreach (var deck in _decks)
                {
                    if (deck.IsDirty) return true;
                }
                return false;
            }
        }

        public int SlotCount => _decks.Count;

        /// <summary>今編集しているデッキの番号</summary>
        public int SelectedIndex { get; private set; }

        /// <summary>アクティブな（戦闘で使う）デッキの番号</summary>
        public int ActiveIndex { get; private set; }

        /// <summary>今編集しているデッキ</summary>
        public DeckEditModel Selected => _decks[SelectedIndex];

        public DeckEditModel GetDeck(int index) => _decks[index];

        /// <summary>今編集しているデッキがアクティブか</summary>
        public bool IsSelectedActive => SelectedIndex == ActiveIndex;

        /// <summary>保存できるか（アクティブなデッキがちょうど規定の個数か）。アクティブでないデッキの個数は問わない</summary>
        public bool IsSaveValid => _decks[ActiveIndex].IsDeckValid;

        public void Select(int index)
        {
            if (index < 0 || index >= _decks.Count || index == SelectedIndex) return;
            SelectedIndex = index;
            OnSelectionChanged?.Invoke();
        }

        /// <summary>今編集しているデッキをアクティブにする（個数は問わない）。既にアクティブなら false</summary>
        public bool TryActivateSelected()
        {
            if (IsSelectedActive) return false;
            ActiveIndex = SelectedIndex;
            OnActiveChanged?.Invoke();
            return true;
        }

        /// <summary>保存する形（デッキごとに1枚ずつ並べた形）</summary>
        public CardData[][] CreateDecks()
        {
            var decks = new CardData[_decks.Count][];
            for (int i = 0; i < decks.Length; i++) decks[i] = _decks[i].CreateDeck();
            return decks;
        }

        private void HandleDeckChanged()
        {
            OnDeckChanged?.Invoke();
        }
    }
}

using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Window.DeckEdit.Models
{
    // デッキ編集中の状態。所持枚数は固定で、その内訳が「デッキに入っている分」と
    // 「まだ入れていない分」に分かれる、という持ち方をしている。
    //
    // 所持リストに出すのは「まだ入れていない分」なので、デッキへ入れた枚数だけ所持リストから減る。
    // 2つのリストの辻褄が合わなくなる余地を無くすため、引き算は表示側ではなくここで行う
    public sealed class DeckEditModel
    {
        private readonly List<CardData> _cardOrder = new List<CardData>();
        private readonly Dictionary<CardData, int> _ownedCounts = new Dictionary<CardData, int>();
        private readonly Dictionary<CardData, int> _deckCounts = new Dictionary<CardData, int>();
        // 編集を始めたときのデッキの中身（変更があるかを比べるため）
        private readonly Dictionary<CardData, int> _initialDeckCounts = new Dictionary<CardData, int>();

        /// <summary>所持リスト・デッキリストのどちらかが変わったときに発火する</summary>
        public event Action OnChanged;

        public int MaxDeckSize { get; }

        public DeckEditModel(IReadOnlyList<CardData> ownedCards, IReadOnlyList<CardData> deck, int maxDeckSize)
        {
            MaxDeckSize = maxDeckSize;

            foreach (var card in ownedCards)
            {
                if (card == null) continue;
                Register(card);
                _ownedCounts[card]++;
            }

            foreach (var card in deck)
            {
                if (card == null) continue;
                Register(card);

                // デッキにあるカードは必ず所持している扱いにする（所持数より多ければ所持数を引き上げる）
                _deckCounts[card]++;
                if (_deckCounts[card] > _ownedCounts[card]) _ownedCounts[card] = _deckCounts[card];
            }

            foreach (var pair in _deckCounts) _initialDeckCounts[pair.Key] = pair.Value;
        }

        /// <summary>編集を始めたときから、デッキの中身が変わっているか（並び順は問わない）</summary>
        public bool IsDirty
        {
            get
            {
                foreach (var pair in _deckCounts)
                {
                    if (_initialDeckCounts[pair.Key] != pair.Value) return true;
                }
                return false;
            }
        }

        /// <summary>所持しているカードを1枚ずつ並べた形（デッキに入れている分も含む）</summary>
        public CardData[] CreateOwnedCards()
        {
            var owned = new List<CardData>();
            foreach (var card in _cardOrder)
            {
                for (int i = 0; i < _ownedCounts[card]; i++) owned.Add(card);
            }
            return owned.ToArray();
        }

        /// <summary>
        /// デッキをまるごと入れ替える（おまかせ）。所持している分と投入上限を超える分は入れない
        /// </summary>
        public void ReplaceDeck(IReadOnlyList<CardData> deck)
        {
            foreach (var card in _cardOrder) _deckCounts[card] = 0;
            foreach (var card in deck)
            {
                if (card == null || !_deckCounts.ContainsKey(card)) continue;
                if (!CanAddToDeck(card)) continue;
                _deckCounts[card]++;
            }
            OnChanged?.Invoke();
        }

        /// <summary>デッキに入っている総枚数</summary>
        public int DeckCount
        {
            get
            {
                int total = 0;
                foreach (var count in _deckCounts.Values) total += count;
                return total;
            }
        }

        /// <summary>
        /// 保存できる枚数か（ちょうど上限の枚数）。編集中は上限を超えて入れてもよく、判定は保存のときだけ行う
        /// （入れ替えのために一時的に超えられないと、先に抜く順番を強いられて面倒なため）
        /// </summary>
        public bool IsDeckValid => DeckCount == MaxDeckSize;

        /// <summary>まだデッキに入れていない枚数。所持リストの表示枚数でもある</summary>
        public int GetAvailableCount(CardData card)
        {
            return GetOwnedCount(card) - GetDeckCount(card);
        }

        public int GetOwnedCount(CardData card)
        {
            return card != null && _ownedCounts.TryGetValue(card, out var count) ? count : 0;
        }

        public int GetDeckCount(CardData card)
        {
            return card != null && _deckCounts.TryGetValue(card, out var count) ? count : 0;
        }

        /// <summary>所持リストに出すカード（まだ入れていない分が1枚以上あるもの）</summary>
        public IReadOnlyList<CardData> CreateAvailableCards()
        {
            var cards = new List<CardData>();
            foreach (var card in _cardOrder)
            {
                if (GetAvailableCount(card) > 0) cards.Add(card);
            }

            return cards;
        }

        /// <summary>デッキリストに出すカード（1枚以上入っているもの）</summary>
        public IReadOnlyList<CardData> CreateDeckCards()
        {
            var cards = new List<CardData>();
            foreach (var card in _cardOrder)
            {
                if (GetDeckCount(card) > 0) cards.Add(card);
            }

            return cards;
        }

        /// <summary>デッキに戻したときに使う、1枚ずつ並べたデッキ（戦闘へはこの形で渡す）</summary>
        public CardData[] CreateDeck()
        {
            var deck = new List<CardData>(DeckCount);
            foreach (var card in _cardOrder)
            {
                for (int i = 0; i < GetDeckCount(card); i++) deck.Add(card);
            }

            return deck.ToArray();
        }

        /// <summary>
        /// 入れられるか（所持に余りがあり、そのコマンドの投入上限に達していないか）。
        /// デッキの枚数の上限は超えても入れられる（保存のときに IsDeckValid で判定する）
        /// </summary>
        public bool CanAddToDeck(CardData card)
        {
            if (GetAvailableCount(card) <= 0) return false;
            return card.MaxCopiesInDeck <= 0 || GetDeckCount(card) < card.MaxCopiesInDeck;
        }

        public bool CanRemoveFromDeck(CardData card)
        {
            return GetDeckCount(card) > 0;
        }

        public bool TryAddToDeck(CardData card)
        {
            if (!CanAddToDeck(card)) return false;

            _deckCounts[card]++;
            OnChanged?.Invoke();
            return true;
        }

        public bool TryRemoveFromDeck(CardData card)
        {
            if (!CanRemoveFromDeck(card)) return false;

            _deckCounts[card]--;
            OnChanged?.Invoke();
            return true;
        }

        // 表示順は「最初に見つかった順」で固定する。増減のたびに並びが変わらないようにするため
        private void Register(CardData card)
        {
            if (_ownedCounts.ContainsKey(card)) return;

            _cardOrder.Add(card);
            _ownedCounts[card] = 0;
            _deckCounts[card] = 0;
        }
    }
}

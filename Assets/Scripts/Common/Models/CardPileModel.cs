using Live2DCardRogue.Common.ScriptableObjects;
using System.Collections.Generic;
using System;

namespace Live2DCardRogue.Common.Models
{
    public sealed class CardPileModel
    {
        private readonly List<CardData> _cards;

        public IReadOnlyList<CardData> Cards => _cards;
        public bool IsEmpty => _cards.Count == 0;
        public int Count => _cards.Count;

        public event Action<CardData> OnCardAdded;
        public event Action<CardData> OnCardRemoved;
        public event Action OnCleared;

        public CardPileModel(CardData[] cards)
        {
            _cards = new List<CardData>(cards);
        }

        public void AddCard(CardData card)
        {
            _cards.Add(card);
            OnCardAdded?.Invoke(card);
        }

        public void MoveAllTo(CardPileModel targetPile)
        {
            while (_cards.Count > 0)
            {
                var card = _cards[0];
                RemoveCard(card); // 移動元側の OnCardRemoved を確実に発火させる
                targetPile.AddCard(card);
            }
        }

        public void RemoveCard(CardData card)
        {
            if (_cards.Remove(card))
            {
                OnCardRemoved?.Invoke(card);
            }
        }

        /// <summary>
        /// 指定の位置にあるカードを取り除く。同じカードが複数あるとき、どの1枚を抜くかを呼び出し側が決めたい場合に使う
        /// （RemoveCard は先頭の1枚を抜く）。位置がそのカードでなければ何もせず false
        /// </summary>
        public bool RemoveCardAt(int index, CardData expected)
        {
            if (index < 0 || index >= _cards.Count || _cards[index] != expected) return false;

            _cards.RemoveAt(index);
            OnCardRemoved?.Invoke(expected);
            return true;
        }

        public void Clear()
        {
            _cards.Clear();
            OnCleared?.Invoke();
        }

        public void Shuffle()
        {
            var rng = new System.Random();
            int n = _cards.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var value = _cards[k];
                _cards[k] = _cards[n];
                _cards[n] = value;
            }
        }

        public CardData GetCardAt(int index)
        {
            return _cards[index];
        }
    }
}

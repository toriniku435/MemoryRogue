using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Models.EnemyCardPickers
{
    // 残りHPがしきい値以上か未満かで、カードごとの出やすさ（重み）を切り替える。ボス用。
    // 生成カード（_summonCard）は重みでは選ばず、味方がいないときだけ、1ターン目か、
    // 独りのままターンを _summonAfterAloneTurns 回終えたあとに、そのターンの最初の行動として使う。
    // 1ターン分の行動の数は RandomEnemyCardPicker と同じく [min, max] から決める
    [Serializable]
    public sealed class HpPhaseEnemyCardPicker : IEnemyCardPicker
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private CardData _card;
            [Tooltip("残りHPがしきい値以上のときの重み")]
            [SerializeField][Min(0)] private int _weightHigh;
            [Tooltip("残りHPがしきい値未満のときの重み")]
            [SerializeField][Min(0)] private int _weightLow;

            public CardData Card => _card;
            public int WeightOf(bool isLow) => isLow ? _weightLow : _weightHigh;
        }

        [SerializeField][Range(1, 100)] private int _thresholdPercent = 50;
        [SerializeField] private Entry[] _entries;
        [SerializeField] private CardData _summonCard;
        [SerializeField][Min(1)] private int _summonAfterAloneTurns = 3;

        // [SerializeField]が無いのでシリアライズされない。デシリアライズ直後はnullなので遅延生成する
        private System.Random _random;

        public IReadOnlyList<CardData> Pick(IReadOnlyList<CardData> cards, int minActionCount, int maxActionCount, EnemyPickContext context)
        {
            if (cards == null || cards.Count == 0 || maxActionCount <= 0)
            {
                return Array.Empty<CardData>();
            }

            _random ??= new System.Random();

            int min = Math.Max(0, minActionCount);
            int actionCount = min >= maxActionCount ? maxActionCount : _random.Next(min, maxActionCount + 1);
            if (actionCount <= 0)
            {
                return Array.Empty<CardData>();
            }

            var picked = new List<CardData>(actionCount);
            if (ShouldSummon(cards, context)) picked.Add(_summonCard);

            bool isLow = context.HpPercent < _thresholdPercent;
            var weighted = CollectWeighted(cards, isLow, out int total);
            while (picked.Count < actionCount)
            {
                var card = total > 0 ? Draw(weighted, total) : PickFallback(cards);
                if (card == null) break;
                picked.Add(card);
            }
            return picked;
        }

        private bool ShouldSummon(IReadOnlyList<CardData> cards, EnemyPickContext context)
        {
            if (_summonCard == null || !Contains(cards, _summonCard)) return false;
            if (context.LivingAllyCount > 0) return false;
            return context.TurnNumber == 1 || context.TurnsAlone >= _summonAfterAloneTurns;
        }

        // 使えるカード（cards にあるもの）のうち、今の段階で重みが 1 以上のもの。生成カードは重みでは選ばない
        private List<(CardData Card, int Weight)> CollectWeighted(IReadOnlyList<CardData> cards, bool isLow, out int total)
        {
            var weighted = new List<(CardData, int)>();
            total = 0;
            if (_entries == null) return weighted;

            foreach (var entry in _entries)
            {
                if (entry == null || entry.Card == null || entry.Card == _summonCard || !Contains(cards, entry.Card)) continue;
                int weight = entry.WeightOf(isLow);
                if (weight <= 0) continue;
                weighted.Add((entry.Card, weight));
                total += weight;
            }
            return weighted;
        }

        private CardData Draw(List<(CardData Card, int Weight)> weighted, int total)
        {
            int roll = _random.Next(total);
            foreach (var (card, weight) in weighted)
            {
                if (roll < weight) return card;
                roll -= weight;
            }
            return weighted[weighted.Count - 1].Card;
        }

        // 重みが1つも付いていないときは、生成カード以外から均等に選ぶ（設定し忘れでも行動はする）
        private CardData PickFallback(IReadOnlyList<CardData> cards)
        {
            var candidates = new List<CardData>();
            foreach (var card in cards)
            {
                if (card != null && card != _summonCard) candidates.Add(card);
            }
            return candidates.Count == 0 ? null : candidates[_random.Next(candidates.Count)];
        }

        private static bool Contains(IReadOnlyList<CardData> cards, CardData card)
        {
            foreach (var c in cards)
            {
                if (c == card) return true;
            }
            return false;
        }
    }
}

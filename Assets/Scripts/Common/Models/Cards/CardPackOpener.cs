using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Models.Cards
{
    // パックを開けて出るカードを決める処理。
    // 抽選は「レアリティを重みで選ぶ」→「そのレアリティの封入カードから等確率で1枚」の2段階。
    // レアリティごとの枚数の偏りが当たりやすさに影響しないよう、先にレアリティを決めている。
    //
    // アセット(CardPackData)から切り出してあるのは、乱数を外から渡せるようにして
    // 重みの効き方をテストできるようにするため
    public static class CardPackOpener
    {
        /// <param name="pool">封入カード。null要素は無視する</param>
        /// <param name="weights">レアリティごとの重み。封入されていないレアリティの重みは無視する</param>
        /// <param name="count">引く枚数</param>
        public static CardData[] Open(
            IReadOnlyList<CardData> pool,
            IReadOnlyList<RarityWeight> weights,
            int count,
            Random random)
        {
            var byRarity = GroupByRarity(pool);
            var candidates = CreateCandidates(byRarity, weights);

            int totalWeight = 0;
            foreach (var candidate in candidates) totalWeight += candidate.Weight;

            // 引けるレアリティが1つも無ければ空。呼び出し側はそのままforeachできる
            if (totalWeight <= 0 || count <= 0) return new CardData[0];

            var drawn = new List<CardData>(count);
            for (int i = 0; i < count; i++)
            {
                var cards = byRarity[PickRarity(candidates, totalWeight, random)];
                drawn.Add(cards[random.Next(cards.Count)]);
            }

            return drawn.ToArray();
        }

        private static Dictionary<CardRarity, List<CardData>> GroupByRarity(IReadOnlyList<CardData> pool)
        {
            var byRarity = new Dictionary<CardRarity, List<CardData>>();
            if (pool == null) return byRarity;

            foreach (var card in pool)
            {
                if (card == null) continue;

                if (!byRarity.TryGetValue(card.Rarity, out var cards))
                {
                    cards = new List<CardData>();
                    byRarity[card.Rarity] = cards;
                }

                cards.Add(card);
            }

            return byRarity;
        }

        // 重みが正で、かつ実際にカードが封入されているレアリティだけを候補にする
        private static List<RarityWeight> CreateCandidates(
            Dictionary<CardRarity, List<CardData>> byRarity,
            IReadOnlyList<RarityWeight> weights)
        {
            var candidates = new List<RarityWeight>();
            if (weights == null) return candidates;

            foreach (var weight in weights)
            {
                if (weight == null || weight.Weight <= 0) continue;
                if (!byRarity.ContainsKey(weight.Rarity)) continue;

                candidates.Add(weight);
            }

            return candidates;
        }

        private static CardRarity PickRarity(List<RarityWeight> candidates, int totalWeight, Random random)
        {
            int roll = random.Next(totalWeight);
            foreach (var candidate in candidates)
            {
                roll -= candidate.Weight;
                if (roll < 0) return candidate.Rarity;
            }

            return candidates[candidates.Count - 1].Rarity;
        }
    }
}

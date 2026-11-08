using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Models.EnemyCardPickers
{
    // 行動回数を[min, max]から一様ランダムに決め、その回数だけ候補から一様ランダムに選ぶ
    // (敵のカードは消費されないので重複を許す)。
    [Serializable]
    public sealed class RandomEnemyCardPicker : IEnemyCardPicker
    {
        // [SerializeField]が無いのでシリアライズされない。デシリアライズ直後はnullなので遅延生成する
        private Random _random;

        public IReadOnlyList<CardData> Pick(IReadOnlyList<CardData> cards, int minActionCount, int maxActionCount, EnemyPickContext context)
        {
            if (cards == null || cards.Count == 0 || maxActionCount <= 0)
            {
                return Array.Empty<CardData>();
            }

            _random ??= new Random();

            int min = Math.Max(0, minActionCount);
            int actionCount = min >= maxActionCount ? maxActionCount : _random.Next(min, maxActionCount + 1);
            if (actionCount <= 0)
            {
                return Array.Empty<CardData>();
            }

            var picked = new CardData[actionCount];
            for (int i = 0; i < actionCount; i++)
            {
                picked[i] = cards[_random.Next(cards.Count)];
            }

            return picked;
        }
    }
}

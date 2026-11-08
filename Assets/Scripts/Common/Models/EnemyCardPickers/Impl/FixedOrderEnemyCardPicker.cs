using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Models.EnemyCardPickers
{
    /// <summary>
    /// 毎ターン、持っているカードを並べた順にそのまま使う（最大行動回数まで）。
    /// 運に左右されないので、チュートリアルの練習相手のように「何が起きるか」を決めておきたい敵に使う
    /// </summary>
    [Serializable]
    public sealed class FixedOrderEnemyCardPicker : IEnemyCardPicker
    {
        public IReadOnlyList<CardData> Pick(IReadOnlyList<CardData> cards, int minActionCount, int maxActionCount, EnemyPickContext context)
        {
            var picked = new List<CardData>();
            if (cards == null) return picked;
            for (int i = 0; i < cards.Count && picked.Count < maxActionCount; i++)
            {
                picked.Add(cards[i]);
            }
            return picked;
        }
    }
}

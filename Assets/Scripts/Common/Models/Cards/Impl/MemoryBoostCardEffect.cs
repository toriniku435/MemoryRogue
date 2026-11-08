using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 次の自分のターンから _turns ターンの間、メモリーの最大値を _amount 増やす
    [Serializable]
    public sealed class MemoryBoostCardEffect : SelfStatusCardEffectBase
    {
        [SerializeField][Min(1)] private int _turns = 1;
        [SerializeField][Min(0)] private int _amount;

        protected override IStatusEffect CreateStatusEffect() => new MemoryBoostStatusEffect(_turns, _amount);

        // 説明文の {memoryTurns}, {memoryAmount}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["memoryTurns"] = _turns;
            values["memoryAmount"] = _amount;
        }
    }
}

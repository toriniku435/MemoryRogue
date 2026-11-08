using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 次の自分のターンから _turns ターンの間、引く手札を _amount 枚増やす
    [Serializable]
    public sealed class HandSizeUpCardEffect : SelfStatusCardEffectBase
    {
        [SerializeField][Min(1)] private int _turns = 1;
        [SerializeField][Min(0)] private int _amount;

        protected override IStatusEffect CreateStatusEffect() => new HandSizeUpStatusEffect(_turns, _amount);

        // 説明文の {handUpTurns}, {handUpAmount}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["handUpTurns"] = _turns;
            values["handUpAmount"] = _amount;
        }
    }
}

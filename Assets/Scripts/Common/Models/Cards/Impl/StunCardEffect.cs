using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 相手の次のターンから _turns ターンの間、行動させない
    [Serializable]
    public sealed class StunCardEffect : OpponentStatusCardEffectBase
    {
        [SerializeField][Min(1)] private int _turns = 1;

        protected override IStatusEffect CreateStatusEffect() => new StunStatusEffect(_turns);

        // 説明文の {stunTurns}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["stunTurns"] = _turns;
        }
    }
}

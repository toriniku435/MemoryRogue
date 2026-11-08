using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 相手の次のターンから _turns ターンの間、相手が受ける回復をダメージに反転する
    [Serializable]
    public sealed class HealReversalCardEffect : OpponentStatusCardEffectBase
    {
        [SerializeField][Min(1)] private int _turns = 1;

        protected override IStatusEffect CreateStatusEffect() => new HealReversalStatusEffect(_turns);

        // 説明文の {reversalTurns}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["reversalTurns"] = _turns;
        }
    }
}

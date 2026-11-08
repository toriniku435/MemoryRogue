using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 相手の次のターンから _turns ターンの間、相手が行動するたびに _percent % の確率でグリッチ（自滅の行動）を割り込ませる
    [Serializable]
    public sealed class GlitchInjectCardEffect : OpponentStatusCardEffectBase
    {
        [SerializeField][Min(1)] private int _turns = 2;
        [SerializeField][Range(0, 100)] private int _percent = 50;

        protected override IStatusEffect CreateStatusEffect() => new GlitchStatusEffect(_turns, _percent);

        // 説明文の {glitchTurns}, {glitchPercent}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["glitchTurns"] = _turns;
            values["glitchPercent"] = _percent;
        }
    }
}

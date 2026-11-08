using System;
using System.Collections.Generic;
using UnityEngine;

namespace Live2DCardRogue.Common.Models.Cards
{
    [Serializable]
    public sealed class BasicStrengthCardEffect : StrengthCardEffectBase
    {
        [SerializeField][Min(0)] private int _strengthAmount;

        protected override int CalculateStrengthAmount(UnitStatusModel user, UnitStatusModel target)
        {
            return _strengthAmount;
        }

        // 説明文の {strength}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["strength"] = _strengthAmount;
        }
    }
}

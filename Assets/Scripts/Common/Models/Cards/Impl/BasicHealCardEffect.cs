using System;
using System.Collections.Generic;
using UnityEngine;

namespace Live2DCardRogue.Common.Models.Cards
{
    [Serializable]
    public sealed class BasicHealCardEffect : HealCardEffectBase
    {
        [SerializeField][Min(0)] private int _healAmount;

        protected override int CalculateHealAmount(UnitStatusModel user, UnitStatusModel target)
        {
            return _healAmount;
        }

        // 説明文の {heal}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["heal"] = _healAmount;
        }
    }
}

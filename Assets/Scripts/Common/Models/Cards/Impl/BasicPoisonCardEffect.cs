using System;
using System.Collections.Generic;
using UnityEngine;

namespace Live2DCardRogue.Common.Models.Cards
{
    [Serializable]
    public sealed class BasicPoisonCardEffect : PoisonCardEffectBase
    {
        [SerializeField][Min(0)] private int _poisonAmount;

        protected override int CalculatePoisonAmount(UnitStatusModel user, UnitStatusModel target)
        {
            return _poisonAmount;
        }

        // 説明文の {poison}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["poison"] = _poisonAmount;
        }
    }
}

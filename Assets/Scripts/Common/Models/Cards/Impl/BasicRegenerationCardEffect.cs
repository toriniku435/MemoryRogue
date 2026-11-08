using System;
using System.Collections.Generic;
using UnityEngine;

namespace Live2DCardRogue.Common.Models.Cards
{
    [Serializable]
    public sealed class BasicRegenerationCardEffect : RegenerationCardEffectBase
    {
        [SerializeField][Min(0)] private int _regenerationAmount;

        protected override int CalculateRegenerationAmount(UnitStatusModel user, UnitStatusModel target)
        {
            return _regenerationAmount;
        }

        // 説明文の {regeneration}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["regeneration"] = _regenerationAmount;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Live2DCardRogue.Common.Models.Cards
{
    [Serializable]
    public sealed class BasicAttackCardEffect : AttackCardEffectBase
    {
        [SerializeField][Min(0)] private int _damage;

        protected override int CalculateDamage(UnitStatusModel user, UnitStatusModel target)
        {
            return _damage;
        }

        // 説明文の {damage}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["damage"] = _damage;
        }
    }
}

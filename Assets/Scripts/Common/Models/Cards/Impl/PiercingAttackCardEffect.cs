using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // ブロックを貫通する攻撃
    [Serializable]
    public sealed class PiercingAttackCardEffect : AttackCardEffectBase
    {
        [SerializeField][Min(0)] private int _damage;

        protected override int CalculateDamage(UnitStatusModel user, UnitStatusModel target)
        {
            return _damage;
        }

        protected override HpChangeType DamageType => HpChangeType.TakePiercingDamage;

        // 説明文の {damage}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["damage"] = _damage;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 使用者の今のHPの _percent % のダメージ（端数は切り捨て）
    [Serializable]
    public sealed class HpRatioAttackCardEffect : AttackCardEffectBase
    {
        [SerializeField][Min(0)] private int _percent;

        protected override int CalculateDamage(UnitStatusModel user, UnitStatusModel target)
        {
            return user.CurrentHp * _percent / 100;
        }

        // 説明文の {damagePercent}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["damagePercent"] = _percent;
        }
    }
}

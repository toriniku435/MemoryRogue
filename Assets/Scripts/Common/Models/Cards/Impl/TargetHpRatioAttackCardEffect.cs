using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 対象の今のHPの _percent % のダメージ（端数切り捨て、HPが残っていれば最低1）。
    // 使用者のHPで決まる HpRatioAttackCardEffect と違い、相手が元気なほど大きく削る（敵専用: リソースハイジャック）
    [Serializable]
    public sealed class TargetHpRatioAttackCardEffect : AttackCardEffectBase
    {
        [SerializeField][Range(0, 100)] private int _percent = 20;

        protected override int CalculateDamage(UnitStatusModel user, UnitStatusModel target)
        {
            if (target.CurrentHp <= 0 || _percent <= 0) return 0;
            return Math.Max(1, (int)((long)target.CurrentHp * _percent / 100));
        }

        // 説明文の {targetHpPercent}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["targetHpPercent"] = _percent;
        }
    }
}

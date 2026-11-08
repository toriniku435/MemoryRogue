using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 相手全員への攻撃。1体ずつ別々に当たるので、強化は当たる相手ごとに乗り、ブロックもそれぞれが受け止める
    [Serializable]
    public sealed class AreaAttackCardEffect : AttackCardEffectBase
    {
        [SerializeField][Min(0)] private int _damage;

        public override bool TargetsAllOpponents => true;

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

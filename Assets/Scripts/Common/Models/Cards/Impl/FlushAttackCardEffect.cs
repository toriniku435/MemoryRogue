using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // _damage のダメージ。実行する時点でバッファ（手札）が空なら、さらに _bonusDamage を足す（フラッシュ）
    [Serializable]
    public sealed class FlushAttackCardEffect : AttackCardEffectBase, ICommandContextCardEffect
    {
        [SerializeField][Min(0)] private int _damage = 8;
        [SerializeField][Min(0)] private int _bonusDamage = 16;

        // 事情の分からない場面（敵が使う）では、追加は無し
        protected override int CalculateDamage(UnitStatusModel user, UnitStatusModel target) => _damage;

        public void ExecuteWithContext(UnitStatusModel user, UnitStatusModel target, CardExecutionContext context)
        {
            DealDamage(user, target, _damage + (context.BufferCount == 0 ? _bonusDamage : 0));
        }

        // 説明文の {flushDamage}, {flushBonus}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["flushDamage"] = _damage;
            values["flushBonus"] = _bonusDamage;
        }
    }
}

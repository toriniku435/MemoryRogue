using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 実行する時点でバッファ（手札）に残っているコマンドの数 × _damagePerBuffer のダメージ（パイプライン）。
    // 事情の分からない場面（敵が使う）では、ダメージは無い
    [Serializable]
    public sealed class BufferCountAttackCardEffect : AttackCardEffectBase, ICommandContextCardEffect
    {
        [SerializeField][Min(0)] private int _damagePerBuffer = 2;

        protected override int CalculateDamage(UnitStatusModel user, UnitStatusModel target) => 0;

        public void ExecuteWithContext(UnitStatusModel user, UnitStatusModel target, CardExecutionContext context)
        {
            DealDamage(user, target, _damagePerBuffer * context.BufferCount);
        }

        // 説明文の {damagePerBuffer}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["damagePerBuffer"] = _damagePerBuffer;
        }
    }
}

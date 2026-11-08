using System;
using System.Collections.Generic;
using UnityEngine;

namespace Live2DCardRogue.Common.Models.Cards
{
    // _damage のダメージに、この戦闘で倒した敵の数 × _damagePerDefeat を足す（フォレンジック）。
    // 生成で何度も出てくる敵（ボスのボットネット）は、倒すたびに数える。事情の分からない場面（敵が使う）では追加は無し
    [Serializable]
    public sealed class DefeatCountAttackCardEffect : AttackCardEffectBase, ICommandContextCardEffect
    {
        [SerializeField][Min(0)] private int _damage = 6;
        [SerializeField][Min(0)] private int _damagePerDefeat = 10;

        protected override int CalculateDamage(UnitStatusModel user, UnitStatusModel target) => _damage;

        public void ExecuteWithContext(UnitStatusModel user, UnitStatusModel target, CardExecutionContext context)
        {
            DealDamage(user, target, _damage + _damagePerDefeat * context.DefeatedEnemyCount);
        }

        // 説明文の {forensicsDamage}, {forensicsPerDefeat}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["forensicsDamage"] = _damage;
            values["forensicsPerDefeat"] = _damagePerDefeat;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 使用者の今のブロックと同じだけのダメージ（リフレクション）。撃ったあと、ブロックは半分（切り捨て）になる。
    // 2026-11-07 まではブロックを減らさなかったが、ブロックが持ち越されるため溜めて撃ち続けるだけでボスにも勝ててしまい、
    // ほかの方針より突出していたので、撃つたびに溜めを使う形にした
    [Serializable]
    public sealed class BlockStrikeCardEffect : AttackCardEffectBase
    {
        protected override int CalculateDamage(UnitStatusModel user, UnitStatusModel target) => user.BlockValue;

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
            int block = user.BlockValue;
            DealDamage(user, target, block);
            user.SpendBlock(block - block / 2);
        }

        // 数値は無い（ダメージは戦闘中のブロックで決まる）
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
        }
    }
}

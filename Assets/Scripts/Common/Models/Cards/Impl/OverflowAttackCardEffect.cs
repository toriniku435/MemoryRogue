using System;
using System.Collections.Generic;
using UnityEngine;

namespace Live2DCardRogue.Common.Models.Cards
{
    // バッファ（手札）を _purgeCount 個までパージし、パージした数 × _damagePerPurge のダメージを与える（バッファオーバーフロー）。
    // どれをパージするかは積むときにプレイヤーが選ぶ（IHandPurgeCardEffect）
    [Serializable]
    public sealed class OverflowAttackCardEffect : AttackCardEffectBase, IHandPurgeCardEffect
    {
        [SerializeField][Min(1)] private int _purgeCount = 2;
        [SerializeField][Min(0)] private int _damagePerPurge = 25;

        public int HandPurgeCount => _purgeCount;

        // バッファの事情を知らずに実行されたとき（敵が使うなど）は、上限までパージしたものとして撃つ
        protected override int CalculateDamage(UnitStatusModel user, UnitStatusModel target) => _damagePerPurge * _purgeCount;

        public void ExecuteWithPurged(UnitStatusModel user, UnitStatusModel target, int purgedCount)
        {
            int purged = Math.Max(0, Math.Min(purgedCount, _purgeCount));
            DealDamage(user, target, _damagePerPurge * purged);
        }

        // 説明文の {handPurge}, {damagePerPurge}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["handPurge"] = _purgeCount;
            values["damagePerPurge"] = _damagePerPurge;
        }
    }
}

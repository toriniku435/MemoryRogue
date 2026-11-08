using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // バッファ（手札）を _purgeCount 個までパージし、パージした数 × _healPerPurge だけHPを回復する（デフラグ）。
    // どれをパージするかは積むときにプレイヤーが選ぶ（IHandPurgeCardEffect）
    [Serializable]
    public sealed class PurgeHealCardEffect : HealCardEffectBase, IHandPurgeCardEffect
    {
        [SerializeField][Min(1)] private int _purgeCount = 2;
        [SerializeField][Min(0)] private int _healPerPurge = 15;

        public int HandPurgeCount => _purgeCount;

        // バッファの事情を知らずに実行されたとき（敵が使うなど）は、上限までパージしたものとして回復する
        protected override int CalculateHealAmount(UnitStatusModel user, UnitStatusModel target) => _healPerPurge * _purgeCount;

        public void ExecuteWithPurged(UnitStatusModel user, UnitStatusModel target, int purgedCount)
        {
            int purged = Math.Max(0, Math.Min(purgedCount, _purgeCount));
            Heal(user, _healPerPurge * purged);
        }

        // 説明文の {healPurge}, {healPerPurge}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["healPurge"] = _purgeCount;
            values["healPerPurge"] = _healPerPurge;
        }
    }
}

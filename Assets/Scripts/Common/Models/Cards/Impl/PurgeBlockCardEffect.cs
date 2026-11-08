using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // バッファ（手札）を _purgeCount 個までパージし、パージした数 × _blockPerPurge のブロックを得る（スワップアウト）。
    // どれをパージするかは積むときにプレイヤーが選ぶ（IHandPurgeCardEffect）
    [Serializable]
    public sealed class PurgeBlockCardEffect : BlockCardEffectBase, IHandPurgeCardEffect
    {
        [SerializeField][Min(1)] private int _purgeCount = 2;
        [SerializeField][Min(0)] private int _blockPerPurge = 15;

        public int HandPurgeCount => _purgeCount;

        // バッファの事情を知らずに実行されたとき（敵が使うなど）は、上限までパージしたものとして得る
        protected override int CalculateBlockAmount(UnitStatusModel user, UnitStatusModel target) => _blockPerPurge * _purgeCount;

        public void ExecuteWithPurged(UnitStatusModel user, UnitStatusModel target, int purgedCount)
        {
            int purged = Math.Max(0, Math.Min(purgedCount, _purgeCount));
            GainBlock(user, _blockPerPurge * purged);
        }

        // 説明文の {blockPurge}, {blockPerPurge}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["blockPurge"] = _purgeCount;
            values["blockPerPurge"] = _blockPerPurge;
        }
    }
}

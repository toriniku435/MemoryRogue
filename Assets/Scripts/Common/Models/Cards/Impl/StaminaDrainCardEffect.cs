using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 相手のメモリー（スタミナ）を _amount 減らす（敵のメモリーリーク）。
    // 敵のターンはプレイヤーのメモリーが回復した後に来るので、次のプレイヤーのターンに使えるメモリーが減る
    [Serializable]
    public sealed class StaminaDrainCardEffect : CardEffectBase
    {
        [SerializeField][Min(0)] private int _amount = 2;

        public override bool TargetsOpponent => true;

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
            if (_amount <= 0) return;
            target.ChangeStamina(StaminaChangeType.Consume, -_amount);
        }

        // 説明文の {memoryDrain}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["memoryDrain"] = _amount;
        }
    }
}

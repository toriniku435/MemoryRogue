using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 相手の次のターンから _turns ターンの間、相手が引く手札を _amount 枚減らす（妨害。敵のキーロガーなど）。
    // 自分に付ける代償の HandSizeDownCardEffect と同じ状態効果を、相手に付ける
    [Serializable]
    public sealed class HandJamCardEffect : OpponentStatusCardEffectBase
    {
        [SerializeField][Min(1)] private int _turns = 1;
        [SerializeField][Min(1)] private int _amount = 1;

        protected override IStatusEffect CreateStatusEffect() => new HandSizeDownStatusEffect(_turns, _amount);

        // 説明文の {jamTurns}, {jamAmount}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["jamTurns"] = _turns;
            values["jamAmount"] = _amount;
        }
    }
}

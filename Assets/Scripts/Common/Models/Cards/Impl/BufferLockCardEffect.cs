using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 次の自分のターンから _turns ターンの間、バッファ（手札）を1枚も引けなくする（バッファオーバーフローの代償）。
    // 続けて使うと足し合わさり、その次のターンもバッファが空になる
    [Serializable]
    public sealed class BufferLockCardEffect : SelfStatusCardEffectBase
    {
        [SerializeField][Min(1)] private int _turns = 1;

        protected override IStatusEffect CreateStatusEffect() => new BufferLockStatusEffect(_turns);

        // 説明文の {lockTurns}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["lockTurns"] = _turns;
        }
    }
}

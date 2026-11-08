using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 次に実行するコマンドを _repeat 回実行する。代わりにそのコマンドはパージされる（マルチスレッド）
    [Serializable]
    public sealed class MultithreadCardEffect : SelfStatusCardEffectBase
    {
        [SerializeField][Min(2)] private int _repeat = 3;

        protected override IStatusEffect CreateStatusEffect() => new MultithreadStatusEffect(_repeat);

        // 説明文の {repeat}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["repeat"] = _repeat;
        }
    }
}

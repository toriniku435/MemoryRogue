using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 勝利したときに受け取るトークンを _percent % 増やす（マイニング）。
    // 使用者に TokenBonusStatusEffect を付けるだけで、実際に増やすのは戦闘の結果を受け取る側（マップ）
    [Serializable]
    public sealed class TokenBonusCardEffect : CardEffectBase
    {
        [SerializeField][Min(1)] private int _percent = 50;

        public override bool TargetsOpponent => false;

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
            user.AddStatusEffect(new TokenBonusStatusEffect(_percent));
        }

        // 説明文の {tokenBonus}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["tokenBonus"] = _percent;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 敵味方すべてのユニットの回復を _turns ターンの間ダメージに反転する（カーネルパニック）。
    // 相手側は全体攻撃と同じく実行する側が1体ずつ呼び、使用者自身にも付ける（呼ばれるたびに付くが、重ねがけは長い方を残すだけ）
    [Serializable]
    public sealed class HealReversalAllCardEffect : OpponentStatusCardEffectBase
    {
        [SerializeField][Min(1)] private int _turns = 1;

        public override bool TargetsAllOpponents => true;

        protected override IStatusEffect CreateStatusEffect() => new HealReversalStatusEffect(_turns);

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
            base.OnExecute(user, target);
            user.AddStatusEffect(CreateStatusEffect());
        }

        // 説明文の {panicTurns}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["panicTurns"] = _turns;
        }
    }
}

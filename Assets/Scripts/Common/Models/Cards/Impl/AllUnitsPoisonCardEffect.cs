using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 敵味方すべてのユニットにウイルスを感染させる（ゼロデイ）。使用者自身にも付くので、強い代わりに自分も削られる。
    // 対象は選ばない（自分を対象にしたことにする）。プレイヤーが使うと、実行する側から生きているユニットすべてを受け取って1回ずつ付ける。
    // 事情の分からない場面（敵が使う）では、対象と使用者に付ける
    [Serializable]
    public sealed class AllUnitsPoisonCardEffect : PoisonCardEffectBase, ICommandContextCardEffect
    {
        [SerializeField][Min(0)] private int _poison = 8;

        // 誰かを選んで付けるのではないので、対象選びは自分側（選ぶ必要が無い）
        public override bool TargetsOpponent => false;

        protected override int CalculatePoisonAmount(UnitStatusModel user, UnitStatusModel target) => _poison;

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
            base.OnExecute(user, target);
            if (target != user) base.OnExecute(user, user);
        }

        public void ExecuteWithContext(UnitStatusModel user, UnitStatusModel target, CardExecutionContext context)
        {
            foreach (var unit in context.AllUnits)
            {
                base.OnExecute(user, unit);
            }
        }

        // 説明文の {allPoison}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["allPoison"] = _poison;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Live2DCardRogue.Common.Models.Cards
{
    [Serializable]
    public abstract class CardEffectBase : ICardEffect
    {
        // 「どちらのユニットに効くか」はカテゴリ基底ごとに決まりきっているので、
        // 新しいカテゴリを足したときに書き忘れられないよう virtual ではなく abstract にしている
        public abstract bool TargetsOpponent { get; }

        // 全体に作用するのは例外なので、既定は選んだ1体だけ
        public virtual bool TargetsAllOpponents => false;

        // 味方を選んで作用するのも例外（敵の味方回復）。既定は選ばない
        public virtual bool ChoosesAlly => false;

        public void Execute(UnitStatusModel user, UnitStatusModel target)
        {
            OnExecute(user, target);
        }

        protected abstract void OnExecute(UnitStatusModel user, UnitStatusModel target);

        // 数値を持たない効果は無いので、書き忘れられないよう abstract にしている（TargetsOpponent と同じ考え方）
        public abstract void AddDescriptionValues(IDictionary<string, object> values);
    }
}
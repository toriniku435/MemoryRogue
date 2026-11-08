using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 味方1体（使用者自身を含む）の HP を回復する。使用者ではなく target を回復するのが BasicHealCardEffect との違い。
    // 敵が使うときは、行動する側（BattlePhaseDirector）が一番弱っている味方を target に選ぶ（ChoosesAlly）。
    // プレイヤーには味方がいないので、使えば自分を選ぶことになる
    [Serializable]
    public sealed class AllyHealCardEffect : CardEffectBase
    {
        [SerializeField][Min(0)] private int _healAmount;

        public override bool TargetsOpponent => false;

        public override bool ChoosesAlly => true;

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
            if (_healAmount <= 0 || target == null) return;

            // 回復量を増やす効果は使用者側のもの（与える値）、回復の反転は受ける側のもの（ChangeHp）
            int heal = user.CalculateDealtValue(DealtValueType.Heal, _healAmount);
            if (heal > 0) target.ChangeHp(HpChangeType.Heal, heal);
        }

        // 説明文の {allyHeal}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["allyHeal"] = _healAmount;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 相手の HP を回復する。そのままでは敵を助けるだけだが、回復の反転（データ破損・リバースパッチ・カーネルパニック）が
    // 付いた相手にはダメージになる。回復量を増やす効果は使用者側、反転は受ける側で効く
    [Serializable]
    public sealed class OpponentHealCardEffect : CardEffectBase
    {
        [SerializeField][Min(0)] private int _healAmount;

        public override bool TargetsOpponent => true;

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
            if (_healAmount <= 0) return;

            int heal = user.CalculateDealtValue(DealtValueType.Heal, _healAmount);
            if (heal > 0) target.ChangeHp(HpChangeType.Heal, heal);
        }

        // 説明文の {enemyHeal}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["enemyHeal"] = _healAmount;
        }
    }
}

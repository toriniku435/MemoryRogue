using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // カードの代償として、使用者が自分のHPを払う。
    // 払ってもHPは1より下がらない（代償で倒れることはない）。ブロックや耐性では減らせない
    [Serializable]
    public sealed class PayHpCardEffect : CardEffectBase
    {
        [SerializeField][Min(0)] private int _amount;

        public override bool TargetsOpponent => false;

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
            int payable = Math.Min(_amount, user.CurrentHp - 1);
            if (payable <= 0) return;

            user.ChangeHp(HpChangeType.PayHp, -payable);
        }

        // 説明文の {hpCost}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["hpCost"] = _amount;
        }
    }
}

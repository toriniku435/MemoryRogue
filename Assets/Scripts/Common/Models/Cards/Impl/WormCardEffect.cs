using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 毎ターン終了時に _wormAmount + 相手のブロックの半分のダメージを与えるワームに感染させる
    [Serializable]
    public sealed class WormCardEffect : PoisonCardEffectBase
    {
        [SerializeField][Min(0)] private int _wormAmount;

        protected override int CalculatePoisonAmount(UnitStatusModel user, UnitStatusModel target) => _wormAmount;

        protected override IStatusEffect CreatePoisonEffect(int amount) => new WormStatusEffect(amount);

        // 説明文の {worm}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["worm"] = _wormAmount;
        }
    }
}

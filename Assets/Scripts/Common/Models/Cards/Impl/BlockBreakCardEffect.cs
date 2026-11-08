using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    // 相手のブロックをすべて解除する。ダメージは与えない（バイパス）
    [Serializable]
    public sealed class BlockBreakCardEffect : CardEffectBase
    {
        public override bool TargetsOpponent => true;

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
            target.RemoveStatusEffects(effect => effect is BlockStatusEffect);
        }

        // 数値を持たない
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
        }
    }
}

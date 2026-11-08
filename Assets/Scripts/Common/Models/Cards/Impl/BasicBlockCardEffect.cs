using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Live2DCardRogue.Common.Models.Cards
{
    [Serializable]
    public sealed class BasicBlockCardEffect : BlockCardEffectBase
    {
        [SerializeField][Min(0)] private int _blockAmount;

        protected override int CalculateBlockAmount(UnitStatusModel user, UnitStatusModel target)
        {
            return _blockAmount;
        }

        // 説明文の {block}
        public override void AddDescriptionValues(IDictionary<string, object> values)
        {
            values["block"] = _blockAmount;
        }
    }
}

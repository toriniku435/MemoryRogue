using System;
using UnityEngine;

namespace Live2DCardRogue.Common.Statuses
{
    [Serializable]
    public sealed class PoisonResistanceDefinition : StatusEffectDefinitionBase
    {
        [SerializeField][Min(1)] private int _resistanceValue = 1;

        public override IStatusEffect Create()
        {
            return new PoisonResistanceStatusEffect(_resistanceValue);
        }
    }
}

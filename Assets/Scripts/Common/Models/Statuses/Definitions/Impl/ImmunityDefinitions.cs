using System;

namespace Live2DCardRogue.Common.Statuses
{
    [Serializable]
    public sealed class VirusImmunityDefinition : StatusEffectDefinitionBase
    {
        public override IStatusEffect Create() => new VirusImmunityStatusEffect();
    }

    [Serializable]
    public sealed class CorruptionImmunityDefinition : StatusEffectDefinitionBase
    {
        public override IStatusEffect Create() => new CorruptionImmunityStatusEffect();
    }

    [Serializable]
    public sealed class KillImmunityDefinition : StatusEffectDefinitionBase
    {
        public override IStatusEffect Create() => new KillImmunityStatusEffect();
    }
}

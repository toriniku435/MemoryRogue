namespace Live2DCardRogue.Common.Statuses
{
    // データ破損（回復反転）を受け付けない永続効果
    public class CorruptionImmunityStatusEffect : StatusEffectBase
    {
        public const string EffectId = "CorruptionImmunity";

        public CorruptionImmunityStatusEffect() : base(0)
        {
        }

        public override string Id => EffectId;

        // 生まれつきの性質なので、効果をすべて消すコマンド（フォーマット）でも消えない
        public override bool IsTrait => true;

        public override bool BlocksStatusEffect(IStatusEffect incoming)
        {
            return incoming.Id == HealReversalStatusEffect.EffectId;
        }
    }
}

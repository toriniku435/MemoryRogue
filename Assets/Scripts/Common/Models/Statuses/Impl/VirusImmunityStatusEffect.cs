namespace Live2DCardRogue.Common.Statuses
{
    // ウイルス（毒）とワームに感染しない永続効果。付こうとした感染をそのまま弾く
    public class VirusImmunityStatusEffect : StatusEffectBase
    {
        public const string EffectId = "VirusImmunity";

        public VirusImmunityStatusEffect() : base(0)
        {
        }

        public override string Id => EffectId;

        // 生まれつきの性質なので、効果をすべて消すコマンド（フォーマット）でも消えない
        public override bool IsTrait => true;

        public override bool BlocksStatusEffect(IStatusEffect incoming)
        {
            return incoming.Id == PoisonStatusEffect.EffectId || incoming.Id == WormStatusEffect.EffectId;
        }
    }
}

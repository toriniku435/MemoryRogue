namespace Live2DCardRogue.Common.Statuses
{
    // 即死を打ち消す永続効果。HpChangeType.InstantKill だけを 0 にする
    public class KillImmunityStatusEffect : StatusEffectBase
    {
        public const string EffectId = "KillImmunity";

        public KillImmunityStatusEffect() : base(0)
        {
        }

        public override string Id => EffectId;

        // 生まれつきの性質なので、効果をすべて消すコマンド（フォーマット）でも消えない
        public override bool IsTrait => true;

        public override int OnHpChange(HpChangeType type, int value)
        {
            return type == HpChangeType.InstantKill ? 0 : value;
        }
    }
}

namespace Live2DCardRogue.Common.Statuses
{
    // 残りターンの間、受ける回復をダメージに反転する（アンデッドのような効果）。
    // その場の回復も、再生（継続回復）も同じ Heal を通るので、どちらも反転する
    public class HealReversalStatusEffect : TimedStatusEffectBase
    {
        public const string EffectId = "HealReversal";

        public HealReversalStatusEffect(int turns) : base(turns, 0)
        {
        }

        public override string Id => EffectId;

        public override int OnHpChange(HpChangeType type, int value)
        {
            if (type != HpChangeType.Heal || value <= 0) return value;

            return -value;
        }
    }
}

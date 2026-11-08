namespace Live2DCardRogue.Common.Statuses
{
    // 残りターンの間、行動できなくする（1ターンに行動できる回数を 0 にする）
    public class StunStatusEffect : TimedStatusEffectBase
    {
        public const string EffectId = "Stun";

        public StunStatusEffect(int turns) : base(turns, 0)
        {
        }

        public override string Id => EffectId;

        public override int OnStatLimit(StatLimitType type, int value)
        {
            if (type != StatLimitType.ActionCount) return value;

            return 0;
        }
    }
}

namespace Live2DCardRogue.Common.Statuses
{
    // 残りターンの間、ターン開始時に引く手札を Amount 枚減らす（大きな効果の代償）。
    // 増える方とは別の効果にしてあるので、両方が付いていれば差し引きになる
    public class HandSizeDownStatusEffect : TimedStatusEffectBase
    {
        public const string EffectId = "HandSizeDown";

        public HandSizeDownStatusEffect(int turns, int amount) : base(turns, amount)
        {
        }

        public override string Id => EffectId;

        public override int OnStatLimit(StatLimitType type, int value)
        {
            if (type != StatLimitType.HandSize) return value;

            return value - Amount;
        }
    }
}

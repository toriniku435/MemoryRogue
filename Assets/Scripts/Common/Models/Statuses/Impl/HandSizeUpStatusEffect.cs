namespace Live2DCardRogue.Common.Statuses
{
    // 残りターンの間、ターン開始時に引く手札を Amount 枚増やす
    public class HandSizeUpStatusEffect : TimedStatusEffectBase
    {
        public const string EffectId = "HandSizeUp";

        public HandSizeUpStatusEffect(int turns, int amount) : base(turns, amount)
        {
        }

        public override string Id => EffectId;

        public override int OnStatLimit(StatLimitType type, int value)
        {
            if (type != StatLimitType.HandSize) return value;

            return value + Amount;
        }
    }
}

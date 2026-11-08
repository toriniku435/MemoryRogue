namespace Live2DCardRogue.Common.Statuses
{
    // 残りターンの間、スタミナ（メモリー）の最大値を Amount だけ増やす
    public class MemoryBoostStatusEffect : TimedStatusEffectBase
    {
        public const string EffectId = "MemoryBoost";

        public MemoryBoostStatusEffect(int turns, int amount) : base(turns, amount)
        {
        }

        public override string Id => EffectId;

        public override int OnStatLimit(StatLimitType type, int value)
        {
            if (type != StatLimitType.MaxStamina) return value;

            return value + Amount;
        }
    }
}

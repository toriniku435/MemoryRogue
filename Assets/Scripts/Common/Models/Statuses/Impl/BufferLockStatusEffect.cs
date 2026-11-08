namespace Live2DCardRogue.Common.Statuses
{
    // 残りターンの間、ターン開始時にバッファ（手札）を1枚も引けなくする（バッファオーバーフローの代償）。
    // 重ねがけは長い方ではなく足し合わせる：続けて使えば、その次のターンもバッファが空になる
    public class BufferLockStatusEffect : TimedStatusEffectBase
    {
        public const string EffectId = "BufferLock";

        public BufferLockStatusEffect(int turns) : base(turns, 0)
        {
        }

        public override string Id => EffectId;

        public override void OnDuplicated(int value)
        {
            Value += value;
        }

        public override int OnStatLimit(StatLimitType type, int value)
        {
            return type == StatLimitType.DrawCap ? 0 : value;
        }
    }
}

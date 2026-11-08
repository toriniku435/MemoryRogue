namespace Live2DCardRogue.Common.Statuses
{
    // 次に実行するコマンドを Value 回実行する。代わりにそのコマンドはパージされる（山札に戻らない）。
    // 重ねがけは回数の大きい方を残す（永続効果と同じ決まり。足すと際限なく増える）
    public class MultithreadStatusEffect : NextCommandStatusEffectBase
    {
        public const string EffectId = "Multithread";

        public MultithreadStatusEffect(int repeat) : base(repeat)
        {
        }

        public override string Id => EffectId;

        public override int OnStatLimit(StatLimitType type, int value)
        {
            if (!IsArmed || type != StatLimitType.CommandRepeat) return value;

            return value > Value ? value : Value;
        }

        public override CommandDestination OnCommandDestination(CommandDestination destination)
        {
            return IsArmed ? CommandDestination.Purge : destination;
        }
    }
}

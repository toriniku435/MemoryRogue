namespace Live2DCardRogue.Common.Statuses
{
    // 次に実行するコマンドを、捨て札ではなく次のターンの手札へ戻す。
    // パージされるコマンド（マルチスレッドの対象・使用後パージのコマンド）は戻さない（パージの方が強い）
    public class LoopbackStatusEffect : NextCommandStatusEffectBase
    {
        public const string EffectId = "Loopback";

        public LoopbackStatusEffect() : base(1)
        {
        }

        public override string Id => EffectId;

        public override CommandDestination OnCommandDestination(CommandDestination destination)
        {
            if (!IsArmed || destination != CommandDestination.Discard) return destination;

            return CommandDestination.ReturnToHand;
        }
    }
}

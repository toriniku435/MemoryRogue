namespace Live2DCardRogue.Common.Statuses
{
    // 勝利したときに受け取るトークンを Value % 増やす（マイニング）。戦闘中は何もせず、戦闘の結果として読まれるだけ。
    // 使うたびに上乗せする（使ったコマンドはパージされるので、デッキに入れた枚数までしか積めない）
    public class TokenBonusStatusEffect : StatusEffectBase
    {
        public const string EffectId = "TokenBonus";

        public TokenBonusStatusEffect(int percent) : base(percent)
        {
        }

        public override string Id => EffectId;

        public override void OnDuplicated(int value)
        {
            if (value <= 0) return;

            Value = Value > MaxValue - value ? MaxValue : Value + value;
        }
    }
}

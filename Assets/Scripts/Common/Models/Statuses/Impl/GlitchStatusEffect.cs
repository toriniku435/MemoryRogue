using System;

namespace Live2DCardRogue.Common.Statuses
{
    // 残りターンの間、持ち主（敵）が行動するたびに Amount % の確率でグリッチ（自滅の行動）が割り込む。
    // 割り込ませるのは行動を進める側（BattlePhaseDirector）で、ここは確率を差し出すだけ
    public class GlitchStatusEffect : TimedStatusEffectBase
    {
        public const string EffectId = "Glitch";

        public GlitchStatusEffect(int turns, int percent) : base(turns, percent)
        {
        }

        public override string Id => EffectId;

        public override int OnStatLimit(StatLimitType type, int value)
        {
            return type == StatLimitType.GlitchChance ? Math.Max(value, Amount) : value;
        }
    }
}

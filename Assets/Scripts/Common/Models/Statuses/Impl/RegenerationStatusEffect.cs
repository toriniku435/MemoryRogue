using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.Statuses
{
    // 持ち主が自分のターンを終えるたびにValue分HPを回復する永続効果。毒の裏返し。
    // 発動しても値は減らず、解除手段が実装されるまで残り続ける
    public class RegenerationStatusEffect : StatusEffectBase
    {
        public const string EffectId = "Regeneration";

        public RegenerationStatusEffect(int regenerationValue) : base(regenerationValue)
        {
        }

        public override string Id => EffectId;

        // 重ね掛けは大きい方を残す（永続効果の決まり。StatusEffectBaseの既定のまま）

        // 発動は自分のターン終了時。毒と同じタイミングにしてあるので、両方付いていれば同じターンで相殺される
        public override void OnTurnEnd(UnitStatusModel owner)
        {
            if (Value == 0) return;

            // 回復なのでプラス。ワンショット回復と同じ種別を通すため、回復量を変える効果は両方に効く
            owner.ChangeHp(HpChangeType.Heal, Value);
        }
    }
}

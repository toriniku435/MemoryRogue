using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.Statuses
{
    // 持ち主が自分のターンを終えるたびに、Value と持ち主のブロックの半分（BlockRatePercent %、端数切り捨て）を合わせたダメージを与える永続効果（ワーム）。
    // 守りを固めた相手ほど重くなる。ダメージはウイルスと同じ TakePoisonDamage なので、ブロックでは防げない
    // （ブロックは減らない。数えるだけ）。ウイルスとは別の効果なので、両方同時に付く
    public class WormStatusEffect : StatusEffectBase
    {
        public const string EffectId = "Worm";

        /// <summary>ダメージに足すブロックの割合（%）。そのまま足すと守りの厚い敵に強すぎたので半分にしている</summary>
        public const int BlockRatePercent = 50;

        public WormStatusEffect(int value) : base(value)
        {
        }

        public override string Id => EffectId;

        // 重ね掛けは大きい方を残して 1 増やす（強化・継続ダメージの決まり。StatusEffectBase.StackPlusOne）
        public override void OnDuplicated(int value) => StackPlusOne(value);

        public override void OnTurnEnd(UnitStatusModel owner)
        {
            int damage = Value + owner.BlockValue * BlockRatePercent / 100;
            if (damage <= 0) return;

            owner.ChangeHp(HpChangeType.TakePoisonDamage, -damage);
        }
    }
}

using System;

namespace Live2DCardRogue.Common.Statuses
{
    public class BlockStatusEffect : StatusEffectBase
    {
        public const string EffectId = "Block";

        public BlockStatusEffect(int blockValue) : base(blockValue)
        {
        }

        public override string Id => EffectId;
        public override int MaxValue => 999;

        // ブロックは永続効果ではなく、攻撃を受けるたびに削られて尽きる。
        // 積み増してこそ意味があるので、既定（大きい方を残す）ではなく加算し、MaxValueで頭打ちにする
        public override void OnDuplicated(int value)
        {
            // 桁あふれを避けるため、加算してから比較せず残り幅と比較する
            Value = value > MaxValue - Value ? MaxValue : Value + value;
        }

        /// <summary>ダメージを受けずにブロックを減らす（リフレクションの消費）。減らした量を返し、尽きたら期限切れにする</summary>
        public int Spend(int amount)
        {
            if (amount <= 0) return 0;

            int spent = Math.Min(Value, amount);
            Value -= spent;
            if (Value == 0)
            {
                IsExpired = true;
            }
            return spent;
        }

        // valueはHP変動量なのでダメージはマイナス。絶対値をブロック値で相殺する
        public override int OnHpChange(HpChangeType type, int value)
        {
            if (type != HpChangeType.TakeAttackDamage) return value;
            if (value >= 0) return value;

            int damage = -value;
            int absorbed = Math.Min(Value, damage);

            Value -= absorbed;
            if (Value == 0)
            {
                IsExpired = true;
            }

            return -(damage - absorbed);
        }
    }
}

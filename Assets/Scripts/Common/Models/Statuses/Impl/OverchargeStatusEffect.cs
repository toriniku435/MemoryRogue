using System;

namespace Live2DCardRogue.Common.Statuses
{
    // 最大HPを超えた回復の溜まり（オーバーチャージ）。溜めるのも減らすのも UnitStatusModel.ChangeHp が行う:
    // 回復が最大HPであふれた分がこれになり、ダメージはすべての割り込み（ブロックなど）を通った後、HPより先にこれから減る。
    // 割り込みの中で減らさないのは、ブロックより後に減るという順番を、状態効果が付いた順に左右させないため
    public class OverchargeStatusEffect : StatusEffectBase
    {
        public const string EffectId = "Overcharge";

        /// <summary>溜められる上限（最大HPに対する割合）</summary>
        public const int CapPercent = 30;

        /// <summary>これだけ溜まるごとに、与える攻撃ダメージが1増える</summary>
        public const int AmountPerBonus = 5;

        private readonly int _maxValue;

        public OverchargeStatusEffect(int amount, int maxHp) : base(0)
        {
            _maxValue = Math.Max(0, maxHp * CapPercent / 100);
            Value = Math.Min(Math.Max(0, amount), _maxValue);
        }

        public override string Id => EffectId;
        public override int MaxValue => _maxValue;

        // ブロックと同じく、溜まるほど意味があるので加算する（上限で頭打ち）。
        // 上限は最初に付いたときの最大HPで決まる（戦闘中に最大HPは変わらない）
        public override void OnDuplicated(int value)
        {
            if (value <= 0) return;
            Value = value > MaxValue - Value ? MaxValue : Value + value;
        }

        public override int OnDealtValue(DealtValueType type, int value)
        {
            if (type != DealtValueType.AttackDamage || value <= 0) return value;
            return value + Value / AmountPerBonus;
        }

        /// <summary>ダメージを肩代わりする。肩代わりした量を返し、尽きたら期限切れにする（ブロックと同じく次の掃除で外れる）</summary>
        public int Absorb(int damage)
        {
            if (damage <= 0) return 0;

            int absorbed = Math.Min(Value, damage);
            Value -= absorbed;
            if (Value == 0) IsExpired = true;
            return absorbed;
        }
    }
}

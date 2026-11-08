namespace Live2DCardRogue.Common.Statuses
{
    // 与える攻撃ダメージをValue分だけ増やす永続効果。
    // 「相手の被ダメージを増やす」のではなく攻撃側に付くので、対象が何体いても攻撃1回につき1度だけ乗る
    public class StrengthStatusEffect : StatusEffectBase
    {
        public const string EffectId = "Strength";

        public StrengthStatusEffect(int strengthValue) : base(strengthValue)
        {
        }

        public override string Id => EffectId;

        // 重ね掛けは大きい方を残して 1 増やす（強化・継続ダメージの決まり。StatusEffectBase.StackPlusOne）
        public override void OnDuplicated(int value) => StackPlusOne(value);


        public override int OnDealtValue(DealtValueType type, int value)
        {
            if (type != DealtValueType.AttackDamage) return value;

            return value + Value;
        }
    }
}

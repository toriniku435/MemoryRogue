namespace Live2DCardRogue.Common.Statuses
{
    // エンドレスで、深い階層の敵に最初から付く永続効果。与える攻撃ダメージをValue分だけ増やす。
    // 筋力と同じ働きだが別の効果にしてあるのは、敵が自分で筋力を積んだときに「大きい方を残す」で打ち消し合わないようにするため
    public class LayerBoostStatusEffect : StatusEffectBase
    {
        public const string EffectId = "LayerBoost";

        public LayerBoostStatusEffect(int bonus) : base(bonus)
        {
        }

        public override string Id => EffectId;

        // 生まれつきの性質なので、効果をすべて消すコマンド（フォーマット）でも消えない
        public override bool IsTrait => true;

        public override int OnDealtValue(DealtValueType type, int value)
        {
            if (type != DealtValueType.AttackDamage) return value;

            // 上乗せはとても深い階層で大きくなるので、足してもあふれないよう int の上限で止める
            long boosted = (long)value + Value;
            return boosted > int.MaxValue ? int.MaxValue : (int)boosted;
        }
    }
}

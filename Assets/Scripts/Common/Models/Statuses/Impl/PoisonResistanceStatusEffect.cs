using System;

namespace Live2DCardRogue.Common.Statuses
{
    // 受ける毒ダメージをValue分だけ定数軽減する永続効果。
    // TakePoisonDamageだけを見るので、攻撃によるダメージには一切関与しない
    public class PoisonResistanceStatusEffect : StatusEffectBase
    {
        public const string EffectId = "PoisonResistance";

        public PoisonResistanceStatusEffect(int resistanceValue) : base(resistanceValue)
        {
        }

        public override string Id => EffectId;

        // 生まれつきの性質なので、効果をすべて消すコマンド（フォーマット）でも消えない
        public override bool IsTrait => true;

        // valueはHP変動量なので毒ダメージはマイナス。軽減して0に近づける
        public override int OnHpChange(HpChangeType type, int value)
        {
            if (type != HpChangeType.TakePoisonDamage) return value;
            if (value >= 0) return value;

            return Math.Min(value + Value, 0);
        }
    }
}

using System;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    /// <summary>
    /// 攻撃カード効果の共通処理を定義する基底クラスです。
    /// </summary>
    [Serializable]
    public abstract class AttackCardEffectBase : CardEffectBase
    {
        public override bool TargetsOpponent => true; // ダメージは対象に入る

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
            DealDamage(user, target, CalculateDamage(user, target));
        }

        // 素のダメージを、使用者の強化と対象の割り込み（ブロックなど）を通して与える。
        // ダメージの素の値を実行時の事情で変える効果（手札をパージした枚数で決まる、など）もここを通す
        protected void DealDamage(UnitStatusModel user, UnitStatusModel target, int baseDamage)
        {
            if (baseDamage <= 0) return;

            // 強化バフなどは使用者側に付くので、使用者に割り込みを通してもらう
            var damage = user.CalculateDealtValue(DealtValueType.AttackDamage, baseDamage);
            if (damage > 0)
            {
                target.ChangeHp(DamageType, -damage);
            }
        }

        protected abstract int CalculateDamage(UnitStatusModel user, UnitStatusModel target);

        // 相手がどの割り込み（ブロックなど）を通して受けるか。貫通攻撃は別の種別で受けさせる
        protected virtual HpChangeType DamageType => HpChangeType.TakeAttackDamage;
    }
}

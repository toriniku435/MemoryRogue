using System;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    /// <summary>
    /// 強化カード効果の共通処理を定義する基底クラスです。
    /// </summary>
    [Serializable]
    public abstract class StrengthCardEffectBase : CardEffectBase
    {
        public override bool TargetsOpponent => false; // 強化は使用者自身に付く

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
            var baseStrengthAmount = CalculateStrengthAmount(user, target);
            if (baseStrengthAmount <= 0) return;

            var strengthAmount = user.CalculateDealtValue(DealtValueType.Strength, baseStrengthAmount);
            if (strengthAmount > 0)
            {
                // 強化は与えるダメージに乗る効果なので、毒と違い使用者自身に付く
                user.AddStatusEffect(new StrengthStatusEffect(strengthAmount));
            }
        }

        protected abstract int CalculateStrengthAmount(UnitStatusModel user, UnitStatusModel target);
    }
}

using System;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    /// <summary>
    /// 毒カード効果の共通処理を定義する基底クラスです。
    /// </summary>
    [Serializable]
    public abstract class PoisonCardEffectBase : CardEffectBase
    {
        public override bool TargetsOpponent => true; // 毒は対象に付く

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
            var basePoisonAmount = CalculatePoisonAmount(user, target);
            if (basePoisonAmount <= 0) return;

            var poisonAmount = user.CalculateDealtValue(DealtValueType.Poison, basePoisonAmount);
            if (poisonAmount > 0)
            {
                // Blockは使用者自身に付くが、毒は対象に付く
                target.AddStatusEffect(CreatePoisonEffect(poisonAmount));
            }
        }

        // 付けるウイルスの種類。ブロックの分だけ重くなるワームなどは、ここを差し替える
        protected virtual IStatusEffect CreatePoisonEffect(int amount) => new PoisonStatusEffect(amount);

        protected abstract int CalculatePoisonAmount(UnitStatusModel user, UnitStatusModel target);
    }
}

using System;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    /// <summary>
    /// 継続回復カード効果の共通処理を定義する基底クラスです。
    /// その場では回復せず、ターン終了ごとに効く再生を使用者に付与する
    /// </summary>
    [Serializable]
    public abstract class RegenerationCardEffectBase : CardEffectBase
    {
        public override bool TargetsOpponent => false; // 毒と違い、再生は使用者自身に付く

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
            var baseRegenerationAmount = CalculateRegenerationAmount(user, target);
            if (baseRegenerationAmount <= 0) return;

            var regenerationAmount = user.CalculateDealtValue(DealtValueType.Regeneration, baseRegenerationAmount);
            if (regenerationAmount > 0)
            {
                user.AddStatusEffect(new RegenerationStatusEffect(regenerationAmount));
            }
        }

        protected abstract int CalculateRegenerationAmount(UnitStatusModel user, UnitStatusModel target);
    }
}

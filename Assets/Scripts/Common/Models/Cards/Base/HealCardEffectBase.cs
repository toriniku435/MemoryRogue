using System;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    /// <summary>
    /// 回復カード効果の共通処理を定義する基底クラスです。
    /// その場で1回だけHPを戻す（ターンをまたいで効き続けるものは RegenerationCardEffectBase）
    /// </summary>
    [Serializable]
    public abstract class HealCardEffectBase : CardEffectBase
    {
        public override bool TargetsOpponent => false; // 回復は使用者自身に効く

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
            Heal(user, CalculateHealAmount(user, target));
        }

        // 素の回復量を、使用者の割り込みを通して戻す（実行時の事情で量が決まる効果もここを通す）
        protected void Heal(UnitStatusModel user, int baseHealAmount)
        {
            if (baseHealAmount <= 0) return;

            var healAmount = user.CalculateDealtValue(DealtValueType.Heal, baseHealAmount);
            if (healAmount > 0)
            {
                // ChangeHpは符号で方向が決まるので、回復はプラスで渡す（最大HPでの頭打ちはモデル側が見る）
                user.ChangeHp(HpChangeType.Heal, healAmount);
            }
        }

        protected abstract int CalculateHealAmount(UnitStatusModel user, UnitStatusModel target);
    }
}

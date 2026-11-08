using System;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    /// <summary>
    /// ターン数で切れる状態効果（メモリー増設・手札の増減など）を使用者自身に付けるカード効果の基底
    /// </summary>
    [Serializable]
    public abstract class SelfStatusCardEffectBase : CardEffectBase
    {
        public override bool TargetsOpponent => false;

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
            var effect = CreateStatusEffect();
            if (effect != null) user.AddStatusEffect(effect);
        }

        protected abstract IStatusEffect CreateStatusEffect();
    }

    /// <summary>
    /// ターン数で切れる状態効果（停止・回復反転など）を相手に付けるカード効果の基底
    /// </summary>
    [Serializable]
    public abstract class OpponentStatusCardEffectBase : CardEffectBase
    {
        public override bool TargetsOpponent => true;

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
            var effect = CreateStatusEffect();
            if (effect != null) target.AddStatusEffect(effect);
        }

        protected abstract IStatusEffect CreateStatusEffect();
    }
}

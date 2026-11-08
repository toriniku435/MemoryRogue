using System;
using Live2DCardRogue.Common.Statuses;

namespace Live2DCardRogue.Common.Models.Cards
{
    /// <summary>
    /// ブロックカード効果の基底クラス
    /// </summary>
    [Serializable]
    public abstract class BlockCardEffectBase : CardEffectBase
    {
        public override bool TargetsOpponent => false; // ブロックは使用者自身に付く

        protected override void OnExecute(UnitStatusModel user, UnitStatusModel target)
        {
            GainBlock(user, CalculateBlockAmount(user, target));
        }

        // 素のブロック量を、使用者の強化などを通して付ける（実行時の事情で量が決まる効果もここを通す）
        protected void GainBlock(UnitStatusModel user, int baseBlockAmount)
        {
            if (baseBlockAmount <= 0) return;

            var blockAmount = user.CalculateDealtValue(DealtValueType.Block, baseBlockAmount);
            if (blockAmount > 0)
            {
                user.AddStatusEffect(new BlockStatusEffect(blockAmount));
            }
        }

        protected abstract int CalculateBlockAmount(UnitStatusModel user, UnitStatusModel target);
    }
}
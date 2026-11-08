using Live2DCardRogue.Common.Statuses;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class BlockStatusEffectTests
    {
        // OnHpChangeが受け渡しするのはHPの変動量なので、ダメージはマイナスで表される
        [Test]
        public void OnHpChange_TakeAttackDamage_LessThanBlock_FullyAbsorbed_AndReducesRemainingBlock()
        {
            var block = new BlockStatusEffect(10);

            int reducedDamage = block.OnHpChange(HpChangeType.TakeAttackDamage, -4);

            Assert.AreEqual(0, reducedDamage);
            Assert.AreEqual(6, block.Value);
            Assert.IsFalse(block.IsExpired);
        }

        [Test]
        public void OnHpChange_TakeAttackDamage_ExceedsBlock_OverflowPassesThrough_AndExpires()
        {
            var block = new BlockStatusEffect(10);

            int reducedDamage = block.OnHpChange(HpChangeType.TakeAttackDamage, -15);

            Assert.AreEqual(-5, reducedDamage);
            Assert.AreEqual(0, block.Value);
            Assert.IsTrue(block.IsExpired);
        }

        [Test]
        public void OnHpChange_TakeAttackDamage_ExactlyEqualsBlock_FullyAbsorbed_AndExpires()
        {
            var block = new BlockStatusEffect(10);

            int reducedDamage = block.OnHpChange(HpChangeType.TakeAttackDamage, -10);

            Assert.AreEqual(0, reducedDamage);
            Assert.AreEqual(0, block.Value);
            Assert.IsTrue(block.IsExpired);
        }

        // 回復反転などでプラス（回復）が流れてきてもブロックは反応しない
        [Test]
        public void OnHpChange_TakeAttackDamage_PositiveValue_PassesThroughUntouched()
        {
            var block = new BlockStatusEffect(10);

            int result = block.OnHpChange(HpChangeType.TakeAttackDamage, 4);

            Assert.AreEqual(4, result);
            Assert.AreEqual(10, block.Value);
        }

        // ブロックは永続効果の決まり(大きい方を残す)の例外で、積み増す
        [Test]
        public void OnDuplicated_Accumulates()
        {
            var block = new BlockStatusEffect(5);

            block.OnDuplicated(3);

            Assert.AreEqual(8, block.Value);
        }

        [Test]
        public void OnDuplicated_ClampsAtMaxValue()
        {
            var block = new BlockStatusEffect(990);

            block.OnDuplicated(50);

            Assert.AreEqual(999, block.Value); // MaxValue
        }

        // 統合フックの肝。関心の無い割り込み種別では値も状態も触らない
        [Test]
        public void OnHpChange_UnrelatedType_PassesValueThroughUntouched()
        {
            var block = new BlockStatusEffect(10);

            int result = block.OnHpChange(HpChangeType.TakePoisonDamage, -4);

            Assert.AreEqual(-4, result);
            Assert.AreEqual(10, block.Value); // ブロックが削れていない
            Assert.IsFalse(block.IsExpired);
        }

        [Test]
        public void IsSameEffect_ComparesById()
        {
            var block = new BlockStatusEffect(10);
            var otherBlock = new BlockStatusEffect(3);

            Assert.IsTrue(block.IsSameEffect(otherBlock));
            Assert.IsFalse(block.IsSameEffect(null));
        }
    }
}

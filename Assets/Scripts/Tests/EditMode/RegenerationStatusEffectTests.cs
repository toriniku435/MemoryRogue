using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Statuses;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class RegenerationStatusEffectTests
    {
        // 削れたHPを用意するためのヘルパー。ChangeHpは符号で方向が決まるのでマイナスで渡す
        private static UnitStatusModel NewWoundedOwner(int maxHp, int damage)
        {
            var owner = new UnitStatusModel(maxHp: maxHp, maxStamina: 3);
            owner.ChangeHp(HpChangeType.TakeAttackDamage, -damage);
            return owner;
        }

        [Test]
        public void OnTurnEnd_HealsOwnerByItsValue()
        {
            var owner = NewWoundedOwner(maxHp: 30, damage: 10);
            owner.AddStatusEffect(new RegenerationStatusEffect(4));

            owner.EndTurn();

            Assert.AreEqual(24, owner.CurrentHp);
        }

        // 毒と同じく永続効果なので、発動しても値は減らず取り除かれもしない
        [Test]
        public void OnTurnEnd_TicksEveryTurn_AndNeverExpires()
        {
            var owner = NewWoundedOwner(maxHp: 30, damage: 20);
            owner.AddStatusEffect(new RegenerationStatusEffect(3));

            owner.EndTurn();
            owner.EndTurn();
            owner.EndTurn();

            Assert.AreEqual(19, owner.CurrentHp);
            Assert.AreEqual(1, owner.StatusEffects.Count); // EndTurnのIsExpired掃除で消えていない
            Assert.AreEqual(3, owner.StatusEffects[0].Value); // 値も減っていない
            Assert.IsFalse(owner.StatusEffects[0].IsExpired);
        }

        // ターン開始時には何も起きない(StartTurnの掃除でも消えない)
        [Test]
        public void StartTurn_DoesNothing_AndKeepsTheEffect()
        {
            var owner = NewWoundedOwner(maxHp: 30, damage: 10);
            owner.AddStatusEffect(new RegenerationStatusEffect(3));

            owner.StartTurn();

            Assert.AreEqual(20, owner.CurrentHp);
            Assert.AreEqual(1, owner.StatusEffects.Count);
        }

        // 永続効果なので、毒と同じく重ね掛けは大きい方を残す
        [Test]
        public void AddStatusEffect_Duplicated_KeepsTheLargerValue()
        {
            var owner = NewWoundedOwner(maxHp: 30, damage: 20);
            owner.AddStatusEffect(new RegenerationStatusEffect(2));
            owner.AddStatusEffect(new RegenerationStatusEffect(3));

            Assert.AreEqual(1, owner.StatusEffects.Count);
            Assert.AreEqual(3, owner.StatusEffects[0].Value);

            owner.EndTurn();
            Assert.AreEqual(13, owner.CurrentHp);
        }

        // 後から弱い再生を掛けても、今の再生は下がらない
        [Test]
        public void AddStatusEffect_DuplicatedWithSmallerValue_DoesNotWeaken()
        {
            var owner = NewWoundedOwner(maxHp: 30, damage: 20);
            owner.AddStatusEffect(new RegenerationStatusEffect(3));
            owner.AddStatusEffect(new RegenerationStatusEffect(2));

            Assert.AreEqual(3, owner.StatusEffects[0].Value);
        }

        // 回復はChangeHpが最大HPで頭打ちにするので、溢れた分は捨てられる
        [Test]
        public void OnTurnEnd_DoesNotHealAboveMaxHp()
        {
            var owner = NewWoundedOwner(maxHp: 30, damage: 2);
            owner.AddStatusEffect(new RegenerationStatusEffect(10));

            owner.EndTurn();

            Assert.AreEqual(30, owner.CurrentHp);
        }

        [Test]
        public void IsSameEffect_ComparesById()
        {
            var regeneration = new RegenerationStatusEffect(1);

            Assert.IsTrue(regeneration.IsSameEffect(new RegenerationStatusEffect(5)));
            Assert.IsFalse(regeneration.IsSameEffect(new PoisonStatusEffect(5)));
        }

        // 同じターン終了時に両方が走るので、毒と再生が並んでいれば相殺される
        [Test]
        public void OnTurnEnd_CancelsOutWithPoisonOfTheSameValue()
        {
            var owner = NewWoundedOwner(maxHp: 30, damage: 10);
            owner.AddStatusEffect(new PoisonStatusEffect(4));
            owner.AddStatusEffect(new RegenerationStatusEffect(4));

            owner.EndTurn();

            Assert.AreEqual(20, owner.CurrentHp);
        }
    }
}

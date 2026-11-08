using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Statuses;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class PoisonStatusEffectTests
    {
        [Test]
        public void OnTurnEnd_DamagesOwnerByItsValue()
        {
            var owner = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            owner.AddStatusEffect(new PoisonStatusEffect(4));

            owner.EndTurn();

            Assert.AreEqual(26, owner.CurrentHp);
        }

        // 永続効果なので、発動しても値は減らず取り除かれもしない
        [Test]
        public void OnTurnEnd_TicksEveryTurn_AndNeverExpires()
        {
            var owner = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            owner.AddStatusEffect(new PoisonStatusEffect(3));

            owner.EndTurn();
            owner.EndTurn();
            owner.EndTurn();

            Assert.AreEqual(21, owner.CurrentHp);
            Assert.AreEqual(1, owner.StatusEffects.Count); // EndTurnのIsExpired掃除で消えていない
            Assert.AreEqual(3, owner.StatusEffects[0].Value); // 値も減っていない
            Assert.IsFalse(owner.StatusEffects[0].IsExpired);
        }

        // ターン開始時には何も起きない(StartTurnの掃除でも消えない)
        [Test]
        public void StartTurn_DoesNothing_AndKeepsTheEffect()
        {
            var owner = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            owner.AddStatusEffect(new PoisonStatusEffect(3));

            owner.StartTurn();

            Assert.AreEqual(30, owner.CurrentHp);
            Assert.AreEqual(1, owner.StatusEffects.Count);
        }

        // 毒の重ね掛けは加算せず、大きい方を残して1増える（2026-10-04 のルール）。リスト上も1つのまま
        [Test]
        public void AddStatusEffect_StrongerPoison_RaisesToTheStrongerValue()
        {
            var owner = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            owner.AddStatusEffect(new PoisonStatusEffect(2));
            owner.AddStatusEffect(new PoisonStatusEffect(3));

            Assert.AreEqual(1, owner.StatusEffects.Count);
            Assert.AreEqual(4, owner.StatusEffects[0].Value); // 2+3=5ではなく、3+1

            owner.EndTurn();
            Assert.AreEqual(26, owner.CurrentHp);
        }

        [Test]
        public void AddStatusEffect_WeakerPoison_RaisesTheExistingByOne()
        {
            var owner = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            owner.AddStatusEffect(new PoisonStatusEffect(5));
            owner.AddStatusEffect(new PoisonStatusEffect(2));

            Assert.AreEqual(1, owner.StatusEffects.Count);
            Assert.AreEqual(6, owner.StatusEffects[0].Value); // 弱い方を重ねても、今の値+1
        }

        [Test]
        public void IsSameEffect_ComparesById()
        {
            var poison = new PoisonStatusEffect(1);

            Assert.IsTrue(poison.IsSameEffect(new PoisonStatusEffect(5)));
            Assert.IsFalse(poison.IsSameEffect(new BlockStatusEffect(5)));
        }

        // 毒はTakePoisonDamageとして与えられるので、攻撃ダメージ用のBlockには吸収されない
        [Test]
        public void OnTurnEnd_IsNotAbsorbedByBlock()
        {
            var owner = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            owner.AddStatusEffect(new BlockStatusEffect(10));
            owner.AddStatusEffect(new PoisonStatusEffect(4));

            owner.EndTurn();

            Assert.AreEqual(26, owner.CurrentHp);
            Assert.AreEqual(10, owner.StatusEffects[0].Value); // ブロックも削れていない
        }
    }
}

using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Statuses;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class PoisonResistanceStatusEffectTests
    {
        [Test]
        public void ReducesPoisonDamageByItsValue()
        {
            var owner = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            owner.AddStatusEffect(new PoisonResistanceStatusEffect(2));
            owner.AddStatusEffect(new PoisonStatusEffect(5));

            owner.EndTurn();

            Assert.AreEqual(27, owner.CurrentHp); // 5 - 2 = 3
        }

        // 軽減量が毒量以上なら無効化される(回復側に振れることはない)
        [Test]
        public void ResistanceAtOrAbovePoison_NegatesTheDamage()
        {
            var owner = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            owner.AddStatusEffect(new PoisonResistanceStatusEffect(5));
            owner.AddStatusEffect(new PoisonStatusEffect(5));

            owner.EndTurn();

            Assert.AreEqual(30, owner.CurrentHp);
        }

        // 毒にだけ効く。通常の被弾は素通しする
        [Test]
        public void DoesNotReduceOrdinaryDamage()
        {
            var owner = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            owner.AddStatusEffect(new PoisonResistanceStatusEffect(3));

            owner.ChangeHp(HpChangeType.TakeAttackDamage, -10);

            Assert.AreEqual(20, owner.CurrentHp);
        }

        // 永続効果なのでターンをまたいでも消えない
        [Test]
        public void SurvivesTurnCleanup()
        {
            var owner = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            owner.AddStatusEffect(new PoisonResistanceStatusEffect(2));

            owner.StartTurn();
            owner.EndTurn();

            Assert.AreEqual(1, owner.StatusEffects.Count);
            Assert.IsFalse(owner.StatusEffects[0].IsExpired);
        }

        // 永続効果なので、重ね掛けは大きい方を残す(StatusEffectBaseの既定動作)
        [Test]
        public void OnDuplicated_KeepsTheLargerValue()
        {
            var owner = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            owner.AddStatusEffect(new PoisonResistanceStatusEffect(1));
            owner.AddStatusEffect(new PoisonResistanceStatusEffect(2));
            owner.AddStatusEffect(new PoisonResistanceStatusEffect(1));

            Assert.AreEqual(1, owner.StatusEffects.Count);
            Assert.AreEqual(2, owner.StatusEffects[0].Value);
        }
    }
}

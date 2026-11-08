using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Statuses;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class UnitStatusModelTests
    {
        [Test]
        public void TakeDamage_ReducesCurrentHp_AndRaisesOnHpChanged()
        {
            var model = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            int? raisedCurrent = null;
            int? raisedMax = null;
            model.OnHpChanged += (current, max) => { raisedCurrent = current; raisedMax = max; };

            model.ChangeHp(HpChangeType.TakeAttackDamage, -10);

            Assert.AreEqual(20, model.CurrentHp);
            Assert.AreEqual(20, raisedCurrent);
            Assert.AreEqual(30, raisedMax);
        }

        [Test]
        public void TakeDamage_LargerThanCurrentHp_ClampsToZero_DoesNotUnderflow()
        {
            var model = new UnitStatusModel(maxHp: 10, maxStamina: 3);

            model.ChangeHp(HpChangeType.TakeAttackDamage, -999);

            Assert.AreEqual(0, model.CurrentHp);
        }

        [Test]
        public void Heal_CannotExceedMaxHp()
        {
            var model = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            model.ChangeHp(HpChangeType.TakeAttackDamage, -5);

            model.ChangeHp(HpChangeType.Heal, 999);

            Assert.AreEqual(30, model.CurrentHp);
        }

        [Test]
        public void ConsumeStamina_LargerThanCurrentStamina_ClampsToZero_DoesNotUnderflow()
        {
            var model = new UnitStatusModel(maxHp: 30, maxStamina: 3);

            model.ChangeStamina(StaminaChangeType.Consume, -999);

            Assert.AreEqual(0, model.Stamina);
        }

        [Test]
        public void GainStamina_CannotExceedMaxStamina()
        {
            var model = new UnitStatusModel(maxHp: 30, maxStamina: 3);

            model.ChangeStamina(StaminaChangeType.Gain, 999);

            Assert.AreEqual(3, model.Stamina);
        }

        [Test]
        public void RestoreStamina_SetsStaminaToMax_AndRaisesOnStaminaChanged()
        {
            var model = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            model.ChangeStamina(StaminaChangeType.Consume, -2);
            int? raised = null;
            model.OnStaminaChanged += s => raised = s;

            model.RestoreStamina();

            Assert.AreEqual(3, model.Stamina);
            Assert.AreEqual(3, raised);
        }

        [Test]
        public void AddStatusEffect_SameId_StacksValue_InsteadOfDuplicating()
        {
            var model = new UnitStatusModel(maxHp: 30, maxStamina: 3);

            model.AddStatusEffect(new BlockStatusEffect(5));
            model.AddStatusEffect(new BlockStatusEffect(3));

            Assert.AreEqual(1, model.StatusEffects.Count);
            Assert.AreEqual(8, model.StatusEffects[0].Value);
        }

        [Test]
        public void TakeDamage_IsReducedByBlockStatusEffect()
        {
            var model = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            model.AddStatusEffect(new BlockStatusEffect(5));

            model.ChangeHp(HpChangeType.TakeAttackDamage, -8);

            Assert.AreEqual(27, model.CurrentHp); // 8 damage - 5 block = 3 through
        }

        [Test]
        public void StartTurn_PurgesExpiredStatusEffects()
        {
            var model = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            model.AddStatusEffect(new BlockStatusEffect(5));
            model.ChangeHp(HpChangeType.TakeAttackDamage, -5); // fully consumes the block, marking it expired

            model.StartTurn();

            Assert.AreEqual(0, model.StatusEffects.Count);
        }

        // 重ねがけは数が変わらないので OnStatusEffectsChanged は来ない。値が変わったことは別の通知で分かる
        [Test]
        public void AddStatusEffect_Duplicate_RaisesValueChangedOnly()
        {
            var unit = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            unit.AddStatusEffect(new BlockStatusEffect(5));
            int listChanged = 0;
            int valuesChanged = 0;
            unit.OnStatusEffectsChanged += () => listChanged++;
            unit.OnStatusEffectValuesChanged += () => valuesChanged++;

            unit.AddStatusEffect(new BlockStatusEffect(3));

            Assert.AreEqual(0, listChanged);
            Assert.AreEqual(1, valuesChanged);
            Assert.AreEqual(8, unit.StatusEffects[0].Value);
        }

        [Test]
        public void AddStatusEffect_New_RaisesListChangedOnly()
        {
            var unit = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            int listChanged = 0;
            int valuesChanged = 0;
            unit.OnStatusEffectsChanged += () => listChanged++;
            unit.OnStatusEffectValuesChanged += () => valuesChanged++;

            unit.AddStatusEffect(new BlockStatusEffect(5));

            Assert.AreEqual(1, listChanged);
            Assert.AreEqual(0, valuesChanged);
        }

        // ダメージの数字に原因を添えるため、直前の HP 変動の種別と、ターンのフックで起きたならその状態効果を覚えておく
        [Test]
        public void LastHpChange_RecordsTheTypeAndTheTurnEffectThatCausedIt()
        {
            var model = new UnitStatusModel(maxHp: 30, maxStamina: 0);
            var poison = new PoisonStatusEffect(3);
            model.AddStatusEffect(poison);

            model.EndTurn();
            Assert.AreEqual(HpChangeType.TakePoisonDamage, model.LastHpChangeType);
            Assert.AreSame(poison, model.LastHpChangeSource);

            model.ChangeHp(HpChangeType.TakeAttackDamage, -5);
            Assert.AreEqual(HpChangeType.TakeAttackDamage, model.LastHpChangeType);
            Assert.IsNull(model.LastHpChangeSource, "カードなど状態効果の外で起きた変動には原因の効果が無い");
        }
    }
}

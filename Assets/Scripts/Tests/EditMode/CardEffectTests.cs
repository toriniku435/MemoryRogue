using System.Reflection;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Models.Cards;
using Live2DCardRogue.Common.Statuses;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class CardEffectTests
    {
        // _damage / _blockAmount are private [SerializeField]s with no public setter (Inspector-only by design),
        // so tests set them via reflection rather than adding a test-only constructor to production code.
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        [Test]
        public void BasicAttackCardEffect_Execute_DealsDamageToTarget()
        {
            var effect = new BasicAttackCardEffect();
            SetPrivateField(effect, "_damage", 7);
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            var target = new UnitStatusModel(maxHp: 30, maxStamina: 3);

            effect.Execute(user, target);

            Assert.AreEqual(23, target.CurrentHp);
            Assert.AreEqual(30, user.CurrentHp); // user itself is untouched
        }

        [Test]
        public void BasicBlockCardEffect_Execute_AddsBlockStatusEffectToUser_NotTarget()
        {
            var effect = new BasicBlockCardEffect();
            SetPrivateField(effect, "_blockAmount", 5);
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            var target = new UnitStatusModel(maxHp: 30, maxStamina: 3);

            effect.Execute(user, target);

            Assert.AreEqual(1, user.StatusEffects.Count);
            Assert.AreEqual(0, target.StatusEffects.Count);
            Assert.IsInstanceOf<BlockStatusEffect>(user.StatusEffects[0]);
            Assert.AreEqual(5, user.StatusEffects[0].Value);
        }

        [Test]
        public void BasicPoisonCardEffect_Execute_AddsPoisonStatusEffectToTarget_NotUser()
        {
            var effect = new BasicPoisonCardEffect();
            SetPrivateField(effect, "_poisonAmount", 4);
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            var target = new UnitStatusModel(maxHp: 30, maxStamina: 3);

            effect.Execute(user, target);

            Assert.AreEqual(1, target.StatusEffects.Count);
            Assert.AreEqual(0, user.StatusEffects.Count); // Blockと違い対象側に付く
            Assert.IsInstanceOf<PoisonStatusEffect>(target.StatusEffects[0]);
            Assert.AreEqual(4, target.StatusEffects[0].Value);
            Assert.AreEqual(30, target.CurrentHp); // 付与しただけでは削れない
        }

        [Test]
        public void BasicPoisonCardEffect_ZeroAmount_AddsNothing()
        {
            var effect = new BasicPoisonCardEffect();
            SetPrivateField(effect, "_poisonAmount", 0);
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            var target = new UnitStatusModel(maxHp: 30, maxStamina: 3);

            effect.Execute(user, target);

            Assert.AreEqual(0, target.StatusEffects.Count);
        }

        [Test]
        public void BasicStrengthCardEffect_Execute_AddsStrengthToUser_NotTarget()
        {
            var effect = new BasicStrengthCardEffect();
            SetPrivateField(effect, "_strengthAmount", 3);
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            var target = new UnitStatusModel(maxHp: 30, maxStamina: 3);

            effect.Execute(user, target);

            Assert.AreEqual(1, user.StatusEffects.Count);
            Assert.AreEqual(0, target.StatusEffects.Count); // 毒と違い使用者側に付く
            Assert.IsInstanceOf<StrengthStatusEffect>(user.StatusEffects[0]);
            Assert.AreEqual(3, user.StatusEffects[0].Value);
        }

        // 付与しただけではHPは動かず、以降の攻撃に乗る
        [Test]
        public void BasicStrengthCardEffect_BoostsLaterAttacks()
        {
            var strengthEffect = new BasicStrengthCardEffect();
            SetPrivateField(strengthEffect, "_strengthAmount", 4);
            var attackEffect = new BasicAttackCardEffect();
            SetPrivateField(attackEffect, "_damage", 6);
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            var target = new UnitStatusModel(maxHp: 30, maxStamina: 3);

            strengthEffect.Execute(user, target);
            Assert.AreEqual(30, target.CurrentHp);

            attackEffect.Execute(user, target);
            Assert.AreEqual(20, target.CurrentHp); // 6 + 4
        }

        [Test]
        public void BasicStrengthCardEffect_ZeroAmount_AddsNothing()
        {
            var effect = new BasicStrengthCardEffect();
            SetPrivateField(effect, "_strengthAmount", 0);
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            var target = new UnitStatusModel(maxHp: 30, maxStamina: 3);

            effect.Execute(user, target);

            Assert.AreEqual(0, user.StatusEffects.Count);
        }

        [Test]
        public void BasicAttackCardEffect_ZeroDamage_DoesNotCallTakeDamage()
        {
            var effect = new BasicAttackCardEffect();
            SetPrivateField(effect, "_damage", 0);
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            var target = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            bool raised = false;
            target.OnHpChanged += (_, __) => raised = true;

            effect.Execute(user, target);

            Assert.IsFalse(raised);
            Assert.AreEqual(30, target.CurrentHp);
        }

        [Test]
        public void BasicHealCardEffect_Execute_HealsUser_NotTarget()
        {
            var effect = new BasicHealCardEffect();
            SetPrivateField(effect, "_healAmount", 6);
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            user.ChangeHp(HpChangeType.TakeAttackDamage, -10);
            var target = new UnitStatusModel(maxHp: 30, maxStamina: 3);

            effect.Execute(user, target);

            Assert.AreEqual(26, user.CurrentHp);
            Assert.AreEqual(30, target.CurrentHp); // 対象側は動かない
            Assert.AreEqual(0, user.StatusEffects.Count); // 継続回復と違い状態効果は残さない
        }

        // 回復量の頭打ちはUnitStatusModel.ChangeHp側が見る
        [Test]
        public void BasicHealCardEffect_DoesNotHealAboveMaxHp()
        {
            var effect = new BasicHealCardEffect();
            SetPrivateField(effect, "_healAmount", 20);
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            user.ChangeHp(HpChangeType.TakeAttackDamage, -5);

            effect.Execute(user, user);

            Assert.AreEqual(30, user.CurrentHp);
        }

        [Test]
        public void BasicHealCardEffect_ZeroAmount_DoesNothing()
        {
            var effect = new BasicHealCardEffect();
            SetPrivateField(effect, "_healAmount", 0);
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            user.ChangeHp(HpChangeType.TakeAttackDamage, -10);
            bool raised = false;
            user.OnHpChanged += (_, __) => raised = true;

            effect.Execute(user, user);

            Assert.IsFalse(raised);
            Assert.AreEqual(20, user.CurrentHp);
        }

        // 継続回復はその場では回復せず、使用者に再生を付ける（毒と裏返しの関係）
        [Test]
        public void BasicRegenerationCardEffect_Execute_AddsRegenerationToUser_NotTarget()
        {
            var effect = new BasicRegenerationCardEffect();
            SetPrivateField(effect, "_regenerationAmount", 3);
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            user.ChangeHp(HpChangeType.TakeAttackDamage, -10);
            var target = new UnitStatusModel(maxHp: 30, maxStamina: 3);

            effect.Execute(user, target);

            Assert.AreEqual(1, user.StatusEffects.Count);
            Assert.AreEqual(0, target.StatusEffects.Count);
            Assert.IsInstanceOf<RegenerationStatusEffect>(user.StatusEffects[0]);
            Assert.AreEqual(3, user.StatusEffects[0].Value);
            Assert.AreEqual(20, user.CurrentHp); // 付与しただけでは回復しない
        }

        [Test]
        public void BasicRegenerationCardEffect_ZeroAmount_AddsNothing()
        {
            var effect = new BasicRegenerationCardEffect();
            SetPrivateField(effect, "_regenerationAmount", 0);
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);

            effect.Execute(user, user);

            Assert.AreEqual(0, user.StatusEffects.Count);
        }

        // 強化が乗るのは攻撃ダメージだけ。回復量まで増えないことを押さえておく
        [Test]
        public void BasicHealCardEffect_IsNotBoostedByStrength()
        {
            var effect = new BasicHealCardEffect();
            SetPrivateField(effect, "_healAmount", 5);
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            user.ChangeHp(HpChangeType.TakeAttackDamage, -20);
            user.AddStatusEffect(new StrengthStatusEffect(4));

            effect.Execute(user, user);

            Assert.AreEqual(15, user.CurrentHp); // 10 + 5（+4されない）
        }

        // どちら側のユニットを選べるかはカテゴリ基底で決まる。効果の適用先（target/user）とずれていると、
        // 「攻撃カードなのに自分しか選べない」といった形で操作フローが壊れる
        [Test]
        public void TargetsOpponent_IsTrue_ForEffectsThatActOnTarget()
        {
            Assert.IsTrue(new BasicAttackCardEffect().TargetsOpponent);
            Assert.IsTrue(new BasicPoisonCardEffect().TargetsOpponent);
        }

        [Test]
        public void TargetsOpponent_IsFalse_ForEffectsThatActOnUser()
        {
            Assert.IsFalse(new BasicBlockCardEffect().TargetsOpponent);
            Assert.IsFalse(new BasicStrengthCardEffect().TargetsOpponent);
            Assert.IsFalse(new BasicHealCardEffect().TargetsOpponent);
            Assert.IsFalse(new BasicRegenerationCardEffect().TargetsOpponent);
        }
    }
}

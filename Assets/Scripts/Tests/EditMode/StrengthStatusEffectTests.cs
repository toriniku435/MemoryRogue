using System.Reflection;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Models.Cards;
using Live2DCardRogue.Common.Statuses;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class StrengthStatusEffectTests
    {
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static BasicAttackCardEffect NewAttackEffect(int damage)
        {
            var effect = new BasicAttackCardEffect();
            SetPrivateField(effect, "_damage", damage);
            return effect;
        }

        [Test]
        public void IncreasesDealtAttackDamage()
        {
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            var target = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            user.AddStatusEffect(new StrengthStatusEffect(3));

            NewAttackEffect(6).Execute(user, target);

            Assert.AreEqual(21, target.CurrentHp); // 6 + 3
        }

        // 攻撃側に付くので、対象が何体いても攻撃1回につき1度だけ乗る
        [Test]
        public void AppliesOncePerAttack_RegardlessOfTarget()
        {
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            var firstTarget = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            var secondTarget = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            user.AddStatusEffect(new StrengthStatusEffect(2));

            var effect = NewAttackEffect(5);
            effect.Execute(user, firstTarget);
            effect.Execute(user, secondTarget);

            Assert.AreEqual(23, firstTarget.CurrentHp);
            Assert.AreEqual(23, secondTarget.CurrentHp);
        }

        // 攻撃ダメージ以外は素通し
        [Test]
        public void DoesNotAffectBlockOrPoison()
        {
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            var strength = new StrengthStatusEffect(3);

            Assert.AreEqual(5, strength.OnDealtValue(DealtValueType.Block, 5));
            Assert.AreEqual(5, strength.OnDealtValue(DealtValueType.Poison, 5));
        }

        // 受ける側には一切関与しない（相手の被ダメージ増加とは別物）
        [Test]
        public void DoesNotAffectDamageTaken()
        {
            var owner = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            owner.AddStatusEffect(new StrengthStatusEffect(5));

            owner.ChangeHp(HpChangeType.TakeAttackDamage, -10);

            Assert.AreEqual(20, owner.CurrentHp);
        }

        // 元のダメージが0なら強化バフだけでダメージを生み出さない
        [Test]
        public void ZeroBaseDamage_DealsNothing()
        {
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            var target = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            user.AddStatusEffect(new StrengthStatusEffect(5));

            NewAttackEffect(0).Execute(user, target);

            Assert.AreEqual(30, target.CurrentHp);
        }

        // 重ね掛けは大きい方を残して1増え（2026-10-04 のルール）、永続効果なのでターンをまたいでも消えない
        [Test]
        public void Duplicated_KeepsTheLargerPlusOne_AndSurvivesTurnCleanup()
        {
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            user.AddStatusEffect(new StrengthStatusEffect(1));
            user.AddStatusEffect(new StrengthStatusEffect(2));

            user.StartTurn();
            user.EndTurn();

            Assert.AreEqual(1, user.StatusEffects.Count);
            Assert.AreEqual(3, user.StatusEffects[0].Value);
        }

        // 後から弱い強化を掛けても、今の強化は下がらず1増える
        [Test]
        public void Duplicated_WithSmallerValue_DoesNotWeaken()
        {
            var user = new UnitStatusModel(maxHp: 30, maxStamina: 3);
            user.AddStatusEffect(new StrengthStatusEffect(3));
            user.AddStatusEffect(new StrengthStatusEffect(1));

            Assert.AreEqual(1, user.StatusEffects.Count);
            Assert.AreEqual(4, user.StatusEffects[0].Value);
        }
    }
}

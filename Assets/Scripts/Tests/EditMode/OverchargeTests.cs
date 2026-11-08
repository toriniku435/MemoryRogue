using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Models.Cards;
using Live2DCardRogue.Common.Statuses;
using NUnit.Framework;
using System.Reflection;

namespace Live2DCardRogue.Tests.EditMode
{
    // オーバーチャージ: 最大HPを超えた回復が溜まり、攻撃ダメージを増やし、ダメージをHPより先に（ブロックより後に）肩代わりする
    public sealed class OverchargeTests
    {
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{target.GetType().Name} に {fieldName} が見つかりません");
            field.SetValue(target, value);
        }

        private static UnitStatusModel CreatePlayer(int maxHp = 100)
        {
            return new UnitStatusModel(maxHp, 0) { CanOvercharge = true };
        }

        [Test]
        public void HealPastMaxHp_StoresTheExcess()
        {
            var player = CreatePlayer();
            player.ChangeHp(HpChangeType.TakeAttackDamage, -5);

            player.ChangeHp(HpChangeType.Heal, 12);

            Assert.AreEqual(100, player.CurrentHp);
            Assert.AreEqual(7, player.OverchargeValue);
        }

        [Test]
        public void Overcharge_IsCappedAtThirtyPercentOfMaxHp_AndStacks()
        {
            var player = CreatePlayer(200);

            player.ChangeHp(HpChangeType.Heal, 50);
            player.ChangeHp(HpChangeType.Heal, 50);

            Assert.AreEqual(60, player.OverchargeValue);
        }

        [Test]
        public void WithoutCanOvercharge_NothingIsStored()
        {
            var enemy = new UnitStatusModel(100, 0);

            enemy.ChangeHp(HpChangeType.Heal, 20);

            Assert.AreEqual(0, enemy.OverchargeValue);
            Assert.IsEmpty(enemy.StatusEffects);
        }

        [Test]
        public void ReversedHeal_StoresNothing()
        {
            var player = CreatePlayer();
            player.AddStatusEffect(new HealReversalStatusEffect(2));

            player.ChangeHp(HpChangeType.Heal, 10);

            Assert.AreEqual(90, player.CurrentHp);
            Assert.AreEqual(0, player.OverchargeValue);
        }

        [Test]
        public void EveryFiveStored_AddsOneAttackDamage()
        {
            var player = CreatePlayer();
            player.ChangeHp(HpChangeType.Heal, 14);

            Assert.AreEqual(10 + 2, player.CalculateDealtValue(DealtValueType.AttackDamage, 10));
            Assert.AreEqual(5, player.CalculateDealtValue(DealtValueType.Block, 5), "攻撃ダメージ以外には乗らない");
        }

        [Test]
        public void Damage_IsTakenByBlockFirst_ThenOvercharge_ThenHp()
        {
            var player = CreatePlayer();
            player.ChangeHp(HpChangeType.Heal, 10);
            player.AddStatusEffect(new BlockStatusEffect(4));

            player.ChangeHp(HpChangeType.TakeAttackDamage, -20);

            Assert.AreEqual(0, player.BlockValue);
            Assert.AreEqual(0, player.OverchargeValue);
            Assert.AreEqual(94, player.CurrentHp);
        }

        [Test]
        public void PoisonDamage_IsAlsoTakenByOvercharge()
        {
            var player = CreatePlayer();
            player.ChangeHp(HpChangeType.Heal, 10);

            player.ChangeHp(HpChangeType.TakePoisonDamage, -4);

            Assert.AreEqual(6, player.OverchargeValue);
            Assert.AreEqual(100, player.CurrentHp);
        }

        [Test]
        public void InstantKill_IgnoresOvercharge()
        {
            var player = CreatePlayer();
            player.ChangeHp(HpChangeType.Heal, 10);

            player.ChangeHp(HpChangeType.InstantKill, -player.CurrentHp);

            Assert.AreEqual(0, player.CurrentHp);
        }

        [Test]
        public void SpentOvercharge_IsRemovedAtTheNextCleanup()
        {
            var player = CreatePlayer();
            player.ChangeHp(HpChangeType.Heal, 3);
            player.ChangeHp(HpChangeType.TakeAttackDamage, -3);

            player.EndTurn();

            Assert.IsFalse(player.StatusEffects.Exists(e => e.Id == OverchargeStatusEffect.EffectId));
        }

        // ターン終了のフックの中で一覧に効果が加わっても、列挙が壊れない（2026-11-07 に見つかった例外の再発防止）
        [Test]
        public void RegenerationAtFullHp_OnTurnEnd_StoresOvercharge_WithoutThrowing()
        {
            var player = CreatePlayer();
            player.AddStatusEffect(new RegenerationStatusEffect(5));

            Assert.DoesNotThrow(() => player.EndTurn());

            Assert.AreEqual(5, player.OverchargeValue);
        }

        [Test]
        public void HealCardAtFullHp_ThenAttack_DealsTheBonus()
        {
            var player = CreatePlayer();
            var enemy = new UnitStatusModel(100, 0);
            var heal = new BasicHealCardEffect();
            SetPrivateField(heal, "_healAmount", 25);
            var attack = new BasicAttackCardEffect();
            SetPrivateField(attack, "_damage", 6);

            heal.Execute(player, player);
            attack.Execute(player, enemy);

            Assert.AreEqual(25, player.OverchargeValue);
            Assert.AreEqual(100 - (6 + 5), enemy.CurrentHp);
        }
    }
}

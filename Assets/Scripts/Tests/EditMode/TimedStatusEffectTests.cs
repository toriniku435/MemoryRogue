using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Statuses;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    // ターン数で切れる状態効果（TimedStatusEffectBase とその派生）。
    // 数えるのは「効いたまま始まった持ち主のターン」だけ、という決まりをここで固定する
    public sealed class TimedStatusEffectTests
    {
        private static UnitStatusModel NewUnit(int maxStamina = 10) => new UnitStatusModel(maxHp: 50, maxStamina: maxStamina);

        // 付与されたターンは数えないので、2ターンの効果は次の持ち主のターンから2ターン残る
        [Test]
        public void Duration_DoesNotCountTheTurnItWasAppliedIn()
        {
            var unit = NewUnit();
            unit.StartTurn();
            unit.AddStatusEffect(new MemoryBoostStatusEffect(turns: 2, amount: 3));
            unit.EndTurn(); // 付与したターン

            Assert.AreEqual(1, unit.StatusEffects.Count);
            Assert.AreEqual(2, unit.StatusEffects[0].Value);

            unit.StartTurn();
            unit.EndTurn();
            Assert.AreEqual(1, unit.StatusEffects[0].Value);

            unit.StartTurn();
            unit.EndTurn();
            Assert.AreEqual(0, unit.StatusEffects.Count);
        }

        // 相手のターン中に付けられた効果も、次の持ち主のターンの終わりで1つ減る
        [Test]
        public void Duration_AppliedOutsideOwnersTurn_ExpiresAfterOwnersNextTurn()
        {
            var unit = NewUnit();
            unit.AddStatusEffect(new StunStatusEffect(turns: 1));

            unit.StartTurn();
            Assert.AreEqual(0, unit.CalculateLimit(StatLimitType.ActionCount, 2)); // このターンは行動できない
            unit.EndTurn();

            Assert.AreEqual(0, unit.StatusEffects.Count);
            Assert.AreEqual(2, unit.CalculateLimit(StatLimitType.ActionCount, 2));
        }

        // 重ねがけは長い方を残すだけで、足し合わせない（停止を重ねて永久に止められないように）
        [Test]
        public void OnDuplicated_KeepsTheLongerDuration_WithoutAdding()
        {
            var unit = NewUnit();
            unit.AddStatusEffect(new StunStatusEffect(turns: 1));
            unit.AddStatusEffect(new StunStatusEffect(turns: 1));
            Assert.AreEqual(1, unit.StatusEffects[0].Value);

            unit.AddStatusEffect(new StunStatusEffect(turns: 3));
            Assert.AreEqual(3, unit.StatusEffects[0].Value);
        }

        [Test]
        public void MemoryBoost_RaisesMaxStamina_WhilePresent()
        {
            var unit = NewUnit(maxStamina: 10);
            unit.AddStatusEffect(new MemoryBoostStatusEffect(turns: 1, amount: 4));

            Assert.AreEqual(14, unit.MaxStamina);
            Assert.AreEqual(10, unit.BaseMaxStamina);
        }

        // 増設が切れたら、今のスタミナも元の最大値に収まる
        [Test]
        public void MemoryBoost_Expiring_ClampsStaminaToTheBaseMax()
        {
            var unit = NewUnit(maxStamina: 10);
            unit.AddStatusEffect(new MemoryBoostStatusEffect(turns: 1, amount: 4));
            unit.RestoreStamina();
            Assert.AreEqual(14, unit.Stamina);

            int notified = -1;
            unit.OnStaminaChanged += stamina => notified = stamina;
            unit.StartTurn();
            unit.EndTurn();

            Assert.AreEqual(10, unit.MaxStamina);
            Assert.AreEqual(10, unit.Stamina);
            Assert.AreEqual(10, notified);
        }

        // 増える効果と減る効果は別の効果なので、両方付けば差し引きになる
        [Test]
        public void HandSizeUpAndDown_Stack_AsANetChange()
        {
            var unit = NewUnit();
            unit.AddStatusEffect(new HandSizeUpStatusEffect(turns: 2, amount: 2));
            unit.AddStatusEffect(new HandSizeDownStatusEffect(turns: 2, amount: 3));

            Assert.AreEqual(4, unit.CalculateLimit(StatLimitType.HandSize, 5));
        }

        [Test]
        public void HealReversal_TurnsHealingIntoDamage()
        {
            var unit = NewUnit();
            unit.ChangeHp(HpChangeType.TakeAttackDamage, -20); // 30
            unit.AddStatusEffect(new HealReversalStatusEffect(turns: 1));

            unit.ChangeHp(HpChangeType.Heal, 6);

            Assert.AreEqual(24, unit.CurrentHp);
        }

        // 反転するのは回復だけ。ダメージはそのまま
        [Test]
        public void HealReversal_LeavesDamageAlone()
        {
            var unit = NewUnit();
            unit.AddStatusEffect(new HealReversalStatusEffect(turns: 1));

            unit.ChangeHp(HpChangeType.TakeAttackDamage, -5);

            Assert.AreEqual(45, unit.CurrentHp);
        }

        // 再生（継続回復）も同じ Heal を通るので、切れるそのターンの終わりの分まで反転する
        [Test]
        public void HealReversal_AlsoReversesRegenerationTick_OnItsLastTurn()
        {
            var unit = NewUnit();
            unit.AddStatusEffect(new HealReversalStatusEffect(turns: 1));
            unit.AddStatusEffect(new RegenerationStatusEffect(3));

            unit.StartTurn();
            unit.EndTurn();

            Assert.AreEqual(47, unit.CurrentHp);
        }
    }
}

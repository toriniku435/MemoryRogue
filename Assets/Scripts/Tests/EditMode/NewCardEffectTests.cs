using System.Reflection;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Models.Cards;
using Live2DCardRogue.Common.Statuses;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    // 貫通・HP比例・HPの代償・全体攻撃・ターン数で切れる効果を付けるカード効果
    public sealed class NewCardEffectTests
    {
        // 各効果の数値は private [SerializeField] のみ（Inspector 専用）なので、リフレクションで入れる
        private static T NewEffect<T>(string field, int value) where T : new()
        {
            var effect = new T();
            typeof(T).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(effect, value);
            return effect;
        }

        private static UnitStatusModel NewUnit(int maxHp = 40) => new UnitStatusModel(maxHp: maxHp, maxStamina: 10);

        [Test]
        public void PiercingAttack_IgnoresBlock()
        {
            var user = NewUnit();
            var target = NewUnit();
            target.AddStatusEffect(new BlockStatusEffect(10));

            NewEffect<PiercingAttackCardEffect>("_damage", 4).Execute(user, target);

            Assert.AreEqual(36, target.CurrentHp);
            Assert.AreEqual(10, target.StatusEffects[0].Value); // ブロックは減らない
        }

        // 貫通しても、与える側の強化は乗る
        [Test]
        public void PiercingAttack_StillGetsStrength()
        {
            var user = NewUnit();
            user.AddStatusEffect(new StrengthStatusEffect(2));
            var target = NewUnit();

            NewEffect<PiercingAttackCardEffect>("_damage", 4).Execute(user, target);

            Assert.AreEqual(34, target.CurrentHp);
        }

        [Test]
        public void HpRatioAttack_DealsPercentOfUsersCurrentHp_RoundedDown()
        {
            var user = NewUnit(maxHp: 50);
            user.ChangeHp(HpChangeType.TakeAttackDamage, -11); // 39
            var target = NewUnit();

            NewEffect<HpRatioAttackCardEffect>("_percent", 25).Execute(user, target);

            Assert.AreEqual(31, target.CurrentHp); // 39 * 25% = 9.75 → 9
        }

        [Test]
        public void HpRatioBlock_GivesPercentOfUsersCurrentHpAsBlock()
        {
            var user = NewUnit(maxHp: 40);

            NewEffect<HpRatioBlockCardEffect>("_percent", 25).Execute(user, NewUnit());

            Assert.AreEqual(10, user.StatusEffects[0].Value);
        }

        // 代償のHPはブロックでは減らせない
        [Test]
        public void PayHp_IsNotAbsorbedByBlock()
        {
            var user = NewUnit();
            user.AddStatusEffect(new BlockStatusEffect(10));

            NewEffect<PayHpCardEffect>("_amount", 8).Execute(user, user);

            Assert.AreEqual(32, user.CurrentHp);
        }

        // 代償で倒れることはない（HPは1で止まる）
        [Test]
        public void PayHp_NeverDropsBelowOne()
        {
            var user = NewUnit();
            user.ChangeHp(HpChangeType.TakeAttackDamage, -35); // 5

            NewEffect<PayHpCardEffect>("_amount", 8).Execute(user, user);

            Assert.AreEqual(1, user.CurrentHp);
        }

        [Test]
        public void AreaAttack_TargetsAllOpponents_OtherEffectsDoNot()
        {
            Assert.IsTrue(new AreaAttackCardEffect().TargetsAllOpponents);
            Assert.IsTrue(new AreaAttackCardEffect().TargetsOpponent);
            Assert.IsFalse(new BasicAttackCardEffect().TargetsAllOpponents);
        }

        [Test]
        public void StunCard_PutsStunOnTheTarget()
        {
            var user = NewUnit();
            var target = NewUnit();

            NewEffect<StunCardEffect>("_turns", 1).Execute(user, target);

            Assert.AreEqual(0, user.StatusEffects.Count);
            Assert.IsInstanceOf<StunStatusEffect>(target.StatusEffects[0]);
        }

        [Test]
        public void HealReversalCard_PutsTheEffectOnTheTarget()
        {
            var user = NewUnit();
            var target = NewUnit();

            NewEffect<HealReversalCardEffect>("_turns", 1).Execute(user, target);

            Assert.IsInstanceOf<HealReversalStatusEffect>(target.StatusEffects[0]);
            Assert.IsFalse(new HealReversalCardEffect().TargetsAllOpponents);
        }

        // 手札を減らす効果は、大きな効果の代償として使用者自身に付く
        [Test]
        public void HandSizeDownCard_PutsTheEffectOnTheUser()
        {
            var user = NewUnit();
            var target = NewUnit();
            var effect = NewEffect<HandSizeDownCardEffect>("_turns", 2);
            typeof(HandSizeDownCardEffect).GetField("_amount", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(effect, 2);

            effect.Execute(user, target);

            Assert.AreEqual(3, user.CalculateLimit(StatLimitType.HandSize, 5));
            Assert.AreEqual(0, target.StatusEffects.Count);
            Assert.IsFalse(effect.TargetsOpponent);
        }
    }
}

using System.Collections.Generic;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.BattleScene.UseCase;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Statuses;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    // 対象の決め方: クリックだけで使うときの狙い先と、倒れていたときの振り直し。トークンの払い戻しも
    public sealed class BattleTargetResolverTests
    {
        private static UnitStatusModel NewUnit() => new UnitStatusModel(maxHp: 30, maxStamina: 0);

        private static void Defeat(UnitStatusModel unit)
        {
            unit.ChangeHp(HpChangeType.TakePiercingDamage, -unit.CurrentHp);
        }

        [Test]
        public void FindFrontOpponent_PrefersThePriorityTargetWhileItLives()
        {
            var player = NewUnit();
            var front = NewUnit();
            var back = NewUnit();
            var living = new List<UnitStatusModel> { front, back };
            var resolver = new BattleTargetResolver(player, null, () => living, () => back);

            Assert.AreSame(back, resolver.FindFrontOpponent());

            living.Remove(back);
            Assert.AreSame(front, resolver.FindFrontOpponent());
        }

        [Test]
        public void FindFrontOpponent_IsNullWithoutLivingOpponents()
        {
            var resolver = new BattleTargetResolver(NewUnit(), null, () => new List<UnitStatusModel>(), null);
            Assert.IsNull(resolver.FindFrontOpponent());
        }

        [Test]
        public void Resolve_ReaimsADefeatedTarget_AndKeepsItWhenThereIsNoAlternative()
        {
            var player = NewUnit();
            var defeated = NewUnit();
            var next = NewUnit();
            Defeat(defeated);

            var withAlternative = new BattleTargetResolver(player, _ => next, null, null);
            Assert.AreSame(next, withAlternative.Resolve(defeated));

            var withoutAlternative = new BattleTargetResolver(player, _ => null, null, null);
            Assert.AreSame(defeated, withoutAlternative.Resolve(defeated));

            Assert.AreSame(next, withAlternative.Resolve(next));
        }

        [Test]
        public void AllLivingUnits_PutsThePlayerFirst()
        {
            var player = NewUnit();
            var enemy = NewUnit();
            var resolver = new BattleTargetResolver(player, null, () => new List<UnitStatusModel> { enemy }, null);

            CollectionAssert.AreEqual(new[] { player, enemy }, resolver.AllLivingUnits(enemy));
        }

        // 敵の並びを聞けないとき（テストなど）は、対象だけを足す
        [Test]
        public void AllLivingUnits_FallsBackToTheTargetWithoutTheEnemyList()
        {
            var player = NewUnit();
            var enemy = NewUnit();
            var resolver = new BattleTargetResolver(player, null, null, null);

            CollectionAssert.AreEqual(new[] { player, enemy }, resolver.AllLivingUnits(enemy));
            CollectionAssert.AreEqual(new[] { player }, resolver.AllLivingUnits(player));
        }

        [Test]
        public void TokenModel_PaysRefundsAndCountsWhatWasSpent()
        {
            var wallet = new BattleTokenModel(100);
            Assert.IsTrue(wallet.CanPay(100));
            Assert.IsFalse(wallet.CanPay(101));

            wallet.Pay(60);
            Assert.AreEqual(40, wallet.Tokens);
            Assert.AreEqual(60, wallet.Spent);

            wallet.Refund(60);
            Assert.AreEqual(100, wallet.Tokens);
            Assert.AreEqual(0, wallet.Spent);

            Assert.AreEqual(0, new BattleTokenModel(-5).Tokens);
        }
    }
}

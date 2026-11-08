using Live2DCardRogue.Common.Models;
using NUnit.Framework;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class UnitUpgradeModelTests
    {
        // 計算そのものを確かめるための決まり（アセットの調整に左右されないよう、ここで作る）
        private static readonly UnitUpgradeRule Rule = new UnitUpgradeRule(
            hpStep: 20, hpLimit: 300, hpBaseCost: 40, hpCostPerLevel: 10,
            memoryStep: 1, memoryLimit: 15, memoryBaseCost: 60, memoryCostPerLevel: 40);

        private static UnitUpgradeModel NewModel(int tokens, int hp = 100, int memory = 10)
        {
            return new UnitUpgradeModel(tokens, hp, memory, baseHp: 100, baseMemory: 10, Rule);
        }

        [Test]
        public void UpgradeHp_PaysTheCostAndRaisesByOneStep()
        {
            var model = NewModel(1000);
            int cost = model.HpCost;

            Assert.IsTrue(model.TryUpgradeHp());

            Assert.AreEqual(1000 - cost, model.Tokens);
            Assert.AreEqual(100 + Rule.HpStep, model.Hp);
            Assert.AreEqual(1, model.HpLevel);
        }

        // 段が上がるほど高くなる
        [Test]
        public void Cost_RisesWithTheLevel()
        {
            var model = NewModel(10000);
            int first = model.HpCost;
            model.TryUpgradeHp();

            Assert.Greater(model.HpCost, first);
            Assert.AreEqual(Rule.HpBaseCost + Rule.HpCostPerLevel, model.HpCost);
        }

        [Test]
        public void CannotUpgrade_WithoutEnoughTokens()
        {
            var model = NewModel(Rule.HpBaseCost - 1);

            Assert.IsFalse(model.CanUpgradeHp);
            Assert.IsFalse(model.TryUpgradeHp());
            Assert.AreEqual(100, model.Hp);
            Assert.AreEqual(Rule.HpBaseCost - 1, model.Tokens);
        }

        // 上限（HP300・メモリー15）で止まる
        [Test]
        public void StopsAtTheLimits()
        {
            var model = NewModel(100000);
            while (model.TryUpgradeHp()) { }
            while (model.TryUpgradeMemory()) { }

            Assert.AreEqual(Rule.HpLimit, model.Hp);
            Assert.AreEqual(Rule.MemoryLimit, model.Memory);
            Assert.IsTrue(model.IsHpMaxed);
            Assert.IsTrue(model.IsMemoryMaxed);
            Assert.AreEqual(model.HpMaxLevel, model.HpLevel);
            Assert.AreEqual(model.MemoryMaxLevel, model.MemoryLevel);
        }

        // 全段の合計（バランスの前提。値段を変えたらここも見直す）
        [Test]
        public void TotalCostToMaxBoth_Is1550()
        {
            var model = NewModel(100000);
            while (model.TryUpgradeHp()) { }
            while (model.TryUpgradeMemory()) { }

            Assert.AreEqual(100000 - 850 - 700, model.Tokens);
        }

        // 強化済みの状態から始めても、段は強化していない値から数える
        [Test]
        public void Level_CountsFromTheBaseValues()
        {
            var model = NewModel(1000, hp: 160, memory: 12);

            Assert.AreEqual(3, model.HpLevel);
            Assert.AreEqual(2, model.MemoryLevel);
            Assert.AreEqual(Rule.HpBaseCost + Rule.HpCostPerLevel * 3, model.HpCost);
        }

        [Test]
        public void OnChanged_FiresOnlyWhenUpgraded()
        {
            var model = NewModel(Rule.HpBaseCost);
            int changed = 0;
            model.OnChanged += () => changed++;

            model.TryUpgradeMemory(); // 払えない
            model.TryUpgradeHp();

            Assert.AreEqual(1, changed);
        }

        // 上げた分は仮のもので、確定した値は反映するまで変わらない
        [Test]
        public void Upgrade_IsPendingUntilApplied()
        {
            var model = NewModel(1000);
            Assert.IsFalse(model.IsDirty);

            model.TryUpgradeHp();

            Assert.IsTrue(model.IsDirty);
            Assert.AreEqual(1000, model.AppliedTokens);
            Assert.AreEqual(100, model.AppliedHp);
            Assert.AreEqual(1000 - Rule.HpBaseCost, model.Tokens);
        }

        [Test]
        public void Apply_CommitsThePendingUpgrades()
        {
            var model = NewModel(1000);
            model.TryUpgradeHp();
            model.TryUpgradeMemory();

            Assert.IsTrue(model.Apply());

            Assert.IsFalse(model.IsDirty);
            Assert.AreEqual(model.Tokens, model.AppliedTokens);
            Assert.AreEqual(100 + Rule.HpStep, model.AppliedHp);
            Assert.AreEqual(10 + Rule.MemoryStep, model.AppliedMemory);
        }

        [Test]
        public void Apply_WithNothingPending_ReturnsFalse()
        {
            Assert.IsFalse(NewModel(1000).Apply());
        }

        // 破棄すると、払ったトークンも含めて確定した値に戻る（次の1段の値段も戻る）
        [Test]
        public void Discard_ReturnsToTheAppliedValues()
        {
            var model = NewModel(1000);
            model.TryUpgradeHp();
            model.TryUpgradeHp();

            model.Discard();

            Assert.IsFalse(model.IsDirty);
            Assert.AreEqual(1000, model.Tokens);
            Assert.AreEqual(100, model.Hp);
            Assert.AreEqual(Rule.HpBaseCost, model.HpCost);
        }

        // 外でトークンが変わっても、仮に払っている分は差し引いたまま
        [Test]
        public void SetTokens_KeepsThePendingCost()
        {
            var model = NewModel(1000);
            model.TryUpgradeHp();

            model.SetTokens(500);

            Assert.AreEqual(500, model.AppliedTokens);
            Assert.AreEqual(500 - Rule.HpBaseCost, model.Tokens);
        }
    }
}

using System;
using System.Linq;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEditor;

namespace Live2DCardRogue.Tests.EditMode
{
    public sealed class CampaignResultCalculatorTests
    {
        // 計算そのものを確かめるための決まり（アセットの調整に左右されないよう、ここで作る）
        private static CampaignScoreRule Rule()
        {
            return new CampaignScoreRule(1, 1000, new[] { 0, 150, 350, 600, 900, 1200, 1600, 2000 }, CampaignRank.S, CampaignRank.B);
        }

        private static CampaignBalance LoadBalance()
        {
            return AssetDatabase.LoadAssetAtPath<CampaignBalance>("Assets/ScriptableObjects/CampaignBalance.asset");
        }

        [Test]
        public void Calculate_AddsTokensAndSubtractsBackupBoosts()
        {
            var result = CampaignResultCalculator.Calculate(remainingTokens: 1500, backupBoostCount: 1, Rule());

            Assert.AreEqual(1500, result.TokenPoints);
            Assert.AreEqual(1000, result.BackupPenalty);
            Assert.AreEqual(500, result.Score);
            Assert.AreEqual(1500, result.RemainingTokens);
            Assert.AreEqual(1, result.BackupBoostCount);
        }

        [Test]
        public void Calculate_NeverGoesBelowZero()
        {
            var result = CampaignResultCalculator.Calculate(remainingTokens: 100, backupBoostCount: 5, Rule());

            Assert.AreEqual(0, result.Score);
            Assert.AreEqual(CampaignRank.E, result.Rank);
        }

        // 各ランクの最低スコアちょうどでそのランク、1点足りなければ1つ下
        [Test]
        public void RankOf_UsesTheThresholds()
        {
            var rule = Rule();
            foreach (CampaignRank rank in Enum.GetValues(typeof(CampaignRank)))
            {
                int min = rule.MinScoreOf(rank);
                Assert.AreEqual(rank, rule.RankOf(min), $"{rank} at {min}");
                if (rank != CampaignRank.E)
                {
                    Assert.AreEqual(rank - 1, rule.RankOf(min - 1), $"{rank} at {min - 1}");
                }
            }
        }

        // アセットの閾値も、E が 0 で上のランクほど大きい（OnValidate が守るが、手で書き換えたときの念のため）
        [Test]
        public void BalanceThresholds_RiseWithTheRank()
        {
            var rule = LoadBalance().CreateScoreRule();
            var ranks = (CampaignRank[])Enum.GetValues(typeof(CampaignRank));
            Assert.AreEqual(0, rule.MinScoreOf(CampaignRank.E));
            for (int i = 1; i < ranks.Length; i++)
            {
                Assert.Greater(rule.MinScoreOf(ranks[i]), rule.MinScoreOf(ranks[i - 1]));
            }
        }

        [TestCase(CampaignRank.E, EndingRoute.Bad)]
        [TestCase(CampaignRank.C, EndingRoute.Bad)]
        [TestCase(CampaignRank.B, EndingRoute.Normal)]
        [TestCase(CampaignRank.A, EndingRoute.Normal)]
        [TestCase(CampaignRank.S, EndingRoute.Best)]
        [TestCase(CampaignRank.SSS, EndingRoute.Best)]
        public void RouteOf_SplitsIntoThreeRoutes(CampaignRank rank, EndingRoute expected)
        {
            Assert.AreEqual(expected, Rule().RouteOf(rank));
        }

        // エンディングの分岐はリザルトより前に決まっている（結果にルートが入っている）
        [Test]
        public void Calculate_IncludesTheRouteForTheRank()
        {
            var result = CampaignResultCalculator.Calculate(remainingTokens: 2500, backupBoostCount: 0, Rule());

            Assert.AreEqual(CampaignRank.SSS, result.Rank);
            Assert.AreEqual(EndingRoute.Best, result.Route);
        }

        [Test]
        public void Calculate_TreatsNegativeInputAsZero()
        {
            var result = CampaignResultCalculator.Calculate(remainingTokens: -10, backupBoostCount: -3, Rule());

            Assert.AreEqual(0, result.RemainingTokens);
            Assert.AreEqual(0, result.BackupBoostCount);
            Assert.AreEqual(0, result.Score);
        }

        [Test]
        public void ScoreRule_RejectsTheWrongNumberOfThresholds()
        {
            Assert.Throws<ArgumentException>(() => new CampaignScoreRule(1, 1000, new[] { 0, 100 }, CampaignRank.S, CampaignRank.B));
        }
    }
}

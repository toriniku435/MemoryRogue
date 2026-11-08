using System.Collections.Generic;
using System.Linq;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.ScriptableObjects;
using NUnit.Framework;
using UnityEditor;

namespace Live2DCardRogue.Tests.EditMode
{
    // バランスのアセット（難易度ごとに1つ）それぞれが、自分のステージと数値で成り立っているかを確かめる。
    //   - SSS はぎりぎり届く位置に置いてある。初期トークンやステージの報酬を減らすと、気付かないうちに SSS が取れなくなる
    //   - わざと負けて支援のトークンを稼いでも、スコアは必ず下がる
    public sealed class CampaignRankReachabilityTests
    {
        private static IEnumerable<CampaignBalance> AllBalances()
        {
            return AssetDatabase.FindAssets("t:CampaignBalance")
                .Select(guid => AssetDatabase.LoadAssetAtPath<CampaignBalance>(AssetDatabase.GUIDToAssetPath(guid)));
        }

        // スコアの決まりはバランスのアセットごと、ステージはエピソードごとなので、その組み合わせをすべて見る
        private static IEnumerable<(CampaignBalance balance, CampaignData campaign, string label)> AllEpisodes()
        {
            foreach (var balance in AllBalances())
                for (int i = 0; i < balance.EpisodeCount; i++)
                    if (balance.HasEpisode(i)) yield return (balance, balance.GetEpisode(i), $"{balance.name} エピソード{i + 1}");
        }

        [Test]
        public void EveryBalance_HasStages()
        {
            var balances = AllBalances().ToList();
            Assert.IsNotEmpty(balances, "CampaignBalance のアセットが無い");
            foreach (var balance in balances)
            {
                Assert.Greater(balance.EpisodeCount, 0, balance.name + " にエピソード（CampaignData）が設定されていない");
                for (int i = 0; i < balance.EpisodeCount; i++)
                    Assert.IsTrue(balance.HasEpisode(i), $"{balance.name} のエピソード{i + 1}が空欄か、ステージが空");
            }
        }

        [Test]
        public void MaxPossibleScore_ReachesSss()
        {
            foreach (var (balance, campaign, label) in AllEpisodes())
            {
                // 全ステージに勝ったときの報酬（一度も負けず、一切使わない場合が最大）。
                // 最終ステージの報酬は受け取らない（勝ったらそのままエンディングへ進む）ので数えない
                var stages = campaign.CreateStages();
                int rewards = stages.Take(stages.Count - 1).Sum(stage => stage.CreateReward(isVictory: true).Tokens);
                var rule = balance.CreateScoreRule();
                int maxScore = CampaignResultCalculator.Calculate(balance.StartTokens + rewards, 0, rule).Score;
                int sss = rule.MinScoreOf(CampaignRank.SSS);

                Assert.GreaterOrEqual(maxScore, sss, $"{label}: 取れる最大のスコア {maxScore}（初期{balance.StartTokens} + 報酬{rewards}）が SSS の {sss} に届かない");
            }
        }

        [Test]
        public void BackupBoost_AlwaysCostsMoreThanItsTokensGive()
        {
            foreach (var (balance, campaign, label) in AllEpisodes())
            {
                var rule = balance.CreateScoreRule();
                int largestBackupTokens = campaign.CreateStages().Max(stage => stage.CreateReward(isVictory: false).Tokens);

                Assert.Greater(rule.PenaltyPerBackupBoost, largestBackupTokens * rule.PointsPerToken,
                    $"{label}: バックアップブースト1回の減点が、支援のトークン（最大{largestBackupTokens}）以下（わざと負けると得をする）");
            }
        }
    }
}

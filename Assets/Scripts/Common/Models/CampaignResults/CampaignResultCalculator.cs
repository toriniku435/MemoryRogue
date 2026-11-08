using System;

namespace Live2DCardRogue.Common.Models
{
    // キャンペーンの成績を計算する。スコア = 残りトークン × PointsPerToken − バックアップブースト回数 × PenaltyPerBackupBoost（0未満にはしない）。
    // 係数・ランクの閾値・エンディングの分かれ目は CampaignScoreRule（元は CampaignBalance アセット）が持つ
    public static class CampaignResultCalculator
    {
        public static CampaignResult Calculate(int remainingTokens, int backupBoostCount, CampaignScoreRule rule)
        {
            remainingTokens = Math.Max(0, remainingTokens);
            backupBoostCount = Math.Max(0, backupBoostCount);

            // 桁あふれしないよう long で計算してから収める
            long tokenPoints = (long)remainingTokens * rule.PointsPerToken;
            long penalty = (long)backupBoostCount * rule.PenaltyPerBackupBoost;
            int score = (int)Math.Clamp(tokenPoints - penalty, 0, int.MaxValue);

            var rank = rule.RankOf(score);
            return new CampaignResult(
                remainingTokens,
                backupBoostCount,
                (int)Math.Min(tokenPoints, int.MaxValue),
                (int)Math.Min(penalty, int.MaxValue),
                score,
                rank,
                rule.RouteOf(rank));
        }
    }
}

using System;
using System.Collections.Generic;

namespace Live2DCardRogue.Common.Models
{
    // キャンペーンの成績の決まり（スコアの係数・ランクの閾値・エンディングの分かれ目）。
    // 数値は CampaignBalance（ScriptableObject）が持ち、CreateScoreRule でこの形にして渡す
    // （Model は UnityEngine に依存しないので、アセットそのものは受け取らない）
    public sealed class CampaignScoreRule
    {
        private readonly int[] _minScores;

        public int PointsPerToken { get; }
        public int PenaltyPerBackupBoost { get; }

        /// <summary>このランク以上なら Best ルート</summary>
        public CampaignRank BestRouteFrom { get; }

        /// <summary>このランク以上なら Normal ルート（それ未満は Bad）</summary>
        public CampaignRank NormalRouteFrom { get; }

        /// <param name="minScores">ランクごとの最低スコア。CampaignRank の並び（E, D, … SSS）で、ランクの数だけ</param>
        public CampaignScoreRule(int pointsPerToken, int penaltyPerBackupBoost, IReadOnlyList<int> minScores, CampaignRank bestRouteFrom, CampaignRank normalRouteFrom)
        {
            int rankCount = Enum.GetValues(typeof(CampaignRank)).Length;
            if (minScores == null || minScores.Count != rankCount)
            {
                throw new ArgumentException($"ランクの閾値は {rankCount} 個必要です（E から SSS の順）", nameof(minScores));
            }

            PointsPerToken = pointsPerToken;
            PenaltyPerBackupBoost = penaltyPerBackupBoost;
            _minScores = new int[rankCount];
            for (int i = 0; i < rankCount; i++) _minScores[i] = minScores[i];
            BestRouteFrom = bestRouteFrom;
            NormalRouteFrom = normalRouteFrom;
        }

        /// <summary>そのランクに必要な最低スコア</summary>
        public int MinScoreOf(CampaignRank rank)
        {
            return _minScores[(int)rank];
        }

        public CampaignRank RankOf(int score)
        {
            for (int i = _minScores.Length - 1; i > 0; i--)
            {
                if (score >= _minScores[i]) return (CampaignRank)i;
            }
            return CampaignRank.E;
        }

        public EndingRoute RouteOf(CampaignRank rank)
        {
            if (rank >= BestRouteFrom) return EndingRoute.Best;
            if (rank >= NormalRouteFrom) return EndingRoute.Normal;
            return EndingRoute.Bad;
        }
    }
}

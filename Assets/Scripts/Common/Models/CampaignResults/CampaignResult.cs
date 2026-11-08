namespace Live2DCardRogue.Common.Models
{
    // キャンペーン1回分の成績。CampaignResultCalculator が組み立て、エンディング → リザルトへそのまま渡る。
    // エンディングの分岐もリザルトの表示もこれを読むだけで、計算はしない（計算を1箇所に閉じ込めるため）
    public sealed class CampaignResult
    {
        /// <summary>クリア時に残っていたトークン</summary>
        public int RemainingTokens { get; }

        /// <summary>バックアップブースト（敗北時の支援）を受けた回数</summary>
        public int BackupBoostCount { get; }

        /// <summary>残りトークンによる加点</summary>
        public int TokenPoints { get; }

        /// <summary>バックアップブーストによる減点（正の数）</summary>
        public int BackupPenalty { get; }

        /// <summary>加点 − 減点。0未満にはならない</summary>
        public int Score { get; }

        public CampaignRank Rank { get; }

        public EndingRoute Route { get; }

        public CampaignResult(int remainingTokens, int backupBoostCount, int tokenPoints, int backupPenalty, int score, CampaignRank rank, EndingRoute route)
        {
            RemainingTokens = remainingTokens;
            BackupBoostCount = backupBoostCount;
            TokenPoints = tokenPoints;
            BackupPenalty = backupPenalty;
            Score = score;
            Rank = rank;
            Route = route;
        }
    }
}

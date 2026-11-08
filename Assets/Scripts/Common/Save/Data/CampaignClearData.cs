using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.Save
{
    // キャンペーンをクリアした記録（clear.json）。クリアするたびに1件ずつ増える
    [Serializable]
    public sealed class CampaignClearData
    {
        // 2: CampaignClearRecord.episodeIndex を足した。1 の記録には無い（0 = エピソード1 として読める。当時はそれしか無かった）
        // 3: endlessBestLayer（エンドレスの最高到達階層）を足した。2 以前は 0（まだ遊んでいない）として読める
        // 4: discoveredCommands（一度でも手に入れたコマンド）を足した。3 以前は空（まだ何も記録していない）として読める
        // 5: discoveredEnemies（一度でも戦った敵）を足した。4 以前は空として読める
        // 6: challengeRecords（チャレンジごとの最高到達階層と踏破）を足した。5 以前の endlessBestLayer はエンドレスの記録として読み替える
        public const int CurrentVersion = 6;

        public int version = CurrentVersion;
        public List<CampaignClearRecord> clears = new List<CampaignClearRecord>();

        /// <summary>エンドレスで到達した一番深い階層（版 3〜5）。版 6 からは challengeRecords に移す（読むときに読み替える）</summary>
        public int endlessBestLayer;

        /// <summary>チャレンジごとの記録。版 6 から</summary>
        public List<ChallengeRecord> challengeRecords = new List<ChallengeRecord>();

        /// <summary>そのチャレンジ（ChallengeType の名前）の記録。まだ無ければ null</summary>
        public ChallengeRecord FindChallenge(string type)
        {
            if (challengeRecords == null) return null;
            foreach (var record in challengeRecords)
            {
                if (record != null && record.type == type) return record;
            }
            return null;
        }

        /// <summary>ストーリーかエンドレスで一度でも手に入れたコマンドの ID（コレクションで見せる）。版 4 から</summary>
        public List<string> discoveredCommands = new List<string>();

        /// <summary>ストーリーかエンドレスで一度でも戦った敵の ID（コレクションで見せる）。版 5 から</summary>
        public List<string> discoveredEnemies = new List<string>();

        /// <summary>そのエピソード（0始まり）を一度でもクリアしたか</summary>
        public bool HasCleared(int episodeIndex)
        {
            if (clears == null) return false;
            foreach (var record in clears)
                if (record != null && record.episodeIndex == episodeIndex) return true;
            return false;
        }

        /// <summary>そのエピソード（0始まり）のクリアで取った一番良いランク。まだクリアしていない（読めるランクが無い）なら null</summary>
        public CampaignRank? BestRank(int episodeIndex)
        {
            CampaignRank? best = null;
            if (clears == null) return null;
            foreach (var record in clears)
            {
                if (record == null || record.episodeIndex != episodeIndex) continue;
                if (!Enum.TryParse(record.rank, out CampaignRank rank)) continue;
                if (best == null || rank > best.Value) best = rank;
            }
            return best;
        }
    }

    /// <summary>チャレンジ1つぶんの記録</summary>
    [Serializable]
    public sealed class ChallengeRecord
    {
        /// <summary>ChallengeType の名前</summary>
        public string type;
        /// <summary>到達した一番深い階層（1始まり）</summary>
        public int bestLayer;
        /// <summary>最後の階層まで踏破したことがあるか（終わりの無いエンドレスは常に false）</summary>
        public bool cleared;
    }

    [Serializable]
    public sealed class CampaignClearRecord
    {
        /// <summary>クリアした日時（ISO 8601）</summary>
        public string clearedAt;
        /// <summary>クリアしたエピソード（0始まり）。版 2 から</summary>
        public int episodeIndex;
        public int score;
        /// <summary>CampaignRank の名前（E〜SSS）。列挙の並びが変わっても読めるよう名前で持つ</summary>
        public string rank;
        /// <summary>EndingRoute の名前</summary>
        public string route;
        public int remainingTokens;
        public int backupBoostCount;
    }
}

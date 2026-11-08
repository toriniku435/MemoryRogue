using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Save
{
    // クリア記録（clear.sav）: クリアするたびに1件、チャレンジごとの最高到達階層と踏破、一度でも手に入れたコマンドと戦った敵。
    // ランをまたいで残る（中断データを消しても消えない）。どれも増えたときだけ書く
    public sealed class ClearRecordStore
    {
        public const string FileName = "clear.sav";
        // 暗号化する前の形式（平文の JSON）。読むだけで、書くときは新しい形式へ移して消す
        public const string LegacyFileName = "clear.json";

        private readonly SaveFileCodec _codec;

        public ClearRecordStore(SaveFileCodec codec)
        {
            _codec = codec;
        }

        /// <summary>クリア記録。まだ無ければ空</summary>
        public CampaignClearData Load()
        {
            if (_codec.TryReadJson(FileName, LegacyFileName, out var json))
            {
                try
                {
                    var data = JsonUtility.FromJson<CampaignClearData>(json);
                    if (data != null) return Upgrade(data);
                }
                catch (ArgumentException e)
                {
                    Debug.LogWarning($"クリア記録が読めません（壊れている）: {e.Message}");
                }
            }
            return new CampaignClearData();
        }

        /// <summary>クリアを1件記録する</summary>
        public void RecordClear(CampaignResult result)
        {
            var data = Load();
            MergeDiscovered(data, CampaignDeckRepository.OwnedCards);
            data.clears.Add(new CampaignClearRecord
            {
                clearedAt = DateTime.Now.ToString("o"),
                episodeIndex = CampaignProgressRepository.EpisodeIndex,
                score = result.Score,
                rank = result.Rank.ToString(),
                route = result.Route.ToString(),
                remainingTokens = result.RemainingTokens,
                backupBoostCount = result.BackupBoostCount,
            });
            Write(data);
        }

        /// <summary>そのチャレンジの記録を更新する。更新する前の最高到達階層を返す（まだ遊んでいなければ 0）</summary>
        public int RecordChallenge(ChallengeType challenge, int reachedLayer, bool cleared)
        {
            var data = Load();
            var record = data.FindChallenge(challenge.ToString());
            if (record == null)
            {
                record = new ChallengeRecord { type = challenge.ToString() };
                data.challengeRecords.Add(record);
            }
            int previousBest = record.bestLayer;
            if (reachedLayer > record.bestLayer) record.bestLayer = reachedLayer;
            if (cleared) record.cleared = true;
            MergeDiscovered(data, CampaignDeckRepository.OwnedCards);
            Write(data);
            return previousBest;
        }

        public void RecordDiscoveredCommands(IEnumerable<CardData> cards)
        {
            var data = Load();
            if (MergeDiscovered(data, cards)) Write(data);
        }

        public void RecordDiscoveredEnemies(IEnumerable<EnemyData> enemies)
        {
            var data = Load();
            bool added = false;
            foreach (var enemy in enemies)
            {
                if (enemy == null || string.IsNullOrEmpty(enemy.Id) || data.discoveredEnemies.Contains(enemy.Id)) continue;
                data.discoveredEnemies.Add(enemy.Id);
                added = true;
            }
            if (added) Write(data);
        }

        public void Delete()
        {
            _codec.Delete(FileName);
            _codec.Delete(LegacyFileName);
        }

        private void Write(CampaignClearData data)
        {
            _codec.Write(FileName, LegacyFileName, JsonUtility.ToJson(data));
        }

        // 古い版で書かれた記録を、今の形に読み替える
        private static CampaignClearData Upgrade(CampaignClearData data)
        {
            data.clears ??= new List<CampaignClearRecord>();
            data.discoveredCommands ??= new List<string>();
            data.discoveredEnemies ??= new List<string>();
            data.challengeRecords ??= new List<ChallengeRecord>();
            // 版 5 以前のエンドレスの最高到達階層を、チャレンジの記録へ読み替える
            if (data.endlessBestLayer > 0 && data.FindChallenge(ChallengeType.Endless.ToString()) == null)
            {
                data.challengeRecords.Add(new ChallengeRecord { type = ChallengeType.Endless.ToString(), bestLayer = data.endlessBestLayer });
            }
            data.endlessBestLayer = 0;
            // 中身を書き換えたときに新しい版で書かれるように（1 の記録は episodeIndex 0 のまま読める）
            data.version = CampaignClearData.CurrentVersion;
            return data;
        }

        // まだ記録に無いコマンドを足す。足したら true
        private static bool MergeDiscovered(CampaignClearData data, IEnumerable<CardData> cards)
        {
            bool added = false;
            foreach (var card in cards)
            {
                if (card == null || string.IsNullOrEmpty(card.Id) || data.discoveredCommands.Contains(card.Id)) continue;
                data.discoveredCommands.Add(card.Id);
                added = true;
            }
            return added;
        }
    }
}

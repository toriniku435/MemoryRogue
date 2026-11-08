using System.Collections.Generic;
using Live2DCardRogue.Common.Save;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.TitleScene.Models
{
    // エネミーコレクションで「出会ったことがある」とする敵の ID を決める。
    // 戦った敵の記録（clear.sav の discoveredEnemies）に、記録を始める前のデータから分かる分を足す:
    //   - クリアしたエピソード: そのエピソードの全ステージの敵
    //   - 中断しているストーリー: 突破したステージ（今いるステージより前）の敵
    //   - 中断しているエンドレス: 突破した階層の敵（シードから組み直す）
    // 今いるステージで負けたことがあっても、どのステージかは残っていないので、そこは記録に任せる
    public static class EnemyDiscoveryResolver
    {
        /// <param name="clearData">クリア記録（null なら無し）</param>
        /// <param name="campaign">中断しているストーリー（null なら無し）</param>
        /// <param name="endless">中断しているエンドレス（null なら無し）</param>
        public static HashSet<string> Resolve(
            CampaignClearData clearData, CampaignSaveData campaign, CampaignSaveData endless,
            CampaignBalance balance, EndlessBalance endlessBalance)
        {
            var ids = new HashSet<string>();
            if (clearData != null)
            {
                foreach (var id in clearData.discoveredEnemies ?? new List<string>())
                {
                    if (!string.IsNullOrEmpty(id)) ids.Add(id);
                }
                foreach (var record in clearData.clears ?? new List<CampaignClearRecord>())
                {
                    if (record != null) AddEpisode(ids, balance, record.episodeIndex, int.MaxValue);
                }
            }

            if (campaign != null) AddEpisode(ids, balance, campaign.episodeIndex, campaign.stageIndex);

            if (endless != null && endlessBalance != null)
            {
                for (int index = 0; index < endless.stageIndex; index++)
                {
                    AddEnemies(ids, endlessBalance.CreateStage(endless.endlessSeed, index).Enemies);
                }
            }
            return ids;
        }

        // そのエピソードの、stageCount より前のステージの敵を足す
        private static void AddEpisode(HashSet<string> ids, CampaignBalance balance, int episodeIndex, int stageCount)
        {
            var episode = balance != null ? balance.GetEpisode(episodeIndex) : null;
            if (episode == null) return;
            var stages = episode.CreateStages();
            for (int i = 0; i < stages.Count && i < stageCount; i++)
            {
                AddEnemies(ids, stages[i].CreateEnemyLineup());
            }
        }

        private static void AddEnemies(HashSet<string> ids, IEnumerable<EnemyData> enemies)
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null && !string.IsNullOrEmpty(enemy.Id)) ids.Add(enemy.Id);
            }
        }
    }
}

using UnityEngine;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.MapScene.Models;

namespace Live2DCardRogue.MapScene.UseCase
{
    // 遊んでいるモードのルールを選ぶ。チャレンジはシードから階層ごとの中身を組み、ストーリーはエピソードのステージを並べる
    public static class MapRunRuleFactory
    {
        public static MapRunRule Create(CampaignBalance balance, EndlessBalance endlessBalance)
        {
            if (RunModeRepository.IsChallenge)
            {
                if (endlessBalance == null) Debug.LogError("MapSceneInitializer に EndlessBalance が設定されていません");
                return new ChallengeMapRule(endlessBalance, RunModeRepository.ChallengeSeed, RunModeRepository.ChallengeLayerCount);
            }

            var campaign = ResolveCampaign(balance);
            return new CampaignMapRule(campaign.CreateStages(), campaign.OpeningConversation);
        }

        // 遊んでいるエピソードのステージの並び。そのエピソードが無ければ（アセットの設定漏れ）エピソード1で遊ぶ
        private static CampaignData ResolveCampaign(CampaignBalance balance)
        {
            int episode = CampaignProgressRepository.EpisodeIndex;
            if (balance.HasEpisode(episode)) return balance.GetEpisode(episode);

            Debug.LogError($"{balance.name} にエピソード{episode + 1}のステージがありません。エピソード1のステージで遊びます");
            return balance.GetEpisode(0);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Save
{
    // キャンペーンのリポジトリ（進行・デッキ・トークン・ユニット・遊んでいるモード）と、保存する形（CampaignSaveData）の変換。
    // コマンドはアセットの参照ではなく ID で保存し、読むときに CardCatalog から引き直す（名前を変えたコマンドは飛ばすだけで、読み込みは壊れない）
    public static class CampaignSaveSnapshot
    {
        /// <summary>今のリポジトリの中身を、保存する形にする</summary>
        /// <param name="stageCount">ステージ数（タイトルで「LAYER 04 / 10」と出すため。終わりが無ければ 0）</param>
        public static CampaignSaveData Create(int stageCount)
        {
            return new CampaignSaveData
            {
                savedAt = DateTime.Now.ToString("o"),
                episodeIndex = CampaignProgressRepository.EpisodeIndex,
                isEndless = RunModeRepository.IsChallenge,
                endlessSeed = RunModeRepository.ChallengeSeed,
                challengeType = RunModeRepository.IsChallenge ? RunModeRepository.Challenge.ToString() : "",
                challengeLayerCount = RunModeRepository.ChallengeLayerCount,
                stageIndex = CampaignProgressRepository.CurrentStageIndex,
                stageCount = stageCount,
                backupBoostCount = CampaignProgressRepository.BackupBoostCount,
                tokens = CampaignTokenRepository.Tokens,
                maxHp = CampaignUnitRepository.MaxHp,
                maxMemory = CampaignUnitRepository.MaxMemory,
                deck = ToIds(CampaignDeckRepository.Deck),
                decks = ToSavedDecks(CampaignDeckRepository.Decks),
                activeDeckIndex = CampaignDeckRepository.ActiveDeckIndex,
                ownedCards = ToIds(CampaignDeckRepository.OwnedCards),
                purchasedSingles = ToIds(CampaignDeckRepository.PurchasedSingles),
            };
        }

        /// <summary>保存した進捗をリポジトリに戻す。見つからない ID（消した・名前を変えたコマンド）は飛ばす</summary>
        public static void Restore(CampaignSaveData data, CardCatalog catalog)
        {
            // 版 5 のエンドレスには challengeType が無い（空）ので、エンドレス（終わり無し）として戻す
            if (data.isEndless) RunModeRepository.StartChallenge(ChallengeTypeOf(data), data.challengeLayerCount, data.endlessSeed);
            else RunModeRepository.StartCampaign();
            CampaignProgressRepository.Restore(data.stageIndex, data.backupBoostCount, data.episodeIndex);
            CampaignTokenRepository.Initialize(data.tokens);
            CampaignUnitRepository.Initialize(data.maxHp, data.maxMemory);
            // 版 1 のデータには purchasedSingles が無い（JsonUtility では空のまま）ので、シングル購入はまだ何も買っていない扱いになる。
            // 版 2 以前のデータには decks が無いので、deck を1番目のデッキ（アクティブ）にする
            var owned = FromIds(data.ownedCards, catalog);
            var singles = FromIds(data.purchasedSingles, catalog);
            if (data.decks == null || data.decks.Length == 0)
            {
                CampaignDeckRepository.Initialize(FromIds(data.deck, catalog), owned, singles);
                return;
            }

            var decks = new CardData[data.decks.Length][];
            for (int i = 0; i < decks.Length; i++) decks[i] = FromIds(data.decks[i]?.cards, catalog);
            CampaignDeckRepository.Initialize(decks, data.activeDeckIndex, owned, singles);
        }

        /// <summary>中断データに書いてあるチャレンジ（名前が無い・読めなければエンドレス）</summary>
        public static ChallengeType ChallengeTypeOf(CampaignSaveData data)
        {
            return data != null && Enum.TryParse(data.challengeType, out ChallengeType type) ? type : ChallengeType.Endless;
        }

        private static SavedDeck[] ToSavedDecks(IReadOnlyList<CardData[]> decks)
        {
            var saved = new SavedDeck[decks.Count];
            for (int i = 0; i < saved.Length; i++) saved[i] = new SavedDeck { cards = ToIds(decks[i]) };
            return saved;
        }

        private static string[] ToIds(IReadOnlyList<CardData> cards)
        {
            var ids = new List<string>(cards.Count);
            foreach (var card in cards)
            {
                if (card != null) ids.Add(card.Id);
            }
            return ids.ToArray();
        }

        private static CardData[] FromIds(string[] ids, CardCatalog catalog)
        {
            var cards = new List<CardData>();
            if (ids == null) return cards.ToArray();
            foreach (var id in ids)
            {
                var card = catalog != null ? catalog.Find(id) : null;
                if (card != null) cards.Add(card);
                else Debug.LogWarning($"セーブデータのコマンド {id} が見つからないため飛ばします（CardCatalog を確認）");
            }
            return cards.ToArray();
        }
    }
}

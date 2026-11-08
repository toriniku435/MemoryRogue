#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.DebugMenu;
using Live2DCardRogue.Common.Message;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.SceneTransition;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.MapScene.Models;
using Live2DCardRogue.MapScene.UseCase;

namespace Live2DCardRogue.MapScene.Initializer
{
    // マップのデバッグ機能（0707で表示）。ファイルごと #if の中にあるので、リリースビルドには入らない
    public sealed class MapDebugCommands : IDisposable
    {
        private const int DefaultScore = 1500;
        private const int DefaultOwnedCount = 20;
        private const int DefaultTokens = 9999;

        private readonly MapRunRule _rule;
        private readonly CampaignBalance _balance;
        private readonly MapStageTransitionUseCase _transition;
        private readonly MapRunUseCase _run;
        // プレイヤーが手に入れられるコマンドはチップの中身だけ（CardCatalog には敵専用のコマンドも入っている）
        private readonly CardPackData[] _packs;
        private readonly List<IDisposable> _commands = new List<IDisposable>();

        public MapDebugCommands(MapRunRule rule, CampaignBalance balance, MapStageTransitionUseCase transition, MapRunUseCase run,
            CardPackData[] packs)
        {
            _rule = rule;
            _balance = balance;
            _transition = transition;
            _run = run;
            _packs = packs ?? new CardPackData[0];

            _commands.Add(DebugMenuOverlay.AddCommand("Map: Clear Stage", ClearCurrentStage));
            if (_rule.HasStory) _commands.Add(DebugMenuOverlay.AddIntCommand("Map: Clear All (score)", DefaultScore, ClearAllStages));
            _commands.Add(DebugMenuOverlay.AddIntCommand("Map: Jump to Layer", CampaignProgressRepository.CurrentStageIndex + 1, JumpToLayer));
            _commands.Add(DebugMenuOverlay.AddIntCommand("Map: Own All Commands", DefaultOwnedCount, SetAllOwnedCount));
            _commands.Add(DebugMenuOverlay.AddIntCommand("Map: Set Tokens", DefaultTokens, SetTokens));
        }

        public void Dispose()
        {
            foreach (var command in _commands) command.Dispose();
            _commands.Clear();
        }

        // 今いるステージの戦闘に勝ったことにして、マップを開き直す（戦闘から勝って戻ってきたときと同じ結果を渡す）。
        // 報酬の受け取り・次の部屋への移動・保存・最終ステージならエンディングへ、まで普段の流れのまま進む
        private void ClearCurrentStage()
        {
            if (_rule.IsCompleted(CampaignProgressRepository.CurrentStageIndex))
            {
                MessageUseCase.ShowMessage("すべてのステージを踏破しました");
                return;
            }
            if (CampaignTutorialRepository.IsActive)
            {
                MessageUseCase.ShowMessage("チュートリアル中はステージをクリアできません");
                return;
            }

            Debug.Log($"--- [DEBUG] {CampaignProgressRepository.CurrentStageIndex + 1}マス目のステージをクリアしたことにする ---");
            SceneTransitionUseCase.LoadScene(new MapSceneTransitionData(new BattleResult(true)));
        }

        // 全ステージを踏破したことにして、指定したスコアでエンディングへ進む。
        // スコアは「残りトークン = score、バックアップブースト 0回」として計算するので、ランクとルートは通常と同じ決まりで決まる
        private void ClearAllStages(int score)
        {
            while (!_rule.IsCompleted(CampaignProgressRepository.CurrentStageIndex)) CampaignProgressRepository.AdvanceStage();

            var result = CampaignResultCalculator.Calculate(score, 0, _balance.CreateScoreRule());
            Debug.Log($"--- [DEBUG] 全ステージ踏破: スコア{result.Score} ランク{result.Rank} ルート{result.Route} ---");
            _transition.LoadEnding(result);
        }

        // 指定した階層（1始まり）へ飛んで、マップを開き直す（報酬も移動の演出も無し）。チャレンジの深い階層やページ送りを確かめる用
        private void JumpToLayer(int layer)
        {
            int index = Mathf.Max(0, layer - 1);
            if (_rule.StageCount.HasValue) index = Mathf.Min(index, _rule.StageCount.Value - 1);
            CampaignProgressRepository.Restore(index, CampaignProgressRepository.BackupBoostCount, CampaignProgressRepository.EpisodeIndex);
            Debug.Log($"--- [DEBUG] {index + 1}階層目へ飛ぶ ---");
            SceneTransitionUseCase.LoadScene(new MapSceneTransitionData());
        }

        // チップに入っているコマンドすべての所持数を、指定した数にそろえる（多く持っているものは減らす）。
        // デッキは触らないので、所持より多く入っていたデッキはデッキ編集を開いたときに直される
        private void SetAllOwnedCount(int count)
        {
            count = Mathf.Max(0, count);
            var commands = new List<CardData>();
            foreach (var pack in _packs)
            {
                if (pack == null) continue;
                foreach (var card in pack.Cards)
                {
                    if (card != null && !commands.Contains(card)) commands.Add(card);
                }
            }

            var owned = new List<CardData>();
            foreach (var card in commands)
            {
                for (int i = 0; i < count; i++) owned.Add(card);
            }
            CampaignDeckRepository.Initialize(CampaignDeckRepository.Decks, CampaignDeckRepository.ActiveDeckIndex, owned.ToArray(),
                CampaignDeckRepository.PurchasedSingles);
            Debug.Log($"--- [DEBUG] {commands.Count}種のコマンドを{count}個ずつ所持 ---");
            SaveAndReload();
        }

        private void SetTokens(int tokens)
        {
            CampaignTokenRepository.Set(tokens);
            Debug.Log($"--- [DEBUG] トークンを{CampaignTokenRepository.Tokens}にする ---");
            SaveAndReload();
        }

        // 画面の表示（トークン・強化の残高など）を作り直すため、マップを開き直す。
        // 中断して再開しても残るよう保存も普段どおり行う（チュートリアル中・踏破後は保存しない）
        private void SaveAndReload()
        {
            _run.SaveIfAllowed();
            SceneTransitionUseCase.LoadScene(new MapSceneTransitionData());
        }
    }
}
#endif

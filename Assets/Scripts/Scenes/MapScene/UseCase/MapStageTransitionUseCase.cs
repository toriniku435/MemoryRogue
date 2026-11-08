using System;
using UnityEngine;
using Live2DCardRogue.Common.Message;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.Save;
using Live2DCardRogue.Common.SceneTransition;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.MapScene.Models;

namespace Live2DCardRogue.MapScene.UseCase
{
    // マップから出ていくところ: 戦闘へ送り出す、踏破してエンディング / リザルトへ進む、中断してタイトルへ戻る
    public sealed class MapStageTransitionUseCase
    {
        private readonly CampaignBalance _balance;
        private readonly MapRunRule _rule;
        private readonly MapRunUseCase _run;

        public MapStageTransitionUseCase(CampaignBalance balance, MapRunRule rule, MapRunUseCase run)
        {
            _balance = balance;
            _rule = rule;
            _run = run;
        }

        /// <summary>今いる部屋の戦闘へ。全部踏破した後なら伝えるだけ</summary>
        public void StartStage()
        {
            var stage = _rule.GetStage(CampaignProgressRepository.CurrentStageIndex);
            if (stage == null)
            {
                MessageUseCase.ShowMessage("すべてのステージを踏破しました");
                return;
            }

            // 戻り先のデータの作り方を渡しておくことで、戦闘側はマップの存在を知らないまま結果を返せる。
            // チャレンジは一度負けたら終わりなので、負けたときだけリザルトへ
            Func<BattleResult, SceneTransitionData> createReturnData = _rule.EndsOnDefeat ? ReturnToMapOrEndChallenge : (Func<BattleResult, SceneTransitionData>)ReturnToMap;
            var parameter = new BattleSceneTransitionData(
                // 強化した値で、毎回満タンから始める
                new UnitStatusModel(CampaignUnitRepository.MaxHp, CampaignUnitRepository.MaxMemory),
                CampaignDeckRepository.Deck,
                stage.Enemies,
                createReturnData,
                enemyStrength: stage.Strength,
                tokens: CampaignTokenRepository.Tokens,
                bgm: stage.Bgm);

            // 戦う敵を、一度でも戦った敵の記録に残す（タイトルのエネミーコレクションで見せる）
            CampaignSaveService.Default.RecordDiscoveredEnemies(stage.Enemies);
            SceneTransitionUseCase.LoadScene(parameter);
        }

        /// <summary>
        /// 最終ステージを踏破した。成績（ランクとエンディングのルート）をここで先に確定させてから、
        /// エンディング → リザルトへ同じ結果を渡していく（どちらも計算はしない）。チャレンジは記録してそのままリザルトへ
        /// </summary>
        public void CompleteCampaign()
        {
            if (!_rule.HasStory)
            {
                SceneTransitionUseCase.LoadScene(FinishChallenge(cleared: true));
                return;
            }

            var result = CampaignResultCalculator.Calculate(CampaignTokenRepository.Tokens, CampaignProgressRepository.BackupBoostCount, _balance.CreateScoreRule());
            Debug.Log($"--- キャンペーン踏破: スコア{result.Score}（トークン{result.RemainingTokens} / バックアップ{result.BackupBoostCount}回）ランク{result.Rank} ルート{result.Route} ---");
            LoadEnding(result);
        }

        /// <summary>クリアした: 記録を1件足し、中断データ（進捗）は消してからエンディングへ</summary>
        public void LoadEnding(CampaignResult result)
        {
            CampaignSaveService.Default.RecordClear(result);
            CampaignSaveService.Default.DeleteProgress();
            SceneTransitionUseCase.LoadScene(new EndingSceneTransitionData(result));
        }

        /// <summary>メニューの中断: 今の進み具合を保存してタイトルへ戻る（タイトルのストーリー / チャレンジから再開できる）</summary>
        public void Suspend()
        {
            _run.SaveIfAllowed();
            Debug.Log("--- 中断してタイトルへ戻る ---");
            SceneTransitionUseCase.LoadScene(new TitleSceneTransitionData());
        }

        // 戦闘のシーンで呼ばれるので static（このマップのオブジェクトは掴まない）
        public static SceneTransitionData ReturnToMap(BattleResult result)
        {
            return new MapSceneTransitionData(result);
        }

        // チャレンジの戦闘: 勝てばマップへ、負けたらそこでチャレンジを終えてリザルトへ。戦闘のシーンで呼ばれるので static
        private static SceneTransitionData ReturnToMapOrEndChallenge(BattleResult result)
        {
            return result.IsVictory ? new MapSceneTransitionData(result) : FinishChallenge(cleared: false);
        }

        // チャレンジを終える: 到達した階層を最高記録に照らして残し、中断データは消す（もう再開できない）。
        // 負けたなら今いる階層、踏破したなら最後の階層（進行はもう階層数と同じところまで進んでいる）。
        // 負けたときは戦闘のシーンで呼ばれるので static（リポジトリとセーブだけを見る）
        private static ChallengeResultTransitionData FinishChallenge(bool cleared)
        {
            var challenge = RunModeRepository.Challenge;
            int reachedLayer = cleared ? RunModeRepository.ChallengeLayerCount : CampaignProgressRepository.CurrentStageIndex + 1;
            int previousBest = CampaignSaveService.Default.FinishChallengeRun(challenge, reachedLayer, cleared);
            Debug.Log($"--- チャレンジ {challenge} 終了: {reachedLayer}階層{(cleared ? "（踏破）" : "")}（これまでの最高 {previousBest}階層）---");
            return new ChallengeResultTransitionData(challenge, reachedLayer, cleared, previousBest);
        }
    }
}

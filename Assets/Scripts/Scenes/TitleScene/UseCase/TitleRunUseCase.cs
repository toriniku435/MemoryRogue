using UnityEngine;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.Save;
using Live2DCardRogue.Common.SceneTransition;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.TitleScene.UseCase
{
    // タイトルから、ラン（ストーリー / チャレンジ1回分）を始める・再開する・捨てる。ユーザーデータの削除もここ
    public sealed class TitleRunUseCase
    {
        private readonly CardCatalog _cardCatalog;

        /// <param name="cardCatalog">中断したデータのコマンド（ID で保存してある）を引き直す一覧</param>
        public TitleRunUseCase(CardCatalog cardCatalog)
        {
            _cardCatalog = cardCatalog;
        }

        /// <summary>中断したデータ（ストーリーとチャレンジで1つずつ）</summary>
        public bool TryLoadSuspended(RunMode mode, out CampaignSaveData data)
        {
            return CampaignSaveService.Default.TryLoadProgress(out data, mode);
        }

        public void StartNewCampaign(int episodeIndex)
        {
            ClearRunState(episodeIndex);
            RunModeRepository.StartCampaign();
            // チュートリアルを見るかは、マップに着いてから聞く（答えるまでマップはファーストビルドを始めない）
            CampaignTutorialRepository.RequestPrompt();
            Debug.Log($"--- エピソード{episodeIndex + 1}で新しいキャンペーンを始める ---");
            SceneTransitionUseCase.LoadScene(new MapSceneTransitionData());
        }

        // チャレンジには会話もチュートリアルも無い。マップに着いたらすぐファーストビルド
        public void StartNewChallenge(ChallengeCatalog.Entry challenge)
        {
            ClearRunState(0);
            CampaignTutorialRepository.ClearPrompt();
            RunModeRepository.StartChallenge(challenge.Type, challenge.LayerCount, new System.Random().Next());
            Debug.Log($"--- 新しいチャレンジ {challenge.Type}（{(challenge.LayerCount > 0 ? challenge.LayerCount + "階層" : "終わり無し")}、シード {RunModeRepository.ChallengeSeed}）を始める ---");
            SceneTransitionUseCase.LoadScene(new MapSceneTransitionData());
        }

        /// <summary>中断したデータに書いてあるモード（ストーリー / チャレンジ）で再開する</summary>
        public void Resume(CampaignSaveData data)
        {
            CampaignSaveSnapshot.Restore(data, _cardCatalog);
            Debug.Log($"--- {(data.isEndless ? "チャレンジ" : "キャンペーン")}を再開（{data.stageIndex + 1}マス目 / トークン{data.tokens}）---");
            SceneTransitionUseCase.LoadScene(new MapSceneTransitionData());
        }

        public void Discard(CampaignSaveData data)
        {
            CampaignSaveService.Default.DeleteProgress(data.isEndless ? RunMode.Challenge : RunMode.Campaign);
            Debug.Log($"--- 中断した{(data.isEndless ? "チャレンジ" : "キャンペーン")}を破棄した ---");
        }

        /// <summary>
        /// 中断したデータ・クリア記録（エピソードの解放）・チュートリアルを見たかを消す。
        /// 音量・言語・画面モード・戦闘スピードは、遊ぶ人の環境の設定なので残す
        /// </summary>
        public void DeleteUserData()
        {
            var saves = CampaignSaveService.Default;
            saves.DeleteProgress(RunMode.Campaign);
            saves.DeleteProgress(RunMode.Challenge);
            saves.DeleteClearData();
            GameSettingsRepository.SetHasSeenTutorial(false);
            GameSettingsSaveService.Default.Save();
            Debug.Log("--- ユーザーデータを削除した ---");
        }

        // キャンペーンの状態はすべて1回分。空にしておけば、マップが初期値を配り直す（ファーストビルドから始まる）
        private static void ClearRunState(int episodeIndex)
        {
            CampaignProgressRepository.Reset(episodeIndex);
            CampaignTokenRepository.Clear();
            CampaignUnitRepository.Clear();
            CampaignDeckRepository.Clear();
            CampaignTutorialRepository.Finish();
        }
    }
}

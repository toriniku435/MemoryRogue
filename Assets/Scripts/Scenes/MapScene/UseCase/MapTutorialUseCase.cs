using System;
using UnityEngine;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.Save;
using Live2DCardRogue.Common.SceneTransition;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.Tutorial;
using Live2DCardRogue.Common.WindowSystem;

namespace Live2DCardRogue.MapScene.UseCase
{
    // マップでのチュートリアルの流れ: 見るかの確認、マップの説明、練習の戦闘への送り出し、見直し、スキップの確認。
    // 見ないと答えたらファーストビルドへ進む（どう進むかは onSkipped で Initializer が決める）
    public sealed class MapTutorialUseCase : IDisposable
    {
        private readonly CampaignTutorialData _tutorial;
        private readonly WindowHandler<ConfirmShowData> _confirm;
        private readonly Action _openPrompt;
        private readonly Action _onSkipped;
        private IDisposable _session;

        /// <param name="openPrompt">「チュートリアルを見ますか？」を開く（答えは HandleAnswered に返ってくる）</param>
        /// <param name="onSkipped">見ないことにした（ファーストビルドへ進む）</param>
        public MapTutorialUseCase(CampaignTutorialData tutorial, WindowHandler<ConfirmShowData> confirm, Action openPrompt, Action onSkipped)
        {
            _tutorial = tutorial;
            _confirm = confirm;
            _openPrompt = openPrompt;
            _onSkipped = onSkipped;
        }

        /// <summary>マップの説明を流す（見ると答えてマップを開き直したとき）</summary>
        public void PlayMapSequence()
        {
            _session = TutorialUseCase.Play(CampaignTutorialRepository.MapSequence, HandleMapSequenceEnded, ConfirmSkip);
        }

        /// <summary>見るかを聞く。一度でも見ていれば聞かずに「見ない」と同じ流れへ</summary>
        public void AskOrSkip()
        {
            if (GameSettingsRepository.HasSeenTutorial)
            {
                Debug.Log("--- チュートリアルは見たことがあるので、確認を飛ばす ---");
                HandleAnswered(false);
                return;
            }
            _openPrompt.Invoke();
        }

        /// <summary>見るなら、その場でマップの説明を始める（ファーストビルドは練習の戦闘の後）。見ないならファーストビルドへ</summary>
        public void HandleAnswered(bool watch)
        {
            CampaignTutorialRepository.ClearPrompt();
            Debug.Log($"--- チュートリアル{(watch ? "を見る" : "は見ない")} ---");

            if (!watch)
            {
                _onSkipped.Invoke();
                return;
            }
            MarkSeen();
            StartTutorial();
            PlayMapSequence();
        }

        /// <summary>最初から見直す（キャンペーンの状態は変えない。練習の戦闘の結果も使わない）。確かめてから</summary>
        public void Replay()
        {
            _confirm.Show(new ConfirmShowData(
                LocalizationHelper.GetUiText(UiTextKey.TutorialReplayConfirm), null,
                LocalizationHelper.GetUiText(UiTextKey.TutorialReplayOk), LocalizationHelper.GetUiText(UiTextKey.Back),
                () =>
                {
                    MarkSeen();
                    StartTutorial();
                    SceneTransitionUseCase.LoadScene(new MapSceneTransitionData());
                }));
        }

        public void Dispose()
        {
            _session?.Dispose();
            _session = null;
        }

        // 手順はチュートリアルを終えるまでリポジトリが持つ（マップを開き直しても、戦闘へ移っても、そこから取る）
        private void StartTutorial()
        {
            CampaignTutorialRepository.StartFromMap(
                _tutorial != null ? _tutorial.MapSequence : null,
                _tutorial != null ? _tutorial.BattleSequence : null);
        }

        // 見たことを、起動をまたいで覚えておく（設定ファイルに書く）
        private static void MarkSeen()
        {
            if (GameSettingsRepository.HasSeenTutorial) return;
            GameSettingsRepository.SetHasSeenTutorial(true);
            GameSettingsSaveService.Default.Save();
        }

        // ほかの確認の途中なら尋ねず、スキップしなかったことにする
        private void ConfirmSkip(Action onSkip, Action onCancel)
        {
            if (_confirm.IsOpen)
            {
                onCancel.Invoke();
                return;
            }
            _confirm.Show(new ConfirmShowData(
                LocalizationHelper.GetUiText(UiTextKey.TutorialSkipConfirm), null,
                LocalizationHelper.GetUiText(UiTextKey.TutorialSkipOk), LocalizationHelper.GetUiText(UiTextKey.Back),
                onSkip, onCancel));
        }

        // マップの説明が終わった: 最後まで見たら練習の戦闘へ。スキップしたらチュートリアルはおしまいにして、
        // マップを開き直す（ファーストビルドなど、いつもの始まり方をさせる）
        private void HandleMapSequenceEnded(bool completed)
        {
            if (!completed || _tutorial == null)
            {
                CampaignTutorialRepository.Finish();
                SceneTransitionUseCase.LoadScene(new MapSceneTransitionData());
                return;
            }

            CampaignTutorialRepository.MoveToBattle();
            // 手順に合わせた並びのまま引かせる（切らない）。戻り先はマップで、結果は使わない
            var parameter = new BattleSceneTransitionData(
                _tutorial.CreatePlayerStatus(), _tutorial.Deck, _tutorial.Enemies, MapStageTransitionUseCase.ReturnToMap,
                playTutorial: true, shuffleDeck: false);
            SceneTransitionUseCase.LoadScene(parameter);
        }
    }
}

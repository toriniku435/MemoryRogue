using UnityEngine;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.Save;
using Live2DCardRogue.Common.SceneTransition;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.WindowSystem;
using Live2DCardRogue.TitleScene.Presenters;
using Live2DCardRogue.TitleScene.UseCase;
using Live2DCardRogue.TitleScene.Views;

namespace Live2DCardRogue.TitleScene.Initializer
{
    // タイトルシーンのコンポジションルート。「どのモードが選ばれたら、どのウインドウを開くか」を決めるのはここだけで、
    // Presenter はモードを報告するだけ。ランを始める・再開する・捨てるのは TitleRunUseCase、記録を読むのは TitleRecordUseCase。
    // ストーリーもチャレンジも、中断したデータがあれば「再開 / 破棄 / キャンセル」を選ばせ、無いとき・破棄したときだけ
    // エピソード / チャレンジの選択を開く
    public sealed class TitleSceneInitializer : SceneInitializerBase<TitleSceneTransitionData>
    {
        [SerializeField] private TitleSceneViewContainer _viewContainer;

        // 中断したデータのコマンド（ID で保存してある）を引き直す一覧
        [SerializeField] private CardCatalog _cardCatalog;

        // どのエピソードにステージがあるかを見る（マップと同じアセット）
        [SerializeField] private CampaignBalance _balance;

        [SerializeField] private CommandCollectionData _commandCollection;

        [SerializeField] private EnemyCollectionData _enemyCollection;

        // 中断しているチャレンジで突破した階層の敵を組み直す（記録を始める前に戦った敵を、出会ったことにするため）
        [SerializeField] private EndlessBalance _endlessBalance;

        [SerializeField] private ChallengeCatalog _challengeCatalog;

        // ウインドウ。シーンには置かず、CreateWindows でまとめて生成する（開くのは各 Handler の Show）
        private WindowHandler<ConfirmShowData> _confirmWindow;
        private WindowHandler<CampaignResumeShowData> _resumeWindow;
        private WindowHandler _episodeWindow;
        private WindowHandler _challengeWindow;
        private WindowHandler _collectionWindow;
        private WindowHandler _commandCollectionWindow;
        private WindowHandler _enemyCollectionWindow;
        private WindowHandler _optionsWindow;

        private TitleRunUseCase _run;
        private TitleRecordUseCase _records;
        private TitleModePresenter _modePresenter;

        protected override void Initialize(TitleSceneTransitionData data)
        {
            _run = new TitleRunUseCase(_cardCatalog);
            _records = new TitleRecordUseCase(_balance, _endlessBalance);
            _modePresenter = new TitleModePresenter(_viewContainer, HandleModeSelected);
            _modePresenter.Initialize();
            CreateWindows();
        }

        private void CreateWindows()
        {
            var popup = _viewContainer.PopupCanvas;

            // 確認ウインドウは先に作り、尋ねるウインドウ（再開・エピソード・チャレンジ・オプション）で共用する
            _confirmWindow = WindowUseCase.Create(new ConfirmDialogInitData(), popup);
            _resumeWindow = WindowUseCase.Create(new CampaignResumeInitData(_confirmWindow, _run.Resume, DiscardProgress), popup);
            _episodeWindow = WindowUseCase.Create(new EpisodeSelectInitData(_confirmWindow, _records.GetEpisodeState, _run.StartNewCampaign, _records.GetBestRank), popup);
            _challengeWindow = WindowUseCase.Create(new ChallengeSelectInitData(_challengeCatalog, _confirmWindow, _run.StartNewChallenge,
                _records.IsChallengeCleared, _records.GetChallengeBestLayer), popup);
            // 手に入れたコマンド・出会った敵は、開くたびにセーブ（記録と中断データ）から読み直す
            _commandCollectionWindow = WindowUseCase.Create(new CommandCollectionInitData(_commandCollection, _records.LoadDiscoveredCommandIds), popup);
            _enemyCollectionWindow = WindowUseCase.Create(new EnemyCollectionInitData(_enemyCollection, _records.LoadDiscoveredEnemyIds), popup);
            _optionsWindow = WindowUseCase.Create(new GameOptionsInitData(_confirmWindow, DeleteUserData), popup);
            // コレクションの入口。選んだ一覧を、このウインドウの上に重ねて開く
            _collectionWindow = WindowUseCase.Create(new MenuWindowInitData(UiTextKey.Collection, null,
                new MenuItemData(UiTextKey.CollectionCommands, _commandCollectionWindow.Show),
                new MenuItemData(UiTextKey.CollectionEnemies, _enemyCollectionWindow.Show)), popup);
        }

        private void HandleModeSelected(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Campaign:
                    if (_run.TryLoadSuspended(RunMode.Campaign, out var campaign)) _resumeWindow.Show(new CampaignResumeShowData(campaign));
                    else OpenEpisodeSelect();
                    return;
                // 中断したチャレンジはチャレンジ全体で1つ
                case GameMode.Challenge:
                    if (_run.TryLoadSuspended(RunMode.Challenge, out var challenge)) _resumeWindow.Show(new CampaignResumeShowData(challenge));
                    else _challengeWindow.Show();
                    return;
                case GameMode.Collection:
                    _collectionWindow.Show();
                    return;
                case GameMode.Options:
                    _optionsWindow.Show();
                    return;
            }
        }

        // 開くたびにクリア記録を読み直す（エピソードの解放とランクの表示）
        private void OpenEpisodeSelect()
        {
            _records.Reload();
            _episodeWindow.Show();
        }

        // 破棄したら、ストーリーはエピソードを、チャレンジはチャレンジを選ばせる（選ばずに閉じても、中断したデータはもう無い）
        private void DiscardProgress(CampaignSaveData data)
        {
            _run.Discard(data);
            if (data.isEndless) _challengeWindow.Show();
            else OpenEpisodeSelect();
        }

        private void DeleteUserData()
        {
            _run.DeleteUserData();
            _records.Reload();
        }

        private void OnDestroy()
        {
            _modePresenter?.Dispose();
            _optionsWindow?.Dispose();
            _enemyCollectionWindow?.Dispose();
            _commandCollectionWindow?.Dispose();
            _collectionWindow?.Dispose();
            _challengeWindow?.Dispose();
            _episodeWindow?.Dispose();
            _resumeWindow?.Dispose();
            _confirmWindow?.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Live2DCardRogue.Common.Conversation;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.SceneTransition;
using Live2DCardRogue.Common.WindowSystem;
using Live2DCardRogue.Common.Tutorial;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.MapScene.Data;
using Live2DCardRogue.MapScene.Models;
using Live2DCardRogue.MapScene.Presenters;
using Live2DCardRogue.MapScene.UseCase;
using Live2DCardRogue.MapScene.Views;

namespace Live2DCardRogue.MapScene.Initializer
{
    // マップシーンのコンポジションルート。
    // ランの状態の書き換えは MapRunUseCase、マップから出ていくのは MapStageTransitionUseCase、チュートリアルの流れは MapTutorialUseCase。
    // ストーリーとチャレンジ（エンドレスを含む）はこのシーンを共用し、違い（部屋の数と中身・物語の有無・負けたらどうなるか）は
    // RunModeRepository で選んだルール（MapRunRule）に任せる
    public sealed class MapSceneInitializer : SceneInitializerBase<MapSceneTransitionData>
    {
        [SerializeField] private MapSceneViewContainer _viewContainer;

        /// <summary>ゲームバランス（ステージの並び・開始時の持ち物・スコアとランク・ユニット強化・チップの値段の係数）。マスの数はこのステージ数で決まる</summary>
        [SerializeField] private CampaignBalance _balance;

        /// <summary>チャレンジ（エンドレスを含む）の敵の候補・強さの上乗せ・報酬。開始時の持ち物やユニット強化・マーケットは _balance を共用する</summary>
        [SerializeField] private EndlessBalance _endlessBalance;

        /// <summary>キャンペーンの最初のチュートリアル（マップの手順と練習の戦闘）</summary>
        [SerializeField] private CampaignTutorialData _tutorial;

        /// <summary>マーケットに並べるチップ。増やしたいときはここに足す（シングル購入で売るコマンドも、ここに並べたチップの中身から決まる）</summary>
        [SerializeField] private CardPackData[] _shopPacks;

        // ウインドウ。シーンには置かず、CreateWindows でまとめて生成する（開くのは各 Handler の Show）
        private WindowHandler<ConfirmShowData> _confirmWindow;
        private WindowHandler<StageRewardShowData> _rewardWindow;
        private WindowHandler<ConversationShowData> _conversationWindow;
        private WindowHandler<FirstBuildShowData> _firstBuildWindow;
        private WindowHandler _enemyDetailWindow;
        private WindowHandler _unitUpgradeWindow;
        private WindowHandler _deckEditWindow;
        private WindowHandler _cardShopWindow;
        private WindowHandler _optionsWindow;
        private WindowHandler _menuWindow;
        // マップに置いた、ウインドウを開くボタン（敵詳細・ユニット強化・コマンドビルド・マーケット・メニュー）
        private readonly List<PresenterBase> _openButtons = new List<PresenterBase>();

        private MapRunRule _rule;
        private MapRunUseCase _run;
        private MapStageTransitionUseCase _transition;
        private MapTutorialUseCase _tutorialFlow;
        // ユニット強化のウインドウと共有する（買い物でトークンが減ったら書き換える）
        private UnitUpgradeModel _unitUpgradeModel;
        private MapPresenter _mapPresenter;
        private MapTutorialButtonPresenter _tutorialButtonPresenter;
        private TutorialPromptPresenter _tutorialPromptPresenter;
        private IDisposable _roomAnchor;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private MapDebugCommands _debugCommands;
#endif

        protected override void Initialize(MapSceneTransitionData data)
        {
            _rule = MapRunRuleFactory.Create(_balance, _endlessBalance);
            _run = new MapRunUseCase(_balance, _rule, _shopPacks);
            _transition = new MapStageTransitionUseCase(_balance, _rule, _run);

            var battleResult = data?.BattleResult;
            // 練習の戦闘から戻ってきた: 結果は使わない（報酬も進行もバックアップブーストも無し）。チュートリアルはここで終わり
            if (CampaignTutorialRepository.Stage == CampaignTutorialStage.Battle)
            {
                battleResult = null;
                CampaignTutorialRepository.Finish();
                Debug.Log("--- チュートリアルを終えた ---");
            }
            // マップのチュートリアル中は、ファーストビルドも保存もしない（終わってから始める。途中で終えたら最初から）
            bool mapTutorial = CampaignTutorialRepository.Stage == CampaignTutorialStage.Map;
            // 新しいキャンペーンで着いたばかりなら、チュートリアルを見るか聞くまでファーストビルドを待つ
            bool askTutorial = CampaignTutorialRepository.IsPromptPending && !CampaignTutorialRepository.IsActive;

            var firstBuild = mapTutorial || askTutorial ? new CardData[0] : _run.InitializeDeckIfNeeded();
            _run.InitializeTokensAndUnitIfNeeded();
            var arrival = _run.ApplyBattleResult(battleResult);

            // 節目（ファーストビルド・ステージの勝敗）は、反映したらすぐ保存する（デッキ編集・買い物・ユニット強化は、その時点で）。
            // ファーストビルドは演出の途中で終了しても引き直しにならないように、負けはバックアップブーストを無かったことにできないように
            if (firstBuild.Length > 0 || arrival.Reward != null) _run.SaveIfAllowed();

            // 勝って戻ってきて、そのステージに会話があれば、報酬の前に流す
            var clearConversation = arrival.ClearedStage != null ? arrival.ClearedStage.ClearConversation : null;
            bool playClearConversation = clearConversation != null && clearConversation.HasLines;

            _unitUpgradeModel = _run.CreateUnitUpgradeModel();
            CreateWindows();

            _mapPresenter = new MapPresenter(_viewContainer, _confirmWindow, _rewardWindow.Show, _rule, _transition.StartStage, _transition.CompleteCampaign,
                arrival.ArrivedFromIndex, arrival.Reward, holdReward: playClearConversation && !arrival.CompletedNow);
            _mapPresenter.Initialize();

            ShowFirstBuild(firstBuild);
            CreateOpenButtons();
            CreateTutorial(mapTutorial);

            // エピソードを始めたばかりなら、会話 → チュートリアルの確認。勝って戻ってきたなら、会話 → 報酬
            var token = this.GetCancellationTokenOnDestroy();
            if (askTutorial) PlayWhenShownAsync(_rule.OpeningConversation, _tutorialFlow.AskOrSkip, token).Forget();
            // 最後のステージに勝った: 報酬は無いので、会話（無ければすぐ）のあとエンディング / リザルトへ
            else if (arrival.CompletedNow) PlayWhenShownAsync(clearConversation, _transition.CompleteCampaign, token).Forget();
            else if (playClearConversation) PlayWhenShownAsync(clearConversation, _mapPresenter.ShowReward, token).Forget();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _debugCommands = new MapDebugCommands(_rule, _balance, _transition, _run, _shopPacks);
#endif
        }

        // マップで使うウインドウをまとめて生成する（閉じた状態で置き、開くのは各 Handler の Show）
        private void CreateWindows()
        {
            var popup = _viewContainer.PopupCanvas;

            // 確認ウインドウはマップで1つを共用する（接続開始・ユニット強化・コマンドビルド・チュートリアル・メニュー・オプション）
            _confirmWindow = WindowUseCase.Create(new ConfirmDialogInitData(), popup);
            _rewardWindow = WindowUseCase.Create(new StageRewardInitData(), popup);
            _conversationWindow = WindowUseCase.Create(new ConversationInitData(), popup);
            _firstBuildWindow = WindowUseCase.Create(new FirstBuildInitData(), popup);
            _enemyDetailWindow = WindowUseCase.Create(new EnemyDetailInitData(() => _rule.GetStage(CampaignProgressRepository.CurrentStageIndex)), popup);
            _unitUpgradeWindow = WindowUseCase.Create(new UnitUpgradeInitData(_unitUpgradeModel, _viewContainer.TokenCounter, _confirmWindow, _run.ApplyUnitUpgrade), popup);
            // おまかせはファーストビルドと同じ組み方。右上からも同じ敵詳細を重ねて開ける（デッキを組みながら相手を確かめられるように）
            _deckEditWindow = WindowUseCase.Create(new DeckEditInitData(
                () => CampaignDeckRepository.OwnedCards, () => CampaignDeckRepository.Decks, () => CampaignDeckRepository.ActiveDeckIndex,
                _balance.CreateAutoDeckRule(), _confirmWindow, _run.ApplyDeckSaved, _enemyDetailWindow.Show, HasNextStage), popup);
            _cardShopWindow = WindowUseCase.Create(new CardShopInitData(_viewContainer.TokenCounter, _run.CreateCardShopModel, HandleShopPurchased), popup);
            // ユーザーデータの削除はタイトルだけ
            _optionsWindow = WindowUseCase.Create(new GameOptionsInitData(_confirmWindow), popup);
            _menuWindow = WindowUseCase.Create(new MenuWindowInitData(UiTextKey.Menu, _confirmWindow,
                new MenuItemData(UiTextKey.Suspend, _transition.Suspend,
                    confirm: new MenuConfirmData(UiTextKey.SuspendConfirm, UiTextKey.SuspendConfirmDetail, UiTextKey.SuspendOk, UiTextKey.Back)),
                new MenuItemData(UiTextKey.Options, _optionsWindow.Show)), popup);
        }

        // 敵詳細は、全部踏破した後は見せる相手がいないので押せない
        private void CreateOpenButtons()
        {
            _openButtons.Add(new WindowOpenButtonPresenter(_viewContainer.EnemyDetailButton, _viewContainer.EnemyDetailLabel, UiTextKey.EnemyDetail, _enemyDetailWindow.Show, HasNextStage));
            _openButtons.Add(new WindowOpenButtonPresenter(_viewContainer.UnitUpgradeButton, _viewContainer.UnitUpgradeLabel, UiTextKey.UnitUpgrade, _unitUpgradeWindow.Show));
            _openButtons.Add(new WindowOpenButtonPresenter(_viewContainer.DeckEditButton, _viewContainer.DeckEditLabel, UiTextKey.DeckEdit, _deckEditWindow.Show));
            _openButtons.Add(new WindowOpenButtonPresenter(_viewContainer.CardShopButton, _viewContainer.CardShopLabel, UiTextKey.CardShop, _cardShopWindow.Show));
            _openButtons.Add(new WindowOpenButtonPresenter(_viewContainer.MenuButton, _viewContainer.MenuLabel, UiTextKey.Menu, _menuWindow.Show));
            foreach (var button in _openButtons) button.Initialize();
        }

        // 見直すボタンと、今いる部屋の目印（「MapRoom:Current」）。チャレンジには物語もチュートリアルも無いので、見直すボタンも出さない
        private void CreateTutorial(bool mapTutorial)
        {
            _tutorialFlow = new MapTutorialUseCase(_tutorial, _confirmWindow, () => _tutorialPromptPresenter.Open(), StartFirstBuild);
            _tutorialPromptPresenter = new TutorialPromptPresenter(_confirmWindow, _tutorialFlow.HandleAnswered);
            _tutorialPromptPresenter.Initialize();

            _viewContainer.TutorialButton.gameObject.SetActive(_rule.HasStory);
            if (_rule.HasStory)
            {
                _tutorialButtonPresenter = new MapTutorialButtonPresenter(_viewContainer.TutorialButton, _viewContainer.TutorialLabel, _tutorialFlow.Replay);
                _tutorialButtonPresenter.Initialize();
            }
            _roomAnchor = TutorialAnchorRegistry.RegisterResolver("MapRoom", _ => _mapPresenter.CurrentCellTransform);
            if (mapTutorial) _tutorialFlow.PlayMapSequence();
        }

        private bool HasNextStage()
        {
            return _rule.GetStage(CampaignProgressRepository.CurrentStageIndex) != null;
        }

        // チュートリアルを見ないことにした: その場でチップを開けてデッキを組み、保存して、開けたチップを見せる
        private void StartFirstBuild()
        {
            var firstBuild = _run.InitializeDeckIfNeeded();
            if (firstBuild.Length > 0) _run.SaveProgress();
            ShowFirstBuild(firstBuild);
        }

        // 開けていなければ（空なら）ウインドウは開かない
        private void ShowFirstBuild(CardData[] firstBuild)
        {
            _firstBuildWindow.Show(new FirstBuildShowData(firstBuild, _balance.FirstBuildChipCount, CampaignDeckRepository.Deck.Length));
        }

        // 強化のウインドウは開くたびにモデルの値から始めるので、買い物で減ったトークンもモデルへ渡しておく
        private void HandleShopPurchased(CardData[] purchased, CardData[] singles, int remainingTokens)
        {
            _run.ApplyShopPurchase(purchased, singles, remainingTokens);
            _unitUpgradeModel.SetTokens(remainingTokens);
        }

        // 画面の切り替えの覆いが晴れてから会話を流し、読み終えたら next を呼ぶ（覆いの下でウインドウを開かないように）。
        // 会話が無ければ、すぐ next を呼ぶ
        private async UniTaskVoid PlayWhenShownAsync(ConversationData conversation, Action next, CancellationToken token)
        {
            await UniTask.WaitUntil(() => !SceneTransitionEffect.IsTransitioning, cancellationToken: token);
            _conversationWindow.Show(new ConversationShowData(conversation, next));
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _debugCommands?.Dispose();
#endif
            _mapPresenter?.Dispose();
            foreach (var button in _openButtons) button.Dispose();
            _openButtons.Clear();
            _tutorialButtonPresenter?.Dispose();
            _tutorialFlow?.Dispose();
            _roomAnchor?.Dispose();
            _tutorialPromptPresenter?.Dispose();
            _menuWindow?.Dispose();
            _optionsWindow?.Dispose();
            _cardShopWindow?.Dispose();
            _deckEditWindow?.Dispose();
            _unitUpgradeWindow?.Dispose();
            _enemyDetailWindow?.Dispose();
            _firstBuildWindow?.Dispose();
            _conversationWindow?.Dispose();
            _rewardWindow?.Dispose();
            _confirmWindow?.Dispose();
        }
    }
}

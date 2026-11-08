using System;
using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Interaction;
using Live2DCardRogue.Common.Statuses;
using Live2DCardRogue.BattleScene.Constants;
using Live2DCardRogue.BattleScene.Data;
using Live2DCardRogue.BattleScene.Directors;
using Live2DCardRogue.BattleScene.Models;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.ScriptableObjects;
using Live2DCardRogue.Common.SceneTransition;
using Live2DCardRogue.Common.WindowSystem;
using Live2DCardRogue.Common.Localization;
using Live2DCardRogue.Common.Presenters;
using Live2DCardRogue.Common.Tutorial;
using Cysharp.Threading.Tasks;
using Live2DCardRogue.BattleScene.Views;
using Live2DCardRogue.BattleScene.UseCase;
using Live2DCardRogue.BattleScene.Presenters;
using Live2DCardRogue.Common.Views;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Live2DCardRogue.Common.DebugMenu;
#endif

namespace Live2DCardRogue.BattleScene.Initializer
{
    // 戦闘シーンのコンポジションルート。モデル → UseCase / Director → Presenter の順に組み立て、戦闘のループを始める
    public sealed class BattleSceneInitializer : SceneInitializerBase<BattleSceneTransitionData>
    {
        // 以下はこのシーンを単体で再生したときのモックデータ。
        // タイトルから遷移してきた場合は BattleSceneTransitionData が優先される（CreateMockParameter参照）

        /// <summary>
        /// プレイヤーデッキのモックデータ
        /// </summary>
        [SerializeField] private CardData[] _playerDeckMock;

        /// <summary>
        /// 敵のモックデータ。手前から順に並べ、BattleSceneViewContainer.EnemyStatusViews と同じ順で対応させる。
        /// 同じEnemyDataを並べれば同じ敵が複数体出る
        /// </summary>
        [SerializeField] private EnemyData[] _enemyDataMocks;

        [SerializeField][Min(1)] private int _playerMaxHpMock = 100;
        [SerializeField][Min(0)] private int _playerMaxStaminaMock = 10;
        // 単体で再生したときに使えるトークン（トークンを払うコマンドを試す用）
        [SerializeField][Min(0)] private int _tokensMock = 500;
        // 単体で再生したときの BGM（ボス戦の曲を試す用）
        [SerializeField] private BgmId _bgmMock = BgmId.Battle;

        [SerializeField] private BattleSceneViewContainer _viewContainer;

        /// <summary>シーンの BGM。曲はステージごとに違う（ボス戦）ので、渡ってきたデータで差し替える</summary>
        [SerializeField] private SceneBgm _sceneBgm;

        /// <summary>プレイヤーの動く見た目（プレイヤーのデータアセットはまだ無いので、ここに置く）。未設定なら立ち絵の1枚絵のまま</summary>
        [SerializeField] private UnitVisualDefinition _playerVisual;

        /// <summary>グリッチ（コードインジェクション）で敵の行動に割り込ませる、敵専用のカード</summary>
        [SerializeField] private CardData _glitchCard;

        // ウインドウ。シーンには置かず、CreateWindows でまとめて生成する（開くのは各 Handler の Show）
        private WindowHandler<ConfirmShowData> _confirmWindow;
        private WindowHandler _optionsWindow;
        private WindowHandler _menuWindow;
        private WindowHandler _logWindow;
        private WindowHandler<BattlePileShowData> _pileWindow;
        // 画面に置いた、ウインドウを開くボタン（メニュー・ログ）
        private readonly List<PresenterBase> _openButtons = new List<PresenterBase>();

        // 作ってから破棄するまで、ほかから呼ぶことの無い Presenter（作った順に入れ、逆順に破棄する）
        private readonly List<PresenterBase> _presenters = new List<PresenterBase>();
        // 対象の候補一覧とチュートリアルの目印が、選んでいるカードの位置を聞く
        private BattleCardHandPilePresenter _handPilePresenter;

        private UnitStatusModel _playerStatusModel;
        private BattleCommandQueueUseCase _queueUseCase;
        private BattleCommandExecuteUseCase _executeUseCase;
        // 払ったトークンを戦闘の結果として戻り先へ渡す
        private BattleTokenModel _tokens;
        private BattlePhaseDirector _phaseDirector;
        private BattleLogWiring _log;
        private BattleTutorialEventBridge _tutorialBridge;
        private IDisposable _handCardAnchors;
        private IDisposable _tutorial;
        // 結果から戻り先のシーンのデータを作る（null なら戦闘画面に留まる）
        private Func<BattleResult, SceneTransitionData> _createReturnData;
        // マウスならカーソルを合わせて説明を出し、タッチならタップで出す（手札は2回目のタップで使う）
        private InteractionMode _interactionMode;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly List<IDisposable> _debugCommands = new List<IDisposable>();
#endif

        // 戦闘の間だけ使うモデル。組み立ての段どうしで受け渡す
        private sealed class BattleModels
        {
            public BattleCardPilesModel Piles;
            public BattleTokenModel Tokens;
            public CommandHistoryModel History;
            public BattlePhaseModel Phase;
            public CardCommandModel Commands;
            public CardSelectionModel Selection;
            // プレイヤーが今指しているユニット（強調表示用）
            public TargetFocusModel Focus;
            // カーソルを合わせているカード（詳細を大きく出す）
            public CardPreviewModel Preview;
            // 今効果を出しているカード（ダメージの数字に原因を添える）
            public CurrentActionModel CurrentAction;
            public EnemyPartyModel Enemies;
            // クリックだけでコマンドを使うときに狙う敵。敵ユニットのクリックで選び直す
            public PriorityTargetModel PriorityTarget;
        }

        protected override void Initialize(BattleSceneTransitionData data)
        {
            var parameter = data ?? CreateMockParameter();
            _createReturnData = parameter.CreateReturnData;
            _playerStatusModel = parameter.PlayerStatus;
            // 最大HPを超えた回復を溜められるのはプレイヤーだけ
            _playerStatusModel.CanOvercharge = true;
            _interactionMode = InteractionModeDetector.Current;
            if (_sceneBgm != null) _sceneBgm.Override(parameter.Bgm);

            var models = CreateModels(parameter);
            _tokens = models.Tokens;
            var targets = new BattleTargetResolver(
                _playerStatusModel,
                // 実行時に対象が倒れていた場合の振り直し先だけ、敵の並びを知っているモデルに聞かせる
                models.Enemies.FindNextLivingTarget,
                models.Enemies.GetLivingStatuses,
                () => models.PriorityTarget.Current);
            _queueUseCase = new BattleCommandQueueUseCase(_playerStatusModel, models.Piles, models.Commands, models.Selection, targets, models.Tokens);
            _executeUseCase = new BattleCommandExecuteUseCase(_playerStatusModel, models.Piles, models.Commands, targets, models.Tokens, models.History,
                models.Enemies.Summon, () => models.Enemies.DefeatedCount);
            _phaseDirector = new BattlePhaseDirector(
                _playerStatusModel, models.Enemies, models.Phase, models.Commands,
                new BattleDrawUseCase(_playerStatusModel, models.Piles),
                _executeUseCase,
                new BattleTurnEndUseCase(_playerStatusModel, models.Piles, models.Commands, models.Selection, models.History),
                // 行動の間の待ち時間は戦闘速度の設定で割る
                () => GameSettingsRepository.BattleSpeed,
                _glitchCard);

            CreatePilePresenters(models);
            CreateUnitPresenters(models);
            CreateTargetPresenters(models);
            Add(new BattleCardDetailPresenter(models.Preview, models.Selection, _viewContainer.CardDetailView));

            _log = new BattleLogWiring(_phaseDirector, _executeUseCase, _playerStatusModel, _viewContainer.PlayerStatusView.PortraitSprite, models.Enemies.Enemies);
            Add(new BattleLogFeedPresenter(_log.Log, models.Phase, _viewContainer.LogFeedView, _log.Formatter));
            CreateWindows(models);
            CreateOpenButtons();

            // 誰がカードを使ったかを数字の原因に使い、敵の行動には効果音を鳴らす
            Add(new BattleActionPresenter(_executeUseCase, _phaseDirector, _playerStatusModel, models.CurrentAction));

            _phaseDirector.OnBattleFinished += HandleBattleFinished;
            StartTutorial(parameter, models);
            _phaseDirector.StartBattleLoop(this.GetCancellationTokenOnDestroy()).Forget();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            RegisterDebugCommands();
#endif
        }

        private BattleModels CreateModels(BattleSceneTransitionData parameter)
        {
            var deck = new CardPileModel(Resolve(parameter.DeckIds, GameAssetRepository.FindCard));
            // 渡されたデッキは並べた順のままなので、最初の手札が毎回同じにならないよう切っておく。
            // チュートリアルは手順がデッキの並びに合わせてあるので切らない
            if (parameter.ShuffleDeck) deck.Shuffle();

            var enemies = new EnemyPartyModel(CreateEnemies(Resolve(parameter.EnemyIds, GameAssetRepository.FindEnemy), parameter.EnemyStrength));
            return new BattleModels
            {
                Piles = new BattleCardPilesModel(deck, new CardPileModel(new CardData[0]), new CardPileModel(new CardData[0])),
                Tokens = new BattleTokenModel(parameter.Tokens),
                History = new CommandHistoryModel(),
                Phase = new BattlePhaseModel(),
                Commands = new CardCommandModel(BattleConstants.MaxCommandCount),
                Selection = new CardSelectionModel(),
                Focus = new TargetFocusModel(),
                Preview = new CardPreviewModel(),
                CurrentAction = new CurrentActionModel(),
                Enemies = enemies,
                PriorityTarget = new PriorityTargetModel(enemies.GetLivingStatuses),
            };
        }

        // 手札（バッファ）・山札・アーカイブ・パージの山と、実行リスト
        private void CreatePilePresenters(BattleModels models)
        {
            _handPilePresenter = Add(new BattleCardHandPilePresenter(
                models.Piles.Hand, models.Phase, models.Selection, _viewContainer, _queueUseCase, _playerStatusModel, models.Commands, models.Preview, _interactionMode));
            if (_viewContainer.BufferEmptyView != null)
            {
                Add(new BattleBufferEmptyPresenter(_viewContainer.BufferEmptyView, models.Piles.Hand, models.Commands, models.Phase, _playerStatusModel));
            }
            Add(new BattleCardDeckPilePresenter(models.Piles.Deck, _viewContainer));
            Add(new BattleCardDiscardPilePresenter(models.Piles.Discard, _viewContainer));
            Add(new BattleCardPurgePilePresenter(models.Piles.Purge, _viewContainer));
            Add(new BattleCommandListPresenter(models.Commands, models.Phase, _viewContainer, _queueUseCase, models.Focus, models.Preview, models.Selection, _interactionMode));
        }

        // ユニットの状態・次の行動の予告・動く見た目。
        // どのユニットも対象に選ばれ得るので配線は同じ。どのカードでどちらを選べるかは UseCase が決める
        private void CreateUnitPresenters(BattleModels models)
        {
            Add(new UnitPresenter(
                _playerStatusModel, _viewContainer.PlayerStatusView, models.Selection, models.Focus, showStamina: true,
                // プレイヤーの立ち絵はシーンの PlayerUnitView/Portrait に置いてある絵を使い、名前は出さない
                portrait: null, enemyData: null,
                canBeTargeted: _queueUseCase.CanSelectTarget,
                onTargetClicked: target => _queueUseCase.SelectTarget(target),
                currentActionCard: () => models.CurrentAction.Card));

            var enemies = models.Enemies.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var view = _viewContainer.EnemyStatusViews[i];
                Add(new UnitPresenter(
                    enemies[i].Status, view, models.Selection, models.Focus, showStamina: false,
                    portrait: enemies[i].Data.Portrait,
                    enemyData: enemies[i].Data,
                    canBeTargeted: _queueUseCase.CanSelectTarget,
                    // 対象を選んでいる途中ならその対象に、そうでなければ優先ターゲットの切り替え
                    onTargetClicked: target =>
                    {
                        if (!_queueUseCase.SelectTarget(target)) models.PriorityTarget.Select(target);
                    },
                    currentActionCard: () => models.CurrentAction.Card));
                if (view.IntentView != null) Add(new EnemyIntentPresenter(enemies[i], view.IntentView, models.Preview, _interactionMode));
            }

            // タッチでは、状態効果の説明もタップで開く（ほかをタップするまで開いたまま）
            if (_interactionMode == InteractionMode.Touch)
            {
                Add(new StatusEffectTapPresenter(_viewContainer.PlayerStatusView, models.Preview));
                foreach (var view in _viewContainer.EnemyStatusViews) Add(new StatusEffectTapPresenter(view, models.Preview));
            }

            // 動く見た目にするとアイコンもその定義のものに替わるので、アイコンを使う候補一覧（CreateTargetPresenters）より先に作る
            Add(new UnitVisualPresenter(_playerStatusModel, _viewContainer.PlayerStatusView, _playerVisual, models.CurrentAction));
            for (int i = 0; i < enemies.Count; i++)
            {
                Add(new UnitVisualPresenter(enemies[i].Status, _viewContainer.EnemyStatusViews[i], enemies[i].Data.Visual, models.CurrentAction));
            }
        }

        // 対象の候補一覧（選んだカードの上に出す）と、敵の頭上の優先ターゲットの印
        private void CreateTargetPresenters(BattleModels models)
        {
            var enemies = models.Enemies.Enemies;
            // 並びは画面の左から: プレイヤー、敵（手前から順）
            var candidates = new List<BattleTargetCandidate> { new BattleTargetCandidate(_playerStatusModel, _viewContainer.PlayerStatusView.PortraitSprite) };
            var enemyStatuses = new List<UnitStatusModel>();
            var enemyViews = new List<BattleUnitStatusView>();
            for (int i = 0; i < enemies.Count; i++)
            {
                candidates.Add(new BattleTargetCandidate(enemies[i].Status, _viewContainer.EnemyStatusViews[i].PortraitSprite));
                enemyStatuses.Add(enemies[i].Status);
                enemyViews.Add(_viewContainer.EnemyStatusViews[i]);
            }

            Add(new BattleTargetPickerPresenter(
                models.Selection, models.Focus, _viewContainer.TargetPickerView, candidates,
                canBeTargeted: _queueUseCase.CanSelectTarget,
                onTargetPicked: target => _queueUseCase.SelectTarget(target),
                pendingCardAnchor: () => _handPilePresenter.PendingCardTransform));
            Add(new PriorityTargetPresenter(models.PriorityTarget, enemyStatuses, enemyViews));
            Add(new EnemySummonPresenter(models.Enemies, enemyViews));
        }

        // 戦闘の出来事を流し、手札のカードを「HandCard:<コマンドの ID>」で指せるようにする（チュートリアルが無ければ誰も聞いていないだけ）。
        // 最後まで進んだら戦闘はそのまま続ける。スキップしたら、確かめたうえで練習の戦闘をそのまま終えて戻る
        private void StartTutorial(BattleSceneTransitionData parameter, BattleModels models)
        {
            _tutorialBridge = new BattleTutorialEventBridge(models.Commands, models.Phase, _phaseDirector);
            _handCardAnchors = TutorialAnchorRegistry.RegisterResolver("HandCard", id => _handPilePresenter.FindCardTransform(id));
            // 手順はチュートリアルを終えるまで CampaignTutorialRepository が持っている
            if (parameter.PlayTutorial) _tutorial = TutorialUseCase.Play(CampaignTutorialRepository.BattleSequence, HandleTutorialEnded, ConfirmTutorialSkip);
        }

        private T Add<T>(T presenter) where T : PresenterBase
        {
            presenter.Initialize();
            _presenters.Add(presenter);
            return presenter;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // デバッグ機能（0707で表示）。リリースビルドには入らない
        private void RegisterDebugCommands()
        {
            _debugCommands.Add(DebugMenuOverlay.AddCommand("Battle: Win", () => _phaseDirector.DebugForceFinish(isVictory: true)));
            _debugCommands.Add(DebugMenuOverlay.AddCommand("Battle: Lose", () => _phaseDirector.DebugForceFinish(isVictory: false)));
            _debugCommands.Add(DebugMenuOverlay.AddIntCommand("Battle: Speed %", Mathf.RoundToInt(GameSettingsRepository.BattleSpeed * 100f), percent => GameSettingsRepository.SetBattleSpeed(percent / 100f)));
        }
#endif

        // 遷移データの ID を、預かったコマンド・敵に引き直す。見つからないもの（エラーはリポジトリが出す）は外して始める
        private static T[] Resolve<T>(string[] ids, Func<string, T> find) where T : class
        {
            var found = new List<T>(ids.Length);
            foreach (var id in ids)
            {
                var asset = find.Invoke(id);
                if (asset != null) found.Add(asset);
            }
            return found.ToArray();
        }

        // BattleSceneを単体で再生したときは何も渡ってこないので、Inspectorのモックデータで立ち上げる
        private BattleSceneTransitionData CreateMockParameter()
        {
            Debug.Log("--- 引き継ぎデータが無いため、Inspectorのモックデータで開始します ---");
            return new BattleSceneTransitionData(
                new UnitStatusModel(_playerMaxHpMock, _playerMaxStaminaMock),
                _playerDeckMock,
                _enemyDataMocks,
                tokens: _tokensMock,
                bgm: _bgmMock);
        }

        // 敵データと表示先のViewを手前から順に突き合わせる。ステージによって敵の数は変わるので、余ったViewは隠す。
        // 逆に View が足りない場合は出したい敵を出せないため、そこだけエラーにする
        private List<BattleEnemy> CreateEnemies(EnemyData[] enemyDataList, EnemyStrength strength)
        {
            var views = _viewContainer.EnemyStatusViews;
            int count = Mathf.Min(enemyDataList.Length, views.Count);
            if (enemyDataList.Length > views.Count)
            {
                Debug.LogError($"敵データ({enemyDataList.Length})に対して敵View({views.Count})が足りません。{count}体で開始します");
            }
            var enemies = BattleEnemyFactory.Create(enemyDataList, count, strength);

            // 生成される敵は、出てくるまで隠した空きの枠に置く（枠が足りなければ生成できないだけ）
            foreach (var reserve in BattleEnemyFactory.CreateReserves(enemies, strength))
            {
                if (enemies.Count >= views.Count)
                {
                    Debug.LogError($"生成される敵（{reserve.Data.Id}）を置く敵Viewが足りません");
                    break;
                }
                enemies.Add(reserve);
            }

            for (int i = 0; i < views.Count; i++)
            {
                views[i].gameObject.SetActive(i < enemies.Count && !enemies[i].IsReserve);
            }
            return enemies;
        }

        // 戦闘の結果を戻り先のシーンへ渡す。戻り先が指定されていなければこの画面に留まる
        private void HandleBattleFinished(bool isVictory)
        {
            if (_createReturnData == null)
            {
                Debug.Log("--- 戻り先が指定されていないため、この画面に留まります ---");
                return;
            }

            // マイニングで積んだトークンの上乗せ（% ）も渡す。勝ったときだけ効くかどうかは受け取る側が決める
            int tokenBonus = 0;
            foreach (var effect in _playerStatusModel.StatusEffects)
            {
                if (effect.Id == TokenBonusStatusEffect.EffectId) tokenBonus += effect.Value;
            }

            // どこへ戻るか（チャレンジで負けたらリザルトへ、など）は遷移元が決める
            var result = new BattleResult(isVictory, tokenBonus, _tokens.Spent);
            SceneTransitionUseCase.LoadScene(_createReturnData.Invoke(result));
        }

        private void HandleTutorialEnded(bool completed)
        {
            Debug.Log($"--- 戦闘のチュートリアル{(completed ? "を終えた" : "をスキップ")} ---");
            if (completed) return;

            // スキップしたら、練習の戦闘はここで終えて戻る（戻り先が無い単体の再生では、戦闘を続ける）。
            // 戻り先には負けとして返す（練習の戦闘なので、マップは結果を使わない）
            if (_createReturnData == null) return;
            SceneTransitionUseCase.LoadScene(_createReturnData.Invoke(new BattleResult(false)));
        }

        // 戦闘で使うウインドウをまとめて生成する（閉じた状態で置き、開くのは各 Handler の Show）。
        // 戦闘は、どのウインドウが開いている間も止めない
        private void CreateWindows(BattleModels models)
        {
            var popup = _viewContainer.PopupCanvas;

            // 確認ウインドウは戦闘で1つを共用する（オプション・あきらめる・チュートリアルのスキップ）
            _confirmWindow = WindowUseCase.Create(new ConfirmDialogInitData(), popup);
            // ユーザーデータの削除はタイトルだけ
            _optionsWindow = WindowUseCase.Create(new GameOptionsInitData(_confirmWindow), popup);
            // あきらめるは決着がつくまで押せ、確かめて OK したら敗北として戦闘を打ち切る
            _menuWindow = WindowUseCase.Create(new MenuWindowInitData(UiTextKey.Menu, _confirmWindow,
                new MenuItemData(UiTextKey.Options, _optionsWindow.Show),
                new MenuItemData(UiTextKey.GiveUp, _phaseDirector.Surrender, MenuItemStyle.Danger,
                    canPress: () => !_phaseDirector.IsDecided,
                    confirm: new MenuConfirmData(UiTextKey.GiveUpConfirm, UiTextKey.GiveUpConfirmDetail, UiTextKey.GiveUp, UiTextKey.Back))), popup);
            _logWindow = WindowUseCase.Create(new BattleLogInitData(_log.Log, _log.Formatter), popup);
            _pileWindow = WindowUseCase.Create(new BattlePileInitData(models.Piles.Discard, models.Piles.Purge), popup);
        }

        // アーカイブ・パージの山は、クリックするとその中身の一覧を開く
        private void CreateOpenButtons()
        {
            _openButtons.Add(new WindowOpenButtonPresenter(_viewContainer.MenuButton, _viewContainer.MenuButtonLabel, UiTextKey.Menu, _menuWindow.Show));
            _openButtons.Add(new WindowOpenButtonPresenter(_viewContainer.LogButton, _viewContainer.LogButtonLabel, UiTextKey.BattleLog, _logWindow.Show));
            foreach (var button in _openButtons) button.Initialize();

            _viewContainer.DiscardCountView.OnClicked += ShowArchive;
            _viewContainer.PurgeCountView.OnClicked += ShowPurge;
        }

        private void ShowArchive() => _pileWindow.Show(new BattlePileShowData(BattlePileKind.Archive));

        private void ShowPurge() => _pileWindow.Show(new BattlePileShowData(BattlePileKind.Purge));

        // スキップすると戦闘を終えてマップに戻ることも添える。ほかの確認の途中なら尋ねず、スキップしなかったことにする
        private void ConfirmTutorialSkip(Action onSkip, Action onCancel)
        {
            if (_confirmWindow.IsOpen)
            {
                onCancel.Invoke();
                return;
            }
            _confirmWindow.Show(new ConfirmShowData(
                LocalizationHelper.GetUiText(UiTextKey.TutorialSkipConfirm),
                LocalizationHelper.GetUiText(UiTextKey.TutorialSkipBattleDetail),
                LocalizationHelper.GetUiText(UiTextKey.TutorialSkipOk), LocalizationHelper.GetUiText(UiTextKey.Back),
                onSkip, onCancel));
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            foreach (var command in _debugCommands) command.Dispose();
            _debugCommands.Clear();
#endif
            for (int i = _presenters.Count - 1; i >= 0; i--) _presenters[i].Dispose();
            _presenters.Clear();
            foreach (var button in _openButtons) button.Dispose();
            _openButtons.Clear();
            _viewContainer.DiscardCountView.OnClicked -= ShowArchive;
            _viewContainer.PurgeCountView.OnClicked -= ShowPurge;
            _tutorial?.Dispose();
            _handCardAnchors?.Dispose();
            _tutorialBridge?.Dispose();
            _pileWindow?.Dispose();
            _logWindow?.Dispose();
            _menuWindow?.Dispose();
            _optionsWindow?.Dispose();
            _confirmWindow?.Dispose();
            _log?.Dispose();
            if (_phaseDirector != null)
            {
                _phaseDirector.OnBattleFinished -= HandleBattleFinished;
                _phaseDirector.Dispose();
            }
        }
    }
}

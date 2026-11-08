# BattleScene(日本語版)

`Live2DCardRogue.BattleScene.*`(`Assets/Scripts/Scenes/BattleScene/`)の詳細ドキュメントです。[docs/battle-scene.md](battle-scene.md)(英語版、`CLAUDE.md`から`@`importされる方)の対訳。まずルートの[CLAUDE.ja.md](../CLAUDE.ja.md)でこのシーンが従う一般的なレイヤー/配線の規約を読んでください — このファイルはBattleScene固有の内容だけを扱います。

## コンポジションルート

`BattleSceneInitializer.Initialize(data)`(`SceneInitializerBase`が呼ぶ。`data`がnullならInspectorのモックを使う)は、順に: モデル(`CreateModels`。privateの`BattleModels`にまとめる: `BattleCardPilesModel`(山札/手札/捨て札/パージと戻り待ち)、`BattleTokenModel`、`CommandHistoryModel`、`BattlePhaseModel`、`CardCommandModel`(実行リスト)、`CardSelectionModel`(対象選択待ちのカード)、フォーカス・プレビュー・実行中の行動・優先ターゲットのモデル、`EnemyPartyModel`)、コマンドのユースケース(`BattleDrawUseCase`、`BattleCommandQueueUseCase`、`BattleCommandExecuteUseCase`、`BattleTurnEndUseCase`。`BattleTargetResolver`を1つ共有。下の「コマンドのユースケース」参照)、`BattlePhaseDirector`、山札・ユニット・対象のPresenter、バトルログ(`BattleLogWiring`。`BattleLogRecorder`をDirectorとユースケースにつなぐ)、ウインドウを構築し、バトルループを開始します。Presenterはすべて`Add`を通し、`OnDestroy()`で逆順に破棄します。敵は`CreateEnemies()`が組み立てます: 敵データと`BattleSceneViewContainer.EnemyStatusViews`を**手前から順に**突き合わせ(余ったViewは隠す)、`UseCase/BattleEnemyFactory`が1体につき`UnitStatusModel`1つ(`EnemyStrength`と初期の状態効果込み)を作って`BattleEnemy`(`Data/BattleEnemy.cs`)にまとめ、それらを`EnemyPartyModel`(`Models/EnemyPartyModel.cs`)に入れます — 並び順を知っているのはここだけで、次に行動するのが誰かも、倒れた対象のカードが誰に振り直されるかも、同じ並びから決まります。同じ`EnemyData`アセットを何度並べてもよく(1体ごとに作られるのは`UnitStatusModel`だけ)、現在のシーンはこれでトロイの木馬2体を出しています。渡されたデッキはここで一度**シャッフル**します(`deck.Shuffle()`)。そうしないと、最初の手札がデッキを並べた順のままになるためです。余ったViewは隠します(ステージによって敵は少ないことがある)。敵がViewより多いときは出せない敵を落とし、エラーログを出します — 敵が黙って1体消えている方が、赤いログが出るより気付きにくいためです。`UnitPresenter`はプレイヤー側も敵側もすべて同じ配線です(`canBeTargeted` / `onTargetClicked`のどちらも`BattleCommandQueueUseCase`を指します) — どれも対象に選ばれ得るためで、そのカードでどちら側を選べるかを決めるのは配線ではなくユースケースです。正確な配線順序はファイル自体を参照してください(短いので「何が何に依存しているか」の一次情報源として十分です)。

## ターン/フェーズループ

`BattlePhaseDirector.StartBattleLoop`は、両ユニットのHPが残っている間、非同期ループ(UniTask)を回します: `ExecutePlayerTurnAsync` → `ExecuteEnemyTurnAsync` → 繰り返し。`BattlePhaseModel.CurrentPhase`(`BattlePhase.Draw` / `Player` / `PlayerExecution` / `Enemy`)はループの進行に合わせて更新され、他のコードが挙動を制御するために参照する唯一の情報源です — 例えば`BattleCardHandPilePresenter`は`CurrentPhase == BattlePhase.Player`でない限りカードのクリックを捨てますし、`BattleCommandListPresenter`が実行ボタンを`interactable`にするのもこのフェーズの間だけです。`Draw`は`ExecutePlayerTurnAsync`内の`BattleDrawUseCase.Draw()`実行中だけ同期的に有効になり、途中で観測されることはありません — 将来`BattlePhaseModel.OnPhaseChanged`の購読者がドロー処理専用の通知を必要とした時のために存在しています。

`PlayerExecution`は、実行ボタンを押してから積んだカードが1枚ずつ処理され終わるまでの区間です。これは「プレイヤーのターンである」ことと「プレイヤーが操作できる」ことを別の事実として扱うために存在します — これが無いと、リストを消化している最中もフェーズは`Player`のままで、既に消化中のリストにクリックでカードを積めてしまいます。

各ユニットの`UnitStatusModel.StartTurn()` / `EndTurn()`は、対応する`Execute*TurnAsync`の中で**そのユニット自身の**ターンを挟みます — すなわち状態効果のターンフックは自分のターンでだけ発火します。`BattleTurnEndUseCase.EndPlayerTurn()`とは別物であることに注意してください: 前者は手札を捨て・スタミナを回復し・実行リストに残っていたものを片付けるもので、`UnitStatusModel.EndTurn()`は状態効果のフック実行と期限切れの掃除です。両方がこの順番で起き、どちらもDirectorが呼びます(ボタンからはどちらも呼ばれません)。

プレイヤーフェーズは`UniTaskCompletionSource`でブロックしており、これは`CardCommandModel.OnExecuteRequested`が発火して初めて完了します。発火させるのは`BattleCommandQueueUseCase.RequestExecuteCommands()`です(実行ボタン。配線は`BattleCommandListPresenter`)。イベントは両者が既に持っているモデルにあるので、Directorは自分のコンストラクタでこれを購読し`Dispose()`で解除し、`BattlePhaseDirector`を参照するユースケースはありません。

その後`ExecutePlayerCommandsAsync`がフェーズを`PlayerExecution`にし、リストが空になるまで`PlayerCommandDelayMs`ごとに`BattleCommandExecuteUseCase.ExecuteNext()`を呼びます。敵のHPが0になった時点で抜けるので、倒したあとの残りを死体に撃ち込みません。実行し切ったあとは`PlayerTurnEndDelayMs`だけ待ってから戻るので、最後の1枚の結果が画面に残ったまま手札が捨て札に流れることはありません。1枚も積んでいなかった場合はこの待機を入れないので、何もせずターンを終える操作が重くなることもありません。形は意図的に`ExecuteEnemyTurnAsync`と揃えてあります: 進行の間合いはDirectorが持ち、どれだけ起こり得るかはモデルが持つ、という分担です。リストの長さは`CardCommandModel`の上限で既に抑えられているので、このループは自前の回数管理を持ちません — 「リストが尽きるまで」で安全です。

`ExecuteEnemyTurnAsync`は`EnemyPartyModel.Enemies`を**手前から順に**回し、1体が行動を終えてから次の敵に移ります。予定したカードは、まず`enemy.Status.CalculateLimit(StatLimitType.ActionCount, 予定の枚数)`の枚数に切り詰めます。停止した敵がターンを飛ばすのはこの仕組みです: `StartTurn()`、`BattleEnemy.PlannedCards`(プレイヤーのターンの頭で`EnemyData.PickCards()`を**1度だけ**呼んで決めたもの。下の「敵の予告とグリッチ」参照)を`foreach`(各行動の前に`EnemyActionDelayMs`待機して`BattleCommandExecuteUseCase.ExecuteEnemyCard(card, enemy.Status, playerModel)`)、最後に`EndTurn()`。倒れている敵はまるごと飛ばすので、行動もしなければ状態効果のターンフックも走りません。何回行動するか・どのカードを・どの順番でかというターン全体は、敵の`EnemyData`アセットが持つ`[SerializeReference] IEnemyCardPicker`が組み立てます(ルートのドキュメントの「敵データ」を参照)。したがって`BattlePhaseDirector`は`EnemyData`を保持するだけで、選択ロジックについては何も知りません。現状これが敵の「AI」のすべてです。

こちらのループにも防御は不要です: `PickCards()`は常にnullでないリストを返し、その長さはアセットの行動回数で既に上限がかかっています。途中で抜けるのは`_playerModel.CurrentHp == 0`のときだけで(1体の行動中と、敵から敵へ移る間の両方で見ています)、倒れたプレイヤーを残りの予定行動分殴り続けないようにしています。

戦闘が終わるのは、プレイヤーが倒れたときか、**すべての**敵が倒れたとき(`EnemyPartyModel.AreAllDefeated`)です。プレイヤーの実行リストを途中で打ち切る条件も同じものです。1体だけ倒れた場合は止まりません: `BattleCommandExecuteUseCase.ExecuteNext`が実行の直前に対象を見て、倒れていれば次の敵に振り直すためです(下の「実行リスト」参照)。

**終わったあとの行き先と、敵の強さは遷移のデータで決まります。** 終わると戦闘は`BattleResult`(勝敗・マイニングの上乗せ・払ったトークン)を作り、`BattleSceneTransitionData.CreateReturnData`がそれから作ったデータでロードします。マップが渡す関数は`MapSceneTransitionData`に包むか、チャレンジでの負けを`ChallengeResultTransitionData`でResultSceneへ送ります。戦闘はシーン名を一切見ません。関数が無ければ(単体再生)留まります。練習の戦闘でチュートリアルをスキップしたときも同じく負けとして返し、マップはチュートリアル中なので使いません。`BattleSceneTransitionData.EnemyStrength`(既定は`EnemyStrength.None`)は`CreateEnemies`で当てはめます。最大HPを`HpPercent`倍にし、`AttackBonus`が0より大きければ`LayerBoostStatusEffect`を付けます。どのモードの何階層の値なのかを戦闘は知りません([docs/map-scene.ja.md](map-scene.ja.md)の「チャレンジ」参照)。

**デバッグ: 勝敗を強制する。** `BattlePhaseDirector.DebugForceFinish(isVictory)`(エディタとDevelopment Buildのみ。ルートの「デバッグ機能」参照)は、負ける側のHPを0にしてから(敵はボスが即死無効なので`TakePiercingDamage`で、プレイヤーはオーバーチャージに肩代わりされない`InstantKill`で)、ターンの途中でもループを打ち切ります。`RunTurnsAbortableAsync`が、シーンのトークンにつないだトークンで`RunTurnsAsync`を回し、自分が起こした取り消しだけを握りつぶします。手札を触れないようフェーズは`PlayerExecution`のままにし、その後は通常の決着(勝敗のトースト → `OnBattleFinished`)と同じ流れなので、キャンペーンならマップへ戻って報酬も普段どおり出ます。`BattleSceneInitializer`が0707のデバッグパネルに**Battle: Win / Battle: Lose**として登録します。`RunTurnsAbortableAsync`そのものはどのビルドにも入ります(下の「あきらめる」が使う)。デバッグ専用なのは`DebugForceFinish`だけです。

**あきらめる。** `BattlePhaseDirector.Surrender()`は同じ打ち切りで戦闘を敗北として終えます。HPは減らしません(ログに「倒れた」を出さない)。決着の処理は`_isSurrendered`を見て「敗北... 接続を切断した」を出し、`OnBattleFinished(false)`を送るので、マップでは普通の敗北(バックアップブースト)として扱われます。決着後(`IsDecided`)は何もしません。

## バトルログ(`Window/BattleLog/`、`UseCase/BattleLogRecorder.cs`、`Presenters/BattleLogFeedPresenter.cs`)

敵味方の行動は`BattleLogModel`に書き込まれ、行動中は上部中央にライブで表示され、**ログ**ウインドウでいつでも振り返れます。

ログのデータは`Common/`にあります(`Common/Models/BattleLog/`に`BattleLogModel`・`BattleLogKind`・`BattleLogEntry`、`Common/Presenters/BattleLog/`に`BattleLogFormatter`・`BattleLogLine`)。ログのウインドウの初期化データがモデルとフォーマッタを運ぶためです。ウインドウ自体は`Window/BattleLog/`(Presenter、View、`BattleLogRowView`、Initializer)です。バトルだけがすることはここに残します: `BattleLogRecorder`(バトルのModelを見張る)とライブ表示(`BattleLogFeedPresenter` / `BattleLogFeedView`)です。

- **データで記録し、表示するときに文章にする。** `BattleLogEntry`が持つのは種類(`BattleLogKind`)・ユニット・カード・状態効果・数値だけで、文章は持ちません。`BattleLogFormatter`が`UIText`の`Log*`の書式で`BattleLogLine`にするので、言語を切り替えればログ全体が訳し直されます。同じ敵が2体いればA・Bを付けます(`トロイの木馬A`)。
- **カードの効果はログを知らない。** `BattleLogRecorder`が全ユニットの`OnHpChanged`・`OnStatusEffectsChanged`・`OnStatusEffectValuesChanged`を見張り、*差分*を書きます(ダメージ・回復・状態効果の付与/増減/消滅・撃破)。ブロックが攻撃を吸収したときは値が変わるだけで通知が来ないので、HPの変化のたびにも状態効果の差分を取ります。5のブロックに攻撃が入れば「ブロック -5」→「3ダメージ」です。時間制の効果は付いたときと消えたときだけ書き、残りターンが減るたびには書きません。0まで使い切ったブロックが掃除されても重ねて書きません。新しいカード効果を足しても、ここを直さずに記録されます。
- **進行側からの通知。** Initializerが次のものを`Record*`につなぎます:
  - `BattleCommandExecuteUseCase.OnCardExecuting`(使用者・カード・対象。プレイヤーも敵も)
  - `BattlePhaseDirector.OnPlayerTurnStarted` / `OnEnemyTurnStarted`(ターンの区切り)
  - `OnUnitTurnEnding` / `OnUnitTurnEnded`。この間のHPの変化は継続ダメージ/継続回復として書き、それを起こした状態効果(`UnitStatusModel.LastHpChangeSource`)を持たせるので、行には効果の名前が出ます:「トロイの木馬Aにウイルスで4ダメージ」(`UiTextKey.LogDamageFrom` / `LogHealFrom`)。効果が分からないときだけ、汎用の「継続効果で」(`LogDamageOverTime` / `LogHealOverTime`)に戻ります
  - `OnEnemyStunned`
- **状態効果は数値入りで説明する。** 状態効果が付いたり増えたりするたびに、レコーダーは`StatusDetail`の記録を足します。効果のインスタンスは値が変わり続けるので、*その時点の*値(と時間制の効果の`Amount`)を控えておきます。表示は一段深く字下げして控えめにし、文章は`StatusEffectLogDetail`の表の書式で作ります(`{0}`は値・残りターン、`{1}`は効きの大きさ)。データ破損なら「2ターンの間、回復がダメージになる」、ブロックなら「攻撃ダメージを5まで防ぐ」です。0まで減った(使い切ったブロックなど)ときは説明を付けません。
- **敵味方で色分け。** 各記録には`Actor`(カードの使用者、またはターンを終えるユニット)が付きます。味方の行動はシアン、敵の行動はマゼンタ、ターンの区切りは黄色です。結果の行はカードの行の下に字下げします。各行には主語のユニットの顔が出ます(`BattleLogRowView`)。
- **ライブ表示**(`BattleLogFeedPresenter` / `BattleLogFeedView`、`Canvas/LogFeed`。`CardDetail`と同じ場所の、すぐ手前に描く)。`PlayerExecution`で開いて敵の番の間も開いたままにし、直近のターンの区切りから見せます。フェーズが`Draw` / `Player`に戻ると、少し待ってから閉じます。最新の7行を下詰めで出し、新しい行はちらつきながら入ります。入力は受けません。
- **ログウインドウ**(`BattleLogWindowPresenter` / `BattleLogWindowView`。BattleSceneの`PopupCanvas`上の`Prefabs/BattleLogWindow.prefab`を、左下の`Canvas/LogButton`で開く。実行ボタンの隣で補助の操作と読めるよう、`CyberButton`を`localScale` 0.7倍にしている。斜めのスライス画像は縦に伸ばすと折れるので、高さではなく縮尺で小さくする)。戦闘全体のログを、`BattleLogRowView.prefab`の行の`CommonScrollList`で見せます。右端の細いシアンのスクロールバー(`ScrollRect.verticalScrollbar`。全部が収まるときは隠れる)でスクロールできることを見せ、表示枠の`RectMask2D`には縦の`softness`を付けて、行が上下の端で切れずにふわっと消えるようにしています。開くと一番下まで送り(`CommonScrollList.ScrollToEnd`)、一番下を見ていたときだけ新しい行を追いかけます。閉じるボタンか背面のクリックで閉じ、戦闘はその後ろで進み続けます。

## パージ・ループバック・コマンド切れ(2026-09-27)

- **パージの山。** `BattleSceneInitializer`が4つ目の山`Purge`を`BattleCardPilesModel`に持たせます。`CardData.PurgeOnUse`が立っているコマンド(`Mining`)や、`Multithread`が効いたコマンドは、アーカイブではなくここへ行き、この戦闘の間は山札に戻りません。数は`Canvas/PurgeCountView`(アーカイブの`CardPileCountView`の複製。0.75倍でその右、マゼンタ寄りの色)に出し、`BattleCardPurgePilePresenter`が`UiTextKey.PurgePile`(パージ / Purged)のラベルと一緒に流し込みます。
- **コマンドの行き先。** `BattleCommandExecuteUseCase.ExecuteNext`は効果を実行したあと、`BeginCommand`と`EndCommand`の間で`player.CalculateCommandDestination(PurgeOnUse ? Purge : Discard)`に行き先を聞きます(ルートの「状態効果」参照)。`ReturnToHand`なら`BattleCardPilesModel.Returning`に置き、`BattleDrawUseCase.Draw`が引く**前に**手札へ移すので、戻ったコマンドも手札の枚数に数えます。`Multithread`が効くと効果を`CommandRepeat`回実行し、途中で対象が倒れたら次の敵へ振り直します。`OnCardExecuting`(ログの「使った」行)は1回だけです。
- **エクスプロイトチェーンは1度きり。** チェーンの段の効果は`IChainCardEffect`(`StartsChain`、`CompletesChain(user)`)を実装します。`BattleCommandExecuteUseCase.ExecuteNext`は、コマンドを実行する前に「これでチェーンが成立するか」を聞き(実行するとチェーンの状態が消えるため)、このターンの段を覚えておきます(`CommandHistoryModel.ChainCards`。1段目で数え直し、`BattleTurnEndUseCase.EndPlayerTurn`で忘れる)。成立したら、最後の段をパージの山へ送り、それまでの段もアーカイブ(ループバックで戻り待ちにいればそこ)からパージの山へ移します。どれも`OnCardDisposed`で知らせるので、ログにも出ます。3段目が順番どおりに実行されれば、相手が即死を無効にしていても成立とみなします。崩れたチェーンは普段どおりアーカイブへ行きます(`ExploitChainPurgeTests`)。
- **パージで負ける。** 山札・手札・アーカイブ・実行リスト・戻り待ちがすべて空で、パージされたものがあると`IsDeckExhausted`がtrueになります。Directorはプレイヤーのコマンドを1つ実行するたびにこれを見て、敗北で戦闘を終えます(`OnDeckExhausted`、トースト「敗北... コマンドがすべてパージされた」)。ただし、その最後のコマンドで敵を全滅させていれば勝ちです。
- **ログ。** `OnCardDisposed`(アーカイブ以外へ行ったプレイヤーのコマンド)は`CardPurged` / `CardReturned`、Directorの`OnDeckExhausted`は`DeckExhausted`として書きます(`UiTextKey.LogCardPurged`、`LogCardReturned`、`LogDeckExhausted`)。
- **トークン。** 戦闘はプレイヤーのトークンを`BattleSceneTransitionData.Tokens`で受け取り(単体再生の代わりは`BattleSceneInitializer._tokensMock`)、`BattleTokenModel`に持たせます。トークンを払うコマンド(クラウドバースト)は積んだときに(`BattleCommandQueueUseCase`)そこから払い、バッファに戻すと返します。払った分は勝っても負けても`BattleResult.TokensSpent`で返します。
- **リドゥと事情。** `BattleCommandExecuteUseCase.ExecuteNext`はリドゥを一つ前のコマンドとして実行し(`ExecutePrevious`。`BattleTargetResolver.Resolve`で振り直し、チェーンの段は飛ばす)、それ以外に実行したコマンドを覚えます。バッファや全ユニットを見る効果には、実行時に作った`CardExecutionContext`を渡します(ルートのドキュメントの「カードシステム」参照)。
- **トークンの上乗せ。** `HandleBattleFinished`がプレイヤーの`TokenBonusStatusEffect`の値を足して`BattleResult.TokenBonusPercent`にし、マップが勝利のトークンに掛けます([docs/map-scene.ja.md](map-scene.ja.md))。

- **パージするものを選ぶ(バッファオーバーフロー)。** `HandPurgeCount > 0`のカードでは、`SelectTarget`はすぐには積まず、`CardSelectionModel.BeginPurgeChoice(card, target, capacity)`を呼びます。これが`PendingCard`を空にし(対象の候補一覧は閉じる)、`OnPurgeChoiceChanged`を飛ばします。手札Presenterはそのカードを浮かせたまま`Canvas/PurgePicker`(`BattlePurgePickerView`。`// SELECT PURGE`、案内、`n/2`、決定の`CyberButton`。対象の候補一覧と同じくカードに付いていく)を開き、ほかのバッファのクリックを選ぶ/外すに変えます(`BattleCardView.SetPurgeMarked`。`CardView.prefab`の`PurgeMark`でマゼンタの`PURGE`を重ね、選んだものは半分だけ浮かせる)。その間はカーソルを合わせても選択は動かず、どれも暗くせず、何も無い所のクリックで取り消します。決定で`ConfirmPurgeChoice(cards)`(1個以上・上限以下・どれもバッファにあり、積むカード自身ではない)を呼ぶと、選んだものはコマンドと一緒にバッファから抜けてパージの山へ飛び、実行まで`CardCommand`に預けられます。コマンドを戻すと一緒に戻り、ターンの終わりに残っていればコマンドと一緒にアーカイブへ行きます。ほかにパージできるバッファが無いときは選べません(「パージできるバッファがありません」)。
- **選んでいる間、実行ボタンは決定ボタンになる。** パージの選択中は実行ボタンの文字が「決定」(`UiTextKey.PurgeConfirm`)になり、パネルの決定とまったく同じことをします。1個も選んでいなければ押せません。どちらもモデルを通ります。手札Presenterが選んだ数を伝え(`CardSelectionModel.SetPurgeChosenCount`)、`BattleCommandListPresenter`は`IsChoosingPurge` / `CanConfirmPurge`で文字と`interactable`を決め、押されたら`RequestPurgeConfirm`を呼びます。その`OnPurgeConfirmRequested`を、手札Presenterがパネルのボタンと同じメソッドで受けます。選択が終われば「実行」に戻ります。

**敵の対象。** `ExecuteEnemyTurnAsync`は対象にプレイヤーを渡しますが、`ChoosesAlly`が立っているカード(ボットネットの`BotRepair`)だけは`EnemyPartyModel.FindWeakestLiving()`を渡します。

## 敵の予告とグリッチ(2026-09-29)

- **予告。** プレイヤーがバッファを引いた直後に、`BattlePhaseDirector.PlanEnemyActions`が生きている敵ごとに`EnemyData.PickCards()`を呼び、`BattleEnemy.SetPlan`(`PlannedCards`、`OnPlanChanged`)に預けます。敵のターンはその予定をそのまま行い、終わったら消します。`EnemyIntentPresenter`がそれを`BattleEnemyIntentView`に出します。`UnitView.prefab`の`InfoStack/Intent`の行で、HPバーの下、状態効果の上です(`InfoStack`は静的な`VerticalLayoutGroup`なので、予告を出さないプレイヤーでは状態効果が上に詰まります)。1行(`BattleEnemyIntentSlotView`)は命令メモリーと同じく`0x00 [絵] 名前`です。停止で行わない分(ターンと同じ`CalculateLimit(ActionCount)`で切る)は暗くし、敵の状態効果が変わるたびに見直します。行にカーソルを乗せると、`CardPreviewModel`経由で`// COMMAND DATA`パネルにそのカードを出します(持ち主の目印は行ごとに1つ)。
- **見出し。** 2つの一覧を取り違えないよう、どちらにも見出しを付けます。予告には`// 次の行動` / `// NEXT ACTION`(`UiTextKey.IntentHeader`。`Intent`の中にあるので一緒に隠れる)、状態効果には`// 状態効果` / `// STATUS`(`UiTextKey.StatusHeader`。`StatusEffectParent`の先頭の子で、効果が無い間は隠す)です。
- **状態効果は名前だけ。** `StatusEffectView`は説明を`Tooltip`の子に持ち、カーソルが乗っている間だけ行の真上に開きます。吹き出しは行のレイアウトの外(`ignoreLayout`)に置き、`overrideSorting`付きの入れ子の`Canvas`にして手札より手前に描きます(Canvasを有効にした時点で上書きが外れることがあるので、開くたびに掛け直す)。プレハブはレイヤー`UI_Default`です。`Default`の入れ子のCanvasはUIカメラに映らず、何も描かれないまま気付けません。
- **状態効果が付いたとき。** `UnitStatusModel.OnStatusEffectApplied`(新しく付いた・重なった。引数は付いた後の効果)で、効果の名前と値を文字だけで浮かせます(`BattleUnitEffectView.PlayStatusText`。黄色で、数字の出始めより下から。同時にいくつも付けば下へ段をずらす)。無効化で弾かれたとき(`OnStatusEffectBlocked`)は「ウイルス 無効」(`UiTextKey.StatusBlocked`)を灰色で浮かせ、`Error`の音を鳴らします。
- **グリッチ。** 予定の行動の前ごとに、ディレクターが`CalculateLimit(StatLimitType.GlitchChance, 0)`%で判定し、当たれば`_glitchCard`(敵専用の`SystemError`。`BattleSceneInitializer`に設定)を敵自身を使用者・対象として使います。予告には意図的に出しません。
- **バッファロック。** `BattleDrawUseCase.Draw`は「最低1枚」の後に`CalculateLimit(StatLimitType.DrawCap, int.MaxValue)`で頭打ちにするので、バッファオーバーフローのロックでバッファを空にできます。ループバックで戻るコマンドはそれでもバッファに入ります。

- **生成される敵。** `PlanEnemyActions`は各敵に`EnemyPickContext`(HP、生きている味方の数、何ターン目か、`BattleEnemy.TurnsAlone`)を渡し、各敵のターンの終わりに`BattleEnemy.CountTurnEnd`が独りのターン数を更新します。生成される敵は戦闘の始めに作る枠(`BattleEnemyFactory.CreateReserves`。`CreateEnemies`が隠した敵Viewに置く)にいて、`EnemyPartyModel.Summon`で戦場に戻ります。`EnemySummonPresenter`がViewを出して`MemoryWrite`を鳴らし、`UnitPresenter`が`UnitStatusModel.OnRevived`で描き直し、`BattleLogRecorder`は記録を合わせるだけです。ボスのターン中に生成されたので、次のプレイヤーの番まで予定はありません。ルートの「ボス」参照。

## 間合いと戦闘速度

`BattlePhaseDirector`の待ち時間はすべて標準の速さでのミリ秒で、`GameSettingsRepository.BattleSpeed`(1が標準。0.5〜3に収める。`Func<float>`で渡す)で割って使います:

| 待ち | 標準 |
| --- | --- |
| プレイヤーのカード1枚ごと | 900 ms |
| 敵の1行動ごと | 1100 ms |
| 敵の最後の行動 → その敵のターン終了時の処理(ウイルス・ワーム・継続回復など)。状態効果が付いているときだけ | 700 ms |
| 敵から次の敵へ | 400 ms |
| プレイヤーの番 → 敵の番 | 500 ms |
| プレイヤーの最後のカードのあと | 700 ms |
| 決着 | 1500 ms |

間を置いているのは、ひとつひとつの行動をログと画面で追えるようにするためです。オプション画面ができるまでは、デバッグパネルの**Battle: Speed %**で変えられます。

## バトルのメニュー(共通の`Window/Menu/`、`Prefabs/MenuWindow.prefab`)

`Canvas/MenuButton`(左下、ログボタンのすぐ上。元はオプションのボタンで、`BattleSceneViewContainer`は`FormerlySerializedAs`で参照を引き継いでいる)で、共通のメニューウインドウ(`PopupCanvas`の下に生成、`MenuWindowView` / `MenuWindowPresenter`。ルートの「ボタンを並べるだけのウインドウ」を参照)を開きます。**オプション**と**あきらめる**(取り返しがつかない操作なので`MenuItemStyle.Danger`でマゼンタ)があり、Initializerは`MenuWindowInitData`を組み立てるだけです。マップのメニューと同じウインドウなので背後をぼかしますが、ログのウインドウと同じく戦闘は止めません。閉じるボタンか背面のクリックで閉じます。

- **オプション**は、メニューの上にオプションのウインドウを重ねて開きます(`GameOptionsPresenter.Open`)。メニューは後ろに残ります。
- **あきらめる**は共用の`ConfirmDialog`で尋ねます(「あきらめますか？」、詳細「この戦闘は敗北になり、バックアップブーストを受けます」、OKは「あきらめる」、キャンセルは「戻る」)。OKでメニューを閉じて`BattlePhaseDirector.Surrender`を呼びます。`canPress`は`!IsDecided`なので、決着後はボタンが暗くなって押せず、確認中に決着がついていたらOKでも何もしません(勝っていたのを負けにはしない)。
- **確認ウインドウはいくつかの用途で共用。** オプションの「変更を破棄しますか？」とチュートリアルのスキップも、`WindowHandler<ConfirmShowData>`の同じウインドウを使います。答えはそれぞれの`ConfirmShowData`のコールバックに返ります。

2026-10-06までは、ボタンのすぐ上にぼかし無しで出るバトル専用の小さなパネル(`BattleMenuPresenter` / `BattleMenuView`、`Canvas/BattleMenu`)でしたが、ユーザーの要望で共通のメニューウインドウに置き換えました。

## アーカイブとパージの一覧(`Window/BattlePile/`、`Prefabs/BattlePileWindow.prefab`)

**アーカイブ**か**パージ**の山をクリックすると(`CardPileCountView.OnClicked`。聞いている相手がいるときだけクリックの音を鳴らす)、`PopupCanvas`上の`BattlePileWindow`(`BattlePileWindowView`)が開きます。`// ARCHIVE` アーカイブか`// PURGED` パージ、その山のコマンドを入った順に(`CollectionCardView.prefab`の`CommonScrollList`)、数(`UiTextKey.PileCount`、「5個」)、右にカーソルを合わせたコマンドの拡大表示(`CardListDetailView`、`// COMMAND DATA`)です。閉じるか背面のクリックで閉じます。

- **戦闘は止めません**(ログのウインドウと同じ)。開いている間に見ている山が変われば、一覧と数をその場で直します。
- **同じコマンドが何枚あっても、印は1枚だけ.** カーソルを合わせたコマンドの光る縁は何番目かで付けるので、ブロックが2枚あっても両方は光りません。
- **コマンドデッキは見せません**。引く順が分かってしまうためです。
- ウインドウはタイトルのコマンドコレクションを削った複製で、一覧のカードと拡大表示も同じ部品です。2026-10-04に`TitleScene`から`Common/Views/CardList/`へ移し、`CardListItemView`(プレハブ名は`CollectionCardView.prefab`のまま)と`CardListDetailView`に改名しました。

## オプション(`Common/Presenters/GameOptionsPresenter.cs`、`Prefabs/GameOptionsWindow.prefab`)

タイトルと同じオプションのウインドウです(@docs/title-scene.mdの「オプション」)。BGMと効果音の音量、戦闘スピード、言語、画面モードと、デフォルトに戻す / キャンセル / 反映。ユーザーデータの削除はタイトルだけのもので、ここでは出しません。バトルは`onDeleteUserData`を渡さずに`GameOptionsPresenter`を作るので、その行が隠れ、ウインドウが1行分短くなります。バトルのメニューの**オプション**で開き(`openButton: MenuView.OptionsButton`)、ログのウインドウと同じく、開いている間も戦闘は止めません。反映した戦闘スピードは次の待ち時間から、音量はすぐに(動かしている間も聞こえる)効き、言語を変えると各Presenterの`OnLocaleChanged`で戦闘ログも含めて画面の文言が切り替わります。

2026-10-01に、バトル専用の`BattleOptionsPresenter` / `BattleOptionsModel` / `BattleOptionsWindowView` / `BattleOptionsWindow.prefab`(戦闘スピードだけ)を置き換えました。確認ウインドウは「あきらめる」と共用です(`WindowHandler<ConfirmShowData>`。それぞれの問いは自分の表示データで答えを受け取る)。オプションも確認ウインドウも、シーンには置かず開始時に生成します(`WindowUseCase`。ルートの「ウインドウはシーンに置かず、生成します」を参照)。

## カード使用の流れ

カードを使う手順(2026-09-29に変更): 手札のカードを**クリック**すると、そのまま**実行リスト**に積まれます(`BattleCommandQueueUseCase.TryPlayWithoutChoosing(card, preferFrontTarget: true)`)。選ぶ余地が無いカード(自分に効く、敵全員に効く、生きている敵が1体だけ)はその対象(`FindAutoTarget`)、生きている敵が複数いて1体を選ぶカードは**優先ターゲット**(後述)に向けます。パージするコマンドは続けてパージの選択に入ります。別の敵に向けたいときは、カードに**カーソルを合わせ**ます。少し止まると、選ぶ余地のあるカードはカードの上に`// SELECT TARGET`パネルを開き(`TrySelectCard`)、そこで選ぶと積まれます。パネルが開いているままカードをクリックしても、優先ターゲットに向けて積みます。あとは実行ボタンでリストを先頭から実行し、ターンを終えます。以前は操作を1つに揃えるため、どのカードも対象選択を挟んでいましたが、自分に効くカードの余分な1クリックに見合いませんでした。

**タッチは2回タップ**(2026-10-17)。手札への入力の読み方は戦略 `IHandInteraction`(`Presenters/CardPile/HandInteraction/`)で、Initializerが`Common/Interaction/InteractionModeDetector.Current`(`UnityEngine.Device.Application.isMobilePlatform`。`#if UNITY_ANDROID`ではなく実行時に決める。`Device`の方でなければならない: Device Simulatorが差し替えるのはそちらだけで、`UnityEngine.Application.isMobilePlatform`はエディタでは常にfalse。エディタでは **Tools > Interaction > Force Touch Mode** でタッチ版に固定でき、マウスのクリックがタップの代わりになる。次の戦闘から効く)から選びます。`BattleCardHandPilePresenter`は操作(使う・選ぶ・浮かせる・パージの印・詳細表示)を`IHandOperations`として持ち、戦略はどの入力でどれを呼ぶかだけを決めます。

- `PointerHandInteraction`はこの節に書いたとおり: 乗ったら浮かせ、少し止まったら対象パネル、クリックで使い、離れたら外す。
- `TouchHandInteraction`はカーソルの出入りを使いません(タッチでは指が触れた瞬間にenter、離した瞬間にexitが来るので、反応すると詳細が消え「離れたら外す」も効いてしまう)。**1回目のタップ**で浮かせ、選ぶ余地があれば対象パネルを開きます。**浮いているカードをもう一度タップ**すると使います(優先ターゲットへ。使えなければ理由が出る)。対象の候補のタップでその敵を選び、何も無い所のタップでカードを下ろします。パージの印はこれまでどおり1回のタップで付け外しします。

**タッチでは、タップで説明を出したままにします。** タッチでは`CardPreviewModel.Owner`が「今説明を開いている1つ」で、別のものをタップしたときだけ変わります(コマンドを使っても、何も無い所をタップしても変わらない)。例外は実行ボタンで、フェーズが`PlayerExecution`になると`BattleCommandListPresenter`が消します(`CardPreviewModel.Clear`)。説明が実行の結果を隠さないようにするためです。どれもタップで書き、消しはしません: 手札(どの番でも)、実行リストのコマンド、敵の次の行動の行(`BattleEnemyIntentSlotView.OnClicked`)、状態効果。状態効果にはカードが無いので、`StatusEffectTapPresenter`(タッチのときだけ、ユニットの表示1つにつき1つ)がその行を持ち主にしてカードはnullで書きます。これで詳細パネルは閉じ(`BattleCardDetailPresenter`は持ち主がいてカードが無ければ閉じる)、その行の説明が開きます(`BattleUnitStatusView.SetExpandedStatusEffect`。カーソルで開くのは切ってある)。実行リストのコマンドは、1回目のタップで説明を出して並べ替えの矢印を出し(`BattleCommandCardView.SetPinned`。カーソルは切ってある)、対象のユニットを強調します。2回目のタップでバッファに戻します。

**優先ターゲット**(2026-10-04)。`PriorityTargetModel`(`Models/`)がプレイヤーの選んだ敵を持ちます。`Current`はその敵が生きている間はその敵、そうでなければ生きている中で一番手前の敵です(なので最初は一番手前)。対象を選んでいる途中でないときに敵ユニットをクリックすると、その敵を選びます: Initializerが各敵の`onTargetClicked`を、まず`SelectTarget`に、断られたら(選択中のカードが無い)`PriorityTargetModel.Select`に配線しています。`BattleTargetResolver`が`getPriorityOpponent`で読みます(`FindFrontOpponent`)。`PriorityTargetPresenter`は、生きている敵が2体以上いる間だけ、その敵にだけ`BattlePriorityTargetView`(`UnitView.prefab`の`PriorityMarker`: 名前と頭の間の、下向きの黄色い`ui_cursor`と飾りの`TARGET`。横一線から開き、2pxきざみで上下に揺れる)を出し、名前を黄色にします(`BattleUnitStatusView.SetPriorityTarget`)。選び直すと`CardSelect`が鳴ります。選んだ敵が倒れると、次のHP変化で印が一番手前の生きている敵へ移ります。

カードをユニットにドラッグ&ドロップする旧方式は**廃止しました**。`BattleCardView`にドラッグハンドラはなく、`UnitPresenter`はスクリーン座標の当たり判定を持たず、`CardDropCandidate` / `RegisterDropTarget` / `ClearDropTargets`は削除済みです。これでUIカメラの用途も無くなったため、`BattleSceneViewContainer`は`Canvas`を公開しなくなりました。将来またスクリーン座標の当たり判定が必要になったら、旧コードが正しくやっていた点だけは踏襲してください: `Camera.main`でも`null`でもなく`_viewContainer.Canvas.worldCamera`を渡すこと(このプロジェクトのCanvasは`Screen Space - Camera`のため)。

フローを最初から最後まで:

1. **`BattleCardView`**(`Views/BattleCardView.cs`。`Common/Views/`の`CardView : MonoBehaviour`基底 + ポインタのクリック/出入りのハンドラを実装するこのサブクラス)は入力を流すだけです: `Initialize(cardData, onClicked, onHovered)`で素の`Action`を2つ受け取ります。1つ目は`OnPointerClick`で呼びます。2つ目は、カーソルが**カードの上に`_hoverDwell`(0.1秒)とどまったとき**に1回だけ呼びます。この待ちがあるので、選んだカードの上の候補一覧へカーソルを動かす途中で、隣のカードをかすめても選択が移りません。引数を渡さないのは意図的で、Presenterが View ごとに別のコールバックを作るため「どの View が押されたか」は既にコールバック側に入っているからです。フェーズ・スタミナ・対象については何も知らず、そのクリックが受け付けられたかどうかすら知りません。
2. **`BattleCardHandPilePresenter`**がフェーズの門番です: `BattlePhase.Player`以外なら入力を捨てます。
   - **今使えないカードはグレーアウトします。** `BattlePhase.Player`の間、`CanSelectCard`がfalseの手札には、カードの形をした暗い板(`DisabledOverlay`)を重ねます。理由がメモリー不足(`CanAfford`がfalse)なら、コストの数字もマゼンタにします。これで「メモリー不足」と「実行リストが満杯」を見分けられます。`RefreshPlayable`は、手札の同期・スタミナの変化・実行リストの変化・フェーズの変化のたびにやり直します。プレイヤーの番以外は、そもそも何も選べないので暗くしません。
   - **カーソルを合わせたとき**(`HandleCardHovered`)は、選ぶ余地のある使えるカードなら選択してパネルを開きます。それ以外のカードは**指すだけ**(`_inspectedCardView`)で、浮き上がり(光る縁も対象の一覧も無し)、カーソルが離れると下ります。トーストは出しません。浮かせるのはカーソルが乗った瞬間(`HandleHoverChanged`)で、止まるのを待つのはパネルを開くときだけなので、バッファを素早くなぞっても追いつきます。`CardView.prefab`にはカードの下に浮かせる量と同じ高さの透明な`LiftHitArea`があり、浮いたカードも元の場所で当たりを持ち続けるので、下端で上下にばたつきません。
   - **クリックしたとき**(`HandleCardClicked`)は上のとおりにカードを使います。断られたときは理由をトーストで出します。使えない*理由*はこれで分かります。
   - **離れると選択は外れます。** 対象を選んでいる間、選択が保たれるのは、カーソルが選んだカードか対象パネル(`BattleTargetPickerView.OnPointerOverChanged`)に乗っている間だけです。どちらにも乗っていない状態が`LeaveGraceMs`(180ms。カードとパネルの隙間を渡れる長さ)続くと、選択を取り消します。何も無い所のクリック(`BattleCancelAreaView`)でも取り消します。このため、バトルフィールドのユニットをクリックして対象を選ぶのは実用的ではなくなり、パネルで選びます。
   - **クリックが断られたとき**は、今までどおり理由をトーストで出し、そのカードを「指す」状態にします(合わせたときと同じ状態)。何も無い所をクリックすると、選択も「指しているだけ」も解除します。
   - **どの1枚か。** `CardData`はアセットの共有参照なので、同じカードが手札に2枚あっても同じオブジェクトです。どちらを指したかを知っているのはPresenterだけで、手札のViewに生成時それぞれ専用のクロージャを渡しているのはそのためです。
3. **`BattleCommandQueueUseCase.TrySelectCard`**が「そのクリックを受け付けるか」の残りのルールをすべて持ちます:
   - 実行リストが一杯、またはコストが残スタミナを超える場合は`MessageUseCase`のトーストを出して断る;
   - それ以外は受け付けます: カードが`CardSelectionModel`に入り、ユニットが選ばれるのを待ちます。この時点ではまだ積まれず、スタミナも払いません。
4. **対象選択**は逆向きに流れます: `BattleUnitTargetView`(ユニットの`TargetArea`子オブジェクトに付く)が`OnPointerClick`を`BattleUnitStatusView.OnTargetClicked`へ流し、それを購読している`UnitPresenter`が、注入された`Action<UnitStatusModel>`で報告します。このコールバックはInitializerで`BattleCommandQueueUseCase.SelectTarget`に繋がれています。`SelectTarget`は対象選択中でないとき、**および選択中のカードでは選べないユニットだったとき**は何もしないので、間違った側をクリックしても、それ以外のタイミングでクリックしても無害です。どちら側が選べるかは`CardData.TargetsOpponent`(ルートドキュメントの「カードシステム」参照)と「それがプレイヤー自身のモデルか」の比較、それに「まだ生きているか」で決まります — 敵が複数いる場合、相手に作用するカードは生きている敵すべてがハイライトされてどれでも選べ、倒れた敵はハイライトもされずクリックもできません。受け付け側もハイライト側も同じ`BattleCommandQueueUseCase.CanSelectTarget`に尋ねるので、押せそうな見た目と実際に押せるかがズレません。`UnitPresenter`はユースケースの型を直接呼び**ません** — `Func<UnitStatusModel, bool>`と`Action<UnitStatusModel>`を持ち、自分のユニットについての事実を報告するだけです。
5. **スタミナは実行時ではなく、実行リストに積んだ時点で払います** — だから`TrySelectCard`の「払えるか」判定は既に積んである分を知らなくてよく、スタミナ表示も積んだ瞬間に動きます。積んだカードをクリックで手札に戻す`ReturnCommandToHand`が、`StaminaChangeType.Gain`として払い戻します。

**ユニットまでカーソルを運ばずに対象を選ぶ。** カードが選択待ちの間、`BattleTargetPickerPresenter`が、選んだ手札のカードのすぐ上に小さな`// SELECT TARGET`パネル(`BattleTargetPickerView`)を開き、そのカードで選べるユニットを並べます。各候補(`TargetOptionView.prefab`)には、ユニットの立ち絵とHPが出ます。候補をクリックしたときの行き先はユニットをクリックしたときと同じ`SelectTarget`で、並べるユニットも同じ`CanSelectTarget`で決めるので、2つの経路の判断が食い違うことはありません。このパネルは、ユニットが手札から遠いので置いたものです。バトルフィールドのユニットを直接クリックしても、今までどおり選べます。

- **受け取るもの。** Presenterには`BattleTargetCandidate`(ユニットと立ち絵)の決まった一覧を渡します。一覧はInitializerが画面の並び順(プレイヤー、敵を手前から順)で組み立てます。選んだカードは、`BattleCardHandPilePresenter.PendingCardTransform`につないだ`Func<RectTransform>`で受け取るので、2つのPresenterが互いを参照することはありません。
- **位置。** Viewには目印の位置ではなく`Func`そのものを渡し、`LateUpdate`のたびに聞き直させます。カードが浮いたり滑ったりしても付いていき、プレイヤーが**同じカードの別の1枚**に選び直したときも、そちらへ移って開き直します。この選び直しでは選択中のViewは変わりますが、`CardSelectionModel.PendingCard`(同じ`CardData`)は変わらないので、通知が来ません。目印を毎フレーム聞き直すことで、これに気付けるようにしています。

**ユニットを「指す」(`TargetFocusModel`)。** ユニットを指す手段は2つあります。対象の候補一覧の候補にカーソルを合わせること、そして実行リストに積んだカードにカーソルを合わせること(そのカードの`CardCommand.Target`)です。どちらも`TargetFocusModel`に書き込み、各`UnitPresenter`がそれを読んで、自分の`BattleUnitTargetView`を強調色(黄色、速い脈打ち)に切り替えます。積んだカードにカーソルを合わせるのは対象選択中ではないので、選択待ちのカードがあるかどうかに関係なく強調します。`Unfocus(target)`は、そのユニットが指されているときだけ外します。こうしておくと、候補から隣の候補へそのまま移ったとき、古い候補の遅れて届くExitで新しい強調が消されずに済みます。Viewは非表示になったときや退場を始めたときにもExitを報告するので、一覧が閉じたり、カードが消費されたりしても、ユニットが強調されたまま残ることはありません。「指す」はカードを選ぶことの一部ではないので、`CardSelectionModel`とは別のモデルにしています。

**カードの詳細をカーソルで見る(`CardPreviewModel`、`BattleCardDetailPresenter`)。** 手札のカード(使えるかどうか・どのフェーズかに関係なく)や、実行リストのカードにカーソルを合わせると、画面上部の中央の空いている所にある`// COMMAND DATA`パネル(`CardDetailView : CardView`。カード購入の結果ウインドウでも使うので`Common/Views/`にある)に、そのカードのイラスト・コスト・名前・効果テキストを大きな字で出します。

- **書き込む側。** 手札と実行リストのPresenterが、Viewのカーソル通知から`CardPreviewModel.Show(owner, card)` / `Hide(owner)`を呼びます。手札のカードはカーソルの出入りを**すぐに**知らせます。少し止まるのを待つのは、合わせて選ぶ処理のほうだけです。
- **誰が見せているか。** モデルは見せている側(owner)を覚えます。ownerは手札のViewか`CardCommand`で、中身には触らない目印です(同じカードの複数枚は`CardData`を共有しているため)。こうして、カーソルが離れた側から遅れて届くExitで、移った先のカードの表示が消えないようにしています。
- **何も合わせていないとき。** パネルは選択待ちのカードを出し続けます。候補一覧へカーソルを動かす間も、効果を読めるようにするためです。
- **入力は受けません。** パネルは入力を一切受け取りません。
- **切り替えは中身をその場で入れ替えるだけです。** 横一線から(縦スケールで)開くのは、何も出ていない所から出てくるときだけです。出ている間に別のカードを指したときは、枠と見出しはそのままで中身だけを入れ替えます(切り替え時に上から塗り替える「画面の書き換え」演出も試しましたが、やめました)。
- **閉じるのは少し待ちます。** `Hide`は`_hideDelay`(0.12秒)待ってから閉じます。カードからカードへ移ると「前から外れる → 次に乗る」の順で通知が来る(カードの隙間を通るときも同じ)ので、すぐ閉じると、移るたびに一瞬閉じて開き直していたためです。
- **時間の進め方。** 時間は実時間で進め、1フレームあたりの上限を付けています。

**描画順。** `CardDetail`は`HandParent`のすぐ前に描きます。`HandParent`はCanvasの最後から2番目の子で、実行リストの受け皿より手前に描かれます(浮かせたカードが受け皿の下に潜らないように)。`TargetPicker`は最後で、手札より手前です。

放っておくと見えない2つの状態には表示を付けてあります: 対象選択待ちのカードは手札の列の中で`SelectedCardOffsetY`だけ浮き、対象に選べるユニットはカードが選択待ちの間`BattleUnitTargetView`の選択可能色に変わります。浮かせる1枚は`CardData`ではなく**View**で覚えています — `CardSelectionModel.PendingCard`と比べる実装にすると、同じカードが手札にある分だけ全部浮いてしまうためです。`BattleHandLayout.Reposition`はクリック時と`CardSelectionModel.OnPendingCardChanged`の両方でやり直します(後者が、積まれた時やターンが終わった時にハイライトを落とす役です)。手札側とユニット側が同じ`CardSelectionModel`を見ているので、どちらのPresenterも相手の存在を知らずに済みます。

手札を増やす効果で列の幅が`MaxRowWidth`(1220。5枚ちょうどの幅)を超えると、カードの間隔を詰めて重ねて並べます。メモリーのゲージや実行ボタンにかからないようにするためです。メモリーのゲージも同じで、`SegmentGaugeView`は`_segmentParent`の幅に収まるまでコマと間隔を細くします。そのため`PlayerStamina/Gauge`には幅(268)だけを与え、高さは0にしてあります(基準点が下端なので、高さを与えるとコマが上にずれる)。

**手札のレイアウトは完全にスクリプト駆動です — `HandParent`に`HorizontalLayoutGroup`はありません。** `BattleHandLayout.Reposition`(`Presenters/CardPile/BattleHandLayout.cs`。`BattleCardHandPilePresenter`が持つ)が中央揃えの横一列(`CardSpacing`定数 + プレハブ自身の`rect.width`に`localScale`を掛けたもの — 手札が実行リストの受け皿の下に収まるよう、`CardView.prefab`のルートは`0.85`に縮小してあります)を計算し、各`BattleCardView`に`MoveTo` / `PlayEnter`で目標位置を渡します。呼び出し元は`Sync()`(山札が変化するたび)と、上記の選択状態の変化時です。そこまでカードを動かすのは`BattleCardMotionView`で(@docs/art.ja.md の「カードの動き」を参照)、以後`anchoredPosition`はこれが持ち、Presenterは目標を渡すだけです。これはLayout Groupに対する意図的な選択です: Layout Groupは、レイアウト再計算の外で子の位置を設定しようとするコード(または将来のtween/アニメーション)と常に競合しますが、スクリプトで位置を決める方式ならアニメーションとうまく組み合わさります — カードの動きの演出は、まさにこれを前提にしています。新しいパネルのためにLayout Groupを復活させたくなったら、まずそのパネルが将来アニメーション付きの再配置を必要とするかどうかを考えてください — もし必要なら、この方式に従ってください。`BattleCommandListPresenter`も同じ理由で同じやり方で実行リストを並べていますが、1点だけ違います: 枚数ではなく`CardCommandModel.MaxCount`ぶんの枠で間隔を取り、**左から**詰めます(枚数が増えるたびに中央に寄せ直さない)。こうすることで、積んだカードが実行順と同じ番号の枠(シーンの`CommandTray/Slot1..3`)の上に乗ります。

**バッファの見出し。** `Canvas/BufferLabel`(`UiTextKey.BufferLabel`、バッファ / Buffer)が手札の列の下に添えられ、手札Presenterが`Sync`で流し込みます。

**バッファが空のとき。** プレイヤーの手番で、バッファが空で、何も積んでいないときは、`Canvas/BufferEmpty`(`BattleBufferEmptyView`。`HandParent`のすぐ下に描き、入力は受けない)が手札の場所に`// BUFFER EMPTY`と理由、どうすればよいかを出します。プレイヤーに`BufferLockStatusEffect`があれば「バッファロック中のため、このターンはコマンドを読み込めません」(`UiTextKey.BufferEmptyLocked`)、無ければ「バッファに読み込めるコマンドがありません」(`BufferEmpty`)、その下に「実行を押してターンを終了します」(`BufferEmptyHint`)。横線から少しちらついて開き、案内は明滅します。出すかどうかは`BattleBufferEmptyPresenter`が決めます(手札のSync、実行リスト・フェーズ・状態効果の変化のたび)。全部積んでバッファが空になったときは、理由が画面に見えているので出しません。

**重なりの順。** 手札が重なるとき、`BattleHandLayout.Reposition`は手札の並びを左から順に描画順へ並べ直し(右のカードが左のカードの上に重なる)、浮かせているカード(選択中、なければ指しているもの)を最前面に出します。

`BattleCardView`プレハブのアンカー/ピボットは中央(`anchorMin`/`anchorMax`/`pivot`すべて`(0.5, 0.5)`)のままにしておく必要があります — `BattleHandLayout.Reposition`の計算はカードのピボットが視覚的な中心であることを前提にしています。`CommandCardView.prefab`も同様です。

## 実行リスト(`Models/CardCommandModel.cs`、`Presenters/BattleCommandListPresenter.cs`、`Views/BattleCommandCardView.cs`)

`CardCommandModel`は実行待ちカードの並びで、上限は`BattleConstants.MaxCommandCount`(3)、中身は`CardCommand { Card, Target }`(`Data/CardCommand.cs`)です。`Target`がnullになることはありません — 使用者に効くカードも、プレイヤーが自分のユニットを選んで積むので、`BattleCommandExecuteUseCase.ExecuteNext`はすべてのコマンドを同じ扱いで実行できます。

4つ目の`CardPileModel`にしなかったのは、違いが2つあるためです: 枚数に上限があること、そして**並び順そのものが編集対象**であること。同じ理由で、`CardPileModel`の追加/削除/クリアの3本立てではなく`OnChanged`1本だけを公開しています — どの購読者も結局リスト全体を描き直しますし、「並べ替え」には1枚単位のイベントがそもそも無いからです。`RemoveAt`は範囲外なら`null`を返し、`Swap`は範囲外なら何もしません。これが呼び出し側の範囲チェックを省ける根拠です(Directorの「リストが尽きるまで実行」ループも、端のカードの片方向の矢印も、これに依存しています)。

`BattleCommandListPresenter`はコマンド領域全体を受け持ち、**実行ボタンもここに含みます** — ボタンはこのUIの一部であって独立した何かではないので、Initializerではなくここで配線しています(`onClick` → `RequestExecuteCommands`、`interactable` → フェーズ)。`Sync`は`BattleSceneViewContainer.CommandListParent`配下で、**`CardCommand`のインスタンスごと**に1枚の`BattleCommandCardView`を(プールから)割り当てます。積むたびに新しいインスタンスが作られるので、同じカードが2枚積まれていても区別できます。そのためViewはコマンドと一緒に動きます: 並べ替えや実行後の前詰めでは`MoveToSlot`が新しい番地へ滑らせ、書き込みの走査線をもう一度走らせます(「書き換え」)。リストから抜けたコマンドは、抜けた理由によって消え方が変わります。

- **Return**(プレイヤーがクリックして戻した。`_returningCommand`で覚えている)は、手札の方へ落ちます。
- **Execute**(`PlayerExecution`中に抜けた)は、光って潰れて上へ抜けます。
- **Discard**(それ以外、つまりターン終了時の残り)は、捨て札へ飛びます。

また、同期のたびに各Viewへコールバックを渡し直します — 並べ替えでカードの位置が変われば、そのカードにとっての「左へ動かす」の意味も変わるからです。実行ボタンのラベルも`LocalizationHelper.GetUiText(UiTextKey.Execute)`でここから流し込むので、`OnLocaleChanged`は`Sync`をやり直します。

`BattleCommandCardView : CardView`は、カーソルが乗っている間(`IPointerEnterHandler`/`IPointerExitHandler`)、かつ隣がある側だけ(`SetMovable(canMoveLeft, canMoveRight)`)矢印を出します。これが成立しているのは次の2点のためです:

- 矢印ボタンは**カードの子で、カードの矩形の内側**に置いてあります。uGUIのpointer enter/exitは階層をさかのぼるので、子のボタンへカーソルを移してもカード側の`OnPointerExit`は飛びません — 矢印がカーソルの下から消えることがありません。
- カード本体のクリックは手札に戻す操作ですが、矢印を押したときはそうなりません。uGUIはレイキャストが当たったオブジェクトから見て**一番近い**`IPointerClickHandler`にクリックを配るので、ボタンが自分のクリックを消費するからです。

`CommandCardView.prefab`は`CardView.prefab`に矢印ボタン2つを足し、手札とユニットの間に収まるようルートを`0.6`に縮小したものです。`BattleCommandListPresenter.SlotPosition`はプレハブの`rect.width`にこの`localScale`を掛けて並べるので、大きさを変えたいときはプレハブのスケールだけ触れば十分です — 間隔の計算まで直さないこと。

## カード山札Presenter(`Presenters/CardPile/`)

`CardPilePresenterBase`(`Common/Presenters/PresenterBase.cs`のサブクラス)が3つの山札Presenter共通の基底クラスです: コンストラクタは`CardPileModel`を保持するだけ(サブクラスには`protected readonly Pile`フィールドとして公開 — 同名の型`CardPileModel`をシャドウしないよう、意図的に`CardPileModel`という名前は避けています)、`OnInitialize()`が`Pile`の3つのイベントを購読して最初の`Sync`を行い、`OnDispose()`で購読解除します。また`OnLocaleChanged()`を`Sync(Pile)`として実装し`sealed`にしています — 山札Presenterにとって言語変更時の答えは常に「`Sync`をやり直す」なので、3つの具象Presenter側は何も実装しません。`BattleCardHandPilePresenter`の`Sync`は、インデックスごとに入れ直すのではなく**差分**を取ります: 新しい手札と今のViewを先頭から突き合わせ、`CardData`でViewとカードを対応させます。カードを失ったViewだけが退場し、新しいカードにだけ新しいViewを(プールから。Viewは破棄せず`SetActive`で切り替えます)割り当てます。インデックスで入れ直すと、手札の途中の1枚が抜けたときに**末尾の**Viewが消えて見えてしまうためです。

- **同じカードが複数あるとき。** モデルは、プレイヤーが押したのと同じ1枚を抜く必要があります。そうしないと、Viewの突き合わせ(Viewをモデルの並び順に保つ)で*もう片方*が飛んでいきます。そこでPresenterは押したViewの位置(`_cardViews`はモデルの並び順に保たれている)を`TryPlayWithoutChoosing` / `TrySelectCard` / `ConfirmPurgeChoice`に渡し、`CardSelectionModel.PendingHandIndex`が対象選択とパージの選択の間それを引き継ぎ、`AddCommand`は`CardPileModel.RemoveCardAt`で位置を指定して抜きます(後ろから順に。位置が分からなければ`RemoveCard`)。View側では、 `CardData`は共有参照なので、データでは区別できません。対応付けでは、選択が解除された直後のView(`_releasedCardView`。積まれたカードが手札から抜ける直前に届く「選択解除」の通知で覚えます)を避けます。こうして、プレイヤーが実際に選んだ1枚が抜けるようにしています。
- **行き先・出どころ**はフェーズで判断します。`Player`中に抜けたカードは積まれたもの(Upload。実行リストへ)、それ以外は捨てられたものです。`Player`中に増えたカードは実行リストから戻ったもの、それ以外は山札から引いたものです。
- **時間差。** 引く・捨てるは1枚ごとにイベントが来るので、連続する登場・退場には`Sync`をまたいで時間差を付けます。

その後再配置します(上記参照)。加えて`OnInitialize`/`OnDispose`をオーバーライドし、基底の山札イベントに上乗せして`CardSelectionModel`も購読します。`BattleCardDeckPilePresenter`/`BattleCardDiscardPilePresenter`は、単に`cardPileModel.Count`を(`Common/Views/`の)`CardPileCountView`に渡すだけで`Sync`を実装しています — この2つの山札には1枚ごとのViewはありません。

## ユニットステータス表示(`Presenters/UnitPresenter.cs`、`Views/BattleUnitStatusView.cs`)

`UnitPresenter`はユニットごとに1つ — プレイヤー1つと、`BattleSceneViewContainer.EnemyStatusViews`の要素ごとに1つ — で、そのユニットの`UnitStatusModel`、`BattleUnitStatusView`、共有の`CardSelectionModel`、`showStamina`のbool、`onTargetClicked`コールバックを渡して構築します。`Initialize()`は一度だけ`view.SetStaminaActive(showStamina)`を呼び(Initializerはプレイヤーには`true`、敵には`false`を渡します — スタミナはプレイヤー自身のリソースなので、敵に表示しても意味がありません)、最初のHP/スタミナ同期を行います。`BattleUnitStatusView.SetStamina`自体も`_staminaParent.activeSelf`をチェックするので、非表示中に誤って呼ばれても何もしません。プレイヤーのスタミナはユニットの上ではなく、手札の左下(`Canvas/PlayerStamina`)に出しています: プレイヤーの`_staminaParent`/`_staminaText`がそちらを指していて、ユニットプレハブ自身の`Stamina`子オブジェクトは、敵のViewが隠す対象を持てるようにするためだけの非表示の置き物です。

`canBeTargeted`は`CardSelectionModel.OnPendingCardChanged`のたびに尋ねてハイライトを決め、`onTargetClicked`はクリックを報告します。どちらも`BattleCommandQueueUseCase`そのものではなく素のデリゲートで、どのユニットにも同じ組を渡しています — ユニットのPresenterは「どちら側か」も「他に何体いるか」も知らないので、敵を1体から2体にしたときもこのクラスは一切変わりませんでした。

敵の名前(`EnemyName`テーブル、`LocalizationHelper.GetEnemyName`)は立ち絵の上に出します(`UnitView.prefab`の`NameText`)。`UnitPresenter`はそのために`EnemyData`を受け取り、言語が変わると名前を引き直します。プレイヤーは`null`を渡すので、名前の欄は隠れます。

`BattleUnitStatusView`は単一の`sealed`クラスです(汎用基底+シーンサブクラスには分割していません) — HP/スタミナテキスト、`_staminaParent`、`_targetView`がすべて直接このクラスにあります。これはBattleScene以外で表示だけのバージョンが必要になったことが無いためです。`_targetView`は子オブジェクト(`TargetArea`)の`BattleUnitTargetView`を指し、Inspectorで独立して割り当てられるので、クリック領域をステータスパネル全体ではなくスプライト形状の子に合わせられます。このコンポーネントは`Graphic`を「レイキャストの当たり判定」と「選択可能のハイライト」の2役で使っているので、隠したいときも`Graphic`自体を無効化しないこと(透明にすること) — 無効化するとそのユニットはクリックできなくなります。将来別シーンが対象選択の概念無しでユニットステータス表示を必要とするなら、そこが分割を再検討するタイミングです(ルートドキュメントのレイヤー構造にある「View」を参照) — それが実際に必要になる前に先回りして分割しないこと。

**ブロックはバリアとして見せます(`Views/BattleBlockBarrierView.cs`、`UnitView.prefab`の`Barrier`)。**

立ち絵の手前(`TargetArea`の奥なので、クリックは照準に届きます)にエネルギーの壁を張り、ブロック値をバッジに出します。

| 変化 | 壁の動き |
| --- | --- |
| ブロックを得た | 横一線から開いて光る |
| 重ねた | 脈打つ |
| 攻撃を吸収した | 揺れて破片が散る。張ってからの最大値に対して3/4・1/2・1/4を下回るごとにひびが1段ずつ増え、壁も薄くなる |
| 0になった | 砕ける(横に広がりながら潰れ、破片を大きく散らす) |

`UnitPresenter`はユニットの`BlockStatusEffect`の値を合計し、`OnHpChanged`(吸収)・`OnStatusEffectsChanged`(得た/切れた)・`OnStatusEffectValuesChanged`(重ねた)のたびに渡します。Viewは値が増えたか減ったかで演出を選ぶだけで、ブロックの仕組みは知りません。

## コマンドのユースケース(`UseCase/`)

プレイヤーがコマンドで行うことは、**呼ぶ側ごとに**4つのユースケースに分けてあります。どれも自分では状態を持たず、続く状態はすべてモデルにあり、`BattleSceneInitializer`が各ユースケースに必要なモデルだけを渡します。2026-10-08までは、これが1つの`BattleCommandUseCase`(中に`BattleCommandExecutor`と`BattleTokenWallet`)で、選ぶことと実行することが同じモデルを触るため分けずにいました。モデルではない状態(戻り待ちの山、チェーンとリドゥの記憶、トークン)をモデルへ移したうえで、ユーザーの依頼で分けました。

| ユースケース | 呼ぶ側 | すること |
| --- | --- | --- |
| `BattleDrawUseCase` | Director | プレイヤーのターンの初めの`Draw()` |
| `BattleCommandQueueUseCase` | 手札と実行リストのPresenter、ユニットと対象一覧のクリック | カード・対象・パージするものを選ぶ、積む、戻す、並べ替える、`RequestExecuteCommands()` |
| `BattleCommandExecuteUseCase` | Director | `ExecuteNext()`、`ExecuteEnemyCard()`、`IsDeckExhausted`。`OnCardExecuting` / `OnCardDisposed`を発行(ログ、`BattleActionPresenter`) |
| `BattleTurnEndUseCase` | Director | `EndPlayerTurn()` |

共有するモデル:

- `BattleCardPilesModel` — 山札・手札・捨て札・パージと、`Returning`(ループバックで次のバッファに戻るコマンド。画面には出さない)。`PileOf(destination)`と`HasOnlyPurged`も。
- `BattleTokenModel` — 戦闘で使えるトークン。積んだときに払い、戻したときに返し、使った量を数えます(`BattleResult.TokensSpent`)。
- `CommandHistoryModel` — このターンのエクスプロイトチェーンの段と、リドゥが繰り返す「リドゥ以外で最後に実行したコマンド」(カード・対象・パージ数)。どちらも`BattleTurnEndUseCase`が忘れさせます。
- `CardCommandModel.OnExecuteRequested` — 実行ボタンの要求。積む側のユースケースとDirectorが互いを参照しないよう、モデルに置いてあります。

`BattleTargetResolver`(`UseCase/`、`BattleTargetResolverTests`)は、積む側と実行する側が共有する唯一の補助クラスです。自動の対象、優先ターゲット / 手前の敵、正しい対象かどうか、倒れた対象の振り直し(`Resolve`)、生きている全ユニット(プレイヤーが先頭)。敵の状態は持たず、並びは`Func`で聞くだけなので(`EnemyPartyModel.FindNextLivingTarget`、`GetLivingStatuses`、`PriorityTargetModel.Current`)、テストからは2行のスタブを渡せます。敵の`UnitStatusModel`やDirectorを持つユースケースはありません。

**積む**(`BattleCommandQueueUseCase`)。カードを選ぶことと積むことは、断る理由の判定(メモリー・リストの上限・トークン・パージできるものが無い)と、積むときの支払いを共有するので、1つのクラスに置いてあります。

- `TrySelectCard(CardData)` — 対象選択に入ります。ルールは上の「カード使用の流れ」に挙げた通りで、断るときは理由をトーストで出します。
- `TryPlayWithoutChoosing(CardData, preferFrontTarget)` — 手札クリックの入口。選ぶ余地の無い対象か優先ターゲットに積むか、`NeedsTarget`を返します。
- `CanSelectCard(CardData)` — 同じ判定を、トーストを出さずに行います(カーソルを合わせて選ぶとき用)。どちらも非公開の`GetSelectRefusal`1つを通るので、判定が食い違うことはありません。
- `CanSelectTarget(UnitStatusModel)` — 選択中のカードをそのユニットに向けられるか。選択中のカードが無ければ`false`。Presenterのハイライトも`SelectTarget`もここを通ります。
- `SelectTarget(UnitStatusModel)` — ユニットクリックの入口。対象選択中のカードをそのユニット宛てで積みます。選択中でない、または選べない側のユニットなら何もせず`false`を返します。
- `ConfirmPurgeChoice(cards)`、`CancelSelection()`、`ReturnCommandToHand(int index)`(メモリーとトークンを返し、パージ用に預かったコマンドも戻す)、`SwapCommands(int, int)`。
- `RequestExecuteCommands()` — 実行ボタン。対象選択を解除し、Director向けに`CardCommandModel.OnExecuteRequested`を発火します。自分では何も実行しません(進行の間合いはDirectorのものなので)。

**実行する**(`BattleCommandExecuteUseCase`)。

- `ExecuteNext()` — リストの先頭を実行し、リストが空なら`false`を返します。預かったコマンドのパージ、マルチスレッドの繰り返し、リドゥ(一つ前のコマンドとして実行)、エクスプロイトチェーンの記録、`CalculateCommandDestination`が示す行き先への移動を行います。ここではスタミナを取りません — 積んだ時点で払い済みです。効果を走らせる前に対象を確かめ、選んだあとに倒れていた場合は`BattleTargetResolver.Resolve`が次の生きている敵(手前から順、末尾なら手前に戻る)へ振り直します。誰も残っていなければ元の対象のまま撃ちます — Director側が`AreAllDefeated`でリストを止めるので実害はありません。
- `ExecuteEnemyCard(CardData cardData, UnitStatusModel user, UnitStatusModel target)` — 敵のモデルもカードも持つユースケースは無いので、**両方のユニットを明示的に受け取ります**。やることはカード効果の実行だけです: 敵には手札・山札・捨て札もスタミナも無いので、**カードもコストも消費されず**、山にも一切触れません。何回行動できるかは`EnemyData`側の責任です。
- `IsDeckExhausted` — パージの山のほかに何も残っていない(リストも空)。そのときDirectorが戦闘を敗北で終えます。

**引く・ターンを終える。** `BattleDrawUseCase.Draw()`は先に`Returning`を手札へ移し、`BattleConstants.MaxHandSize`をプレイヤーの`CalculateLimit(StatLimitType.HandSize, …)`に通した枚数まで引きます(最低1枚、そのうえで`DrawCap`で制限)。山札が尽きたら捨て札を山札に戻してシャッフルします。`BattleTurnEndUseCase.EndPlayerTurn()`は選択とこのターンの記憶を消し、実行リストに残っていたもの(通常は無い)と手札を捨て札へ流し、プレイヤーのスタミナを全回復します。イベントは発火しません: 呼ばれる側ではなく、Directorが呼ぶ側です。

**テスト**は`Tests/EditMode/BattleCommandRig.cs`で4つをInitializerと同じ組み合わせで作ります(`rig.Queue.TrySelectCard`、`rig.Execute.ExecuteNext`など)。1つのテストで引く・積む・実行する・ターンを終えるを続けて行うことが多いためです。

## 画面レイアウトと見た目

レイアウトは一般的なHIGの原則と、カードゲームでプレイヤーが慣れている配置に合わせています。好みではなく理由で判断できるようにするためです:

- **上段: バトルフィールド。** プレイヤーは左で右向き、敵は右(`Portrait`のxスケールを`-1`にして反転)。各ユニットは上から 立ち絵 → HPバー → 状態効果 の順に読めます。ユニットは`UnitView.prefab`のインスタンスで、見た目と演出は**すべて**プレハブ側にあります。インスタンスが上書きしているのは位置・立ち絵のスプライト・反転だけなので、見た目を変えるときはインスタンスではなくプレハブを直してください。
- **中段: 実行リスト。** 命令メモリの基板(`CommandTray`)として描き、各枠に番地`0x00`〜`0x02`を振っているので、1枚目を積む前から上限と順番が見えます。積んだカードはその枠に「書き込まれる」演出が出ます(`BattleCommandWriteEffectView`)。実行リストのカードは0.6倍だと説明文が読めないので、名前とイラストだけを見せます。
- **下段: 手札**(中央)。手札を使う元手であるスタミナは左下の山札の上です。プレイヤーには**メモリー残量**として見せています(カードはメモリーに読み込む命令なので)。メモリーチップが並んだ`SegmentGaugeView`と`n/max`の数字で表示します。山札の見出しは**コマンドデッキ / COMMAND DECK**、捨て札は**アーカイブ / ARCHIVE**です(どちらも`UIText`の値で、キーは`DeckPile` / `DiscardPile`のまま)。主操作の実行ボタン(一番大きく彩度の高い操作)は右下の捨て札の上です。カードゲームで「ターン終了」がある位置です。
- 画面が暗いので、文字は明るい色(`#E8FBFF`)。暗い`INK`の文字はネオンの塗りの上(カードの名前帯)だけに使います。実行ボタンは半透明で斜めに傾いたシアンのパネルに明るい斜体の文字で、カーソルを合わせたときと押したときに波紋が出ます(`ButtonRippleEffect`、@docs/art.ja.md 参照)。HPの数字はバーの中ではなく上に置き、ライム色の塗りに重ならないようにしています。

どの画像がどこから来ていて、どう差し替えるかは [docs/art.ja.md](art.ja.md) にあります。HPの増減は`BattleUnitEffectView`が演出し(揺れ、透明度の点滅、浮かぶ数字、パーティクル)、`UnitPresenter`が前回表示したHPとの差分と**その原因**を渡します。原因は数字の下の小さな暗いタグに出します(縁取りではなくタグにしたのは、日本語の字がフォントの2枚目のアトラスにあり、そちらには縁取りのマテリアルが引き継がれないため)。原因は、ターンのフックでHPを変えた状態効果(`UnitStatusModel.LastHpChangeSource`。ウイルス、ワーム、リジェネ)、そうでなければ実行中のコマンド(`CurrentActionModel`。`BattleActionPresenter`が`OnCardExecuting`で書き、ターンの始まりとターン終了時の継続効果の前に消す)です。回復が反転してダメージになったときは、反転させた効果の名前を添えます(「リペアパケット(データ破損)」)。同じ場所で`Hit` / `VirusTick` / `Heal`を鳴らします。HPが0のユニットは`BattleUnitStatusView.SetDefeated`で薄くなります。`BattleUnitTargetView`は選択可能な間その色を脈打たせ、押せることが分かるようにしています。

**動くユニット(`Presenters/UnitVisualPresenter.cs`)。** ユニットごとに1つ、`UnitPresenter`の後、対象の候補一覧の前に作ります(候補一覧が見た目のアイコンを拾えるように)。`Initialize`で`EnemyData.Visual`か`BattleSceneInitializer._playerVisual`を`BattleUnitStatusView.ApplyVisual(definition)`に渡すと、Viewは`Portrait`の中に`IUnitVisual`を作り(敵の左右反転と被弾の揺れはそのまま効く)、1枚絵は隠してそのスプライトはアイコンとして残し、見た目を`BattleUnitEffectView`の色の掛け先にします。そのうえで:

- **行動。** `CurrentActionModel.Set(user, card)`(`OnCardExecuting`から。プレイヤー・敵とも)が`OnActionStarted`を出し、その`user`のPresenterが`PlayAction`を呼びます。
- **被弾と撃破**は専用の配線を持ちません。`BattleUnitStatusView.PlayHpChanged`がHPの減少で`PlayHit`を呼び、`SetDefeated`が見た目へそのまま流すので、`UnitPresenter`の既存のHP処理に乗ります。
- **定義が無い**(`_visual`が未設定の敵)ときは、今までどおり1枚絵のままです。

コマの中身と、実装の差し替え方は [docs/art.ja.md](art.ja.md) の「キャラクター」にあります。

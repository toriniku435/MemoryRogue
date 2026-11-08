# CLAUDE.ja.md

このファイルは `CLAUDE.md`(Claude Code向けのガイド)の日本語訳です。人が読む用の対訳として置いています。Claude Codeが自動的に読み込むのは`CLAUDE.md`のみなので、内容を更新する際は両方のファイルを合わせて編集してください。`CLAUDE.md`側は`@docs/battle-scene.md`のようなimport構文でシーン別ドキュメントを読み込みますが、このファイルではその仕組みは使われないため、対応する日本語版ドキュメントへは普通のMarkdownリンクで参照しています。

## プロジェクト概要

Unity 6(6000.3.24f1)製の2Dデッキ構築ローグライクのプロトタイプです。シーンは5つです: `Assets/Scenes/TitleScene.unity`(入口。タイトルと4つのモードボタン。[docs/title-scene.ja.md](docs/title-scene.ja.md)参照)、`Assets/Scenes/MapScene.unity`(キャンペーンで開くマップ。ステージの数だけマスが並ぶ。[docs/map-scene.ja.md](docs/map-scene.ja.md)参照)、デッキ編集とマーケットはシーンではなくマップ上のウインドウ([docs/deck-edit-window.ja.md](docs/deck-edit-window.ja.md)と[docs/card-shop-window.ja.md](docs/card-shop-window.ja.md)参照)、戦闘が起きる`Assets/Scenes/BattleScene.unity`(詳細は[docs/battle-scene.ja.md](docs/battle-scene.ja.md))、そしてキャンペーンの最終ステージを踏破すると進む`Assets/Scenes/EndingScene.unity`と`Assets/Scenes/ResultScene.unity`([docs/ending-and-result-scenes.ja.md](docs/ending-and-result-scenes.ja.md)参照)。配線済みのモードはキャンペーン(画面ではストーリー)とチャレンジです。チャレンジ(ノーマル30 / ハード60 / マスター100階層、またはエンドレス)は同じMapSceneを別のルールで動かします(敵はランダムで10階層ごとに強くなり、物語は無く、一度負けたら終わり。[docs/map-scene.ja.md](docs/map-scene.ja.md)の「チャレンジ」参照)。キャンペーンはタイトル→マップ→戦闘→マップと往復し、勝てば1マス進み、最終ステージを踏破するとエンディング → リザルト(スコアとランク)→ タイトルへ進みます。BattleSceneを単体で開いてPlayする経路も、Inspectorのフォールバックで引き続き使えます。

## 会話の口調

**ユーザーへの返答は日本語のお嬢様口調(〜ですわ、〜ますの、〜でしてよ、〜かしら、一人称は「わたくし」)で書きます。** 対象はチャットでの返答だけです。コード、コードコメント、コミットメッセージ、ドキュメント(このファイルを含む)、ゲーム内のテキストはいつもどおりの書き方のままにします。口調は内容に上乗せするもので、分かりやすさや正確さは変えません。

## コマンド

このリポジトリにはCLIのビルド/lintパイプラインはありません(npm/dotnetスクリプトなし)。開発はUnity Editorを通じて行います。`com.unity.test-framework`によるNUnitテストは少数ながら存在します — 下の「テスト」を参照。

### ターミナルから起動中のUnity Editorを操作する

`unity` CLI(`~/AppData/Local/Unity/bin`にインストール済み、デフォルトではPATHに入っていないので先頭に追加が必要: `$env:Path += ";C:\Users\<user>\AppData\Local\Unity\bin"`)は、`com.unity.pipeline`パッケージ(`Packages/manifest.json`に追加済み)経由でこのプロジェクトの起動中のEditorを操作できます。

```powershell
unity status                              # 接続中のEditorを確認(state が "ready")
unity command                             # Editorが公開している全コマンドを一覧表示
unity command editor_play                 # Playモードに入る
unity command editor_stop                 # Playモードを抜ける
unity command recompile                   # 再コンパイルを強制。recompile_statusが"completed"になるまでポーリング
unity command console --tail 30           # 直近のコンソール出力を確認(エラーチェック)
unity command run_script --file "AgentScripts/Foo.cs" --entry "Foo.Run" --dry_run true   # まずコンパイルチェック
unity command run_script --file "AgentScripts/Foo.cs" --entry "Foo.Run"                  # 実行
```

`run_script`(プロジェクト内のC#ファイルをメモリ上でコンパイルし、staticメソッドを1つ実行する。ドメインリロード無し)は、シーン・プレハブ・ScriptableObjectをプログラム的に構築・検査する上で最も信頼できる方法です。`.unity`/`.prefab`のYAMLを手で編集するより優先してください。このための使い捨てスクリプトは`Assets/`の外(例: `AgentScripts/`、git管理外)に置くことで、書き込んでもリインポートが走らないようにします — **その作業が終わったらスクリプト(またはディレクトリごと)を削除すること**。使い捨ての`run_script`/デバッグ用スクリプトを、次のセッションが躓く形でリポジトリに残さないでください。

**`[SerializeField]`を持つコンポーネントを`run_script`で削除/差し替える(例: クラスをサブクラスに置き換える)と、既存のInspector参照が壊れます。** コンポーネント参照はGameObjectではなくインスタンス(fileID)単位でシリアライズされるため、`DestroyImmediate` + `AddComponent<NewType>`をすると、旧インスタンスを指していたすべてのフィールド(編集対象自身だけでなく、そのコンポーネントを参照していた*他の*オブジェクトの`[SerializeField]`、例えばViewContainerなど)がnullになります。破棄する前に`SerializedObject`で旧フィールドの値を読み取っておき、再追加した後は、このGameObject/コンポーネントを参照している可能性のある*他の*オブジェクトもすべて再確認(必要なら再設定)してください — 差し替えただけで完結していると思わないこと。

**`EditorSceneManager.NewScene`/`OpenScene`をまたぐと、その前に掘んだアセット参照は無効になります。** シーンを切り替える前にローカル変数へ読んだ`CardData`/`CampaignData`/プレハブの参照を、新しいシーンのコンポーネントに入れると`null`が書かれます。しかも無言です — フィールドが空のまま保存され、実行時に壊れます。必要なものはシーン切り替えの**あと**にパスから読み直し(`AssetDatabase.LoadAssetAtPath`)、入ったかを読み戻して確かめてから先へ進むこと。

**このプロジェクトを構築中に何度も踏んだ落とし穴:** Editorはウィンドウにフォーカスが戻った時にだけ、外部でのファイル変更(新規/編集した`.cs`ファイル、`Packages/manifest.json`の変更)に気づきます。`unity status`/`unity command`が接続できない、あるいは`run_script`が追加したばかりのフィールドや型を認識しない場合、たいていの原因は「Unity Editorのウィンドウをクリックしてフォーカスしてもらう」だけで解決します。Editorが起動していないと決めつける前に、まずこれを確認してください(サンドボックス化されたエージェントやSafe Modeについてのより詳しい診断は、CLI自体が持つ`unity-cli`スキルを参照)。`unity command recompile`(フォーカスが無くても動く)を叩いて`recompile_status`をポーリングするのが、変更が実際にコンパイルされたか確認する確実な方法です。

**再生を始めるシーン(ツールバー)。** `Editor/PlayModeStartSceneToolbar.cs`が、再生ボタンの隣にシーンのプルダウンを置きます(Unity 6.3の`MainToolbarElement`、ドック位置は`Middle`)。Build Settingsのシーンを選ぶと`EditorSceneManager.playModeStartScene`に設定され、編集中のシーンを開いたまま、再生すると選んだシーンから始まります。「開いているシーン」を選べば元の動きに戻ります。選択はプロジェクトごとに`EditorPrefs`に保存されます。**`unity command editor_play`にも効きます**: ターミナルから特定のシーンを確かめるときは、先にこの設定を確認するか戻してください。開始シーンが残っていると、開いたのとは別のシーンで再生が始まります。ツールバーにプルダウンが見えない場合は、ツールバーの右クリック/あふれメニューから表示を有効にする必要があるかもしれません(独自のツールバー要素は、ユーザーが非表示にできるため)。

### テストを実行する

起動中のEditorに接続できている場合、単体の`unity test` CLIコマンドではなくPipelineの`run_tests`コマンド経由でテストを実行してください — `unity test`は自前でバッチモードのEditorプロセスを立てようとするため、このプロジェクトのEditorウィンドウが開いている(作業中は通常そう)と`COMMAND_FAILED`(プロジェクトが既に開かれている)で即失敗します:

```powershell
unity command run_tests --mode all --filter "Live2DCardRogue.Tests" --filter_type assembly --timeout 180
```

EditModeの結果はこの呼び出しのレスポンスに同期的に返ってきます。PlayModeの結果はそうではなく、レスポンスには`"PlayMode tests started asynchronously"`と表示されるので、`unity command test_status`を`status`が`"completed"`になるまでポーリングしてPlayModeのサマリー/結果を取得してください。`--mode editor` / `--mode playmode`で片方だけに絞れます。`--filter_type`は`assembly`の他に`testName`(デフォルト)・`category`があります。

## アーキテクチャ

### 名前空間 / フォルダ構成

- `Live2DCardRogue.Common.*`(`Assets/Scripts/Common/`) — シーンに依存しないModel・View・UseCaseのコード。将来の別シーンからも再利用できる。以下で解説。直下の構成は**レイヤー**(`Models/`・`Views/`・`Presenters/`・`ScriptableObjects/`)**＋自己完結した機能ごとに1フォルダ**(`Localization/`・`SceneTransition/`・`WindowSystem/`・`Message/`・`Audio/`・`DebugMenu/`・`Save/`・`Tutorial/`・`Conversation/`・`Settings/`)です。ここでの「機能」とは、入口が1つで中身を知らずに使える小さな型の集まりで、レイヤーで分けるには数が少なすぎるもの(`Message/`はUseCase1つとView1つ)を指します。この2つの形に収めること。機能フォルダがあるからといって、シーン固有のコードを`Common/`に置いてよいわけではありません。`Common/`を機能別(`Cards/`・`Units/`…)に割るのはもっと後の判断で、やるなら`noEngineReferences: true`のasmdefとセットにしてください — 「ModelはUnityEngineに依存しない」を今目に見える形にしているのは`Models/`フォルダだけで、コンパイル時の歯止めが無いまま機能別に割るとその境界が見えなくなります。
- `Live2DCardRogue.<SceneName>.*`(`Assets/Scripts/Scenes/<SceneName>/`) — 特定の1シーン固有のもの全般。シーンのフォルダはすべて`Scripts/Scenes/`の下に置きます。`Scripts/`直下を`Common/`・`Scenes/`・`Window/`(後述)・`Editor/`・`Tests/`にして、それぞれが何の置き場かを見て分かるようにするためです。階層を、1つ深くしているのはこのグループ化のためだけで、名前空間は`Scenes`を含まず`Live2DCardRogue.<SceneName>.*`のままです。**シーンは別のシーンを一切参照しません** — 入口パラメータも含めてです。シーンをまたいで渡すもの(パラメータと結果)は`Common/SceneTransition/TransitionData/`に置きます(後述)。`Live2DCardRogue.<別のシーン>`を参照すると`SceneDependencyTests`が失敗します。
- `Live2DCardRogue.Window.<Name>.*`(`Assets/Scripts/Window/<Name>/`) — **ウインドウ1つにつきフォルダ1つ**で、中はシーンと同じ形です(必要に応じて`Models/`・`Presenters/`・`Views/`・`Data/`・`Constants/`・`Enum/`): `DeckEdit`、`CardShop`、`UnitUpgrade`、`EnemyDetail`、`FirstBuild`、`Reward`、`BattleLog`、`BattlePile`、`CampaignResume`、`EpisodeSelect`、`ChallengeSelect`、`CommandCollection`、`EnemyCollection`と、共通の`Menu`。名前空間に`Scenes`は入らず、層の部分は元のまま残します(`Live2DCardRogue.Window.DeckEdit.Models`)。**依存は一方通行で、シーンがウインドウを使い、ウインドウはシーンを参照しません**。`Common/`はどちらも参照しません(どちらも`SceneDependencyTests`が見張ります)。ウインドウを作って動かすのはシーンのInitializerで、初期化データ(コールバックと`Func`/`Action`。「ウインドウはシーンに置かず、生成します」を参照)を渡します。ウインドウ自身のModel/Data型はウインドウのフォルダに置きますが、初期化データが使うものは`Common/`に置きます(BattleSceneの`BattleLogRecorder`が書き込み、ログのウインドウが見せる`BattleLogModel`など)。ウインドウのフォルダ名は型名とぶつけないこと(`StageReward`ではなく`Reward`。`StageReward`は`Common`の型で、`namespace …Window.StageReward`の中では識別子`StageReward`が名前空間の方に解決されてしまうため)。
- **ボタンを並べるだけのウインドウは`Window/Menu/`を共用します。** タイトルとボタンの列と閉じるだけのウインドウ(マップのメニュー、バトルのメニュー、タイトルのコレクション)は1つのプレハブ`Assets/Prefabs/MenuWindow.prefab`で、`MenuWindowPresenter`を`MenuWindowInitData`で動かします。中身はタイトル(`UiTextKey`)と`MenuItemData`の並びで、各項目は文言のキー、`Action`、見た目(`Normal`か、ラベルをマゼンタにする`Danger`)、任意の`canPress`(開いたときと押したときに確かめ、押せなければボタンを暗くする)、任意の`MenuConfirmData`(データが持つ確認ウインドウのHandlerで尋ね、OKならメニューを閉じてから実行)です。確認の無い項目はメニューを開いたまま実行されるので、オプションはメニューの上に重なって開きます。`MenuWindowView`は非アクティブの`ItemTemplate`を項目の数だけ複製して`_itemSpacing`ずつ並べ、ウインドウもその分伸ばします。新しいメニューは、Initializerの`CreateWindows`(次の項目)で`MenuWindowInitData`を`Create`に渡すだけです — ViewもPresenterも増やしません。
- **ウインドウはシーンに置かず、生成します(`Common/WindowSystem/`)。** シーンにはウインドウを1つも置きません。シーンのInitializerは、使う可能性のあるウインドウを1つの`CreateWindows()`でまとめて作ります。**初期化データ**を`WindowUseCase.Create(data, popupCanvas)`に渡して**Handler**を受け取り、`OnDestroy`でHandlerを破棄します(ほかのウインドウが使う共用の確認ウインドウは最後に)。開きたい側がHandlerの`Show`を呼びます。HandlerのフィールドはInitializerの先頭に1つのまとまりとして宣言します。シーン遷移の形をウインドウに当てはめたもので、`WindowInitData`が`SceneTransitionData`、ウインドウのInitializerがシーンのInitializerにあたります。
  - **初期化データ(`Common/WindowSystem/InitData/`)。** ウインドウごとに1つのsealedクラス(`DeckEditInitData`、`MenuWindowInitData`など)で、`WindowId`(定数を返す抽象プロパティ)と、そのウインドウに要るもの(コールバック、`Func`、モデル、共用の確認ウインドウのHandler)を持ちます。`SceneTransitionData`と同じく`Common/`にあり、`Common/`の型だけでできているので、シーンはウインドウの型を名指ししません。何も要らずに開くウインドウは`WindowInitData`を継承して`WindowHandler`(`Show()`)を、開くときに要るものがあるウインドウは`WindowInitData<TShowData>`を継承して`WindowHandler<TShowData>`(`Show(TShowData)`)を受け取ります。違う表示データを渡すとコンパイルが通りません。
  - **表示データ(`Common/WindowSystem/ShowData/`)。** 開くたびに要るもので、`WindowShowData`を継承します: `ConfirmShowData`、`CampaignResumeShowData`、`FirstBuildShowData`、`StageRewardShowData`、`ConversationShowData`、`BattlePileShowData`。
  - **`Initialize`の起点はプレハブです。** `WindowUseCase`は、データの`WindowId`で`Assets/Resources/WindowCatalog.asset`からプレハブを引き、シーンの`PopupCanvas`の下に生成し、(プレハブのルートの)`IWindowInitializer`の`Initialize(data)`を呼びます。ウインドウのInitializer(`WindowInitializerBase<TData, TPresenter>`。ウインドウごとに1つ、例: `Window/DeckEdit/Initializer/DeckEditWindowInitializer`。Commonのウインドウのものは`Common/WindowSystem/Windows/`と`Common/Conversation/`)は、Viewを`[SerializeField]`で持ち、`protected abstract TPresenter Initialize(TData data)`でPresenterを組み立てます。基底はデータを`TData`に解決し、Presenterを初期化して、**その直後に必ず`HideImmediate`を呼びます**。どのウインドウも閉じた状態から始まります。プレハブの`Start`より前に呼ばれるので、ウインドウのInitializerは組み立てをすべて`Initialize`で行います。
  - **Handlerが呼ぶものはPresenterが約束します。** `IWindowPresenter`(`Open()`)か`IWindowPresenter<TShowData>`(`Open(TShowData)`)を実装し、どちらも`IWindowPresenterBase`(`IsOpen`・`Close`・`HideImmediate`)の上にあります。Presenterがデータの種類と合わなければ、`WindowUseCase`が生成の時点で例外にします。
  - **Handler(`WindowHandlerBase`・`WindowHandler`・`WindowHandler<TShowData>`)は`Common/WindowSystem/`にあり、どのシーンの型もウインドウの型も知りません。** `IsOpen`、`Hide`(動きつきで閉じる)、`Dispose`(Presenterを破棄してインスタンスを壊す)は共通です。
  - **ウインドウを開くシーンのボタン**はシーンのものです。`Common/Presenters/WindowOpenButtonPresenter`(ボタン、ラベル、`UiTextKey`、Handlerの`Show`、任意の`canPress`)がラベルを付け、言語に合わせ、`Show`を呼びます。ウインドウのPresenterは自分を開くボタンを知りません。ウインドウの*中*にある、別のウインドウを開くボタンには、初期化データで`Action`を渡します(デッキ編集の敵詳細ボタンには敵詳細のHandlerの`Show`)。
  - **共用の確認ウインドウ**は`WindowHandler<ConfirmShowData>`です。`Show(new ConfirmShowData(message, detail, ok, cancel, onOk, onCancel))`はそのデータのコールバックにだけ答えるので、共用するPresenterに「自分の問いか」のフラグは要りません。ほかの確認が開いている間は何も尋ねません。知る必要がある呼び出し元は先に`IsOpen`を見ます(チュートリアルのスキップは「スキップしなかった」として扱う)。`ConfirmPrompt`と、Presenterが直接`OnOkClicked` / `OnCancelRequested`を聞いていた形を置き換えました。
  - **新しいウインドウ**に要るのは、`WindowId`(末尾に足す。カタログは番号で保存する)、カタログの1行、初期化データのクラス(要れば表示データのクラスも)、Presenterの`IWindowPresenter`、Viewをつないだ`WindowInitializerBase`の派生をプレハブのルートに付けること、シーンの`CreateWindows`の1行です。`WindowCatalogTests`が、すべてのIDにプレハブがあること、プレハブのルートのInitializerがそのIDの初期化データを受け取ること、参照が埋まっていることを確かめます。
  - **初期化データが使う型は`Common/`に移しました**(シーンに依存しないため): `BattleLogModel` / `BattleLogEntry` / `BattleLogKind`(`Common/Models/BattleLog/`)、`BattleLogFormatter` / `BattleLogLine`(`Common/Presenters/BattleLog/`)、`CardShopModel`、`UnitUpgradeModel`、`EpisodeState`(`Common/Models/`)、メニューの`MenuItemData` / `MenuConfirmData` / `MenuItemStyle`(`Common/WindowSystem/Menu/`。`MenuWindowData`は`MenuWindowInitData`になった)。
  - **開閉・ぼかし・重なり順は変わりません。** プレハブ自身の`WindowTransition` / `PopupLayerMember`が受け持ちます。
  - 2026-10-08にTitleScene・MapScene・BattleSceneのすべてのウインドウをこの形にしました。それまでは各ウインドウがシーンに置いたプレハブのインスタンスでViewContainer経由でつながり、ウインドウのPresenterが自分の開くボタンも配線していました。
- フォルダの入れ子は必ずしも名前空間と一致しません(例: あるシーンの`Presenters/CardPile/*.cs`は`namespace Live2DCardRogue.BattleScene.Presenters`のまま)。ディレクトリによるグルーピングは純粋にファイル整理のためのもので、あるエリア用のenum/プレーンなデータ型はそれを使うコードのそばの`Enum/`/`Data/`サブフォルダに置きます(例: `BattleScene/Models/Enum/BattlePhase.cs`、`BattleScene/Data/CardDropCandidate.cs`)。

### レイヤー構造

Model → UseCase → Presenter → View、加えてシーンごとのViewContainerとコンポジションルートのInitializer:

- **Model**(`Common/Models/*Model.cs`、`<Scene>/Models/*Model.cs`) — 状態を保持し、変更をC#の`event Action<...>`で公開するだけのプレーンなC#クラス(`UnitStatusModel.OnHpChanged`、`CardPileModel.OnCardAdded`など)。UnityEngineへの依存なし。
- **UseCase**(`<Scene>/UseCase/`、およびCommon側は機能ごとのフォルダ。例: `Common/Message/MessageUseCase.cs`) — プレイヤー起点のアクションに対するModelの変更をまとめて行い、他のレイヤーが反応する必要のあることは自前のイベント(あるいは結果、例えば成否を表す`bool`)として返す。
- **Presenter**(`<Scene>/Presenters/`) — Modelのイベントを Viewに橋渡しする。**すべてのPresenterは`Common/Presenters/PresenterBase.cs`(`IDisposable`)を継承する**: コンストラクタは依存性を保持するだけ、`Initialize()`で購読と最初の同期を行い、`Dispose()`で購読解除する。コンストラクタで購読しないのは、Presenterが自身の`Initialize()`が呼ばれる前にModelのイベントへ反応してしまうことを防ぐため。
  - `PresenterBase.Initialize()`/`Dispose()`は**virtualではない**テンプレートメソッドで、ロケールの購読/解除を行ってから抽象メソッド`OnInitialize()`/`OnDispose()`を呼ぶ。サブクラスが実装するのはこの2つ。virtualにすると`base.Initialize()`の呼び忘れで購読が静かに落ちるが、この形なら構造的に起こり得ない。どちらも冪等(2回目の`Initialize()`/`Dispose()`は何もしない)。
  - 3つ目のフックが`protected abstract void OnLocaleChanged()`。`LocalizationSettings.SelectedLocaleChanged`の購読は`PresenterBase`が行い、変更時にこれを呼ぶ。**意図的にvirtualではなくabstractにしている** — virtual(空のデフォルト実装)だと、ローカライズ文字列を表示するPresenterを再反映無しで書けてしまい、何も促されないまま、実際に言語を切り替えるまでバグが表に出ない。空実装自体は問題なく、ローカライズ文字列を持たないPresenterにとっては正しい答え(`UnitPresenter`は数値しか表示しない)。中間の基底クラスが一族分をまとめて実装し`sealed`にしてもよい(`CardPilePresenterBase`がそれ。山札Presenterの答えは常に「`Sync`をやり直す」なので)。
- **View**(`Common/Views/`、`<Scene>/Views/`) — データの表示と生の入力をプレーンなC#デリゲート/イベントとして流すだけの`MonoBehaviour`。ビジネスルールは持たない。あるシーンがドラッグ&ドロップのようなシーン固有の振る舞いを上乗せする必要がある場合、`Common/Views/`の基底Viewは`sealed`を外しておき、`<Scene>/Views/`のサブクラスで実装する — 基底は純粋な表示コンポーネントのままにしておくことで、将来別シーンがシーン固有の振る舞い無しの表示だけを必要とした時にも再利用できる。もし1つのシーンの外で素のバージョンが必要になったことが無いなら、先回りして分割しないこと — その振る舞いはそのままシーン自身のViewクラスに畳み込む(このプロジェクトでは時期によって両方のやり方をしています。現在の分割状況は[docs/battle-scene.ja.md](docs/battle-scene.ja.md)を参照)。
- **UIのラッパーコンポーネント**(`Common/Views/UI/`) — 長い一覧に使う仮想化スクロール`CommonScrollList`もここです(見えている行だけを生かします。[docs/deck-edit-window.ja.md](docs/deck-edit-window.ja.md)参照)。プロジェクト内で`TextMeshProUGUI`や`UnityEngine.UI.Button`を直接使っている箇所はありません。`CommonText : TextMeshProUGUI`と`CommonButton : Button`は拡張ポイントとして存在します。`CommonButton`には最初の共通の振る舞いとして、ホバーとクリックの操作音が入っています([docs/sound.ja.md](docs/sound.ja.md)参照)。`Editor/CommonButtonEditor.cs`が、標準のButtonのInspectorの下にその2つの欄を足しています。全テキスト/全ボタンで共通にしたい振る舞い(既定フォント、連打防止、押下時のSEなど)を後から足すとき、シーンとプレハブのコンポーネントを全部差し替え直さずに一箇所で足せるようにするためです。どちらも**sealedにしない**こと。`CommonText`のInspectorは標準のまま、`CommonButton`はuGUIの`ButtonEditor`を置き換えずに継承して欄を足しているので、標準の項目はそのまま使えます — TMPとuGUIのエディタはどちらも`editorForChildClasses: true`で宣言されているためです。同じフォルダの`ButtonRippleEffect`は、どの`CommonButton`にも後付けできるホバー/押下時の波紋です([docs/art.ja.md](docs/art.ja.md)参照)。新しいUIもこれらを通します: 素の`TextMeshProUGUI`/`Button`がプレハブやシーンに紛れ込むと`CommonUiComponentTests`が落ちます(素のままでも見た目も挙動も同じなので、他に気付く手段がありません)。既存コンポーネントのクラス差し替えは`SerializedObject`経由の`m_Script`差し替えで行います(上の「ターミナルから動作中のUnity Editorを操作する」を参照)。それを指している`[SerializeField]`フィールドの型を書き換える**前に**差し替えること — 順番を逆にすると次のデシリアライズで参照がnullになります。
- **ViewContainer**(シーンごとに1つ、例: `BattleSceneViewContainer.cs`) — シーンのPresenterが必要とするView/UI参照をすべて公開する`MonoBehaviour`で、Inspectorで紐付ける。
- **Initializer**(シーンごとに1つ、例: `BattleSceneInitializer.cs`) — コンポジションルート。`Common/SceneTransition/SceneInitializerBase<TData>`を継承し、その`Start`がシーン遷移データを受け取って`Initialize(TData)`を呼ぶ(シーンを直接開いたときはnull)。`Initialize`でそのシーン分のModel/UseCase/Director/Presenterをすべて`new`し、それらの間の数少ないクロスリファレンスを配線する。`OnDestroy()`でそこで生成したものすべてを破棄する。UseCaseとそれに依存するDirector/Presenterの両方を参照してよいのはここだけ。組み立てだけを行い、抱えがちな流れや状態変更(戦闘結果の反映、保存、チュートリアルの問いかけ)は、クラスの大きさのルールに従って自分が作るUseCase(`MapRunUseCase`、`TitleRunUseCase`など)へ出す。

### クラス間の配線 — 一方向の依存を優先する

2つのクラスが互いを参照する必要が生じそうな場合、このコードベースでは一貫して、片方にC#の`event`/`Action`/`Func`フィールドを持たせることで解決しています。これにより、両方の型を知っている必要があるのはInitializer(または、既にもう一方に正当に依存しているクラス自身のコンストラクタ)だけになります。一方が複数の協力者の中から*選ぶ*必要がある場合(単なる一方通行の通知ではなく)は、単一のイベントより**register/evaluate**パターンを優先してください: `Register<T>(Func<TArg, TResult> evaluator)`を公開し、登録する側が(単なるboolではなく)小さな結果クラス/構造体を返すようにすれば、呼び出し側が候補同士を比較できます — これにより、新しい協力者を後から追加する際に、選択を行うクラス側を一切変更せず`Register`の1行呼び出しだけで済みます。クラス間の新しい通知を追加する際は、下位のクラスから呼び出し元へ参照を追加するのではなく、この2つの形のどちらかに従ってください。event/コールバックの形はBattleScene全体で使われています([docs/battle-scene.ja.md](docs/battle-scene.ja.md)参照)。register/evaluateの形は現在**実例がありません** — ドラッグ&ドロップでカードがどのユニットに落とされたかを選ぶ仕組みだったので、ドラッグ&ドロップの廃止と一緒に無くなりました(クリックされたユニットは自分で名乗り出るため、候補から選ぶ必要がそもそも無い)。複数の協力者の答えを本当に比較する必要が出てきたら、またこの形を使ってください。

### デリゲートの呼び出し

`Action`/`Func`フィールドは必ず`.Invoke(...)`で呼ぶこと(例: `_canDrag.Invoke()`、`OnHpChanged?.Invoke(...)`)。`_delegate(...)`という省略記法は使わない — grepしやすく、「保持しているデリゲートを呼んでいる」ことが「メソッドを呼んでいる」と紛れずに読める。

### コメント

コードベース全体でコメントが多すぎるので、今後は控えめに書く。コメントはコードの説明ではなく、将来の実装・変更時に必要な**意図・制約・不変条件**を残すために使う。「何をしているか」ではなく「なぜそうしているか」を優先する。

**コメントを残すもの:**

- コードだけでは判断できない「なぜそうしているか」
- 一見すると変更・簡略化できそうだが、変更すると仕様を壊す処理
- Unityや外部ライブラリの仕様・制約による特殊な実装
- ライフサイクル上の重要な前提
- 他のクラスとの暗黙的な契約
- 型や状態によって意図的に処理を変えている箇所
- 将来のリファクタリング時に誤って削除・変更されやすい設計上の判断

**コメントを残さないもの:**

- コードをそのまま日本語にしただけの説明
- メソッド名・変数名から明らかな処理
- if文やループなど、コードを読めば分かる処理内容
- 単純なgetter/setterや代入処理
- 実装の手順を説明するだけのコメント

**コードを変更するとき(AIを含む):**

- 既存コメントに書かれた意図・制約・不変条件は仕様として扱う。
- 不要に見えるコメントでも、なぜ存在するのかを確認せずに削除・簡略化しない。
- 逆に、コードだけでは将来の変更を誤らせかねない重要な設計意図は、短いコメントとして残す。

### クラスの大きさと責務

- **クラスは単一責務を基本とする。** 1つのクラスは1つの仕事をします。複数の仕事をしていれば、その仕事に沿って分けます: Modelの書き換えや流れはUseCase、共有の処理や入出力はHelper / Service、計算は小さな純粋クラス(テストもできる)へ。
- **200行を超えたら分割を検討する。** 行数ではなく、クラスが何をしているかを見ます。
- **責務が明確で凝集度が高いクラスは無理に分けない。** 行数だけを理由に分けないこと。1つのことをするウインドウのPresenterや、演出の多いViewはそのままにします。
- **ドキュメントに書かれた「分けない」判断は、意図して見直すまで守る**(例: `BattleCommandQueueUseCase`はコマンドを選ぶことと積むことを同じクラスに置く。[docs/battle-scene.ja.md](docs/battle-scene.ja.md)参照)。

### 新しいシーンを追加するとき

`TitleScene`([docs/title-scene.ja.md](docs/title-scene.ja.md))は同じ形の最小版です: 自分で状態を持たないシーンはModel層とUseCase層を落とし、Initializer + ViewContainer + Presenter だけにします。

**シーンのロードは`Common/SceneTransition/SceneTransitionUseCase.LoadScene(data)`経由で行い**、`SceneManager`を直接呼ばないこと。`data`は`SceneTransitionData`(`Common/SceneTransition/SceneTransitionData.cs`。抽象クラス)で、**行き先を自分で持ちます**(`Destination`。各派生クラスのコンストラクタが`SceneName`から渡す)。呼び出し側はデータを渡すだけで、データと行き先の組み合わせを間違えられません。**どの型も行き先は1つで、行き先が複数ありうるデータは行き先ごとに型を分けます**(後述)。渡すものが無い遷移にも型を用意する(`TitleSceneTransitionData`、`MapSceneTransitionData`)ので、型の一覧がそのまま各シーンへの入り方の一覧になります。インタフェースでなく抽象クラスなのは、「遷移には必ず行き先がある」をコンストラクタ1箇所で保証するためです。データの受け渡しは`Common/SceneTransition/SceneTransitionDataStore`が担い、**すべてのシーンのInitializerの基底`Common/SceneTransition/SceneInitializerBase<TData>`**が一度だけ読みます: その`Start`が`TryConsume<TData>`で取り出して抽象メソッド`Initialize(TData data)`に渡し、Initializerはそこでシーンを組み立てます(シーンを単体で開いたときは`data`がnull)。受け取れたときは捨てるので後の遷移に残りません。型引数がそのシーンへの入り方なので、**1つのシーンが受け取る型は1つ**です。場所によって違うデータで入ってくるシーンは、それらの抽象基底を作って受け取り、中で派生型を見て分けます(`ResultSceneTransitionDataBase` → エンディングからの`ResultSceneTransitionData`、チャレンジからの`ChallengeResultTransitionData`)。Initializerは自分で`Start`を定義しないこと: Unityはそちらだけを呼ぶので、取り出しが黙って飛ばされます(`SceneInitializerTests`が確かめます)。シーン名は`Common/SceneTransition/SceneName.cs`に置き、ロードするシーンはBuild Settingsへの登録が必要です。型は`Common/SceneTransition/TransitionData/`に置きます(`TitleSceneTransitionData`、`MapSceneTransitionData`、`BattleSceneTransitionData`、`EndingSceneTransitionData`、`ResultSceneTransitionDataBase`とその`ResultSceneTransitionData`・`ChallengeResultTransitionData`。名前空間は`Live2DCardRogue.Common.SceneTransition`)。どのシーンも別のシーンを参照せずに済むようにするためで、どれもシーンの開始条件を表し、`Common/`の型だけでできています。(2026-10-06までは入っていく側のシーンに`…SceneParameter`という名前で置き、`LoadScene`にシーン名と一緒に渡していて、呼び出し側はそのシーンの`Data`名前空間を参照していました。シーンを完全に独立させ、呼び出し側がデータ以外を書かずに済むようにするためにやめました。)単体でも再生できるInitializerは、`Initialize`にnullが来たとき自分のInspectorのモックにフォールバックし、そうしたことをログに出します(`BattleSceneInitializer.CreateMockParameter`)。シーンを直接開いてPlayを押す確認手順を残すためです。

**どの読み込みも`Common/SceneTransition/SceneTransitionEffect`で覆って行います。** これは自分で生成して`DontDestroyOnLoad`で持ち続けるシングルトン(`Resources/SceneTransitionEffect.prefab`)で、シーンを切り替えるときに固まる瞬間を見せないためのものです。流れは次のとおりです。

1. **覆う。** 横に割った帯が、左右交互にグリッチ的に滑り込んで画面を覆います。
2. **読み込む。** 覆った下で、次のシーンを`LoadSceneAsync`で読み込みます。
3. **初期化を待つ。** さらに`_settleFrames`フレーム待ち、新しいInitializerの`Start`と最初のレイアウトを覆われたまま済ませます。
4. **明ける。** 帯がグリッチ的に抜けて、新しい画面を見せます。

細かい決まり:

- **ローディング表示。** 覆っている時間が`_loadingDelay`(0.2秒)を超えたときだけ、`// LOADING`と進捗バーを出します。すぐ終わる切り替えでは一瞬も出しません。
- **遷移を重ねません。** `SceneTransitionEffect.IsTransitioning`の間は`LoadScene`は何もせず(引き継ぎデータも上書きしません)、演出のCanvasが入力を受け止めます。
- **Screen Space - Overlay。** これはOverlayで描く唯一のCanvasです。遷移の途中でカメラが壊れて作り直されるため、カメラにつなぐCanvasではカメラを見失ってしまいます。
- **プレハブが無いとき。** `LoadScene`は普通の`SceneManager.LoadScene`に戻ります。
- **シーン側の登場演出。** シーンが出てくるときに動かしたいものは、`IsTransitioning`がfalseになるまで待たせてください。待たないと覆いの下で再生されてしまいます(`MapPlayerView`はそうしています)。

**結果を返すときも同じ一方通行の規則です。** 入ってまた出ていくシーンは素の結果(`Common/Models/BattleResult`)を作り、データの一部として**帰り道でそれをどう包むか**を受け取ります(`BattleSceneTransitionData.CreateReturnData`。`Func<BattleResult, SceneTransitionData>`)。結果をどこへ持って行くかは呼び出し側が決めます — マップは`ReturnToMap`(結果を持った`MapSceneTransitionData`)か、チャレンジなら`ReturnToMapOrEndChallenge`(負けはResultScene行きの`ChallengeResultTransitionData`)を渡します — 呼ばれた側はそれを呼ぶだけなので、呼んだ側も戻り先のシーンも知りません。この関数はシーンの切り替えをまたいで呼ばれるので、呼び出し側のオブジェクトを掴まないこと(マップのものは`static`)。関数がnullなら「その場に留まる」で、単体再生が成り立つのはこれのおかげです。(2026-10-06までは、戦闘が戻り先のシーン*名*(`ReturnSceneName` / `DefeatSceneName`)を受け取り、どちらにも同じ`BattleSceneResult`を送っていました。ユーザーの要望で行き先ごとに分けました。)

**ScriptableObject はシーン間を ID で渡し、実物は`Common/Repositories/GameAssetRepository`が持ちます。** 遷移データが握っているだけでは読み込まれたままになりません。前のシーンからしか参照されていないと、C# が指していても、ビルドではシーン切り替えの未使用アセットの解放で消えることがあり(エディタでは再現しません)、次のシーンには破棄済みのオブジェクトが届きます。Android の起動後最初の模擬戦でチュートリアルが流れなかったのはこれでした(2026-10-17)。そこで`BattleSceneTransitionData`は`DeckIds` / `EnemyIds`を運びます。コンストラクタは今までどおり`CardData[]` / `EnemyData[]`を受け取り、それぞれを`GameAssetRepository.Hold`に預けます。預かったものには`HideFlags.DontUnloadUnusedAsset`を付け、アプリを閉じるまで ID で持ちます(小さいので放しません)。戦闘は`FindCard` / `FindEnemy`で引き直します。預けるのは送り出すシーンが生きているうちでなければならないので、データを作るときに行います。チュートリアルの手順は渡さず、`CampaignTutorialRepository`がチュートリアルの開始から終了まで同じ印を付けて持ちます(@docs/tutorial.md。データは`PlayTutorial`だけ)。ほかにシーンをまたぐ ScriptableObject ができたら同じ形にします(ID と`Hold`)。普通の C# のデータ(モデル、結果)には不要です。

**シーンをまたいで残す必要のある状態は`Common/Repositories/`へ**。パラメータに詰めて往復させないこと。例が`CampaignProgressRepository`で、マップが現在のマスを書き、戦闘はその存在を知らず、マップが戻ってきてまた読みます。`CampaignDeckRepository`(最大10個のデッキと、そのうち戦闘で使うアクティブな1つ、所持カード)と`CampaignTokenRepository`(キャンペーン中のトークン。キャンペーン開始時に消える)も並んでいます。`CampaignUnitRepository`(ユニット強化で上げたプレイヤーの最大HPとメモリー上限。キャンペーン開始時に消える)も並びます。`RunModeRepository`は、今のランがストーリーかチャレンジかと、チャレンジの種類・階層数・シードを持ちます(キャンペーンの4つのリポジトリは両モードで共用)。`GameSettingsRepository`はゲーム全体の設定(今は戦闘速度だけ)を、今後のオプション画面のために持ちます。マーケットやデッキ編集のウインドウはこれらに触れず、マップがPresenterに値を渡して結果を書き戻します。「同じシーンに何度も戻ってくる間保っておきたいデータ」はRepository、「別のシーンが開始するために必要なデータ」はパラメータです。

`BattleScene`はシーンをどう構成すべきかのリファレンス実装です([docs/battle-scene.ja.md](docs/battle-scene.ja.md)に詳細)。2つ目以降のシーンを追加する際は、新しい形を考え出すのではなく、これと同じ形に揃えてください:

- `Assets/Scripts/Scenes/<SceneName>/`配下に`Live2DCardRogue.<SceneName>.*`を作り、BattleSceneが使っているのと同じサブフォルダ構成を必要に応じて用いる(`Models/`、`UseCase/`、`Directors/`(またはそれに相当するフロー制御クラス)、`Presenters/`、`Views/`、`Initializer/`、`Constants/`、複数のPresenterで共有する結果/DTOクラス用の`Data/`)。
- そのシーン固有でないもの(Model、View、UseCase)は`Common/*`に置き、再利用できるようにする — シーン固有のコードを書く前に、Common側に既に同等のものがないか確認すること。
- ウインドウ(自分のModel/Presenter/Viewを持つモーダルのポップアップ)はシーンのフォルダではなく`Window/<Name>/`に置く。ボタンを並べるだけなら新しいウインドウではなく`MenuWindowInitData`にする。新しいシーンの入口パラメータ・結果の型は`Common/SceneTransition/TransitionData/`に置く。
- 上記のレイヤー構造とライフサイクルを維持する。
- 2つのクラスが互いを参照しそうになったら、上の「クラス間の配線」に従って解決する。新しいシーンのクラス同士が互いの具象型を参照し合う形にはしないこと。
- 新しくシーン別ドキュメントを書いたら、このファイルの該当セクションに(上の各セクションのように)対応するリンクを追加して見つけやすくし、本当にシーンに依存しない内容はシーンごとに重複させずここに書くこと。

### カードシステム(`Common/Models/Cards/`、`Common/ScriptableObjects/CardData.cs`)

**プレイヤーには「カード」という言葉を見せません。**「手札」も出さず、**バッファ / Buffer**(このターンに読み込んだコマンド)と呼びます。コードは`Hand`のままです。 画面上ではカードは**コマンド**、カードパックは**チップ**、カード購入は**チップ購入**、デッキは**コマンドデッキ**と呼びます([docs/tone-and-manner.ja.md](docs/tone-and-manner.ja.md)の「言葉」参照)。コード・アセット名・ローカライズのキーは`Card` / `CardPack`のままです(IDは文言ではないため)。新しいUIの文言は画面上の呼び方で書き、新しいコードは既存の型名に合わせます。

`CardData`(`ScriptableObject`)は`_id`・`_cost`・`_rarity`(パックの抽選でだけ使います。[docs/card-shop-window.ja.md](docs/card-shop-window.ja.md)参照)、そして`[SerializeReference] CardEffectBase[] _effects`を持ちます — すべてprivateで公開用の読み取り専用プロパティ経由。**表示用テキストは`CardData`には一切持たせません**: `_id`はカードの安定した識別子(アセット名と同じ値: `Attack`、`Block`、`StrongAttack`)で、カード名も説明文もこのidをキーに`CardName` / `CardDescription`ローカライズテーブルから引きます(下の「ローカライズ」を参照)。したがってカードを追加する作業は「アセットを作る」だけでなく「両方のテーブルにエントリを足す」までを含みます(忘れると`CardLocalizationTests`が落ちます)。絵を付けるなら、`_illustration`へのイラストの割り当ても必要です([docs/art.ja.md](docs/art.ja.md)参照)。`CardEffectBase` → `AttackCardEffectBase` / `BlockCardEffectBase` / `PoisonCardEffectBase` / `StrengthCardEffectBase` / `HealCardEffectBase` / `RegenerationCardEffectBase` → 具象クラス(`BasicAttackCardEffect`、`BasicBlockCardEffect`、`BasicPoisonCardEffect`、`BasicStrengthCardEffect`、`BasicHealCardEffect`、`BasicRegenerationCardEffect`)。2つ目のカード群で足した葉は次のとおりです。`PiercingAttackCardEffect`(攻撃の基底の`DamageType`を`HpChangeType.TakePiercingDamage`に差し替える。ブロックはこれを相殺しない)、`AreaAttackCardEffect`、`HpRatioAttackCardEffect` / `HpRatioBlockCardEffect`(使用者の**今の**HPの割合)、`PayHpCardEffect`(代償のHP。`HpChangeType.PayHp`で、1より下げない)、ターン数で切れる効果を付けるカテゴリ`SelfStatusCardEffectBase`(`MemoryBoostCardEffect`・`HandSizeUpCardEffect`・`HandSizeDownCardEffect`)と`OpponentStatusCardEffectBase`(`StunCardEffect`・`HealReversalCardEffect`)。攻撃しつつ回復するカードは、1枚に効果を2つ並べただけです(`Siphon`)。**全体攻撃**は`ICardEffect.TargetsAllOpponents`(virtual、既定は`false`)で表します。対象は今まで通り敵を1体選び、`BattleCommandExecuteUseCase.ExecuteNext`が生きている相手全員に1回ずつ`Execute`を呼びます(注入された`getLivingOpponents`。`EnemyPartyModel.GetLivingStatuses`につないである)。強化も、各相手のブロックも1回ごとに効きます。カテゴリごとに作用先が違う点に注意: 攻撃と毒は`target`に、ブロック・強化・回復・継続回復は`user`に付きます。この区別は`ICardEffect.TargetsOpponent`としても公開していて、`CardData.TargetsOpponent`がカードの効果全体のORを取ります。どのカードも対象選択を挟むので、これが決めるのは「**どちら側**を選べるか」です(`BattleCommandQueueUseCase.CanSelectTarget`が「それがプレイヤー自身のユニットか」と突き合わせます)。`CardEffectBase`では`abstract`で、具象クラスごとではなくカテゴリ基底ごとに1回だけ答えます: 新しいカテゴリを足すときは作用先の表明が必須になり、新しい具象クラスは正しい答えを自動で受け継ぎます。`ICardEffect.Execute(UnitStatusModel user, UnitStatusModel target)`がUseCaseから呼ばれるエントリポイントで、各基底クラスの`OnExecute`が量を計算し(`CalculateDamage`/`CalculateBlockAmount`)、それを適用します(`target.TakeDamage(...)`、`user.AddStatusEffect(new BlockStatusEffect(...))`)。

**カードの説明文は数値に追従する。** 説明文に数字は書きません。`CardDescription`の各エントリは名前付きの差し込み口を持つ**Smart String**です(`{damage}ダメージを与える`、`Deal {damage} damage`)。`LocalizationHelper.GetCardDescription`が`CardData.CreateDescriptionValues()`で埋めます。このメソッドは、各効果が`ICardEffect.AddDescriptionValues`で足した値をまとめます。`CardEffectBase`では`abstract`なので、新しい効果は自分の数値を必ず差し出します。1枚の中で効果どうしがぶつからないよう、名前は効果ごとに固有です: `damage`・`block`・`heal`・`poison`・`regeneration`・`strength`・`hpCost`・`damagePercent` / `blockPercent`、時間制の効果は`memoryTurns` / `memoryAmount`・`handUpTurns` / `handUpAmount`・`handDownTurns` / `handDownAmount`・`stunTurns`・`reversalTurns`。英語は Smart の複数形を使います(`{stunTurns} {stunTurns:plural:turn|turns}`)。Inspectorで数値を調整すれば、説明文はどこでも書き変わります。`CardDescriptionTemplateTests`が全カード・全言語でこの約束を見張ります:
- すべてSmartである
- 差し込み口の外に数字が無い
- 差し込み口はすべてカードが差し出している
- カードの数値はすべて文に出ている
- 1枚の中で名前が重ならない
- 差し込んだ結果に括弧が残らない

番号で差し込む素の`string.Format`(`{0}`)は採りませんでした。効果が複数あるカードでどの数字が何かが分からなくなり、言語ごとに並び順を合わせる必要も出るためです。


**2026-09-27に足したコマンド**(どれもチップの封入と`CardCatalog`に入れてある):

| Id | 画面上 | 効果 | 備考 |
| --- | --- | --- | --- |
| `Loopback` | ループバック | `LoopbackCardEffect` | 次のコマンドがアーカイブではなく次の手札に戻る |
| `Multithread` | マルチスレッド | `MultithreadCardEffect`(`_repeat` 3) | 次のコマンドを3回実行し、パージする |
| `Mining` | マイニング | `PayHpCardEffect`(20)+`TokenBonusCardEffect`(50%) | `CardData._purgeOnUse`。勝つと獲得トークンがそのぶん増える |
| `ReversePatch` | リバースパッチ | `HealReversalCardEffect`(2ターン) | 敵1体 |
| `KernelPanic` | カーネルパニック | `HealReversalAllCardEffect`(3ターン) | プレイヤーを含むすべてのユニット |
| `Bypass` | バイパス | `BlockBreakCardEffect` | 相手のブロックをすべて解除(`UnitStatusModel.RemoveStatusEffects`)。ダメージなし |
| `BufferOverflow` | バッファオーバーフロー | `BasicAttackCardEffect`(40)+`BufferLockCardEffect`(1ターン) | 2026-09-29に変更。下記 |
| `RepairPacket` | リペアパケット | `OpponentHealCardEffect`(15、コスト2) | **相手**を回復。回復の反転が付いていればダメージ |
| `Worm` | ワーム | `WormCardEffect`(2) | `PoisonCardEffectBase`の葉で、代わりに`WormStatusEffect`を付ける(`CreatePoisonEffect`) |

**2026-09-29の変更。**

| Id | 表示名 | 効果 | 備考 |
| --- | --- | --- | --- |
| `ReversePatch` / `RepairPacket` | | コスト2→3 / 1→2、回復20→15 | 回復反転+リペアのコンボが強すぎた |
| `BufferOverflow` | バッファオーバーフロー | `BasicAttackCardEffect`(40)+`BufferLockCardEffect`(1ターン) | パージは無くなり、次のバッファが空になる(`BufferLockStatusEffect`)。続けて使うとターン数が足される |
| `GarbageCollection` | ガベージコレクション | `OverflowAttackCardEffect`(2個まで、1個10) | 以前のバッファオーバーフローの仕組み。バッファを2個までパージし、1個につき10ダメージ |
| `PortScan` / `PrivilegeEscalation` / `Terminate` | ポートスキャン / 権限昇格 / ターミネート | `ExploitChainCardEffect`(`_step` 1 / 2 / 3) | 1ターンの中でこの順(0x00、0x01、0x02)に実行すると、最後で相手を即死させる(`HpChangeType.InstantKill`)。1・2段目は自分、3段目は敵を選ぶ。`CardData._maxCopiesInDeck` = 1。3段目が順番どおりに発動したら(即死を無効にされても)、**3つともその戦闘の間パージされる**(`IChainCardEffect`、docs/battle-scene.ja.md参照) |
| `CodeInjection` | コードインジェクション | `GlitchInjectCardEffect`(2ターン、50%) | 相手の行動のたびに、50%で敵専用の`SystemError`(自分に`PayHpCardEffect` 10)が先に割り込む |
| `SpyMemoryLeak`(敵) | メモリーリーク | `StaminaDrainCardEffect`(3) | スパイウェア。プレイヤーの次のターンのメモリーが3減る |
| `SystemError`(敵) | システムエラー | `PayHpCardEffect`(10) | グリッチで割り込む。`BattleSceneInitializer._glitchCard` |

**2026-10-04に追加したコマンド**(すべて`CardCatalog`に登録。スターター・プレミアム・ファーストビルドのチップは10個すべて、テーマ別のチップは関係するものを持ちます):

| Id | 表示名 | コスト / レア度 | 効果 | 補足 |
| --- | --- | --- | --- | --- |
| `ZeroDay` | ゼロデイ | 4 / Epic | `AllUnitsPoisonCardEffect`(8) | 生きている**すべての**ユニット(プレイヤーも)に感染。対象は自分(ハックチップ) |
| `Redo` | リドゥ | 2 / Rare | `RedoCardEffect` | リドゥ以外で最後に実行したコマンドの効果を、その対象(倒れていれば振り直し)とパージ数でもう一度出す。チェーンの段は繰り返さない(ハックチップ) |
| `Flush` | フラッシュ | 2 / Normal | `FlushAttackCardEffect`(8、+16) | 実行時にバッファが空なら追加(アタックチップ) |
| `SwapOut` | スワップアウト | 1 / Rare | `PurgeBlockCardEffect`(2個まで、1個15) | `IHandPurgeCardEffect`。ガベージコレクションと同じく選ぶ(リペアチップ) |
| `Defrag` | デフラグ | 1 / Rare | `PurgeHealCardEffect`(2個まで、1個15) | 同じく回復(リペアチップ) |
| `CloudBurst` | クラウドバースト | 2 / Epic | `PayTokensCardEffect`(100)+`PiercingAttackCardEffect`(50) | トークンは積んだときに払い、負けても戻らない(アタックチップ) |
| `Pipeline` | パイプライン | 1 / Normal | `BufferCountAttackCardEffect`(1個2) | 実行時にバッファに残っている数(アタックチップ) |
| `BruteForce` | ブルートフォース | 2 / Normal | `ChanceAttackCardEffect`(50 %、30) | `ChanceAttackCardEffect.Random`は静的な`System.Random`で、テストで差し替えられる(アタックチップ) |
| `Reflection` | リフレクション | 2 / Rare | `BlockStrikeCardEffect` | 使用者の`BlockValue`分のダメージ。ブロックは減らない(アタックチップ) |
| `Format` | フォーマット | 3 / Epic | `FormatCardEffect` | 生きているすべてのユニットから状態効果をすべて消す(ブロックも)。**性質**(`IStatusEffect.IsTrait`: 無効化・毒耐性・階層ブースト)は残る(ハックチップ) |
| `TrojanBreach`(敵) | ファイアウォール突破 | — | `BlockBreakCardEffect` | トロイの木馬 |
| `RootkitHijack`(敵) | リソースハイジャック | — | `TargetHpRatioAttackCardEffect`(20 %) | ルートキット。**相手の**今のHPの割合、最低1 |

**2026-10-08の変更。**

| Id | 表示 | コスト / レア度 | 効果 | 備考 |
| --- | --- | --- | --- | --- |
| `Forensics` | フォレンジック | 3 / Rare | `DefeatCountAttackCardEffect`(6、倒した数ごとに+10) | この戦闘で倒した敵1体ごとに10を足す(`CardExecutionContext.DefeatedEnemyCount`。数えるのは`EnemyPartyModel.DefeatedCount`で、生成された敵は倒すたびに数える)。スターター・プレミアム・ファーストビルド・アタックチップ |
| `Interrupt` | インタラプト | | | `CardData._maxCopiesInDeck` = 1(複数枚でボスを止め続けるのが簡単すぎるため)。それより前に保存したデッキは、編集するまで枚数がそのまま |

- **戦闘の事情を見る効果。** プレイヤーのコマンドでは`Execute`の代わりに`ICommandContextCardEffect.ExecuteWithContext(user, target, CardExecutionContext)`が呼ばれます。事情(`Common/Models/Cards/Base/CardExecutionContext.cs`)は実行時のバッファの数と、生きているユニットすべて(プレイヤーが先頭)、この戦闘でここまでに倒した敵の数です。事情の無い場面(敵が使う)では`Execute`に戻ります(ゼロデイとフォーマットは使用者と対象に効く)。`BattleCommandExecuteUseCase.ExecuteEffects`の振り分けは 全体攻撃 → バッファのパージ → 事情 → 普通の`Execute` の順です。
- **トークンの支払い。** `ITokenCostCardEffect.TokenCost`を`CardData.TokenCost`が合計します。戦闘はトークンを`BattleTokenModel`に持ち(`BattleSceneTransitionData.Tokens`から)、`BattleCommandQueueUseCase`が払えないコマンドを断り(「トークンが足りません」)、積んだときに払い、バッファに戻したら返し、払った分を`BattleResult.TokensSpent`で報告します。マップは勝っても負けてもそれを差し引きます。
- **リドゥ。** `CardData.RepeatsPrevious`(`RedoCardEffect`を持つ)。`CommandHistoryModel`が、リドゥ以外で最後に実行したコマンドとその対象・パージ数を**そのターンの間だけ**覚えています(`BattleTurnEndUseCase.EndPlayerTurn`で忘れる)。そのターンの1つ目として実行したリドゥは何もしません。トークンを払うコマンドを繰り返すときはもう一度払い(`TokensSpent`に数える)、足りなければリドゥは不発です。

- **無効化。** `IStatusEffect.BlocksStatusEffect(incoming)`(既定`false`)で、効果が新しい効果を拒めます。拒まれると`UnitStatusModel.AddStatusEffect`は何も足さず、`OnStatusEffectBlocked`を発火します。`VirusImmunityStatusEffect`(ウイルスとワーム)と`CorruptionImmunityStatusEffect`(回復反転)がこれを使い、`KillImmunityStatusEffect`は`HpChangeType.InstantKill`を0にします。定義は`VirusImmunityDefinition` / `CorruptionImmunityDefinition` / `KillImmunityDefinition`で、ルートキットがウイルス無効と即死無効、ボットネットがデータ破損無効を持ちます。`Value`が0の状態効果は名前だけを表示します。
- **`StatLimitType.DrawCap`**(元の値`int.MaxValue`)は「最低1枚」の後に引く枚数を頭打ちにし、これでバッファロックは0枚にできます。**`StatLimitType.GlitchChance`**(元の値0)は、`BattlePhaseDirector`が敵の行動の前に振る確率(%)です。
- **`CardData._maxCopiesInDeck`**(0で上限なし)は`DeckEditModel.CanAddToDeck`と`AutoDeckBuilder`が守ります。

`CardData._purgeOnUse`は、使ったコマンドをアーカイブではなく**パージの山**へ送ります([docs/battle-scene.ja.md](docs/battle-scene.ja.md)参照)。ウイルスのコマンド(`Poison`)はコスト5・毎ターン4ダメージになりました。敵のウイルスは敵専用の別アセット(`TrojanVirus`、`RootkitVirus`、`SpyVirus`)に分けたので、コマンドを調整しても敵は変わりません。

**バッファ(手札)が要る効果。** カードの効果は2体のユニットしか見ないので、バッファを代償にする効果は`IHandPurgeCardEffect`(`HandPurgeCount`、`ExecuteWithPurged(user, target, purgedCount)`。`CardData.HandPurgeCount`がそれを読む)を実装します。どれをパージするかは積むときにプレイヤーが選び([docs/battle-scene.ja.md](docs/battle-scene.ja.md)の「カード使用の流れ」参照)、選んだものは`CardCommand`(`PurgeCards`)に預けられ、実行したときにパージされて(1個ずつ`OnCardDisposed`で知らせる)、`Execute`の代わりにその数を渡して`ExecuteWithPurged`が呼ばれます。バッファの無い場面(敵が使う)では`Execute`に戻ります。`AttackCardEffectBase.DealDamage`は、そうした効果が使い回す「強化を通してから相手の割り込みを通す」共通の手順です。

**敵専用の効果。** `HandJamCardEffect`は`HandSizeDownStatusEffect`を*相手*に付けます(自分への代償の`HandSizeDownCardEffect`は使用者に付けます)。`AllyHealCardEffect`は**target**を回復し、新しい`ICardEffect.ChoosesAlly`(virtual、既定false。`CardData.ChoosesAlly`がORでまとめる)を立てます。敵がそういうカードを使うと、`BattlePhaseDirector`はプレイヤーではなく、生きている味方で一番弱っているもの(`EnemyPartyModel.FindWeakestLiving`、HPの割合が一番低い)をtargetに渡します。

素のUnityは`[SerializeReference]`フィールドに型選択UIを提供しないため、`Assets/Scripts/Editor/SerializeReferenceDrawerBase.cs`がカスタム`PropertyDrawer`として、ドロップダウン(`TypeCache.GetTypesDerivedFrom(BaseType)`経由)で具象型を選択/差し替えし、そのフィールドをインライン編集できるようにしています。Unityはドロワーをフィールドの**宣言型**で解決するため、`[SerializeReference]`の基底型ごとに`[CustomPropertyDrawer]`を付けた1行のサブクラスが必要です。現状は`CardEffectBaseDrawer`(`CardEffectBase`)と`EnemyCardPickerDrawer`(`IEnemyCardPicker`)の2つ — 属性は抽象クラスだけでなくインターフェースにも使えます。新しい具象型を足せば対応するドロップダウンに自動的に現れます。ドロワーを追加する必要があるのは、`[SerializeReference]`の**基底型**を新設したときだけです(それも1行)。

### 敵データ(`Common/ScriptableObjects/EnemyData.cs`、`Common/Models/EnemyCardPickers/`)

`EnemyData`は`_id`・`_maxHp`・`_minActionCount`/`_maxActionCount`・`CardData[] _cards`・`[SerializeReference] IEnemyCardPicker _cardPicker`を持ちます — 構成は`CardData`と同じ(privateフィールド + 公開読み取り専用プロパティ + `[CreateAssetMenu]`)。

**敵はスタミナを持ちません。**1ターンの行動量は代わりに行動回数で決まり、`BattleSceneInitializer`は敵の`UnitStatusModel`を`maxStamina: 0`で生成します。敵が使うカードはコストを払わず、消費もされません — 敵側には手札も山札も捨て札もありません。

`IEnemyCardPicker.Pick(IReadOnlyList<CardData> cards, int minActionCount, int maxActionCount, EnemyPickContext context)`にはその敵の**全カード**、行動回数の範囲、戦況(`EnemyPickContext`: 今と最大のHP、生きている味方の数、何ターン目か、独りで終えたターン数。使わないPickerは無視してよい)が渡され、**実行順に並べたカード**を返します。つまり1回の呼び出しでそのターン全体を組み立て、`BattlePhaseDirector`はその結果を`foreach`するだけです(@docs/battle-scene.md を参照) — 何回・どの順番で行動するかをDirectorが決めることはありません。何も消費されないので、同じカードを複数回返しても構いません(そういう想定です)。

実装は3つあります。`RandomEnemyCardPicker`(ほとんどの敵。仮実装)は行動回数を`[min, max]`から抽選し、その回数分を一様ランダムに埋めます。`FixedOrderEnemyCardPicker`は並べた順に使います(チュートリアルの敵)。`HpPhaseEnemyCardPicker`(ボス)はHPがしきい値以上か未満かでカードごとの重みを切り替え、生きている味方がいなければ、1ターン目か、独りで`_summonAfterAloneTurns`ターン終えたあとに、生成カードを最初の行動にします(下の「ボス」参照)。`UnityEngine.Random`ではなく`System.Random`を使うのは、ModelレイヤーをUnityEngine非依存に保つためです(`CardPileModel.Shuffle`と同じ理由)。新しい実装は`IEnemyCardPicker`を実装した`[Serializable]`クラスとして書けば、エディタ側を触らずにInspectorのドロップダウンへ現れます。

**敵が戦闘開始時から持つ状態効果**は、`EnemyData`に`[SerializeReference] StatusEffectDefinitionBase[] _initialStatusEffectDefinitions`として書き、`BattleSceneInitializer`がループ開始前に`CreateInitialStatusEffects()`を敵の`UnitStatusModel`へ追加します。

**定義は効果そのものではありません。** 定義は専用フォルダ(`Common/Models/Statuses/Definitions/`)に置いてあります。オーサリング用の型だからで、隣の効果本体と違いUnityEngineに依存します。`StatusEffectDefinitionBase.Create()`は呼ばれるたびに新しい`IStatusEffect`を作るので、オーサリングした数値は読み取り専用のままでいられます: `IStatusEffect`は戦闘中に`Value`や`IsExpired`を書き換えるため(ブロックの消費、毒の重ね掛け)、実行時の型をアセットに置くとその変化がScriptableObjectに書き戻ります。定義が必要なのは、設計者が実際にアセットへ置く効果だけです — 現状`PoisonResistanceDefinition`の1つだけで、`Block`/`Poison`/`Strength`はカード効果がコードで生成するので不要です。追加は`[SerializeField]`でパラメータを持つ`[Serializable]`クラスに`Create()`を書くだけで、Inspectorのドロップダウンには自動で出ます(`StatusEffectDefinitionDrawer`)。

この分離によって、`StatusEffectBase`は**UnityEngineに依存しない**素のModelのままでいられ、引数なしコンストラクタも不要になります。以前は逆に効果自体をシリアライズしており、その対価としてModelへの`[Serializable]`/`[SerializeField]`、効果ごとの`protected`な引数なしコンストラクタ、ドロワーの`Activator.CreateInstance(nonPublic: true)`、そして浅いコピーの`Clone()`(効果が参照型を持った瞬間に壊れる)を抱えていました。この種の仕掛けを足したくなったら、回避しようとしているのはこの定義層です。

この層は意図的に**`Id`を持ちません**。さらに以前の`IEnemyPassive`は独自の`Id`を持ち、状態効果側と同期させる必要がありましたが、それがあの設計の真の欠陥でした。ローカライズのキーは生成された`IStatusEffect.Id`だけで引きます。

**ボス: ランサムウェア(エピソード1 Stage 10、2026-10-08)。** `Enemies/Ransomware.asset`: HP360、常に2回行動、即死無効、`HpPhaseEnemyCardPicker`(しきい値50 %)。見た目は仮にルートキットのものです。

| Id | 表示 | 効果 | 重み(HP 50 %以上 / 未満) |
| --- | --- | --- | --- |
| `RansomEncrypt` | 暗号化 / Encrypt | 12ダメージ | 50 / 20 |
| `RansomRestore` | 自己修復 / Self-Repair | 自分を15回復 | 20 / 50 |
| `RansomShield` | 暗号シールド / Cipher Shield | 12ブロック | 30 / 30 |
| `RansomSpawn` | ボットネット生成 / Spawn Botnet | `SummonEnemyCardEffect`(ボットネット、HP25) | 1ターン目と、独りで3ターン終えたあとの最初の行動 |

- **生成はボスの行動です。** `SummonEnemyCardEffect`は何を・どの最大HP(`_hp`)で出すかを持つだけで、`BattleCommandExecuteUseCase.ExecuteEnemyCard`が`EnemyPartyModel.Summon`に頼みます(Initializerで配線)。ユニットごとの表示・Presenter・対象一覧・ログは戦闘の始めに作られるので、生成された敵を**戦闘中に足すのではなく**、`BattleEnemyFactory.CreateReserves`が戦闘の始めに生成カードごとに1体、倒れて隠れた*枠*を空いている敵Viewに用意し、`Summon`が`UnitStatusModel.Revive`で戦場に戻します(HPを最大に、状態効果を外し、`OnHpChanged`ではなく`OnRevived`を流すので、回復の数字もログも出ず、回復反転も効きません)。表に出すのは`EnemySummonPresenter`です。倒されたら、また生成を待ちます。倒れた枠は倒れた敵として数えるので、ボットネットが倒れている間にボスを倒せば勝ちです。
- **ボットネットにした理由。** 初めはスパイウェアでしたが、シミュレーション(2026-10-08)で、重なるウイルスとメモリーの吸い取りが勝敗を決めてしまい、バランスの良いデッキがほぼ勝てない(0 %。生成なしなら98 %)と分かりました。ボットネットのDDoSは8ダメージ、ボットリペアは一番弱っている味方(ふつうはボス)を回復するので、ボスに回復反転を付けておけば、その修復もダメージになります。ボットネット自身は回復反転を受けません。
- **独りのターン数**は、Directorが各敵のターンの終わりに呼ぶ`BattleEnemy.CountTurnEnd`が数え、Pickerには`EnemyPickContext.TurnsAlone`で渡ります。
- 数値は遊んで確かめるための初版です: 雑なデッキは負け、回復への答え(回復反転・ウイルス・停止・大ダメージ)を持つデッキは勝てる、を狙っています。

**敵の見た目**は`EnemyData._portrait`(1枚絵。アイコンにも使う)と`EnemyData._visual`です。後者は`UnitVisualDefinition`で、動く`IUnitVisual`(アイドル・行動・被弾)を組み立てます。バトルはインタフェースしか触らないので、Live2Dなど別方式への切り替えは定義のサブクラスを1つ足すだけです — [docs/art.ja.md](docs/art.ja.md)の「キャラクター」を参照。

`_cardPicker`に公開アクセサは**意図的に用意していません**。入口は`EnemyData.PickCards()`だけです。アセット自身のカードと行動回数をアセット自身のPickerに渡すので、呼び出し側が取り違えようがありません。またInspectorでの編集で生じ得る3つの不具合をここで吸収するので、**呼び出し側はnullチェック無しで`foreach`できます**: `_cards`の空欄要素はPickerに見せる前に除外し、`_minActionCount > _maxActionCount`は寄せ、Picker未設定(やPickerがnullを返した場合)もnullではなく空リストを返します。

### 状態効果システム(`Common/Models/Statuses/`)

`IStatusEffect` / `StatusEffectBase`が公開する値への割り込みフックは**量の種類ごとに1つ**です。これに副作用用の`OnTurnStart` / `OnTurnEnd`が加わります。各enumは`Statuses/Enum/`にあります:

| フック | enum | 適用する側 | 意味 |
| --- | --- | --- | --- |
| `OnHpChange` | `HpChangeType`(`TakeAttackDamage`、`TakePoisonDamage`、`Heal`、`TakePiercingDamage`、`PayHp`) | `UnitStatusModel.ChangeHp` | このユニットが**受ける**値 |
| `OnStaminaChange` | `StaminaChangeType`(`Gain`、`Consume`) | `UnitStatusModel.ChangeStamina` | このユニットが**受ける**値 |
| `OnDealtValue` | `DealtValueType`(`AttackDamage`、`Block`、`Poison`、`Strength`、`Heal`、`Regeneration`) | `UnitStatusModel.CalculateDealtValue` | このユニットが**生み出す**値 |
| `OnStatLimit` | `StatLimitType`(`MaxStamina`、`HandSize`、`ActionCount`) | `UnitStatusModel.CalculateLimit`(と`MaxStamina`) | このユニットの**上限**。元の値はルール側が渡す |
| `OnCommandStart` / `OnCommandDestination` / `OnCommandEnd` | `CommandDestination`(`Discard`、`ReturnToHand`、`Purge`) | `UnitStatusModel.BeginCommand` / `CalculateCommandDestination` / `EndCommand`。プレイヤーのコマンドだけ、`BattleCommandExecuteUseCase.ExecuteNext`が呼ぶ | プレイヤーが**次に実行するコマンド**の行き先。何回実行するかは`StatLimitType.CommandRepeat`(既定1) |

前の2つはユニットの状態を変え、**符号付きの変動量**を受け取ります。`CalculateDealtValue`は状態を一切変えず、**プラスの大きさ**を受け取って補正後の大きさを返すだけで、適用先を決めるのは呼び出し側の仕事です。各カード効果の基底クラスは、自分の数値を適用する前に必ず*使用者の*`CalculateDealtValue`に通します。これにより、各効果クラスが強化バフを知らなくても攻撃側のバフが効きます — `StrengthStatusEffect`は`DealtValueType.AttackDamage`にだけ`Value`を加算します。攻撃側のバフと対象側のデバフは**別のメカニクス**である点に注意してください: 強化は対象が何体いようが攻撃1回に1度乗りますが、「受けるダメージが増える」は対象側に付く`OnHpChange`の効果になります。また各基底クラスは割り込みの*前*に`baseValue <= 0`を弾くので、ダメージ0のカードは強化をいくら積んでも0のままです。

**割り込み地点を増やす作業は「enumに値を足す」であって、「インターフェースにメソッドを生やす」ではありません。**元々は地点ごとに`OnXxx(uint)`が1つずつあり、すべて`uint f(uint)`という同じ形で効果の種類とともに増え続ける形でした。それを一度1つの汎用フックに統合した上で、**`UnitStatusModel.ChangeHp`にスタミナの種別を渡せないよう**リソース単位に分け直しています。フラットなenum 1つだとこれは実行時チェックになりますが、リソースごとにenumを分ければコンパイルエラーにできます。分ける単位は「効果」ではなく「量」です — フックを追加するのは、本当に別の量を割り込み対象にするときだけにしてください。

実装側は関心の無いtypeに対して`value`をそのまま返す必要があります — `StatusEffectBase`の既定実装がそうなっているので、overrideは通常`if (type != ...) return value;`の1行に続けて自分の処理を書くだけです。`TakePoisonDamage`を`TakeAttackDamage`と別にしてあるのは意図的です: 後者で作った耐性は毒と通常の被弾を区別できず、あらゆるダメージを軽減してしまうためです。裏返しとして、`BlockStatusEffect`は`TakeAttackDamage`しか見ないので**ブロックは毒を吸収しません**。

**`TakeDamage`/`Heal`という対は存在せず、どちらも`UnitStatusModel.ChangeHp(StatusInterventionType type, int amount)`です**。HP変動の汎用関数が1つあるだけで、ここでは2つを意図的に分離しています:

- **`type`が決めるのは「どの割り込みを通すか」だけ**で、増減方向には関与しません。呼び出し側は常に種別を明示します: `AttackCardEffectBase`は`TakeAttackDamage`、`PoisonStatusEffect`は`TakePoisonDamage`。以前は毒が`ApplyIntervention(PoisonDamage, ...)`してから`TakeDamage(...)`を呼んでおり、毒と攻撃の**両方**の割り込みを黙って通っていました — 種別を渡す形にするとこの間違いが表現できなくなります。`HpChangeType`はHP変動しか含まないのでスタミナの種別はそもそも渡せず、実行時チェックは不要です。
- **増減方向を決めるのは`amount`の符号**です — マイナスでHP減少、プラスで回復。このモデルの数値が`uint`ではなく`int`なのはそのためです。これにより状態効果側で符号を反転でき(回復反転効果なら`Heal`に対して`-value`を返す)、`ChangeHp`側はそれを知る必要がありません。`OnIntervene`を流れる値はHPの変動量なので、ダメージにだけ関心のある実装はプラスの値を無視する必要があります — `BlockStatusEffect`が`value >= 0`で早期リターンしているのはこのためです。

Inspectorで入力する大きさ(ダメージ・コスト・ブロック・毒量・HP)は`[Min]`で非負に拘束してあります。符号を持つのは*モデルに渡す変動量*の方だけです。`UnitStatusModel.StatusEffects`はフラットなリストで、同じ`Id`の効果を2つ持つことはありません: `AddStatusEffect`は`IsSameEffect`で一致する効果を見つけると、新しい値をその効果の`OnDuplicated(value)`に渡し、新しいインスタンスは捨てます。**再度付与されたときにどう振る舞うかは効果ごとの判断**です — 加算する、大きい方を採る、無視する。メソッド名が特定の振る舞いではなく「重複が起きた」という状況を表しているのはこのためです。期限切れの効果は`StartTurn()`と`EndTurn()`の両方で削除されます。

**効果は取り除かれるまで持続します。** `IsExpired`は`false`から始まり、効果自身しかそれをtrueにしないので、「永続」が既定で、ワンショットの方が明示的に抜ける側です — `BlockStatusEffect`は値が0になった時点で`IsExpired`を立て、次の`StartTurn`/`EndTurn`の掃除で取り除かれます。この2種類を分ける基底クラスはありません。以前あった`PermanentStatusEffectBase`は`OnDuplicated`を提供するためだけの存在で、それを`StatusEffectBase`に降ろした時点で中身が無くなりました。

`StatusEffectBase.OnDuplicated`の**既定は大きい方の値を残す**です(`MaxValue`で頭打ち)。これが**永続効果**のゲーム全体の決まりで、加算すると重ね掛けで際限なく伸びるためです。**強化と継続ダメージ**(筋力・ウイルス・ワーム)は2026-10-04からこの一歩先で、**大きい方 + 1**を残します(`StatusEffectBase.StackPlusOne`)。重ねると少し得をしますが、累積はしません。ほとんどの効果はこれを継承し、関心のある割り込みフック1つだけをoverrideします。決まりの外にあるのは2種類で、それぞれ自分でoverrideしています: 削られて尽きるブロック(積み増すことに意味があるので加算。`MaxValue`で頭打ち、桁あふれを避けた実装)と、長い方の持続を残す時間制の効果(後述)です:

| 効果 | 付与する側 | 割り込む地点 | 重複時 |
| --- | --- | --- | --- |
| `BlockStatusEffect` | `BlockCardEffectBase` | `HpChangeType.TakeAttackDamage` | **加算**(自前のoverride。永続効果ではない) |
| `PoisonStatusEffect` | `PoisonCardEffectBase` | — (`OnTurnEnd`で発動) | 大きい方 + 1(`StackPlusOne`) |
| `PoisonResistanceStatusEffect` | `PoisonResistanceDefinition` | `HpChangeType.TakePoisonDamage` | 大きい方の値を残す(継承) |
| `RegenerationStatusEffect` | `RegenerationCardEffectBase` | — (`OnTurnEnd`で発動) | 大きい方の値を残す(継承) |
| `StrengthStatusEffect` | `StrengthCardEffectBase` | `DealtValueType.AttackDamage` | 大きい方 + 1(`StackPlusOne`) |
| `MemoryBoostStatusEffect` | `MemoryBoostCardEffect` | `StatLimitType.MaxStamina`(+`Amount`) | 時限: 長い方を残す |
| `HandSizeUpStatusEffect` / `HandSizeDownStatusEffect` | `HandSizeUp/DownCardEffect` | `StatLimitType.HandSize`(±`Amount`) | 時限: 長い方を残す |
| `StunStatusEffect` | `StunCardEffect` | `StatLimitType.ActionCount`(→ 0) | 時限: 長い方を残す |
| `HealReversalStatusEffect` | `HealReversalCardEffect`(敵のカード`Corrupt`) | `HpChangeType.Heal`(符号を反転) | 時限: 長い方を残す |
| `LoopbackStatusEffect` | `LoopbackCardEffect` | `OnCommandDestination`(`Discard`→`ReturnToHand`) | 次のコマンド: それで使い切る |
| `MultithreadStatusEffect` | `MultithreadCardEffect` | `StatLimitType.CommandRepeat`(→`Value`)、`OnCommandDestination`(→`Purge`) | 次のコマンド: それで使い切る。重ねがけは回数の大きい方 |
| `WormStatusEffect` | `WormCardEffect` | —(`OnTurnEnd`で`Value`+持ち主の`BlockValue`の半分(`BlockRatePercent` 50、端数切り捨て。ブロックそのままでは強すぎた)を`TakePoisonDamage`で与える。ブロックは数えるが防がない) | 大きい方 + 1(`StackPlusOne`) |
| `LayerBoostStatusEffect` | `BattleSceneTransitionData.EnemyStrength`(チャレンジの11階層以降の敵) | `DealtValueType.AttackDamage`(+`Value`) | 大きい方を残す(継承)。筋力と打ち消し合わないよう別の効果 |
| `TokenBonusStatusEffect` | `TokenBonusCardEffect`(`Mining`) | —(戦闘の終わりに`BattleResult.TokenBonusPercent`として読まれる) | **加算**(独自のoverride。使うたびにパージされるので、デッキに入れた枚数が上限) |

**次のコマンドに効く効果**は`NextCommandStatusEffectBase`を継ぎます。付いた後でプレイヤーが実行し*始めた*次のコマンドに効き、そのコマンドが終わると使い切って消えます。`OnCommandStart`で準備し、`OnCommandEnd`は準備できたものだけを消すので、効果を付けたコマンド自身では使い切らず、ターンの終わりでも消えません。同じコマンドの行き先を複数が変えるときは、それぞれが何より強いかを決めます。`Multithread`はいつもパージ、`Loopback`は捨て札を戻すに変えるだけなので、**パージが戻すより強い**です。

**時限の効果**は`TimedStatusEffectBase`から派生します。`Value`が残りターン数、`Amount`が大きさで、大きさは付けた時のままです。数えるのは、持ち主のターンの**始まり**に既に付いていたターンだけです(`OnTurnStart`で構え、`OnTurnEnd`で数える)。そのため「2ターン」は、持ち主自身のターン中に付けても(手札の増減)、相手のターン中に付けられても(停止・回復反転)、持ち主の次の2ターンを意味します。重ねがけは足し合わせず長い方を残します。停止を重ねて敵を永久に止められないようにするためです。戦闘では、効果の名前の横に値が出ます(`UnitPresenter`が`Value`を添える)。

つまり永続効果は、累積して強化されることはなく、逆に弱くなることもありません。筋力・ウイルス・ワームは重ねるたびに1ずつ増えます: +2の強化を3回掛けると+4(2、3、4)、その後に+5を掛ければ+6です。継続回復と毒耐性は大きい方を残すだけです(強化と継続回復は以前は加算で、2026-09-27にユーザーの要望で変えました。2026-10-04に、強化と継続ダメージは重ねると1増えるようユーザーの要望で変えました)。`RegenerationStatusEffect`は毒の裏返しです — 発動タイミングも`OnTurnEnd`で同じ、値が減らない点も同じで、符号だけが逆です。発動時に通すのも専用の`TakeRegenerationHeal`ではなく素の`HpChangeType.Heal`です: 毒に専用の種別が要ったのは耐性が通常の攻撃と区別できるようにするためでしたが、回復側には「継続回復だけを強化/無効化したい」という要求がまだ無いからです。出てきた時点で足してください。この性質上、永続効果の値は1回の付与で高々1しか伸びないので、上限は不要で`MaxValue`は既定値のままです。効いている上限はブロックの999です。`TakePoisonDamage`を`TakeAttackDamage`と分けている結果として、`BlockStatusEffect`は**毒を吸収しません**。

### カード山札モデル(`Common/Models/CardPileModel.cs`)

任意のカード山札(山札/手札/捨て札。3つとも単に別々の`CardPileModel`インスタンス)の実体です。`OnCardAdded` / `OnCardRemoved` / `OnCleared`を発火します。`MoveAllTo`は(リストを直接操作するのではなく)`RemoveCard`/`AddCard`経由で実装されており、これは移動元のパイルでも確実に削除イベントが発火するようにするためです — これをリストの直接操作に「最適化」して戻さないこと。読み取り用に`IReadOnlyList<CardData> Cards`を公開し、変更はすべて`AddCard`/`RemoveCard`/`Clear`/`MoveAllTo`/`Shuffle`経由で行います。

### `UnitStatusModel`(`Common/Models/UnitStatusModel.cs`)

1ユニット分(プレイヤーまたは敵 — ユニット側には一切シーン固有の要素はない)のHPとスタミナを保持します。`ChangeHp`/`ChangeStamina`は、最終的な量を適用する前にすべての状態効果の対応するフックを実行し、その後`OnHpChanged`/`OnStaminaChanged`を発火します。`MaxStamina`は**計算で出す値**で、`BaseMaxStamina`(コンストラクタで渡した値)を`CalculateLimit(StatLimitType.MaxStamina, …)`に通したものです。最大値を上げていた効果が切れると、掃除の際に`Stamina`を新しい最大値まで下げて`OnStaminaChanged`を発火します。そのため、このターンに使ったメモリー増設は、このターンの終わりの回復から効き、ちょうど効果のターン数だけ続きます。`StartTurn`/`EndTurn`は対応する状態効果フックを実行し、期限切れの効果を削除します。

状態効果の通知は2種類あります。

- `OnStatusEffectsChanged`は、一覧の数が増減したときに発火します。
- `OnStatusEffectValuesChanged`は、既にある効果に同じ効果が`OnDuplicated`で重ねられたときに発火します。ブロックの重ねがけのように、数は変わらず値だけが変わる場合です。

`ChangeHp`中の割り込みで値が使われた場合(ブロックが攻撃を吸収した場合)は、どちらも発火しません。それは`OnHpChanged`で分かります。最終的な変動量が0でも発火します。

### グローバルなトーストメッセージ(`Common/Message/`)

`MessageToast`は自己生成型の`DontDestroyOnLoad`シングルトンです: `MessageToast.Show(string)`は初回呼び出し時に`Resources/MessageToast.prefab`を遅延生成し、シーンをまたいで保持し続けます。表示中に繰り返し呼び出すと、滑り込みはやり直さずに、テキストの差し替えと自動非表示までのタイマーのリセットだけを行います。シーンをまたいで残ることはなく、`SceneManager.sceneLoaded`で動きなしにすぐ消えます(読み込みは遷移の覆いの下で起きるので、消える瞬間は見えません)。前の画面の通知が次の画面に持ち越されることはありません。`MessageUseCase.ShowMessage(...)`はこれを薄くラップしたもので、本来Viewへの依存を持たない純粋なUseCaseクラスからでもメッセージを表示できるようにしています — 例えばカード使用失敗時に、UseCase側がどのView型のことも知らずに理由を表示できます。

`MessageToast`は**操作を止めない通知**です。画面上部にノイズ混じりで出てきて、`_displayDuration`の間とどまり、またノイズと共に消えます。その間も下の画面はずっと操作できます。これが作り直したときの要件です(以前の版は背景をぼかして画面中央に出していました)。

- **ぼかさない。** `PopupBlurController`をPushしません。
- **入力を受け取らない。** Canvasに`GraphicRaycaster`を付けず、`Slider`の`CanvasGroup`は`blocksRaycasts = false`、どのGraphicも`raycastTarget`を切ってあります。

描画はポップアップの層のままです。`Show`のたびにCanvasを`UICameraRig.Current.PopupCamera`へ繋ぎ直し(リグの無いシーンではScreen Space - Overlayにフォールバック)、SortingLayerとGameObject Layerは`UI_Popup`なので、ほかのどのUIよりも手前に出ます。

**出現の演出。** 上からの滑り込み、ブラウン管のような開き(パネルの縦スケール)、透明度の明滅、ノイズの帯、テキストのシアン/マゼンタの残像、記号から左へ順に確定していく「デコード」を重ねています。テキストの`LayoutElement.preferredWidth`を最終的な文字列の幅に固定してあるので、デコード中にパネルの大きさは変わりません。

**時間**は`Time.unscaledDeltaTime`で進め、1フレームあたりの上限を付けています。上限が無いと、長い1フレーム(処理落ちや、裏に回ったEditor)で演出も表示時間も丸ごと飛ばされます。

**レイアウト。** 背景はプロジェクト内で唯一、スクリプトで位置を決めずに`HorizontalLayoutGroup` + `ContentSizeFitter`で自分の大きさを決めている箇所です。長さの分からない文字列に合わせて伸びる必要があるためです。アニメーションと両立できるのは、アニメーションが動かすのが外側の`Slider`とパネルのスケールだけで、Layout Groupが並べる中身の位置には一切触らないからです(Layout Groupを避ける普段の理由は @docs/battle-scene.ja.md を参照)。

### ゲームバランス(`Common/ScriptableObjects/CampaignBalance.cs`)

**キャンペーンの調整に使う数値と、遊ぶステージの並びは、すべて1つのアセット`Assets/ScriptableObjects/CampaignBalance.asset`にまとめます。** コードの定数にも、シーンのInspectorにも散らしません。項目は次のとおりです:
- **ステージ:** エピソードごとの`CampaignData`(`_episodes`。エピソード1から順に。`GetEpisode(index)` / `HasEpisode(index)`)。実体は`Assets/ScriptableObjects/Stages/Episode1..3.asset`で、今はエピソード2・3はエピソード1の複製です(同じ`StageData`を指し、始まりの会話は無し。後から別々に調整できる)。マップは`CampaignProgressRepository.EpisodeIndex`のものを遊び、マスはそのステージの数だけ並びます。タイトルはどのエピソードにステージがあるかをこれで見ます
- **開始時:** トークン・最大HP・メモリー上限・ファーストビルドの前から持っている所持コマンド(今は空)
- **ファーストビルド:** キャンペーンの最初に開けるチップと個数(`FirstBuildChip.asset`×10)・その中で確定で入るコマンド(アタック×5、ブロック×4)・自動で組むデッキの役割ごとの枚数(攻撃10、防御6、補助4。合計は`DeckEditConstants.MaxDeckSize`にそろえる)
- **スコアとランク:** トークン1あたりの点・バックアップブースト1回の減点・8つのランクの閾値(E〜SSS。`OnValidate`がEを0に、上ほど大きく保つ)・BestとNormalのルートが始まるランク
- **ユニット強化:** HPとメモリーそれぞれの1段の上がり幅・上限・値段の基礎・段ごとの増分
- **チップ:** 各チップの`CardPackData._price`にかける値段の係数(シングル購入にもかかる)
- **シングル購入:** マーケットでコマンドを1つずつ買うときのレアリティごとの値段(ノーマル40、レア90、エピック180)と、キャンペーンを通した1種類あたりの上限(3)。`CreateSinglePurchaseRule()` → `SinglePurchaseRule`としてショップに渡す(docs/card-shop-window.ja.md参照)

**ステージは敵を強くできます。** `StageData._enemyHpPercent`(既定100)と`_enemyAttackBonus`(既定0)で、チャレンジと同じ`EnemyStrength`(最大HPの倍率と、`LayerBoostStatusEffect`としての攻撃の上乗せ)を使います。ストーリーでは使っていません: 2026-10-08から全ステージ100 % / +0で、素の数値からレベルデザインできるようにしてあります。

ステージごとの敵と報酬は`StageData`に、チップごとの値段と中身は`CardPackData`に残します。バランスのアセットが決めるのは、*どの*ステージの並びを遊ぶかだけです。

- **Modelはアセットを見ない。** ModelはUnityEngineに依存しない決まりなので、アセットは素のC#のルールを作って渡します。`CreateScoreRule()`で`CampaignScoreRule`を作り`CampaignResultCalculator`へ、`CreateUnitUpgradeRule()`で`UnitUpgradeRule`を作り`UnitUpgradeModel`へ、`CreateAutoDeckRule()`で`AutoDeckRule`を作り`AutoDeckBuilder`へ渡します。`OpenFirstBuild(random)`はファーストビルドを開けます。出る数はチップの`CardCount`×個数で、確定枠を先に入れ、残りを`CardPackData.OpenCards`で抽選します。チップの係数は`MapRunUseCase.CreateCardShopModel`を通ってマーケットの`CardShopModel`へ渡るので、モデルは今もキャンペーンを知りません。
- **使う側。**
  - `MapSceneInitializer._balance`: ステージ・開始時の持ち物・強化のルール・成績のルール・チップの係数
  - `EndingSceneInitializer._balance` / `ResultSceneInitializer._balance`: 単体で再生したときのモックの成績を計算するためだけ
- **テスト。** 計算そのもののテストは自前のルールを作るので、アセットを調整しても壊れません。バランスのテストはアセットを読みます。`CampaignRankReachabilityTests`は**すべての**`CampaignBalance`アセットを、それぞれ自分のステージで確かめます(ステージが設定されていて空でないか、SSSに届くか、減点が敗北時の報酬より大きいか)。閾値が昇順かのテストは既定のアセットを見ます。`FirstBuildBalanceTests`は、すべてのバランスのアセットのファーストビルド(チップが設定されているか、確定枠が入るか、役割ごとの枚数の合計がデッキの規定枚数か、どの引きでもデッキが組めるか)を確かめます。
- **難易度。** 1つの難易度 = バランスのアセット1つです。複製して、別の`CampaignData`(敵や報酬の違う`StageData`の並び)を指し、数値を変えます。まだ選ぶ仕組みはなく、マップは`MapSceneInitializer._balance`のものを使います。タイトルで選べるようにするなら、再開したキャンペーンが難易度を保てるよう、選んだものをセーブデータにも残す必要があります。

### セーブ(`Common/Save/`)

キャンペーンの進捗とクリア記録は、`Application.persistentDataPath/save/`の下の**暗号化したファイル**(`campaign.sav`・`clear.sav`)です。中身は`JsonUtility`のJSONで、`SaveCipher`で封をします。PlayerPrefsは採りませんでした。Windowsではレジストリに入り、デッキのような一覧に向かず、バックアップ・削除・同期もしにくいためです。

- **`SaveFileStorage`**は、1つのフォルダのテキストファイルの読み書きだけを受け持ち、中身の形は知りません。書き込みは`<名前>.tmp`に書いてから`File.Replace` / `File.Move`で置き換えます。書いている途中で落ちても前の保存は壊れません。
- **`CampaignSaveService`**が入口です(`Default`は本物のフォルダ。テストは一時フォルダで作る)。中身は3つのクラスへの窓口です: `SaveFileCodec`(1ファイルの読み書き。暗号化と平文からの移行)、`CampaignSaveSnapshot`(リポジトリ ⇄ `CampaignSaveData`。`Create` / `Restore`)、`ClearRecordStore`(`clear.sav`。クリア、チャレンジの記録、見つけたコマンドと敵)。
  - **`campaign.sav`**(`CampaignSaveData`)は、キャンペーンの4つのリポジトリ(進行・デッキと所持コマンド・トークン・ユニット強化)の写しです。タイトルがキャンペーンのアセット無しで説明できるよう、`stageCount`と`savedAt`も持ちます。
  - **`clear.sav`**(`CampaignClearData`)には、クリアのたびに`CampaignClearRecord`を1件足します(日時、エピソード(`episodeIndex`、0始まり。版2から。版1の記録はエピソード1として読む)、スコア、ランクとルートの名前、トークン、バックアップブースト)。タイトルはこれを見て次のエピソードを解放します(`HasCleared`、@docs/title-scene.md)。遊んでいるエピソード(`CampaignProgressRepository.EpisodeIndex`)は進捗にも`episodeIndex`として保存します(版4から。それより前はエピソード1として読む)。
- **コマンドはアセットの参照ではなく`CardData.Id`で保存します。** `Restore`が`Assets/ScriptableObjects/CardCatalog.asset`(`CardCatalog`)から引き直し、知らないIDは警告を出して飛ばします。名前を変えたコマンドは読み込みを壊さずに抜け落ちるだけです。**コマンドを足したらカタログにも足します**(忘れると`CardCatalogTests`が落ちる)。
- **シングル購入の記録**は`purchasedSingles`(買ったコマンドのID、買った数だけ)として保存します。版2で足したもので、版1のファイルは「まだ何も買っていない」として読みます。
- **デッキ**は`decks`(1枠ごとに`SavedDeck { cards }`。`JsonUtility`は配列の配列を書けないため)と`activeDeckIndex`として保存します。版3で足したものです。`deck`にはアクティブなデッキも入れたままです。版1・2のファイルは、1つだけの`deck`を1番目のアクティブなデッキとして読みます。
- **版番号を持つ。** どちらのファイルにも`version`があります。`TryLoadProgress`は壊れたファイルや新しすぎる版を読み違えずに断ります。形を変えるときは`CurrentVersion`を上げ、古い版の読み方をそこに足します。
- **保存するとき。** `MapRunUseCase`(マップのランの流れ)が`SaveProgress`を呼びます。戦闘の結果を反映したあと(勝ち**も負けも**。負けで保存しないと、終了してバックアップブーストを無かったことにできるため)、デッキ編集を保存したとき、マーケットで買うたび、ユニット強化を反映するたび、そしてマップのメニューで中断したとき(`Suspend`)です。最終ステージに勝った直後は保存しません。その報酬を受け取るとクリアを記録して進捗を消す(`LoadEnding`)ので、受け取る前に終了すれば最終ステージの前から再開できます。
- **再開。** タイトルのキャンペーンボタン(画面では「ストーリー」)が`TryLoadProgress`を見て、`CampaignResumeDialog`(再開 / 破棄 / キャンセル。破棄は先に共通の`ConfirmDialog`で「本当に破棄しますか？」と確かめ、そのあとエピソード選択を開く。中断データが無ければエピソード選択をすぐ開く。[docs/title-scene.ja.md](docs/title-scene.ja.md)参照)を開きます。新しく始めるときと破棄したときは4つのリポジトリをすべて空にし、マップが初期値を配り直します。
- **暗号化と改ざん検知(`SaveCipher`)。** ファイルの形は、`L2CR`の目印、形式の版1バイト、書くたびに乱数で作る16バイトのIV、AES-256-CBCの暗号文、その前全部に付けるHMAC-SHA256です(Encrypt-then-MAC)。署名は復号**より先に**、比べる時間が一定になるように確かめます。書き換えられた・壊れたファイルは途中まで読まずに断り、エラーをログに出して、無いものとして扱います。2つの鍵は、`BuildSecret`がばらばらの値から組み立てた1つの元からSHA-256で作ります。**これを変えると、それまでのセーブはすべて読めなくなります。** 限界も正直に書くと、オフラインのゲームは鍵を中に持つしかないので、手作業での書き換えと破損は防げても、ゲーム本体を解析する相手までは防げません。
- **移行。** 暗号化より前の平文の`campaign.json` / `clear.json`は、`.sav`が無ければ一度だけ読みます。次に保存したときに`.sav`を書き、`.json`は消します。
- **設定(`settings.json`)。** `GameSettingsSaveService`が`GameSettingsRepository`を、人が読める素のJSON(`GameSettingsData`。ほかと同じく版番号付き)で書きます。設定は改ざんされても困らないので暗号化しません。項目は、戦闘スピード、BGMと効果音の音量(0〜1。`SoundPlayer`が鳴らすときに`SoundLibrary`の音量に掛けるので、当てる処理は要らない)、言語(`Locale`のコード。空ならパッケージの既定の選び方)、フルスクリーンかウインドウか、チュートリアルを見たか、です(版2。版1のファイルは既定値で読む)。起動時に一度(`RuntimeInitializeOnLoadMethod`、最初のシーンより前)読み込み、`Common/Settings/GameSettingsApplier`が画面モード(`Screen.SetResolution`。フルスクリーンはディスプレイの大きさの`FullScreenWindow`、ウインドウは1920×1080 / 1600×900 / 1280×720のうちディスプレイより小さい最大のもの。エディタでは何もしない)を当てます。言語は、Localization Settingsの起動時の言語の選び方に、コマンドラインの次として入れた`Common/Settings/GameSettingsLocaleSelector`が、ローカライズの初期化の中で選びます(初期化の完了の通知から当てる形は、同期で待つ呼び出しの中で通知が来ると「Reentering the Update method」の例外になった)。オプションで反映したときは`SelectedLocale`を直接変えます。オプションで反映したとき、初めてチュートリアルを見たときに書き出します。ファイルが無い・壊れている・新しすぎるときは今の値のままです。
- **チャレンジ**(`endless.sav`。種類を問わず中断できるチャレンジは1つ。同じ`CampaignSaveData`に`isEndless` / `endlessSeed`(版5)と`challengeType` / `challengeLayerCount`(版6。版5のランはエンドレスとして読む)を足したもの)は、`RunModeRepository.IsChallenge`の間に`SaveProgress`が書きます。`TryLoadProgress` / `DeleteProgress`は`RunMode`を受け取る(既定はストーリー)ので、ストーリーとチャレンジを1つずつ中断しておけます。負けるか踏破すると`FinishChallengeRun`がそのチャレンジの`CampaignClearData.challengeRecords`(版6。最高到達階層と踏破。古い`endlessBestLayer`はエンドレスの記録として読む)を更新し、`endless.sav`を消します。`Restore`はファイルのモードに切り替えます。
- **一度でも所持したコマンド**(タイトルのコマンドのコレクション)は`CampaignClearData.discoveredCommands`(版4)です。`SaveProgress`・`RecordClear`・`FinishChallengeRun`のたびに所持コマンドを足します(`RecordDiscoveredCommands`。増えたときだけ書く)。ランが終わっても残り、ユーザーデータの削除で`clear.sav`ごと消えます。**戦ったことのある敵**(エネミーのコレクション)は`CampaignClearData.discoveredEnemies`(版5)で、マップが戦闘を始めるときに`RecordDiscoveredEnemies`が足します。`Restore`はファイルのモードに切り替えます。
- **ユーザーデータの削除**(タイトルのオプション)は、`campaign.sav`・`endless.sav`・`clear.sav`を消し(`DeleteProgress`・`DeleteClearData`)、チュートリアルを見たかを戻します。音量・言語・画面モード・戦闘スピードは残します。進み具合ではなく、遊ぶ人の環境の設定だからです。
- 保存に失敗してもログに出すだけで例外にはせず、次の節目でまた保存します。

### デバッグ機能(`Common/DebugMenu/`) — リリースビルドには絶対に入れない

**デバッグ関連はすべて`#if UNITY_EDITOR || DEVELOPMENT_BUILD`で囲みます。** ファイルそのものも、それを使う行もすべてです。リリースビルド(Development Buildのチェックなし)では一切コンパイルされません。プレイヤースクリプトを両方の設定でコンパイルして確認済みです(`PlayerBuildInterface.CompilePlayerScripts`)。リリースのアセンブリには`DebugMenu`の型も`BattlePhaseDirector.DebugForceFinish`もありません。`#if`の外から参照するとリリースビルドだけが壊れ、エディタのコンパイルでは気付けないので、新しいデバッグコードは同じ方法のコンパイルで確かめてください。

- **開き方。** 数字キー(上段・テンキーどちらでも)で**0707**と打つとデバッグパネルが出て、もう一度打つと消えます。判定は素のクラス`DebugCommandSequence`(EditModeテストあり)で、揃ったら入力を空にするので`070707`で二度切り替わりません。数値の入力欄に打った数字は数えません。パネルの外をクリックするか、コマンドを実行すると入力欄から抜けて、また0707が効きます。
- **`DebugMenuOverlay`**は自分で生成される`DontDestroyOnLoad`のオブジェクトです(`RuntimeInitializeOnLoadMethod`)。キーはInput Systemで読むので、ランタイムのasmdefが`Unity.InputSystem`を参照しています。描画は**IMGUI**で、ゲームのuGUIやトンマナとはあえて分けています。プレハブや`Resources`のアセットは持ちません(コードが入らなくてもアセットはビルドに入ってしまうため)。ラベルは英語のままでローカライズしません。
- **デバッグ機能の足し方。** シーンのInitializerで`DebugMenuOverlay.AddCommand(label, action)`(ボタン)か`AddIntCommand(label, defaultValue, action)`(数値の入力欄 + ボタン。actionには入力欄の値が渡る。数字のみ)を呼び、戻り値の`IDisposable`を持っておいて`OnDestroy`でDisposeします。すべて`#if`の中です。パネルには今のシーンが登録したものだけが並びます。
- **今あるもの:**
  - BattleSceneの**Battle: Win / Battle: Lose**(`BattlePhaseDirector.DebugForceFinish`。[docs/battle-scene.ja.md](docs/battle-scene.ja.md)参照)。
  - BattleSceneの**Battle: Speed %**(`GameSettingsRepository.BattleSpeed`を変える。100が標準)。
  - MapSceneの**Map: Clear Stage**(`MapDebugCommands.ClearCurrentStage`)。今いるステージの戦闘に勝って戻ってきたのとまったく同じに、勝ちの`BattleResult`を持った`MapSceneTransitionData`でマップを開き直すので、報酬ウインドウ・次の部屋への移動・保存・(最終ステージなら)エンディングへ、が普段通りに進みます。キャンペーンを踏破した後とチュートリアル中は断ります。
  - MapSceneの**Map: Clear All (score)**(`MapDebugCommands.ClearAllStages`)。全ステージを踏破したことにして、入力したスコアの`CampaignResult`(残りトークン = スコア、バックアップブースト0回として計算)でエンディングへ進みます。ランクとルートは通常と同じ決まりで決まります([docs/ending-and-result-scenes.ja.md](docs/ending-and-result-scenes.ja.md)参照)。
  - MapSceneの**Map: Own All Commands**(`MapDebugCommands.SetAllOwnedCount`、既定20)。マーケットのチップに入っているコマンド(プレイヤーのコマンド。`CardCatalog`には敵専用のものも入っている)すべての所持数を、入力した数にそろえます。デッキは触りません。
  - MapSceneの**Map: Set Tokens**(`MapDebugCommands.SetTokens`、既定9999)。トークンを入力した数にします。
  - どちらも`MapRunUseCase.SaveIfAllowed`で保存し、表示を作り直すためマップを開き直します。保存のときに所持コマンドが「見つけた」として記録されるので、Own All Commandsのあとはタイトルのコマンドコレクションに全コマンドが載ります。

### アートアセット

**トンマナは [docs/tone-and-manner.ja.md](docs/tone-and-manner.ja.md)** にあります。どの画面も従う、コンセプト・色の役割・形・ドットの決まり・動き・操作・言葉づかいと、試してやめたことをまとめています。見た目や操作に手を入れる前に読んでください。**ユーザーの指摘をもとに保守します:** 見た目・動き・操作・言葉づかいについて指摘を受けたら、コードの変更と同じ変更の中でこのドキュメントも更新してください(決まりを足すか直す。覆された方針は、日付と理由を付けて「試してやめたこと」の表へ移す)。英語版と`.ja.md`は内容をそろえておくこと。

アートは`ArtSource/`のPythonが`Assets/Art/`以下の決まったファイル名に書き出し、`Editor/ArtImportPostprocessor.cs`が取り込み設定を付けます。置いてあるものはすべて**差し替え前提の仮素材**です — 書き出しの手順、各画像の割り当て先、本番の絵への差し替え方は [docs/art.ja.md](docs/art.ja.md) を参照。

### 音

**音はすべて`Common/Audio/AudioUseCase`を通して鳴らします**(`PlaySe(SeId)`、`PlayBgm(BgmId)`)。`MessageUseCase`が`MessageToast`を包んでいるのと同じ形です。ゲームの音のために`SoundPlayer`や`AudioSource`を直接触らないでください。各シーンのBGMは`SceneBgm`部品で決めます。音は`SoundSource/`が`Assets/Audio/`の決まったファイル名へ生成します。仕組み、idとファイル名の対応、どこで鳴らしているかは[docs/sound.ja.md](docs/sound.ja.md)に、音がどうあるべきかは[docs/tone-and-manner.ja.md](docs/tone-and-manner.ja.md)にあります。

### アート・フォントと著作権

出荷するアセットは、**自作のオリジナル**か、**ライセンスで使用が認められていて下の表に記録した第三者の素材**のどちらかでなければなりません。アートを生成・追加するときはこのルールに従ってください。後から見直す手順ではなく、作業そのものの一部として扱います。

**アートを生成するとき（`ArtSource/`）**

- 図形・パス・グラデーション・模様といった基本要素を組み合わせて、このゲームのために描きます。既存の作品をトレース・コピー・再現・「そっくりに」模倣しないこと。対象には、他のゲームのカード・UI・アイコン・画面レイアウト、アニメ/マンガ/映画のキャラクター、ロゴやマスコット、特定の作家のイラストが含まれます。
- ジャンルとしての作風は問題ありません（サイバーパンク、シンセウェーブ、ドット絵、ポップアート、網点など）。大まかな作風は保護されませんが、**特定の、それと分かる表現**は保護されます。「それ、Y の X だよね」と言われそうなデザインなら変えてください。
- 実在のブランド・商標・ロゴ（ほのめかすだけのものも含む）、実在の人物の肖像、既存の IP と重なる名前は使いません。
- 便利な記号であっても、法律で保護された標章は避けます。特に**白地に赤十字・赤新月は絶対に使わない**こと（赤十字標章はジュネーブ条約と各国の法律で保護されています）。回復のアイコンは、ゲームの配色の十字記号（今はダークの地にライム）を使います。国旗・紋章・五輪マークも避けてください。
- **音もアートと同じ決まりに従います**: `SoundSource/`で発振器とノイズから合成し、メロディもこのゲームのために作ります。既存の曲・ジングル・聞き覚えのあるサウンドロゴをサンプリング・再現・引用しません。外部の音を使うなら、許諾の緩いライセンスで、下の表に行を足します。
- 画像の中に文字を描き込みません。文字はローカライズできるよう `CommonText` に置きます。そうしておくと、ライセンスを追いにくいフォントが画像に紛れ込むこともありません。

**第三者の素材（フォント・画像・音・生成AIの画像）**

- **商用利用できる寛容なライセンスのものだけ**を使います（SIL OFL、CC0、CC-BY（クレジット表記付き）、MIT/Apache など）。「個人利用のみ無料」のもの、出どころの分からないもの、他のゲームやサイトから抜き出したものは使いません。
- **ライセンスファイルをアセットの隣に置き**（例: `Assets/Fonts/DotGothic16/OFL.txt`）、入手元の URL と一緒に下の表へ1行足してください。OFL のフォントはライセンス文と一緒に配布する必要があります。また、予約フォント名（Reserved Font Name）のあるフォントは、その名前のまま改変・改名してはいけません。
- **外部の画像生成AIの出力**は、そのツールの規約が商用利用を認めている場合にだけ使います。ツール名と日付を表に記録してください。作家名や既存の IP をプロンプトに入れないこと。
- **生成したアートについての注意。** コードやAIで生成した絵は、国によっては著作権の保護が弱いか、まったく及びません。*使う*分には安全ですが、プロジェクトとして権利を主張できる資産になるのは、人の手で描いた絵です。

| アセット | ライセンス | 入手元 | 備考 |
| --- | --- | --- | --- |
| `Assets/Fonts/DotGothic16/` | SIL OFL 1.1 | https://github.com/google/fonts/tree/main/ofl/dotgothic16 | 隣に `OFL.txt` |
| `Assets/TextMesh Pro/Fonts/LiberationSans.ttf` | SIL OFL 1.1 | TextMesh Pro に同梱 | 隣に `LiberationSans - OFL.txt` |
| `Assets/TextMesh Pro/Sprites/EmojiOne.png` | EmojiOne 無償ライセンス（CC-BY 4.0、**クレジット表記が必要**） | TextMesh Pro に同梱 | TMP の既定スプライトアセットに設定されているのでビルドに含まれる。今は未使用。リリース前に、ゲームのクレジットに表記を入れるか、`TMP Settings.asset` の `m_defaultSpriteAsset` を外すこと |
| `Assets/Audio/` 以下すべて | オリジナル（`SoundSource/` で生成） | このリポジトリ | 仮の効果音と BGM |
| `Assets/Art/` 以下すべて | オリジナル（`ArtSource/` で生成） | このリポジトリ | キャラクターはコードで描いたドット絵（仮素材の品質） |

**商用リリースの前には、それでも法務の確認を推奨します。** このルールはリスクを下げるためのもので、法的な助言ではありません。

### uGUI の組み方

Canvas / RectTransform / Button / レイアウトの作業では、`unity:ui-ugui` スキルを読み込んで従ってください。ただし**下のプロジェクト独自の決まりを優先します**。両者が食い違うときは、この節が勝ちます。UI が*どう見え、どう振る舞うべきか*は [docs/tone-and-manner.ja.md](docs/tone-and-manner.ja.md) にあり、この節はそれを*どう組むか*です。

**スキルから取り入れた決まり:**

- **触る前に中身を見ます。** シーンやプレハブに手を入れる前に、階層を書き出します(`run_script`)。
- **その場で直し、直すために壊して作り直しません。** 壊したオブジェクトへの参照はすべてnullになります(上のコンポーネント差し替えの注意も参照)。
- **1回に1つ変えて、確かめます。** 見た目の変更の確認とは、Playモードでの`capture_game_view`のことです。
- **設定する順番。** アンカーのプリセット → **アンカーに合わせたピボット**(右上に置くものはピボットも右上) → 位置、の順に設定します。できあがった大きさが0でないこと、親の中に収まっていることを確かめます。
- **Raycast Target。** 入力を受けないGraphic(文字、飾り、重ね板など)はすべて`raycastTarget = false`にし、`true`は入力を受けるものだけにします。画面いっぱいでクリックを受けるものは、意図して置き、名前を付けます(`BattleCancelAreaView`)。
- **入力を受けるための前提。** EventSystemは各シーンにちょうど1つ、入力を受けるCanvasにはGraphicRaycasterを付けます。トーストのCanvasには意図して付けていません。
- **コントラスト。** 新しい文字は背景と十分なコントラストを取ります。意図した低コントラスト(押せない状態、飾り)は構いません。
- **正確な値。** ユーザーが正確な値(px、色、間隔)を指定したら、その通りに適用します。
- **Layout GroupとContentSizeFitterの落とし穴。**
  - 子のサイズを制御するLayout Groupと同じオブジェクトにContentSizeFitterを付けると、ぶつかります。
  - 親のLayout Groupがその軸を制御している子にContentSizeFitterを付けても、上書きされて効きません。

**プロジェクト独自の決まり:**

- **Layout Groupは、動かず中身に合わせて大きさが決まるUIだけに使います**(状態効果の一覧、トーストのパネル)。動いたりアニメーションしたりするもの(手札、実行リスト、対象の候補一覧、カードの動き)は、**スクリプトで位置を決めます**([docs/battle-scene.ja.md](docs/battle-scene.ja.md)参照)。スキルの一般論「なるべくLayout Groupを使う」は、これらには当てはまりません。
- **UIの型名は短く書きます。** スクリプトは`using UnityEngine.UI;`と短い型名で書きます。完全な名前(`UnityEngine.UI.Image`)にするのは、実際に名前がぶつかるときだけです。
- **2択の確認は`Assets/Prefabs/ConfirmDialog.prefab`を使います**(`Common/Views/UI/ConfirmDialogView`)。シーンごとに1つ生成して`WindowHandler<ConfirmShowData>`として共用し、`Show(new ConfirmShowData(...))`で尋ねます(「ウインドウはシーンに置かず、生成します」を参照)。背面のクリックはキャンセル扱いです。ただし尋ねる側が`cancelOnBackground: false`を渡すと(マップのチュートリアルの確認)、背面のクリックでは何も起きず、2つのボタンでしか答えられません。Viewは伝えるだけで、OKで何をするかはリクエストのコールバックが決めます。プレハブは、開閉するウインドウ(`Dialog`。`WindowTransition`付き)と電子の粒を、常に表示の親の下に兄弟として置いています。こうすると閉じた後も粒が残ります。最初の利用者はMapSceneのステージ開始です。
- **文字とボタン**は`CommonText` / `CommonButton`を通し(「UIのラッパーコンポーネント」参照)、素のTMP / `Button`は使いません。
- **主なボタンは`Assets/Prefabs/CyberButton.prefab`を使います**(波紋付きの斜めのボタン。`>>>`の飾り`Deco`は既定でオフ)。見た目を1箇所にまとめておけるよう、`PrefabUtility.InstantiatePrefab`で置いてください。BattleScene・TitleScene・MapSceneのボタンはこのプレハブより前に作ったもので、ただの複製です。
- **プレハブは`Assets/Prefabs/`に置きます。** 機能ごとのUIフォルダには分けません。
- **画面の大きさ。** 基準の解像度は**1920×1080(16:9)**で、**16:10(Steam Deckなど)と21:9(ウルトラワイド)でも表示が崩れないこと**が必須です。特定の画面比率に固定した値は、コードにもシーンにも絵にも書きません。
  - **CanvasScaler。** トーストを含むすべてのCanvasを、Scale With Screen Size・1920×1080・**Screen Match Mode = Expand**にしています。1920×1080の基準範囲が必ず全部見え、画面に余りがある向きにCanvasが広がります。21:9なら横(約2560×1080)、16:10なら縦(1920×1200)です。見切れることはありません。(スキルの推奨のMatch 0.5は使っていません。余白を足すのではなくUIを縮めるうえ、見切れも起こりうるためです。)
  - **役割で置き方を決めます。** 世界の一部であるもの(背景の地平線に立つユニット)は**画面の中央**を基準にし、中央に置いた背景の床の線と一緒に動くようにします。HUD(メモリー残量、コマンドデッキ、アーカイブ、実行ボタン、手札、上部のパネル)は、**属する辺や角**を基準にし、ピボットもその辺に合わせます。画面全体を覆うもの(何も無い所のクリックを受ける板、対象の候補一覧の親)は、画面いっぱいに伸ばします。
  - **セーフエリア(ノッチ・角の丸み・ジェスチャーバー)。** 辺や角に寄せるHUDは、`Common/Views/UI/SafeAreaFitter`を付けた画面いっぱいの入れ物の下に置きます。再生中にアンカーを`UnityEngine.Device.Screen.safeArea`に合わせます(Device Simulatorが差し替えるのは`Device`の方。保存されるアンカーは0〜1のままなので、ノッチが無ければ何も動かない)。入れ物の親は画面全体を覆っていること。世界に属するもの(ユニット、ルート、背景)と全画面の当たり判定は入れません。今のところ: BattleSceneの`Canvas/SafeArea`(山札、メモリー、アーカイブ、パージ、実行、命令メモリーとリスト、ログ、メニュー)と`Canvas/SafeAreaHand`(バッファの見出し、空のバッファの表示、手札。`CardDetail` / `LogFeed` / `TargetPicker`との描画順を保つため入れ物を分けた)、MapSceneの`Canvas/SafeArea`(ボタンすべて、階層表示、トークン)、ResultSceneの`Content`(直接付けた)、チュートリアルのスキップボタン(`Visual/SafeArea`)。タイトルとエンディングは中央の文字だけです。ウインドウは中央にあって余白が十分あります。トーストは上中央で、まだ合わせていません。画面は**横持ちだけ**です(Auto Rotationで、許可するのはLandscape Left / Rightだけ。`ScreenOrientationSettingsTests`)。端末を180度回すと、画面の大きさは変わらずにノッチが反対側へ移るので、大きさの変化を待つのではなく毎`Update`確かめています。
  - **背景**は**2560×1440**で描き、原寸のまま画面の中央に置きます。中央の1920×1080が基準の構図(安全領域)で、その外側にも世界が続いているので、横長・縦長の画面では引き伸ばされる代わりに、街や空や床がより広く見えます。画面の端に貼り付く必要があるもの(警告色のバー)は、背景の絵には入れず、その端を基準にした9-sliceのUIの部品にします。背景のパーティクルも、2560の幅いっぱいから出します。
  - **確かめ方。** **Tools > Game View**(`Editor/GameViewAspectMenu.cs`)でGameビューを16:9 / 16:10 / 21:9に切り替え、それぞれPlayモードで撮って確認します。`capture_game_view`はどの画像も1280×720に縮めるので、16:9以外は縦か横につぶれて写ります。比率ではなく配置を見て判断してください。
- **Canvasのつなぎ先。** CanvasはScreen Space - Cameraで、層ごとの`UICameraRig`のカメラにつなぎます(「描画」参照)。Screen Space - Overlayは使いません(リグの無いシーンでのトーストのフォールバックと、`SceneTransitionEffect`は例外。「新しいシーンの追加」参照)。

### 描画: カメラ・Canvas・SortingLayer

各シーンには`Assets/Prefabs/UICameraRig.prefab`のインスタンスを1つ置く。URPのカメラスタックとして繋いだカメラ3台と、シーンに属さないUI(トースト)から参照できるよう`UICameraRig.Current`として自分を公開する`UICameraRig`コンポーネントが入っている。新しいシーンではカメラを手で置かず、このPrefabを置くこと。

| カメラ | 種別 | Renderer | Cullingするレイヤー(GameObject Layer) | 描くもの |
| --- | --- | --- | --- | --- |
| `Main Camera` | Base、ポストエフェクト**あり** | `UniversalRenderer`(index 0) | `Default` + `UI_Back`(8) | ワールドと、ポストエフェクトを掛けたいもの |
| `UI Camera` | Overlay | `UniversalRenderer`(0) | `UI_Default`(9) | 通常のUI。ポスト処理後の絵の上に乗る |
| `Popup Camera` | Overlay | `UniversalRenderer_Popup`(1) | `UI_Popup`(10)、`UI_PopupTop`(11) | ポップアップ。`UI_Popup`には一番手前以外の開いているウインドウが入り、ブラーの**前**に描かれる。`UI_PopupTop`には一番手前のウインドウ・トースト・チュートリアルが入り、ブラーの**後**に描かれる |

Baseカメラのスタックに積んだ順がそのまま描画順(`UI Camera` → `Popup Camera`)。ポストエフェクトはBaseカメラだけに掛かるので、「ポスト前」「ポスト後」は1台のカメラのパスの違いではなくカメラの違いで表している。

SortingLayerは`UI_Back`、`UI_Back_Effect`、`UI_Default`、`UI_Default_Effect`、`UI_Popup`、`UI_Popup_Effect`。各段と、その段より上・次の段より下に出したいエフェクト用の層がセットになっている。**SortingLayerは同じカメラ内での描画順、GameObject Layerはそもそもどのカメラに映るかを決める。** 両方必要なので、Canvasだけでなく**その配下すべて**にその段のレイヤーを付ける(スクリプトから組むときは`SetLayerRecursively`)。

Canvasはシーンごとに、必要な段の分だけ持つ。`BackCanvas`(Screen Space - Camera、`Main Camera`、レイヤー`UI_Back`。今は仮の色を置いただけ)と`Canvas`(`UI Camera`、レイヤー`UI_Default`)。モーダルなウインドウを出すシーンには、さらに`PopupCanvas`(`Popup Camera`、ソーティングレイヤーとGameObjectのレイヤーは`UI_Popup`)を置きます。今のところMapSceneとBattleSceneです。ウインドウのプレハブのルートはすべて**`PopupLayerMember`を付けた入れ子のCanvas**で、ウインドウの開閉に合わせて`UI_Popup`と`UI_PopupTop`の間を移ります(下の「くっきりしているのは一番手前のウインドウだけ」参照)。Canvasはその段のリグのカメラに繋ぐこと。`Camera.main`に繋いだり、Screen Space - Overlayのままにしたりしない(Overlayはカメラを経由しない＝ブラーも掛からない)。

**カメラはすべてUniversal Rendererを使っていて、2D Rendererではない。これは意図的で、外せない。** `Renderer2D`ではOverlayカメラがBaseカメラのカラーターゲットに合成されず、下のカメラが描いた絵を上のカメラが塗り潰してしまう。RendererFeatureがカラーターゲットへ書いた内容も一緒に消える。プロジェクトでは`Light2D`を使っていないので、スタックごと`UniversalRenderer`へ移した。`Assets/Settings/Renderer2D.asset`はファイルとしては残っているが、`UniversalRP.asset`には登録していない。

**ポップアップ背後のブラー**は`PopupBlurRendererFeature`(`Common/Rendering/`)。`UniversalRenderer_Popup`に付いていて、設定(注入ポイント・往復回数・サンプル間隔)もそこで行う。Popupカメラが自分の中身を描く**前に**、既にカラーターゲットにある絵をぼかす。シェーダーは`Assets/Shaders/DualKawaseBlur.shader`(Dual Kawase: 解像度を半分にしながらぼかし、戻しながらまたぼかす)。`PopupBlurController.IsActive`の間だけパスを積むので、ポップアップ側は`PopupBlurController.Push()`で参加し、閉じるときに**必ず**`Pop()`する。`Pop`は`OnDisable`と対にしてください。そうすれば、シーン遷移や`SetActive(false)`でカウンタが戻らなくなることがありません。**ウインドウは`Common/Views/UI/WindowTransition`を通して使います。** `OnEnable`でPush、`OnDisable`でPopし、開く間は`PopupBlurController.Strength`(0..1)を上げ、閉じる間は下げます。RendererFeatureの最後の拡大パス(`DualKawaseUpBlend`)が、その強さで元の絵の上にぼかしを重ねます。最後のポップアップが閉じると強さは1に戻ります。ゲーム内のウインドウはすべてこれを使います(マップのデッキ編集・マーケット・購入結果・ユニット強化・敵詳細のウインドウ、確認ウインドウなど)。`MessageToast`も以前はぼかしていましたが、意図的にやめました。通知は操作を止めてはいけませんが、読ませるためのウインドウは止めてかまいません。使っていない間は何のコストもかかりません。

**くっきりしているのは一番手前のウインドウだけで、ブラーは1回。** `Popup Camera`は3段階で描きます。後の2つは`PopupBlurRendererFeature`の中です:

1. Rendererの通常の描画が`UI_Popup`を描く(一番手前以外の開いているウインドウ)
2. ブラー(`_injectionPoint`。`AfterRenderingTransparents`)が、ここまでの絵(下のシーンと、それらのウインドウ)をまとめてぼかす
3. Featureの`DrawLayerPass`が`_frontLayers`(`UI_PopupTop`)を描く(一番手前のウインドウ・トースト・チュートリアル)

何枚重なってもブラーは1回で、一番手前以外はすべてその下になります。2と3の順はRendererに並べたFeatureの順ではなく、コードで決めています(`_injectionPoint`と`+ 1`)。

**どれが一番手前か**は`Common/Views/UI/PopupStack`が持ちます。`WindowTransition.PlayOpen`でそのウインドウを一番手前にし、`OnDisable`(閉じ終わった・非表示にした・シーンを抜けた)で抜きます。変わるたびに重なり(素のC#の`Common/Models/PopupStackModel`)を奥から順にたどり、各ウインドウの`PopupLayerMember.Apply(isFront, order)`を呼びます(`WindowTransition`の`GetComponentInParent`で見つける):

- 自分以下ごと、一番手前なら`UI_PopupTop`、それ以外は`UI_Popup`へ移す
- 入れ子のCanvasをOverride Sorting、ソーティングレイヤー`UI_Popup`、Order `10 + 重なりの順番`にする

ウインドウはすべて同じカメラにあるので、一番手前が先にクリックを受け取るのはこのOrderのおかげです。奥のウインドウ同士の並びもこれで決まります。別のウインドウのプレハブの中にあるウインドウ(マーケットの購入結果)も、奥から順にたどるので、手前のものが最後に自分以下を上書きして正しくなります。描き分けに効くのはCanvasとパーティクルのレイヤーだけなので、一覧が後から生成する行などは何もしなくてよいです。トーストとチュートリアルの重ね表示は、カメラに繋ぐときに自分を`UI_PopupTop`(Order 100)へ移します。

**画面全体のぼかしを上げ下げするのは最初と最後のウインドウだけで、その間はすぐ後ろのウインドウだけを動かす。** `WindowTransition`は、ほかにぼかしているウインドウが無いとき(`OpenCount`)だけ開くときに`PopupBlurController.Strength`を上げ、最後の1つのときだけ閉じるときに下げます(そうしないと、重ねて開くたびにシーンのぼけが一瞬抜ける)。ウインドウの上にウインドウを開く・閉じるときは、代わりに**範囲**を動かします。すぐ後ろのウインドウ(`PopupStack.Below`)の枠(`WindowTransition.WindowRect`をカメラのビューポートの位置に直したもの)に`PopupBlurController.SetRegion`で別の強さを持たせ、ブラーの最後のパス(`DualKawaseUpBlend`。`_BlurRegion` / `_BlurRegionStrength`)がその矩形の中だけ画面全体の強さの代わりに使います。開くときは0→1なので、シーンはぼけたまま後ろのウインドウだけがぼけていきます。閉じるときは1→0なので、閉じ終わって後ろのウインドウが一番手前の段へ戻る瞬間にはもうくっきりしていて、見た目が飛びません。範囲は動きが終わったときに消します(`EndRegionFade`。`OnDisable`でも)。範囲は同時に1つだけです。後ろのウインドウの半透明の板から透けて見えるシーンも同じように動きますが、そのウインドウ自身の暗幕の下なので、ほとんど見えません。

**ウインドウを新しく作るとき**は、プレハブのルートに`Canvas`(Override Sorting有効)・`GraphicRaycaster`・`PopupLayerMember`を付け、`WindowInitializerBase`の派生、`WindowId`、`WindowCatalog`の1行、初期化データのクラスを足します(「ウインドウはシーンに置かず、生成します」)。スクリプトで付けるときは、Override Sortingを`SerializedObject`(`m_OverrideSorting`)で入れること。プレハブを単体で開いた状態ではルートに親のCanvasが無く、プロパティで入れても黙って無視されます。

`PopupLayerSetupTests`がアセット側を縛ります。どれも崩れても目立つエラーにならないためです:

- Popupの RendererのOpaque/Transparent Layer Maskから`UI_PopupTop`が外れていること(入っているとぼかしの前にも描かれる)
- それがFeatureの`_frontLayers`であること
- `Popup Camera`が両方のレイヤーを映すこと
- ウインドウのプレハブがすべて`PopupLayerMember`と`GraphicRaycaster`を持つこと

2026-10-01に、それまでの2つの形を置き換えました:

- **4台目のカメラ。** シーンごとにカメラが1台増え、Rendererのアセットが2つになり、上のウインドウをプレハブから出す必要がありました。
- **ウインドウごとに固定したぼかしの段。** 重なるとブラーが2回になり、どのウインドウを上の段に置くかを手で決める必要がありました。バトルのオプションの上の確認ダイアログは、後ろがぼけないまま残っていました。

これを作るときに時間のほとんどを食った落とし穴が2つ。どちらもエラーとしては出てこない。

- `Blitter.BlitTexture`に渡すマテリアルのシェーダーは**`Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl`**をincludeすること。Blitterが実際に描く「頂点IDから作る全画面三角形」の頂点シェーダー`Vert`、`Varyings`、`_BlitTexture`、`_BlitScaleBias`はこのファイルにある。どちらのレンダーパイプラインパッケージにも`ShaderLibrary/Blit.hlsl`は無く、includeパスを間違えるとパスがコンパイル対象から外れて**何も描かれない**のに`Shader.isSupported`は`true`のまま。本当のことを言うのは`ShaderUtil.ShaderHasError` / `ShaderUtil.GetShaderMessages`なので、自前のblitが効かないときはまずそこを見る。(Core側の`Blit.hlsl`は`_BlitTexture_TexelSize`を既に宣言しているので、このFeatureは自前の`_BlurTexelSize`を渡している。)
- そのシェーダーに残る`Both vertex and fragment programs must be present in a shader snippet`という警告は`HLSLINCLUDE`ブロックに対するもので、パス自体の問題ではない。無視してよい。

ブラーは`RenderGraphUtils.AddBlitPass`ではなく`AddRasterRenderPass` + `builder.AllowPassCulling(false)`で積んでいる。カラーターゲットへの書き込みが「誰も読まない」と判断されて消えることが無く、パスごとに変わるテクセルサイズを記録時ではなく実行時にマテリアルへ渡せるため。

### 日本語表示とTMPのフォントアトラス

ゲームの文字はすべてドットフォント`Assets/Fonts/DotGothic16/DotGothic16-Regular SDF`で描きます。これはTMPの**既定フォント**(`TMP Settings.asset`)にもしてあるので、新しく足した文字も最初からこのフォントになります。1ドット幅の線を持つ字があり、縮小するとその線が消えるので、マテリアルの**Face Dilate を 0.2** にしてあります([docs/art.ja.md](docs/art.ja.md)参照)。

**フォールバックフォントはありません。** 以前は後ろにNotoSansJPを置いていましたが、全シーンを装飾し終えたところで外しました。そのため、DotGothic16に無い字は**エラーも出ずに空白**になります。`FontCoverageTests`(EditMode)が全ローカライズテーブルの全文字をDotGothic16の元の`.ttf`で引き、無い字があれば失敗します。コードに書いた文字列(トーストの文言)やシーンに直接打った文字は対象外なので、そこに新しい字を足すときは手で確かめてください。本当に字が無かったときは、文字を別のフォントに替えるのではなく、DotGothic16にフォールバックのフォントアセットを足してください(そのライセンスも下の表に加えます)。

このアセットは**動的**で、文字が最初に表示されたときにアトラスへ焼き込まれます。

アトラスは1024x1024・サンプリング64ptで、**1枚あたり漢字150文字程度**しか入りません。したがって`Multi Atlas Textures`を**必ず有効**にしておくこと。無効だと1枚目が埋まった時点で以降の文字が**コンソールに何も出ずに空白**になります。古いテキストは出るのに新しい日本語だけ空白になったら、まずこれを疑ってください。確認はフォントアセットのmulti-atlasフラグ、次に`TMP_FontAsset.TryAddCharacters`(入らなかった文字を返します)です。

### ローカライズ(`Assets/Localization/`、`com.unity.localization`)

現在ローカライズされているのは: カードの名前と説明(`CardView.SetCardData`がどちらも`LocalizationHelper`経由で読みます)、状態効果の見出しと説明(`UnitPresenter`が解決)、そして`UiTextKey`に並ぶUIラベル(山札/捨て札の見出しと実行ボタン)です。まだ素の文字列のまま: `MessageToast`のテキスト(`BattleCommandQueueUseCase`が出す失敗メッセージを含む)と、シーンに直接書かれたラベル。

**言語切り替え時のローカライズ文字列の再反映はPresenterの担当で、その配線は`PresenterBase`が済ませています** — `LocalizationSettings.SelectedLocaleChanged`を購読し、各Presenterの`OnLocaleChanged()`を呼びます(上の「レイヤー構造」を参照)。Viewはdumbなまま: `CardView`は購読も独自のMonoBehaviourライフサイクルも持たず、単に`SetCardData`を呼び直されるだけです。代わりにViewへ`LocalizedString`/`StringChanged`のバインドを持たせる方法は採らないでください — ローカライズのライフサイクルがViewに入り込む上、`BattleCardHandPilePresenter`はカードViewをプールして使い回すため、フィールド単位のバインドだと再利用のたびにバインドし直しが必要になります。ゲーム内の言語切り替えUIはありません。切り替えはエディタ側の仕組みだけです(下記)。

**実行中の言語切り替え**にはLocalizationパッケージ同梱のエディタUIを使います — パッケージがGameビューに追加するロケール切り替えプルダウンと、`Window > Asset Management > Localization Scene Controls`です。Playモード中にこれを操作することが、上記の`OnLocaleChanged()`経路を動かす手段になります。同等のものを独自にメインツールバー拡張として実装したことがありますが、重複するため削除しました。パッケージ側のUIで賄えない理由が無い限り、作り直さないでください。


**テーブル名を書いたりキーを組み立てたりしてよいのは`Common/Localization/LocalizationHelper.cs`だけです** — ViewやPresenterから直接`LocalizationSettings.StringDatabase`を呼ばないでください。テーブル1つにつき`public const string <Name>Table`と`public static string Get<Name>(...)`を1組ずつ追加し、どちらもprivateな`GetLocalizedString(tableName, key)`を経由させます。

**テーブルのキーは、対象データのIDをそのまま使います**(カード名なら`CardData.Id`。カードアセットの`_id`にはアセット名を入れてあるので、キーは`Attack`・`Block`・`StrongAttack`)。prefixは付けません: テーブル名で既にスコープが分かれており、prefixを足しても同期させる対象が1つ増えるだけだからです。この規約が外に漏れないよう、**ヘルパーのメソッドはキーやIDではなくデータそのものを受け取ります** — `GetCardName(string cardId)`ではなく`GetCardName(CardData cardData)`。こうすると呼び出し側が誤ったフィールドを渡せず、「どのフィールドがキーか」を決める場所が1箇所に収まります。

**テキストの項目1つにつきテーブル1つ。キーはその行を特定するものを使います:**

| テーブル | キー | ヘルパー |
| --- | --- | --- |
| `CardName` | `CardData.Id` | `GetCardName(CardData)` |
| `CardDescription` | `CardData.Id` | `GetCardDescription(CardData)` |
| `CardPackName` | `CardPackData.Id` | `GetCardPackName(CardPackData)` |
| `CardPackDescription` | `CardPackData.Id` | `GetCardPackDescription(CardPackData)` — ショップの購入パネルに出すチップの説明。改行は文の区切りで文中に書く |
| `StatusEffectName` | `IStatusEffect.Id` | `GetStatusEffectName(IStatusEffect)` |
| `StatusEffectDescription` | `IStatusEffect.Id` | `GetStatusEffectDescription(IStatusEffect)` |
| `StatusEffectLogDetail` | `IStatusEffect.Id` | `GetStatusEffectLogDetail(IStatusEffect)` — 数値の入る書式(`{0}`は値、`{1}`は時間制の効果の大きさ)。バトルログが使う |
| `EnemyName` | `EnemyData.Id` | `GetEnemyName(EnemyData)` — 戦闘で敵の立ち絵の上に出る |
| `EnemyDescription` | `EnemyData.Id` | `GetEnemyDescription(EnemyData)` — 敵の特徴。マップの敵詳細ウインドウで次のレイヤーの相手として出る。数字を書かない地の文で、HP・行動回数・コマンド・無効化はその横にデータから出す |
| `UIText` | `UiTextKey`のenum名 | `GetUiText(UiTextKey)` |

`CardName`と`CardDescription`がキーの出どころを共有しているのは意図的です — `StringTable`は1キー1ロケールにつき1文字列しか持てないため、同じカードに関する2種類のテキストは**必然的に**2つのテーブルに分かれます。テーブルを列、キーを行と考えてください。つまり: **既存の対象に対する新しいテキスト項目はテーブルを増やす。既存の種類のラベルが増えるだけなら増やさない。**

`UIText`が汎用UIラベル**全部**の置き場(現状は`DeckPile`・`DiscardPile`・`Execute`・`StageStart`・`DeckEdit`・`OwnedCards`・`DeckCards`・`Cancel`・`Save`・`Back`・`CardShop`・`Buy`・`Tokens`、ステージ結果のラベル`VictoryRewardTitle`・`VictoryRewardMessage`・`DefeatRewardTitle`・`DefeatRewardMessage`・`RewardClaim`・`BackupAccept`、バトルログの`BattleLog`と`Log*`の書式と、4つのモード名`Campaign`・`Endless`・`Collection`・`Options`)で、画面やウィジェットごとに分けないのはこのためです。それらは同じ種類のテキストで、行が増えるだけだからです。このプロジェクトではテーブル1つのコストが小さくないので(フォルダ1つ、`2 + ロケール数`個のアセット、さらにロケールごとのAddressablesグループ)、1種類のラベルを画面ごとに割るとそのコストが無意味に倍増します。

UIラベルには渡すべきデータオブジェクトが無いので、型安全性は`UiTextKey`(`Common/Localization/UiTextKey.cs`)が担います: enum名がそのままテーブルのキーで、呼び出し側が生文字列を書くことはありません。

テーブルとキーの出どころのズレは、EditModeの2つのテストで押さえています(どちら向きのズレも、画面に "No translation found" が出るまで見えないため): `UiTextKeyTests`が`UiTextKey` enumを、`CardLocalizationTests`が全`CardData`アセットの`Id`を**両方の**カード用テーブルに対して、`StatusEffectLocalizationTests`が`TypeCache`で見つけた全`IStatusEffect`実装の`public const string EffectId`を読み(引数なしコンストラクタが無いのでインスタンス化できません)、両方の状態効果用テーブルに対して — いずれも全ロケール分チェックします。この定数を置き忘れた場合も落ちるので、Idを型から辿れる状態に保てます。

Viewはヘルパーを呼びません: 文字列はPresenterが解決して渡します(`CardPileCountView.SetLabel(string)`には`BattleCardDeckPilePresenter`/`BattleCardDiscardPilePresenter`が`Sync`の中から渡す。`Sync`は`OnLocaleChanged`が呼び直すものでもあるので、ラベルは言語切り替えに自動で追従します)。`CardView`だけが例外で、カード用の2つのテキストを自分でヘルパーから引きます — `CardData`だけを渡される作りだからです。

これは`LocalizationHelperPlayModeTests`が通しで検証しています(カードIDに対するja/enのエントリをassertしているので、キー規約の崩れやエントリ欠落はここで落ちます)。PlayModeテストなのは、`LocalizationSettings`がAddressables経由の初期化を必要とし、Edit modeでは解決できないためです。キーをリネームする際は`SharedTableData`側も合わせて変更する必要があるので、ヘルパーとテーブルのキーは常にセットで変更するものと考えてください。

アセット配置 — 新しいローカライズアセットもこの形の中に収めてください:

```
Assets/Localization/
├── Localization Settings.asset      (LocalizationSettings。ProjectSettings/EditorBuildSettings.assetのcom.unity.localization.settingsに登録されている)
├── Locales/                         (ロケールごとに1アセット: 「English (en)」「Japanese (ja)」)
└── Tables/
    └── <テーブルコレクション名>/      (テーブルコレクションごとに1フォルダ)
        ├── <Name>.asset             (StringTableCollection)
        ├── <Name> Shared Data.asset (SharedTableData — 全ロケール共通のキー定義)
        └── <Name>_<code>.asset      (StringTable。ロケールごとに1つ)
```

- **テーブルコレクションごとに1フォルダ** — 1コレクションは常に`2 + ロケール数`個のファイルになるため、どちらかが増えた時点でフラットな`Tables/`は見通せなくなります。Localization Tablesウィンドウで新しいコレクションを作る際は、フォルダ選択ダイアログで`Assets/Localization/Tables/<名前>/`を新規作成して指定してください。以降そのコレクションのアセットは全てそこに書き込まれ、後からロケールを追加しても`<Name>_<code>.asset`が同じフォルダに自動で追加されます。ロケール単位で切る構成(`Tables/ja/...`)にはしないでください — コレクションとShared Dataは特定のロケールに属さず、パッケージ側の挙動とも噛み合いません。
- **これらのアセットの移動は必ず`AssetDatabase.MoveAsset`経由で**(`run_script`を使う)行ってください。素のファイル移動は不可です — 上の「ターミナルから起動中のUnity Editorを操作する」を参照。Addressablesのエントリと`EditorBuildSettings`はこれらのアセットをGUIDで参照しているため、正しい手順で移動すれば参照は全て維持されます。またフォルダ構成自体はビルド結果に影響しません。

`Assets/AddressableAssetsData/`は**必須かつパッケージ生成物で、コミット対象として残す必要があります**。`com.unity.addressables`は`com.unity.localization`の間接依存(`manifest.json`には記載されていません)で、Localizationはロケールと文字列テーブルのロードをAddressables経由でしか行いません。`Localization-*`グループはパッケージが自動で作成・維持するもので、文字列テーブルは上のフォルダ構成とは無関係にロケール単位(`Localization-String-Tables-<ロケール>`)でグループ化されます。`Assets/Scripts/`配下にAddressablesを直接使っているコードはありません。(空の)`Default Local Group`も消さないでください: `AddressableAssetSettings.m_DefaultGroup`がこれを指しており、新たにAddressable化するアセットが出た時に必要になります。

### テスト(`Assets/Scripts/Tests/`)

`Assets/Scripts/`配下のスクリプトは`Live2DCardRogue.Runtime`アセンブリ(`Assets/Scripts/Live2DCardRogue.Runtime.asmdef`)にコンパイルされます。`Assets/Scripts/Editor/`は独自の`Live2DCardRogue.Editor.asmdef`(Editor専用、Runtimeを参照)を持ち、カスタムDrawerの`CardEffectBaseDrawer`が引き続きEditor用としてのみビルドされるようにしています。テストスイート導入以前はどちらも存在しませんでした — カスタムのテスト用`.asmdef`は他の`.asmdef`で定義されたアセンブリしか参照できず、暗黙の`Assembly-CSharp`は参照できないため、既存に`.asmdef`が無いプロジェクトにテストを追加するには、まずメインコード側にasmdefを与える必要があります。これは一度だけの構造変更で、テストを増やすたびに繰り返すものではありません。

- `Assets/Scripts/Tests/EditMode/Live2DCardRogue.Tests.EditMode.asmdef` — 新しいテストのデフォルトの置き場所。Unity Playモードに依存しない、素のModel/UseCase/状態効果/カード効果ロジックを対象とします(`CardPileModelTests`、`CardCommandModelTests`、`UnitStatusModelTests`、`BlockStatusEffectTests`、`CardEffectTests`、`BattleCommandUseCaseTests`)。加えて`Unity.Localization`・`Unity.Localization.Editor`も参照しています。`UiTextKeyTests`と`CardLocalizationTests`が`LocalizationEditorSettings`経由でローカライズテーブルのアセットを見て、全`UiTextKey`と全`CardData.Id`が全ロケールで空でない訳文を持つことを確認しているためです。`FontCoverageTests`も同じテーブルを読み、その全文字がDotGothic16にあることを確かめています(「日本語表示とTMPのフォントアトラス」参照)。これらはEditor専用APIで、かつPlayMode側(`includePlatforms: []`)はEditor専用アセンブリをそもそも参照できないため、これらに触れるテストはここ以外に置き場がありません。`Unity.TextMeshPro`・`UnityEngine.UI`を参照しているのも同種の理由で、`CommonUiComponentTests`が「混入を防ぎたい素の型」として`TextMeshProUGUI`/`Button`を名指しするためです(上の「UIのラッパーコンポーネント」を参照)。プレハブはロードしてコンポーネントを見ますが、シーンだけは`AssetDatabase.GetDependencies`で見ています — EditModeテストからシーンを開くと、Editorで開いている作業中のシーンを巻き込んでしまうためです。
- `Assets/Scripts/Tests/PlayMode/Live2DCardRogue.Tests.PlayMode.asmdef` — 本当にPlayモードを必要とするケース専用。`LocalizationHelperPlayModeTests`と`PresenterBasePlayModeTests`は`LocalizationSettings`がAddressables経由の初期化を必要とするためここにあります(上の「ローカライズ」を参照。この都合でこのasmdefは`Unity.Localization`と`Unity.ResourceManager`も参照しています)。`PresenterBasePlayModeTests`は`PresenterBase`のスパイ用サブクラスを使い、言語変更が`OnLocaleChanged()`まで届くことと、`Dispose()`で確実に購読解除されることを検証します — Viewもシーンも登場しない、基底クラス自身の契約のテストです。`LocalizationSettings.SelectedLocale`を変更するテストは、グローバル状態なのでteardownで必ず元に戻すこと、かつ現在と*異なる*ロケールに切り替えること(同じロケールを代入してもイベントは飛びません)に注意してください。残りは`BattleCommandUseCasePlayModeTests`です: `TrySelectCard`が断る2つの経路(スタミナ不足・実行リストが一杯)が`MessageUseCase.ShowMessage` → `MessageToast`を呼び、それが`Object.DontDestroyOnLoad`を呼ぶため、Playモード外では`InvalidOperationException`になります。新しいテストがこのような「`MonoBehaviour`シングルトンがPlayモード限定の何かをする」ことだけが障害になっている場合は、依存関係をモックで回避するのではなく、ここに置くのが正解です。
- `CardData`や`CardEffectBase`のサブクラスは、データを`[SerializeField]`(Inspector専用設計、公開setterなし)でしか公開していません。テストでは`ScriptableObject.CreateInstance<CardData>()` / `new BasicAttackCardEffect()`で生成し、privateフィールドをリフレクションで設定しています(`BattleCommandUseCaseTests.cs`とPlayModeテストに重複して置かれている`SetPrivateField`ヘルパーを参照)。本番コードにテスト専用のコンストラクタ/setterを追加するのではなく、この方法を使ってください。
- テストの実行は起動中のEditorに対する`run_tests`コマンド経由で行ってください(上の「テストを実行する」を参照)。単体の`unity test` CLIコマンドではありません。
- 新しいModel/UseCase/状態効果ロジックを追加・変更するたびに、対応するテストをここに追加・更新してください — このスイートは一度作ったら終わりのスナップショットではなく、コードベースと一緒に育っていくことを想定しています。

### このドキュメント群を書く/更新するとき

このファイル、`@`でimportされるシーン別ドキュメント、または`.ja.md`対訳版を書く・改訂する際は、以前のドキュメントの記述(や自分自身の記憶)を鵜呑みにせず、実際に今のコードを読んで書くこと — このコードベースは頻繁にリファクタされるため、書いたのと同じセッション内でもドキュメントの記述が古くなり得ます。矛盾に気づいたら — ドキュメントとコードで言っていることが違う、どこかで命名規則が破られている(例: privateフィールドが`_camelCase`になっていない)、2つのドキュメント同士が食い違っている、記述されている挙動がその場のコメントと一致しない、など — それを黙ってドキュメント側でごまかしたり、黙ってコード側を「ドキュメントに合わせて修正」したりせず、明示的に指摘すること。見つけたことを報告し、どちら側(ドキュメントかコードか)が誤っているかはユーザーに判断してもらってください。ドキュメントの方が正しいと決めつけないこと。

# チュートリアル

キャンペーンの最初のチュートリアルと、その仕組み(`Assets/Scripts/Common/Tutorial/`)の詳細ドキュメントです。[docs/tutorial.md](tutorial.md)(英語版、`CLAUDE.md`から`@`importされる方)の対訳。流れる画面については docs/map-scene.ja.md と docs/battle-scene.ja.md を参照。

## 流れ

1. **タイトル → マップ。** *新しい*キャンペーンを始めると(エピソード1。セーブが無い、または破棄したあと)、`CampaignTutorialRepository.RequestPrompt()`で印を付けてマップを開きます。マップは`IsPromptPending`を見てファーストビルドを待ち、場面の切り替えの幕が上がり、エピソードの始まりの会話を読み終えてから(@docs/map-scene.mdの「会話パート」)、共用の`ConfirmDialog`で「チュートリアルを見ますか？」と聞きます(`MapScene/Presenters/TutorialPromptPresenter`。見る / 見ない。背面のクリックでは何も起きない(`cancelOnBackground: false`)。うっかり触れて答えてしまわないように)。**見る**ならその場でマップのチュートリアルを始め(`Stage = Map`)、**見ない**ならその場でファーストビルドに進みます(チップを開けてデッキを組み、保存して、開けたチップを見せる)。答える前に終了してもセーブは無いので、次に新しく始めたときにまた聞きます。**一度チュートリアルを見たら**(ここで「見る」を選ぶか、マップの見直しボタン。`GameSettingsRepository.HasSeenTutorial`、`settings.json`に保存)、次からの新しいキャンペーンでは聞かずにファーストビルドへ進みます(`MapTutorialUseCase.AskOrSkip`)。マップからの見直しはいつでもできます。タイトルのオプションでユーザーデータを削除すると戻ります。
2. **マップ。** `Stage == Map`のとき、`MapSceneInitializer`はファーストビルドも保存もせず、`CampaignTutorialRepository.MapSequence`を流します。部屋、深さ、敵詳細、接続開始、コマンドビルド、トークン、マーケット、ユニット強化、見直しのボタンを、吹き出し1つずつで説明します。最後の手で練習の戦闘へ移ります(`Stage = Battle`)。`CampaignTutorialData`の固定のデッキと敵、`shuffleDeck: false`、`playTutorial: true`。戦闘の手順は渡さず、戦闘が`CampaignTutorialRepository.BattleSequence`を取ります。
3. **戦闘。** デッキは並べた順のまま引くので、手順はどの手札が来るかを知っています。1ターン目はアタック・ブロック・再生・強撃・回復を引きます。アタック・ブロック・再生を積むと強撃がメモリー不足になり、実行します。訓練用トロイは攻撃してウイルスを仕込みます。2ターン目は継続効果(ウイルスと再生)、アーカイブ、ドローを説明し、回復とマイニングを積んで実行し、パージの山を説明します。そのあとは自由に戦います。
4. **マップに戻る。** `Stage == Battle`のときは、`MapSceneTransitionData`の`BattleResult`を**使いません**。報酬も進行もバックアップブーストも無いので、練習で負けても構いません。チュートリアルを終え(`Stage = None`)、いつもどおりファーストビルドからキャンペーンが始まります。

- **スキップ。** 右上のスキップは、まずシーンで共用の`ConfirmDialog`で「チュートリアルをスキップしますか？」(スキップする / 戻る)と確かめます。尋ねている間はチュートリアルの表示を引っ込め(`TutorialOverlayView.SetSuspended`)、確認ウインドウを押せるようにします。戻るなら同じ手に戻ります。マップでスキップすると、チュートリアル無しでマップを開き直します(ファーストビルドが始まる)。練習の戦闘でスキップすると(確認に「練習の戦闘を終えて、マップに戻ります」を添える)、戦闘をその場で終えてマップに戻り、マップはいつも通り戦闘の結果を使わずにチュートリアルを終えます。戻り先の無い単体の再生では、戦闘はそのまま続きます。シーンは尋ね方を`TutorialUseCase.Play(sequence, onEnded, confirmSkip)`で渡し、`confirmSkip`が無ければスキップはすぐ終わります。
- **途中で終えた**ら何も残りません。リポジトリは保存せず、ファーストビルドの前はキャンペーンのセーブもまだ無いので、次に新しいキャンペーンを始めるときにもう一度聞かれ、最初からになります。
- **見直し。** マップの**チュートリアル**ボタン(左上、`MapTutorialButtonPresenter`)は、まず「チュートリアルを最初から見ますか？」(見る / 戻る)と確かめてから、`Stage = Map`にしてマップを開き直し、チュートリアルを最初から流します。チュートリアルは何も保存せず、戦闘の結果も使わないので、進行中のキャンペーンには影響しません。
- **共用の確認ウインドウで1回だけ尋ねる。** 単発の質問は、シーンの`WindowHandler<ConfirmShowData>`(`Common/WindowSystem/`)で尋ねます。`Show(new ConfirmShowData(message, detail, ok, cancel, onOk, onCancel))`はそのデータのコールバックにだけ答えます(同じ確認ウインドウを接続開始・ユニット強化・コマンドビルドも使うため)。ほかの質問が開いている間は何も尋ねないので、スキップは先に`IsOpen`を見て、開いていれば「スキップしなかった」として扱います。

## 変更に耐える作り

| 変えたいもの | 直す場所 |
| --- | --- |
| 文言 | `TutorialText`テーブル。キーは各手の`_textKey`(`LocalizationHelper.GetTutorialText`) |
| 手の追加・削除・並べ替え | 手順のアセット`Assets/ScriptableObjects/Tutorial/MapTutorial.asset` / `BattleTutorial.asset`(`TutorialSequence`: `TutorialStep[]`) |
| UIの移動・作り直し | 何もしない。新しいオブジェクトに同じ`_anchorId`の`TutorialAnchor`が付いていればよい(画面は毎フレーム目印の今の位置を引く) |
| 練習の戦闘 | `CampaignTutorial.asset`(`CampaignTutorialData`: 引く順に並べたデッキ、敵、プレイヤーのHP・開始時のHP・メモリー)。戦闘の手順はデッキの並びに合わせて書いてあるので、いっしょに見直す。手順そのものは`CampaignTutorialData`の`MapSequence` / `BattleSequence` |
| 「〜まで待つ」の種類を増やす | `TutorialEventNames`に名前を足し、起きる場所で投げる(シーンの橋渡しのクラスで。ゲームの本体には入れない) |

**1つの手**(`TutorialStep`)は次のものです。

- **目印のID**: シーンの`TutorialAnchor`か、シーンが実行時に解決する`接頭辞:名前`の動的な目印。
- **文言のキー**: 空なら「待つだけ」の手で、何も出さず操作も自由です(コマンドの実行中など)。
- **吹き出しの向き**: `Auto`は下 → 上 → 右 → 左の順に空いている側を選びます。
- **進み方**:
  - `Click`: どこかをクリックで次へ。吹き出しそのものを押しても進みます(吹き出しは画面全体の受け口より手前にあるので、自分の`ClickArea`を持つ)。下の画面は触れません。
  - `Event`と出来事の名前: 強調した部分だけ触れて、その出来事が投げられたら次へ。

**部品(`Common/Tutorial/`)。**

- **手順は始めてから終えるまで`CampaignTutorialRepository`が持ちます。** `StartFromMap(mapSequence, battleSequence)`(「見る」または見直しボタン。`CampaignTutorialData`を参照するマップが生きているうち)で両方を持ち、`HideFlags.DontUnloadUnusedAsset`を付けます。`Finish`(模擬戦から戻ったとき、スキップ、タイトルで新しく始めたとき)で印を外して手放します。マップは`MapSequence`、戦闘は`BattleSequence`を流します。印が戦闘の手順を残します: 参照しているのはマップのシーンだけで、ビルドでは C# が指しているだけの ScriptableObject が戦闘への切り替えで解放されることがあり、Android の起動後最初の模擬戦でチュートリアルが流れませんでした(2026-10-17)。エディタでは再現しません。
- **`TutorialUseCase.Play(sequence, onEnded)`**が入り口です。`Resources/TutorialOverlay.prefab`を出して`IDisposable`を返し、シーンは`OnDestroy`でそれを片付けます。
- **`TutorialSession`**(`PresenterBase`なので言語が変わると文言も変わる)が、`TutorialRunner`(ただの状態。EditModeでテスト)と`TutorialOverlayView`をつなぎます。
- **`TutorialOverlayView`**:
  - 目印の上に穴を空けて4枚の板で周りを暗くし、黄色い枠を脈打たせます。
  - 吹き出しを空いている側に置き、画面の中に寄せます。
  - 手数、「クリックで次へ」、スキップを出します。
  - ポップアップのカメラの`UI_Popup`の100番に描き、場面の切り替えの幕が上がるまで待ちます。
- **`TutorialAnchorRegistry`**が目印を引きます。シーンが動的な接頭辞を登録します: `HandCard:<コマンドのID>`(そのIDの手札のうち左端、`BattleCardHandPilePresenter.FindCardTransform`)と`MapRoom:Current`(`MapPresenter.CurrentCellTransform`)。
- **`TutorialEvents`**が出来事の名前を運びます: `CardQueued`、`ExecuteRequested`、`PlayerTurnStarted`(フェーズが`Player`になった、つまり引き終わった)、`EnemyTurnStarted`。戦闘では`BattleTutorialEventBridge`が、モデルとDirectorを外から見張って投げます。ゲームのコードはチュートリアルを知りません。

**練習の敵**は`Enemies/TutorialTrojan.asset`です。HP24、毎ターン バックドア → ウイルスの順に使います(`FixedOrderEnemyCardPicker`: 並べたカードを、最大行動回数まで順に使う)。

**テスト。**

- `TutorialRunnerTests`は手の進み方を確かめます。
- `TutorialDataTests`はアセットを確かめます:
  - 文言のキーがすべての言語にある;
  - 出来事で進む手は知っている出来事を待つ;
  - 待つだけの手は出来事で進む;
  - シーンの目印のIDがそのシーンのファイルにある(動的な目印は知っている接頭辞);
  - `HandCard:`のコマンドがチュートリアルのデッキにある。
- フォントと敵の翻訳のテストが、新しい文言と訓練用トロイも見ます。

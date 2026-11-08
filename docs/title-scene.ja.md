# TitleScene(日本語版)

`Live2DCardRogue.TitleScene.*`(`Assets/Scripts/Scenes/TitleScene/`)の詳細ドキュメントです。[docs/title-scene.md](title-scene.md)(英語版、`CLAUDE.md`から`@`importされる方)の対訳。まずルートの[CLAUDE.ja.md](../CLAUDE.ja.md)で一般的なレイヤー/配線の規約を、モードの遷移先になる予定のシーンについては[docs/battle-scene.ja.md](battle-scene.ja.md)を読んでください。

`Assets/Scenes/TitleScene.unity`が入口のシーンです(Build Settingsのindex 0)。ゲームタイトルと、4つのモードボタン(**ストーリー(コードではキャンペーン) / チャレンジ / コレクション / オプション**)が並びます。

**TitleSceneにはウインドウを1つも置いていません。** `TitleSceneInitializer`が開始時に`WindowUseCase.Create`で`PopupCanvas`の下にすべて生成し(確認、再開、エピソード選択、チャレンジ選択、コレクションのメニューと2つのコレクション、オプション)、Handlerの`Show`で開きます(ルートの「ウインドウはシーンに置かず、生成します」を参照)。以下で「タイトルの`PopupCanvas`上」と書いているのは、そこに生成されるという意味です。

**4つとも配線済み**です。**コレクション**は共通のメニューウインドウ(`Prefabs/MenuWindow.prefab`を`MenuWindowInitData`で生成。`MenuWindowView` / `MenuWindowPresenter`。ルートの「ボタンを並べるだけのウインドウ」を参照)を、見出しコレクション(`UiTextKey.Collection`)で開きます。ボタンはコマンド / エネミーの2つ(`UiTextKey.CollectionCommands` / `CollectionEnemies`)と閉じるで、背面のクリックでも閉じます。`TitleSceneInitializer`は`MenuWindowInitData`を組み立てるだけで、**コマンド**と**エネミー**はその上にそれぞれのコレクションを開きます(下記。それぞれのHandlerの`Show`)。2026-10-06までは専用のウインドウ(`CollectionMenuWindow.prefab`、`CollectionMenuPresenter`、`CollectionCategory`)でした。実績のボタンも予定していましたが、2026-10-04に削除しました。

### エネミーのコレクション(`Prefabs/EnemyCollectionWindow.prefab`)

マップの敵詳細のウインドウを使い回しています。`EnemyDetailWindowView`(タイトルから使えるよう、2026-10-04に`EnemyDetailSlotView`・`EnemyCommandRowView`・表示用のデータ`EnemyDetailInfo`と一緒に`MapScene`から`Common/Views/EnemyDetail/`へ移した)を、`EnemyCollectionPresenter`が動かします。`// ENEMY DATABASE`、エネミーコレクション(`UiTextKey.EnemyCollection`)。

- **左:** `Assets/ScriptableObjects/EnemyCollection.asset`(`EnemyCollectionData`。トロイの木馬・スパイウェア・ルートキット・ボットネット。チュートリアルの敵は入れない)の敵を、上から動く立ち絵つきで並べます。マップの列と違ってスクロールします。`SlotScroll`(`ScrollRect`。表示枠は`RectMask2D`で、枠の左のカーソルと光る縁が切れない幅を取る。中身は`ContentSizeFitter`付きの`Slots`)と、細いシアンのスクロールバー(`AutoHide`)です。`EnemyDetailWindowView._slotScroll`は省略でき、設定すると`SetEnemies`のたびに先頭へ戻します。
- **開いたとき**は一番上の敵を選びます。クリックで選び直すと(黄色の枠とカーソル)、右にその敵を出します。
- **戦ったことの無い敵は伏せます。** 枠では動く立ち絵を黒い影にし(`IUnitVisual.SetColor(black)`)、シアンの**？**を重ね(`EnemyDetailSlotView._unknownMark`)、名前は？？？です。選ぶと右には？？？、`HP ???`、「まだ遭遇していない敵です。戦うとデータが記録されます。」(`UiTextKey.EnemyUnknown`)だけを出し、コマンドは出しません(`EnemyDetailInfoBuilder.CreateUnknown`、`EnemyDetailInfo.IsDiscovered`)。
- **戦ったかどうか:** ストーリーでもエンドレスでも、`MapStageTransitionUseCase.StartStage`がそのステージの敵を記録します(`CampaignSaveService.RecordDiscoveredEnemies` → `CampaignClearData.discoveredEnemies`。版5)。開くときは`TitleScene/Models/EnemyDiscoveryResolver`が、記録を始める前のセーブから分かる分を足します。クリアしたエピソードの全ステージ、中断しているストーリーの今のステージより前、中断しているチャレンジの突破した階層(シードから`TitleSceneInitializer._endlessBalance`で組み直す)です。
- **右:** マップと同じ詳細(HP・行動回数・最初から付いている効果・特徴・使うコマンドとその説明)。組み立ては`Common/Presenters/EnemyDetailInfoBuilder`で、マップの`EnemyDetailPresenter`も使います。コレクションでは敵そのものの値(`EnemyStrength.None`)を見せます。
- `EnemyCollectionTests`が、遊ぶ中で出会う敵(どの`StageData`、どの`EndlessBalance`の候補)がすべて並ぶことを確かめます。

### コマンドのコレクション(`Prefabs/CommandCollectionWindow.prefab`)

`CommandCollectionWindowView` / `CommandCollectionPresenter` / `CommandCollectionModel`(`Window/CommandCollection/`)。タイトルの`PopupCanvas`上にあり、コレクションのウインドウの上に重ねて開きます(閉じるとそこへ戻る。閉じるか背面のクリックで閉じる)。プレハブはコマンドビルドの複製を削ったもので、`// COMMAND DATABASE`、コマンドコレクションです。

- **絞り込みのタブ**は一覧の上に、すべて、続いてチップごと(`UiTextKey.CollectionFilterAll`、各チップの`CardPackName`)。Viewが`Filter`をタブの数だけ複製して(`_filterSpacing`の間隔で)並べ、選んでいるタブの文字は黄色です。タブを押すと一覧を並べ直し、先頭から見せます。
- **一覧**(`// COMMAND INDEX`、`CollectionCardView.prefab`の`CommonScrollList`。部品は共通の`Common/Views/CardList/CardListItemView`)は、ストーリーかエンドレスで**一度でも所持したことがあるコマンドだけ**を見せ、それ以外は伏せたカードに**？**を出します(`_lockedRoot`)。見出しの右に「獲得 26 / 32」(`CollectionProgress`。すべてで数える)。右には細いシアンのスクロールバー(コマンドビルドと同じ。全部収まるときは隠れる)があり、スクロールできることを示します。
- コマンドに**カーソルを合わせる**と、右の拡大表示(`// COMMAND DATA`、共通の`CardListDetailView`。戦闘のアーカイブ・パージの一覧も同じ部品を使う)に出し、一覧のそのカードにシアンの光る縁を付けます。？のカードにも合わせられ、そのときの拡大表示は伏せた？のカードのままです(`_lockedCard`)。名前と説明は見せません。
- **並べるもの**は`Assets/ScriptableObjects/CommandCollection.asset`(`CommandCollectionData`)です。`_packs`がタブ(マーケットのチップの順)、`_otherCards`はどのチップからも出ないコマンド(ステージ報酬の自己修復)です。すべては両方を合わせ、レアリティの低い順、同じなら最初に出てきた順に並べます。敵専用のコマンドは並べません。`CommandCollectionTests`が、手に入るコマンド(どのチップ・ステージ報酬・開始時の持ち物)がすべて並び、敵専用のものが無いことを確かめます。
- **所持したことがあるか**は`CampaignSaveService.LoadDiscoveredCommandIds`で、開くたびに読み直します。`clear.sav`の`discoveredCommands`に、中断しているストーリーとチャレンジの所持コマンドを足したものです(記録を始める前からのランでも数えるため)。

キャンペーンは新しく始めるか、`CampaignSaveService`が保存されたキャンペーン(`campaign.sav`)を見つけたときは、先にタイトルの`PopupCanvas`上の**`CampaignResumeDialog`**を開きます。中身は`// SUSPENDED SESSION`、文言、`LAYER 04 / 10   TKN 820   HP 160   MEM 12`という1行の要約、キャンセル / 破棄 / 再開で、背面のクリックはキャンセル扱いです。`CampaignResumePresenter`が選択を伝え、`TitleSceneInitializer`が実行します:
- **再開**は`CampaignSaveSnapshot.Restore`でリポジトリを戻し(コマンドは`_cardCatalog`から引き直す)、マップを開きます。
- **破棄**は、まず共通の`ConfirmDialog`(これもタイトルの`PopupCanvas`上)で「本当に破棄しますか？ / 中断したストーリーは元に戻せません」と確かめます(キャンセル / 破棄する)。OKのときだけ進捗ファイルを消してエピソード選択を開き、キャンセルなら3択のウインドウに戻ります。
- **エピソード選択**(`Prefabs/EpisodeSelectWindow.prefab`、`EpisodeSelectWindowView` / `EpisodeSelectPresenter`。`// SELECT EPISODE`、エピソード選択)は、中断したデータが無いとき、または破棄した直後に開きます。画面ではモードは**ストーリー / Story**、エピソードは**エピソード1〜3 / Episode 1〜3**(`UiTextKey.EpisodeName`)です。各エピソードのステージは`CampaignBalance`のそのエピソードの`CampaignData`です(`TitleSceneInitializer._balance`、マップと同じアセット)。選ぶと`CampaignProgressRepository.Reset(episodeIndex)`で新しいキャンペーンを始めてマップへ進み、マップはそのエピソードを遊びます。**解放:** エピソード1はいつでも選べ、エピソードN(2以上)は、エピソードN-1のクリア記録が`clear.sav`にあれば選べます(`TitleScene/Models/EpisodeUnlockRule`、`CampaignClearData.HasCleared`。ウインドウを開くたびに読み直す)。まだ選べないエピソードは押せない見た目で、押すと`CommonButton.OnDisabledClicked`で条件をトーストに出します(「エピソード1をクリアすると選べるようになります」)。選べるが`CampaignData`が空欄か空のエピソード(`TitleRecordUseCase.HasEpisodeContent` → `CampaignBalance.HasEpisode`)は押せて、「エピソード2は準備中です」と出します。今は3つともステージがあるので、アセットの設定漏れへの備えです。各エピソードの状態は`EpisodeState`(`Locked` / `NotReady` / `Playable`)です。遊べるエピソードを押すと共用の`WindowHandler<ConfirmShowData>`で「エピソード1を始めますか？」(始める / 戻る)と確かめてから、下の新しいキャンペーンを始めます。閉じるか背面のクリックでウインドウを閉じます。
- **クリアとランク.** クリアしたエピソードは、ボタンの右にライムの`CLEAR`のタグ(チャレンジの選択と同じもの)と、一番良いランクをリザルトのランクの色で出します(`EpisodeSelectWindowView._clearMarks` / `_rankTexts`。`CampaignClearData.BestRank`がそのエピソードの`CampaignClearRecord.rank`のうち一番良いものを返し、ウインドウを開くたびに`TitleRecordUseCase.GetBestRank`が読む)。
- **新しいキャンペーン**は`RunModeRepository.StartCampaign()`を呼び、キャンペーンの4つのリポジトリをすべて空にします(以前は残っていたデッキも含む)。マップが初期値を配り直します。ルートの「セーブ」参照。そのあと`CampaignTutorialRepository.RequestPrompt()`で印を付けてマップを開き、マップが着いた直後に「チュートリアルを見ますか？」と聞きます(docs/tutorial.ja.md参照)。

## チャレンジ(`Window/ChallengeSelect/Presenters/ChallengeSelectPresenter.cs`、`Prefabs/ChallengeSelectWindow.prefab`)

**チャレンジ**ボタン(`GameMode.Challenge`、`UiTextKey.Challenge`。2026-10-04まではエンドレス)は、物語の無いマップを、10階層ごとに強くなるランダムな敵で遊ぶモードです([docs/map-scene.ja.md](map-scene.ja.md)の「チャレンジ」参照)。

- **まず再開を聞く.** 中断データはチャレンジ全体で1つです(`endless.sav`)。あれば、チャレンジを押したときにストーリーと同じ`CampaignResumeDialog`を開きます(`ChallengeResumeMessage`。要約は`ノーマル   LAYER 12 / 30   TKN …`、エンドレスは`LAYER 37`だけ。破棄の確認は`ChallengeDiscardConfirmDetail`)。再開はそのチャレンジを戻し、破棄は消してから選択を開きます。
- **選択**(`ChallengeSelectWindowView`、`// SELECT CHALLENGE`、チャレンジ選択)は`ChallengeCatalog`の順に、ノーマル 30階層・ハード 60階層・マスター 100階層・エンドレスを並べます(`UiTextKey.ChallengeButtonLabel`「{0}  {1}階層」。終わりの無いものは名前だけ。名前は`LocalizationHelper.GetChallengeName`)。文字は斜めの面に収まるよう縮みます。押すと共用の`WindowHandler<ConfirmShowData>`で「ノーマルを始めますか？」(始める / 戻る)と確かめ、`StartNewChallenge`がキャンペーンのリポジトリを空にして`RunModeRepository.StartChallenge(type, layerCount, seed)`を呼び、マップを開きます(チュートリアルは聞かず、ファーストビルドから)。閉じるか背面のクリックで閉じます。
- **クリアマーク.** 階層数のあるチャレンジを一度でも踏破していれば(`ChallengeRecord.cleared`。開くたびに`TitleRecordUseCase.IsChallengeCleared`が読む)、ボタンの右にライムの`CLEAR`のタグを出します(`ChallengeSelectWindowView._clearMarks`。各ボタンの下の`ClearMark`)。エンドレスは終わりが無いので出しません。エンドレスには代わりに最高到達階層を、シアンの`LAYER 37`のタグで出します(`_bestLayerMarks` / `_bestLayerTexts`。エンドレスのボタンの`BestLayerMark`。`TitleRecordUseCase.GetChallengeBestLayer`)。まだ遊んでいなければ出しません。
- **階層数**は`Assets/ScriptableObjects/ChallengeCatalog.asset`(`TitleSceneInitializer._challengeCatalog`)で変えます。`ChallengeCatalogTests`が今の値(30 / 60 / 100 / 終わり無し)を確かめているので、アセットと一緒に直してください。

## オプション(`Common/Presenters/GameOptionsPresenter.cs`、`Prefabs/GameOptionsWindow.prefab`)

**オプション**のボタンで、タイトルの`PopupCanvas`上の`GameOptionsWindow`(`// SYSTEM CONFIG`、オプション)を開きます。**バトルでも同じウインドウ・モデル・Presenterを使います**(@docs/battle-scene.mdの「オプション」)。違いは、タイトルだけが`onDeleteUserData`を渡し、ユーザーデータの行を出すことです。編集は`GameOptionsModel`(`Common/Models/`)に溜め、**反映**で`GameSettingsRepository`に書いて`settings.json`に保存し、画面モードと言語を当てます(`GameSettingsApplier`)。**デフォルトに戻す**は言語以外を既定値に戻し、**キャンセル**(または背面)は、変更があれば共用の`WindowHandler<ConfirmShowData>`で「変更を破棄しますか？」と確かめます。

| 行 | 操作 | 補足 |
| --- | --- | --- |
| BGM / 効果音 | スライダー、5%刻み | 編集中もその音量で聞こえる(`AudioUseCase.PreviewVolumes`。閉じたら戻す)。効果音のスライダーを動かすとクリック音を鳴らす |
| 戦闘スピード | スライダー、0.25倍刻み | |
| 言語 | `OptionSelectorView` ◀ 名前 ▶ | Localization Settingsの言語を、その言語自身の名前で(日本語、English。`LocalizationHelper.GetAvailableLocales`) |
| 画面モード | `OptionSelectorView` | フルスクリーン / ウインドウ。エディタでは変わらない |
| ユーザーデータ | 削除する(マゼンタ) | 「ユーザーデータを削除しますか？」(詳細: 中断したストーリーとクリア記録が消えます。元に戻せません)と確かめてから`TitleSceneInitializer.DeleteUserData`を呼び、トーストを出す。オプションのウインドウは開いたまま。**タイトルだけ**: `onDeleteUserData`が無ければ行を隠し、その分ウインドウを縮める(`GameOptionsWindowView.SetUserDataVisible`) |

`OptionSelectorView`(`Common/Views/UI/`)は使い回せる「◀ 値 ▶」の行です。`ui_cursor`の矢印(左は反転)の`CommonButton`2つと値の文字で、-1 / +1を伝えるだけです。

## 構成

タイトルのウインドウはこのフォルダにはありません。再開のダイアログ、エピソードとチャレンジの選択、2つのコレクションは`Assets/Scripts/Window/`(`CampaignResume`、`EpisodeSelect` — `EpisodeState`と`EpisodeUnlockRule`も —、`ChallengeSelect`、`CommandCollection`、`EnemyCollection`。`Live2DCardRogue.Window.<Name>.*`)にあり、コレクションのメニューは共通の`Window/Menu/`です。組み立てるのは`TitleSceneInitializer`です。ここに残っているのは下のモード選択と`Models/EnemyDiscoveryResolver`です。

このシーンは意図的に薄く、**ModelもUseCaseもありません**。状態を持つものが何も無く、押した結果はenumの分岐だからです。それでもInitializer/ViewContainer/Presenterの形は保っているので、BattleSceneと同じ見た目で読めます。

- `Enum/GameMode.cs` — 4つのモード。enumの並び順は画面の並び順ではなく、どのボタンがどのモードかはボタンごとにInspectorで決めます。
- `Views/TitleModeButtonView.cs` — モードボタン1つ分。自分の`Mode`を知り、`CommonButton`を公開し、ラベルを受け取ります。押された結果何が起きるかは知りません。
- `Views/TitleSceneViewContainer.cs` — ボタンを画面の並び順で保持します。
- `Presenters/TitleModePresenter.cs` — 各ボタンのラベルを`UiTextKey`から流し込み(言語切り替え時も引き直し)、押された`GameMode`を注入された`Action<GameMode>`で報告します。モードを増やす作業は「enumに値を足す・`UiTextKey`を足す・`ToUiTextKey`に1行足す・シーンにボタンを置く」で、対応表を書き忘れた場合は黙って空表示になるのではなく例外になります。
- `Initializer/TitleSceneInitializer.cs` — コンポジションルートであり、**モードごとに何をするかを決める唯一の場所**(モードボタンごとの`switch`と、ウインドウを開くこと)。
- `UseCase/TitleRunUseCase.cs` — ラン(ストーリー / チャレンジ)の開始・再開・破棄と、ユーザーデータの削除。
- `UseCase/TitleRecordUseCase.cs` — 画面のために`clear.sav`を読む。エピソードの状態、最高ランク、チャレンジの記録、見つけたコマンドと敵。

Presenterが登録した`UnityAction`を保持しているのは、`OnDispose`でその同じデリゲートを解除するためです(ラムダは同じインスタンスを持っていないと解除できません)。

## 次のシーンへのデータの渡し方

まだ何もパラメータを渡していません(モード選択がどこにも遷移しないため)が、仕組み自体は入っていて、BattleScene側は既に受け取る作りです。運び役は`Common/`の2つで、どちらも特定のシーンに依存しません:

- `Common/SceneTransition/SceneTransitionDataStore.cs` — 静的な受け渡し口。渡す側が`Set`し、受け取る側のInitializerの基底(`SceneInitializerBase<TData>`)が`TryConsume`します。**受け取れた時点で中身を捨てる**ので、後の遷移にデータが残りません。型が合わなかったときは捨てずに残します。
- `Common/SceneTransition/SceneTransitionUseCase.cs` — `LoadScene(data)`。`SceneTransitionData`を置いてから、その`Destination`をロードします。シーン名は隣の`SceneName.cs`にあり、データの型がそれぞれ自分の行き先を選ぶので、呼び出し側はシーン名を書きません。

渡すデータの型も`Common/`の`Common/SceneTransition/TransitionData/`にあります(`TitleSceneTransitionData`、`MapSceneTransitionData`、`BattleSceneTransitionData`、`EndingSceneTransitionData`、`ResultSceneTransitionData`、`ChallengeResultTransitionData`。それぞれ行き先を自分で持つ。ルートの「新しいシーンを追加する」を参照)。**どのシーンも別のシーンを参照しない**ためで、呼ぶ側も呼ばれる側も`Common/`だけに依存し、BattleSceneは誰が起動したかを知りません。データを構成する要素(`CardData`・`EnemyData`・`UnitStatusModel`)も`Common/`にあります。(2026-10-06までは、入っていく側のシーンの`Data/`に置き、呼び出し側がそのシーンの名前空間を参照していました。)

`BattleSceneTransitionData.PlayerStatus`が最大HPの数値ではなく`UnitStatusModel`そのものなのは、前の戦闘のダメージや状態効果を次の戦闘へ持ち越せるようにするためです。

## BattleSceneを単体で再生する

`BattleSceneInitializer.Initialize(data)`は、渡されたデータがあればそれを使い、`data`がnullなら(`CreateMockParameter`で)自前の`_playerDeckMock` / `_enemyDataMocks` / `_playerMaxHpMock` / `_playerMaxStaminaMock`にフォールバックし、そうしたことをログに出します。モードが未実装の今、**このフォールバックが戦闘を動かす唯一の経路**なので、壊さないようにしてください。「BattleScene.unityを開いてPlayを押す」という、これまでずっと使ってきた戦闘の確認手順を支えているのもこれです。

## 見た目

[docs/tone-and-manner.ja.md](tone-and-manner.ja.md) に沿って装飾しています。

- **背景。** `bg_title.png`は、地平線を y 430 に置き、ロゴの真後ろに大きな太陽を、上下に警告色のバーを配しています。背景のパーティクルと`CyberVolumeProfile`のポストエフェクトは、BattleSceneと共通です。
- **ロゴ。** DotGothic16に`PopShadow`マテリアル(マゼンタのUnderlay)を付けています。`TitleLogoGlitchView`が数秒おきに約0.2秒だけグリッチ(横ぶれ、シアン/マゼンタの残像、明滅)をかけます。
- **モードのボタン。** BattleSceneの実行ボタンと同じ、斜めの半透明ボタンに`ButtonRippleEffect`を付けています(子にEcho/RippleMask、ラベルは斜体)。
  - 斜めの画像は縦に伸ばせないので、高さは108pxです。
  - 傾きに沿って、1段ごとに34pxずつ左へずらして斜めに積んでいます。

このためにシーンのコードは変えていません。すべてシーンの中身と、View 1つの追加です。

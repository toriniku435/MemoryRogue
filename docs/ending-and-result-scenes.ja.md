# EndingScene と ResultScene

`Live2DCardRogue.EndingScene.*` と `Live2DCardRogue.ResultScene.*`(`Assets/Scripts/Scenes/EndingScene/`、`Assets/Scripts/Scenes/ResultScene/`)の詳細ドキュメントです。ルートの`CLAUDE.md`から読み込まれます。キャンペーンからここへ来る流れは[docs/map-scene.ja.md](map-scene.ja.md)を参照してください。

キャンペーンの最終ステージを踏破すると、**マップ(報酬ウインドウ)→ EndingScene → ResultScene → TitleScene**と進みます。どちらのシーンもTitleSceneと同じ最小の形で、Initializer + ViewContainer + Presenterだけで、ModelとUseCaseはありません。シーンはTitleSceneを複製して作り(UICameraRig、`bg_title`のBackCanvas、EventSystem、ポストプロセスのVolume、タイトル曲の`SceneBgm`)、Canvasの中身だけを差し替えています。

## 成績はエンディングより前に決まる

決まりは`Common/Models/CampaignResults/`にあります。リザルトで見せる前に、エンディングがランクで分岐できるようにするためです。

- **`CampaignResultCalculator.Calculate(remainingTokens, backupBoostCount)`**が`CampaignResult`を返します。中身は入力・加点・減点・`Score`・`Rank`・`Route`です。
- **スコア** = 残りトークン × `PointsPerToken` − バックアップブースト回数 × `PenaltyPerBackupBoost`。0未満にはなりません。減点は1回の支援で得られるトークンより必ず大きくしてあり、わざと負けてトークンを稼ぐと必ずスコアが下がります(`CampaignRankReachabilityTests`が各バランスのアセットのステージの報酬で確かめる)。
- **ランク**(`CampaignRank`。E〜SSSで、`>=`で比べられる並び)はランクの閾値から、**ルート**は2つの分かれ目から決まります。これらの数値はすべて**`CampaignBalance`**(ルートの「ゲームバランス」参照)にあり、計算には素のC#の`CampaignScoreRule`(`balance.CreateScoreRule()`)として渡ります。今の閾値は SSS 1950、SS 1600、S 1200、A 900、B 600、C 350、D 150、E 0 です。取れる最大は初期トークン500とステージ1〜9の勝利報酬1470を合わせた1970です(2026-10-04から最終ステージには報酬が無く、そのときにSSSを2000から下げた)。ステージ4〜10は強化やチップの購入がほぼ欠かせないので、SSSは使う量を合計20トークン前後に抑えたときだけ届きます。初期トークンや報酬を変えてSSSに届かなくなると`CampaignRankReachabilityTests`が落ちます。
- **ルート**(`EndingRoute`): 既定ではS以上は`Best`、B〜Aは`Normal`、E〜Cは`Bad`です(バランスのアセットの`_bestRouteFrom` / `_normalRouteFrom`)。

計算するのは`MapStageTransitionUseCase.CompleteCampaign`で、`CampaignTokenRepository.Tokens`と`CampaignProgressRepository.BackupBoostCount`を使います。バックアップブーストの回数は、負けるたびに`ApplyBattleResult`が1増やし、キャンペーン開始時の`Reset()`で0に戻ります。結果は`EndingSceneTransitionData`で渡り、EndingSceneは同じものを`ResultSceneTransitionData`で渡します。どちらのシーンも計算はしません。確かめるときは、マップのデバッグパネル(0707)の**Map: Clear All (score)**で、入力したスコアのままエンディングへ直行できます。単体で再生したときは、Initializerの`_remainingTokensMock` / `_backupBoostCountMock`で動きます。

## EndingScene(仮)

大きな`Ending`と`// TAP TO CONTINUE`、画面全体の`ClickArea`(`TapArea`)で、押すとResultSceneへ進みます。ルートは今はログに出すだけです。本物のエンディングができたら、Presenter(かルートごとのView)で`CampaignResult.Route`を見て出し分けます。上流を変える必要はありません。

## ResultScene

演出はすべて`Views/ResultView.cs`が受け持ち、Presenterはラベル(`Result`・`ResultRemainingTokens`・`ResultBackupBoosts`・`Score`・`Rank`・`BackToTitle`)を流し込み、`Play(result)`を呼び、**タイトルへ**が押されたことを伝えるだけです(`SceneTransitionUseCase.LoadScene(SceneName.Title)`。キャンペーンの状態は次にキャンペーンを選んだときに初期化されます)。

1. 内訳の2行がすべり込みます: 残りトークン`+N`(ライム)、バックアップブースト`-N`(マゼンタ)。
2. スコアが0から数え上がります(`TokenCounterView`、1.4秒)。スコアの行は独立した時間で出すので、数え上げが一瞬で終わるスコア0でも表示されます。
3. ランクに応じて長くなる間のあと、ランクが**デコードされて現れます**(音は`RankReveal`)。横一線から開き、文字がスクランブルしながら左から確定し、シアン/マゼンタの残像と走査線が横にずれます。確定したら控えめな閃光で落ち着きます。
4. タイトルへボタン(`CyberButton`)が出ます。

**豪華さはランクの段階で変わります**(`ResultView.TierOf`):

| 段階 | ランク | 間・デコード | グリッチ | 確定したとき |
| --- | --- | --- | --- | --- |
| 0 | E, D | 短い | 小さい | `Confirm`の音だけ |
| 1 | C, B | | | + 輪1本、かすかな閃光 |
| 2 | A, S | | | + 輪2本、回る放射光、`Reward`のファンファーレ、時々のグリッチ |
| 3 | SS, SSS | 一番長い | 一番大きい | + 輪3本、強い放射光、文字と放射光が虹色 |

ランクの色は`_rankColors`です(E/Dはグレー、C/Bはシアン、Aはマゼンタ、S/SSは金、SSSは白から虹色)。どこをクリックしても(`SkipArea`)デコードまで飛ばせますが、デコード自体は飛ばしません。最初は巨大な文字が落ちてきて画面が揺れる出し方でしたが、ユーザーの要望でデコードに置き換えました([docs/tone-and-manner.ja.md](tone-and-manner.ja.md)の「試してやめたこと」参照)。

### チャレンジのリザルト

チャレンジはResultSceneで終わり、`ChallengeResultTransitionData`(チャレンジ、到達階層、踏破したか、最高到達階層、新記録か)で来ます。負けたときは戦闘からそのまま(マップの`ReturnToMapOrEndChallenge`が作る)、ノーマル / ハード / マスターで最後の階層を踏破したときはマップから(`CompleteCampaign`)です。**記録は遷移の前に送る側がつけます。** `MapSceneInitializer.FinishChallenge`(負けたときは戦闘のシーンで呼ばれるので`static`)が、`RunModeRepository`からチャレンジを、`CampaignProgressRepository`から階層を読み(踏破したなら最後の階層)、`CampaignSaveService.FinishChallengeRun`で記録し(チャレンジごとに`CampaignClearData.challengeRecords`。版6。最高到達階層と踏破したことがあるか。古い`endlessBestLayer`はエンドレスの記録として読む。そのランの`endless.sav`は消す)、データを作ります — ストーリーの成績をマップがエンディングの前に確定させるのと同じです。`ResultSceneInitializer`はそれを`ChallengeResultPresenter`に渡すだけです。(2026-10-06までは、ResultSceneが自分で記録し、遷移データではない`ChallengeResult`を作っていました。)演出は同じ`ResultView`の`PlayEndless`です:

- 題は「ノーマル リザルト」(`UiTextKey.ChallengeResult`。`{0}`はチャレンジの名前)。
- 内訳の2行は**突破した階層**(`ChallengeClearedLayers`)と**最高記録**(`ChallengeBestLayer`、`LAYER 42`。更新したら黄色の「新記録！」を添える)。
- スコアの行と数え上げはありません。
- ランクの代わりに**到達した階層**の数字が、数字でスクランブルしながらデコードされます。見出しは「到達階層」(`ChallengeReachedLayer`)、踏破したときは「踏破」(`ChallengeCleared`)と最後の階層です。段階は10階層ごと(`EndlessTierOf`: 1〜10、11〜20、21〜30、31以上)で、ランクのD / B / S / SSSの色と演出を借ります。

落ち着いたら**画面のどこをタップしても**タイトルへ戻ります(`OnTapAfterFinished`)。タイトルへボタンも使えます。単体で再生したときは、`_endlessMock`でチャレンジのリザルト(`_challengeMock`・`_endlessReachedLayerMock`・`_challengeClearedMock`)を、記録は書かずに見せます。

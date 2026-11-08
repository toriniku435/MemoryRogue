# MapScene(日本語版)

`Live2DCardRogue.MapScene.*`(`Assets/Scripts/Scenes/MapScene/`)の詳細ドキュメントです。[docs/map-scene.md](map-scene.md)(英語版、`CLAUDE.md`から`@`importされる方)の対訳。まずルートの[CLAUDE.ja.md](../CLAUDE.ja.md)で一般的なレイヤー/配線の規約を、ここへの入口は[docs/title-scene.ja.md](title-scene.ja.md)、ステージの先は[docs/battle-scene.ja.md](battle-scene.ja.md)を読んでください。

マップのウインドウはこのフォルダにはありません。`Assets/Scripts/Window/<Name>/`(`Live2DCardRogue.Window.<Name>.*`)にあり — `DeckEdit`、`CardShop`、`UnitUpgrade`、`EnemyDetail`、`FirstBuild`、`Reward`(ステージの報酬ウインドウ)と共通の`Menu` — マップは`MapSceneInitializer` / `MapPresenter`からそれらを使います。以下、ウインドウのパスは`Window/`からの相対、それ以外は`Scenes/MapScene/`からの相対です。マップがほかのシーンに渡すもの(`BattleSceneTransitionData`、`EndingSceneTransitionData`、`ChallengeResultTransitionData`など)と受け取るもの(`MapSceneTransitionData.BattleResult`)は`Common/SceneTransition/TransitionData/`にあります。

`Assets/Scenes/MapScene.unity`がキャンペーンのマップです。ステージ1つにつき1マスが横一列に並び、現在地が強調され、**ステージ開始**と**デッキ確認**のボタンが置かれます(後者はまだTODO)。

## ステージはコードではなくアセットが決める

- `Common/ScriptableObjects/StageData.cs` — 1ステージ分。`_id`と、そのステージで戦う`EnemyData[]`を持ちます。`CreateEnemyLineup()`はnull要素を落とすので、呼び出し側はnullチェック不要です(`EnemyData.PickCards()`と同じ約束)。
- `Common/ScriptableObjects/CampaignData.cs` — 踏破する順のステージ列。`CreateStages()`も同様にnullを落とします。遊ぶのは今のエピソードのもの(`CampaignBalance.GetEpisode(CampaignProgressRepository.EpisodeIndex)`、`MapRunRuleFactory.ResolveCampaign`。そのエピソードにステージが無ければ、エラーを出してエピソード1で遊ぶ)で、エピソードごと、難易度ごとのバランスのアセットごとに自分のステージを持てます。

**マスの数は`CreateStages().Count`です** — コード側にステージ数は一切ありません。ステージの増減はそのエピソードの`Episode1..3.asset`の編集だけで済み、マップも進行の上限も「踏破したか」の判定もそれに追随します。同梱のキャンペーンがたまたま10個(`Assets/ScriptableObjects/Stages/Stage01..10.asset`)なだけで、10という数はルールではありません。

制約が1つあります: 1ステージに出せる敵の数は`BattleSceneViewContainer.EnemyStatusViews`の要素数が上限です(現在2)。少ない場合は`BattleSceneInitializer.CreateEnemies`が余ったViewを隠し、多い場合は出し切れないのでエラーログを出します。

## 進行状況はパラメータではなくRepositoryが持つ

`Common/Repositories/CampaignProgressRepository`は`CurrentStageIndex`(0始まり)を持つstaticクラスです。マップが`Start`で読み、勝利後に書き、マップ→戦闘→マップの往復の間ずっと残ります。これが狙いで、**進行状況を`BattleSceneTransitionData`に詰めて往復させない**ようにしています(必要としているのはマップだけなので)。

`static`にしている理由は`SceneTransitionDataStore`と同じで、シーンのオブジェクトが持つとシーン境界で消えてしまうからです。今はメモリ上だけなので、アプリを終了しても続きから遊べるようにするなら、セーブ/ロードを足す場所はここだけです。

## 往復の流れ

1. **ステージ開始**を押すと、まず確認ウインドウ(`ConfirmDialog.prefab`。マップで共用の`WindowHandler<ConfirmShowData>`として`PopupCanvas`の下に生成。文言は`UiTextKey.StageStartConfirm`と`LAYER nn / NN`)を開きます。キャンセルか背面のクリックなら閉じるだけで、**OK**なら閉じて`MapStageTransitionUseCase.StartStage()`を呼びます。`StartStage()`は現在のステージの敵編成・デッキ・プレイヤーの値から`BattleSceneTransitionData`を組み立てます。このとき**`CreateReturnData`(`ReturnToMap`、チャレンジなら`ReturnToMapOrEndChallenge`)も渡します**。
2. 戦闘が終わると`BattlePhaseDirector`が`OnBattleFinished(bool isVictory)`を発火します(勝敗のトーストを読める程度の待機を挟んでから)。Director自身は遷移しません — `BattleSceneInitializer`が購読していて、`BattleResult`を作り、マップの`CreateReturnData`がそれから作ったデータでロードします。`ReturnToMap`は`MapSceneTransitionData`に包みます(チャレンジでは`ReturnToMapOrEndChallenge`が負けをResultSceneへ送る)。どちらも戦闘のシーンで呼ばれるので`static`です。関数が無ければ「留まる」で、BattleSceneを単体で再生したときがこれです。マップは`Start`で`MapSceneTransitionData.BattleResult`を一度だけ読みます(タイトルから入った・開き直したときはnull)。
3. マップに戻ると、`MapRunUseCase.ApplyBattleResult()`がマスを作る**前に**結果を取り出します。まずステージの報酬を渡し(下の「報酬とトークン」)、そのうえで勝利なら`AdvanceStage()`、敗北なら何もしません。結果として、負けたら同じマスのまま、勝ったら1マス進みます。

BattleSceneはマップの存在を知りません。知っているのは「戻り先のシーン名」だけです。これで依存の向きがマップ→戦闘の一方向に保たれます(パラメータの型の置き場所と同じ理屈)。

## 報酬とトークン

**トークン**はキャンペーン中の通貨です(以前は「ポイント」と呼んでいました。カード購入で使います)。進行状況・デッキと並んで`Common/Repositories/CampaignTokenRepository`が持ち、**寿命はキャンペーン1回分**です。タイトルでキャンペーンを選ぶと`TitleSceneInitializer`が`Clear()`し、マップが未初期化の状態で開いたときに`CampaignBalance.StartTokens`(500)を配ります。

- **ステージごとの報酬はデータ**で、`StageData`に持たせています。勝利は`_victoryTokens`と`_victoryCard`(カード1枚)、敗北は`_defeatTokens`(勝利時の2倍程度、カードなし)。`CreateReward(isVictory)`が`StageReward`(`Common/ScriptableObjects/Data/`)を返すので、呼ぶ側はどのフィールドを使うか分岐しません。
- **表示の前に反映済み。** `ApplyBattleResult`は進む**前**のマス(いま戦ってきたステージ)を読み、トークンをRepositoryに、カードを`CampaignDeckRepository.OwnedCards`に足してから、`StageReward`を見せるものとして`MapPresenter`に渡します。受け取るボタンを押さずにマップを離れても失うものはありません。
- **負けたときは報酬ではなく支援として見せます。** **バックアップブースト**として、励ます文言にしています。負けを繰り返せばトークンが増えますが、立て直しの助けとして意図したものです。
- **マーケット**は開くたびに`CampaignTokenRepository.Tokens`から始め(`CreateCardShopModel`)、買うたびに残りを`Set`で書き戻します(`MapRunUseCase.ApplyShopPurchase`)。ユニット強化ウインドウのトークンもそこで合わせます(`UnitUpgradeModel.SetTokens`)。

**報酬ウインドウ**(`Reward/Views/StageRewardView.cs`、`PopupCanvas`上の`Assets/Prefabs/StageRewardWindow.prefab`)。シーン遷移の覆いが明けるのを待ってから、共通の`WindowTransition`で開きます。

| | 勝利 | 敗北 |
| --- | --- | --- |
| 見出し | `// ACCESS GRANTED :: LOOT DECRYPTED`(シアン) | `// CONNECTION LOST :: BACKUP BOOST`(マゼンタ) |
| 題と本文 | `VictoryRewardTitle` / `VictoryRewardMessage`(題は黄) | `DefeatRewardTitle` / `DefeatRewardMessage`(題はシアン) |
| 中身 | カードが横一線から白く光って開き、後ろにレアリティ色の縁と回る放射光。続いてトークンが数え上がる | トークンだけ。放射光は緑 |
| ボタン | `RewardClaim`(受け取る) | `BackupAccept`(再起動) |

受け取るボタンは数え上げが終わってから出ます。背面をクリックすると最後まで飛ばせます。受け取るとPresenterがウインドウを閉じ、トークン表示が受け取る前の額から数え上がり(`TokenCounterView`、`Common/Views/UI/`)、進んでいれば移動の演出が続きます。

**トークン表示**は**マーケットのボタン(`CardShopButton`)のすぐ上**(`Canvas/Tokens`)に置き、「そこで使うもの」と読めるようにしています。最初はヘッダーの右上にあり、ユーザーの要望で移しました。

**キャンペーンの踏破。** 最終ステージには**報酬がありません**(トークンもコマンドも無し。2026-10-04に変更)。`ApplyBattleResult`は進行を進めて`_completedNow`を立てるだけです。マップに戻ると、切り替えの覆いが晴れてから、そのステージのクリア後の会話(あれば)を流し、`MapStageTransitionUseCase.CompleteCampaign`が`CampaignResult`を計算してEndingSceneを開きます。報酬ウインドウも移動の演出もありません。階層数のあるチャレンジの最後の階層も同じで、ResultSceneへ進みます。また、負けるたびに`CampaignProgressRepository.RecordBackupBoost()`を呼び、その回数をリザルトのスコアが減点します。[docs/ending-and-result-scenes.ja.md](ending-and-result-scenes.ja.md)参照。

**セーブ。** 進捗を保存するのはマップです。戦闘の結果(勝敗とも)、デッキ編集の保存、マーケットで買うたび、ユニット強化を反映するたびに保存し、キャンペーンをクリアしたらクリアを記録して進捗を消します。ルートの「セーブ」参照。

**戦闘で払ったトークン。** `StartStage`は今のトークンを戦闘に渡し(`BattleSceneTransitionData.Tokens`)、`ApplyBattleResult`はほかのどの分岐よりも先に(最後のステージも)`BattleResult.TokensSpent`を差し引きます。負けても戻りません。

**マイニングの上乗せ。** 勝利のトークンは`BattleResult.TokenBonusPercent`だけ増えます(`StageReward.WithTokenBonus`、端数切り捨て。負けたときには乗りません)。報酬ウインドウの詳細の行に`MINING +50%`を添えます。

**敵とステージ(2026-09-27)。** トロイの木馬に3体が加わりました。**ルートキット**(HP110。ブロック8、4ダメージのプローブ、毎ターン3のウイルス。硬くて遅い)、**スパイウェア**(HP60、毎ターン2回行動。キーロガーで2ターン手札-1、毎ターン3のウイルス、データ破損、4ダメージのプローブ。脆くて厄介)、**ボットネット**(HP60。一番弱っている味方を12回復、5ダメージのDDoS)です。ステージは 1 トロイの木馬、2 スパイウェア、3 ルートキット、4 トロイの木馬×2、5 トロイの木馬+スパイウェア、6 ルートキット+ボットネット、7 スパイウェア+ボットネット、8 トロイの木馬+ボットネット、9 ルートキット+スパイウェア、10 ルートキット+ボットネット になりました。報酬は2(リバースパッチ)、5(ループバック)、7(カーネルパニック)、9(マルチスレッド)を変えています。

**敵の強化(2026-09-29)。** ご要望により、どの敵も3〜5割ほど強くしました。トロイの木馬はプレイヤーのアタック・強撃のアセットを使うのをやめ、専用の`TrojanBackdoor`(バックドア、9)と`TrojanPayload`(ペイロード、20)を使います。これで敵を調整してもプレイヤーのコマンドは変わりません。ウイルスは2です。トロイの木馬HP130。ルートキットHP150、行動は常に2回、ブロック12、プローブ7、ウイルス4(プローブはスパイウェアと共有)。スパイウェアHP80、ウイルス4。ボットネットHP80、DDoS 8、リモート修復16。下の勝率のシミュレーションはこれより前のもので、今の数値には合っていません。

ランダムなファーストビルドでのシミュレーション(同じ単純なAI。勝率%):

| ステージ | HP100/M10 | HP160/M11 | HP200/M12 | HP240/M13 |
| --- | --- | --- | --- | --- |
| 1 トロイの木馬 | 93 | 100 | 100 | 100 |
| 2 スパイウェア | 100 | 100 | 100 | 100 |
| 3 ルートキット | 77 | 98 | 99 | 100 |
| 4 トロイの木馬×2 | 27 | 68 | 89 | 95 |
| 5 トロイの木馬+スパイウェア | 47 | 90 | 98 | 98 |
| 6 ルートキット+ボットネット | 34 | 75 | 85 | 92 |
| 7 スパイウェア+ボットネット | 64 | 94 | 98 | 99 |
| 8 トロイの木馬+ボットネット | 52 | 90 | 97 | 98 |
| 9 ルートキット+スパイウェア | 39 | 85 | 94 | 96 |
| 10 ルートキット+ボットネット | 30 | 75 | 88 | 92 |

AIは高いコマンドから使い、回復の反転をわざとボットネットに向けることもしないので、それをするプレイヤーにとってボットネットのステージはこれより易しいはずです。

## デッキ編集(`DeckEdit/Presenters/DeckEditPresenter.cs`、`Prefabs/DeckEditWindow.prefab`)

**コマンドビルド**ボタン(`Canvas/DeckEditButton`)で、ユニット強化と同じように`PopupCanvas`上のデッキ編集ウインドウをマップの上に開きます。閉じるのはキャンセルと保存だけです(背面のクリックでは閉じない)。保存するとデッキを`CampaignDeckRepository`へ書き戻して進捗を保存します(`MapRunUseCase.ApplyDeckSaved`)。詳細は[docs/deck-edit-window.ja.md](deck-edit-window.ja.md)。

## マーケット(`CardShop/Presenters/CardShopPresenter.cs`、`Prefabs/CardShopWindow.prefab`)

**マーケット**ボタン(`Canvas/CardShopButton`)で、`PopupCanvas`上のマーケットのウインドウをマップの上に開きます。閉じるのは閉じるボタンだけです(背面のクリックでは閉じない)。買ったものはその場で反映します。コマンドは`CampaignDeckRepository`へ、トークンは`CampaignTokenRepository`へ入り、マップのトークン表示が数え下がり、進捗を保存します(`MapSceneInitializer.HandleShopPurchased`)。並べるチップは`MapSceneInitializer._shopPacks`です。詳細は[docs/card-shop-window.ja.md](card-shop-window.ja.md)。

## ユニット強化(`Common/Models/UnitUpgradeModel.cs`、`UnitUpgrade/Presenters/UnitUpgradePresenter.cs`、`Prefabs/UnitUpgradeWindow.prefab`)

**ユニット強化**ボタン(`Canvas/UnitUpgradeButton`。マーケットのボタン(`CardShopButton`)の隣。どちらもトークンを使うので、トークン表示を2つの上に置く)でウインドウを開き、トークンでプレイヤーユニットの上限を1段ずつ上げます:

| 項目 | 1段 | 範囲 | 次の1段の値段 | 全段の合計 |
| --- | --- | --- | --- | --- |
| 最大HP | +20 | 100 → 300(10段) | 40 + 10 × 段 | 850 |
| メモリー上限 | +1 | 10 → 15(5段) | 60 + 40 × 段 | 700 |

- **状態。** 値は`Common/Repositories/CampaignUnitRepository`が持ちます(最大HPとメモリー上限だけ。戦闘は毎回満タンから始まる)。寿命はキャンペーン1回分で、`TitleSceneInitializer`が消し、マップが`CampaignBalance.StartMaxHp` / `StartMaxMemory`(段の数え始めでもある)で配り直します。`StartStage`はここから戦闘の`UnitStatusModel`を作ります。
- **決まりは1か所。** 上がり幅・上限・値段は`CampaignBalance`から`UnitUpgradeRule`として渡り、`CanUpgradeHp` / `CanUpgradeMemory`・値段・段は`UnitUpgradeModel`が答えるので、ボタンの活性と実際の強化がズレません。変化があるとPresenterは両方の行を塗り直し(片方を払うともう片方が払えなくなることがある)、ウインドウのトークン表示を数え下げます。
- **反映で確定。** 強化ボタンで上げた分は仮のものです。`UnitUpgradeModel`は確定した値(`AppliedTokens` / `AppliedHp` / `AppliedMemory`)を仮の値と別に持ち、`IsDirty`で未確定の強化があるかを答えます。**反映**(キャンセルの右の`>>>`付き`CyberButton`。未確定の強化があるときだけ押せる)で確定し(`Apply()`)、マップのトークン表示を数え下げ、`(トークン, 最大HP, メモリー上限)`を伝え(`MapRunUseCase.ApplyUnitUpgrade`が書き戻して保存)、閉じます。**キャンセル**(背面のクリックも)は、未確定の強化が無ければすぐ閉じ、あればマップで共用の`ConfirmDialog`で「変更を破棄しますか？」と確かめます(破棄するで`Discard()`して閉じる、戻るで仮の強化を残したままウインドウに戻る)。開くときは必ず確定した値から始めます。確認ウインドウは接続開始と共用なので、それぞれのPresenterは自分が開いたときだけ答えます(`_isConfirmingDiscard` / `_isConfirmingStageStart`)。
- **見た目。** 各行(`UnitUpgradeRowView`)に`140 → 160`、段を示すメモリーチップの`SegmentGaugeView`、値段(黄。払えなければマゼンタ、上限なら緑の`MAX`)、小さな強化ボタン。上がると値が弾みます。
- **駆け引き。** 使ったトークンはリザルトのスコアから消えるので、どれだけ強化してどれだけ残すかはプレイヤーの判断になります。

**この数値にした理由 — バランスのシミュレーション(2026-09-27)。** 本物のルール(`BattleCommandUseCase`、カード効果と状態効果、トロイの木馬自身の`IEnemyCardPicker`)で戦闘を繰り返し、簡単なプレイヤーAIで勝率を測りました。AIの動きは次のとおりです。払えるコマンドをコストの高い順に最大3個積む。HP4割未満で防御と回復を優先する。一番弱い敵を狙う。初期デッキのままで、報酬も使わない。各2000戦:

| プレイヤー | トロイの木馬1体(ステージ1〜3) | 2体(ステージ4〜10) |
| --- | --- | --- |
| HP100 / メモリー10(初期) | 98% | **7%** |
| HP140 / メモリー10 | 100% | 44% |
| HP100 / メモリー15 | 100% | 37% |
| HP160 / メモリー12 | 100% | 86% |
| HP200 / メモリー10 | 100% | 93% |
| HP200 / メモリー13 | 100% | 99.5% |
| HP240 / メモリー14、HP300 / メモリー15 | 100% | 100% |

強化しなければ、ステージ4〜10はバックアップブーストでの再挑戦を重ねない限りほぼ突破できませんでした。メモリーよりHPの効きがずっと大きいので、HPは安く細かく上げられるようにしています。上の値段なら、ステージ4の前に使えるトークン(500 + 320)でHP200 + メモリー12まで買えます。ステージ4〜10はどれも同じトロイの木馬2体なので、ステージ4以降は難しさが平らです。ペース配分を本当に直すなら、敵を変えていくことです。

## ファーストビルド — 初期デッキは引いて決まる(2026-09-27)

新しいキャンペーンは**コマンドもデッキも無い状態**で始まります。最初にマップを開いたとき、`MapRunUseCase.InitializeDeckIfNeeded`が`CampaignBalance`の**ファーストビルド**(`FirstBuildChip`を10個)を開けます。

- **出るもの。** 10×3 = 30個です。アタック×5とブロック×4は確定で(先に枠を取ります)、残りの21個をチップ自身の封入と重み(スターターチップと同じ、ノーマル70 / レア25 / エピック5)で抽選します。引きが悪くても勝てなくならないようにするのが確定枠です。
- **デッキは自動で組む。** 30個はすべて所持コマンドに入り、`AutoDeckBuilder`(`Common/Models/Cards/`)が役割ごとに20個をデッキに選びます。攻撃(攻撃かウイルスの効果)10、防御(ブロック)6、補助(それ以外)4です。役割の中ではレアリティが高いもの、次にコストが低いものを優先し、足りない役割の枠は残りの良いもので埋めます。乱数は使わないので、同じ引きからは必ず同じデッキになります。プレイヤーはデッキ編集で組み直せ、残りの10個も所持に残ります。
- **すぐ保存する。** 引いた直後、何かを見せる前に進捗を保存します。演出の途中で終了しても同じコマンドで再開し、やり直しで引き直すことはできません。
- **マップの上で見せる。** 画面の切り替えの覆いが晴れてから、`FirstBuildPresenter`が`Prefabs/ChipInstallWindow.prefab`(お店の開封ウインドウを共有)を開きます。見出しは`ファーストビルド  チップ10個`で、インストールを1回見せたあと、30個をレアリティの低い順にめくります。クリックで飛ばせます。ボタンは**デッキにセット / Set to Deck**で、閉じると「20個のコマンドをデッキに自動でセットしました」とトーストで伝えます。再開したキャンペーンでは何も出ません。
- **トークンは変わりません**(500)。

**シミュレーション(2026-09-27。AIは上と同じ。400通りの引きを、それぞれ5回戦わせた)。** 強化なしでトロイの木馬1体に対し、ランダムなファーストビルドの勝率は**92.5%**(以前の固定デッキは90.3%)で、1度も勝てなかった引きは0.8%だけでした。2体に対しては、強化なしで13.8%(以前のデッキは0.8%)、HP160 / メモリー11で62%、HP200 / メモリー12で82%、HP240 / メモリー13で91%でした。レアが入るぶん、ファーストビルドは平均して以前のデッキより強くなります。後半のステージの難しさは、お店でのコマンドの単品購入などで後から調整する予定です。

これより前(2026-09-27)は、決まった持ち物で始まりました。ノーマル19個とレアの回復・強化・オーバークロック、決まった20個のデッキです。上のバランスのシミュレーションは、さらに前の、もっと強い固定デッキでのものです。

## チャレンジ — 同じマップに別のルールを差し込む(2026-10-04)

タイトルの**チャレンジ**ボタン(2026-10-04まではエンドレス)からは、**ノーマル(30階層)・ハード(60)・マスター(100)・エンドレス(終わり無し)**を選べ、どれもこのシーンで動きます。何も複製していません。ストーリーと違うところは`MapRunRule`(`Models/MapRunRule.cs`)として差し込み、`MapRunRuleFactory.Create`が`RunModeRepository`を見て選びます。

| | `CampaignMapRule`(ストーリー) | `ChallengeMapRule` |
| --- | --- | --- |
| 部屋 | エピソードの`StageData`。`StageCount`はその数 | `RunModeRepository.ChallengeLayerCount`(30 / 60 / 100)。エンドレスは終わり無し(`StageCount`はnull) |
| 部屋の中身 | `StageData.CreatePlan()` | `EndlessBalance.CreateStage(seed, index)`。階層ごとに使い回す |
| 物語(`HasStory`) | 始まりとクリア後の会話、チュートリアル、エンディング、バックアップブースト | 無し。会話は流さず、チュートリアルのボタンは隠す |
| 敗北(`EndsOnDefeat`) | バックアップブーストを受けてマップへ戻る | 戦闘からそのままResultSceneへ(`ReturnToMapOrEndChallenge`が記録して作る`ChallengeResultTransitionData`) |
| 最後の部屋で勝利 | 報酬は無し。クリア後の会話、エンディング、そしてリザルト | 報酬は無し。記録してから`ChallengeResultTransitionData`でResultSceneへ(ノーマル / ハード / マスター。エンドレスは終わらない) |

- **部屋の中身は`StagePlan`**(`Common/ScriptableObjects/Data/`)です。敵、`EnemyStrength`、勝敗それぞれの報酬、クリア後の会話を持ちます。マップ・報酬の流れ・`EnemyDetailPresenter`(`Func<StagePlan>`を受け取る)はこれしか見ないので、ステージがアセットか乱数かを気にしません。
- **階層数はデータです:** `Assets/ScriptableObjects/ChallengeCatalog.asset`(`ChallengeCatalog`。並べた順がボタンの順。0は終わり無し)を`TitleSceneInitializer._challengeCatalog`に設定しています。ランは始めたときの階層数を持ち続けるので(`RunModeRepository.ChallengeLayerCount`、保存もする)、カタログを調整しても中断中のランの長さは変わりません。
- **ストーリーと共用するもの:** ファーストビルド、トークン、マーケット、ユニット強化、コマンドビルド、キャンペーンの4つのリポジトリ。新しいチャレンジは、新しいキャンペーンと同じくそれらを空にして始めます。
- **`EndlessBalance`**(`Assets/ScriptableObjects/EndlessBalance.asset`。`MapSceneInitializer._endlessBalance`に設定)が、どのチャレンジのバランスでもあります。名前はエンドレスしか無かったころのままです。開始時の持ち物・強化・値段は`CampaignBalance`のままです。
  - **敵.** 候補ごとに、出始める階層と、1体だけで出てよいかを持ちます。トロイの木馬とスパイウェアは1階層から、ルートキットは3階層から、ボットネットは10階層からで単独では出ません(味方を回復するだけなので)。10階層から、60 %の階層で2体になります(戦闘画面に並ぶのは2体まで)。
  - **強さ.** 10階層ごと(`_layersPerTier`)に、敵の最大HPが+25 %、攻撃ダメージが+2されます。1〜10階層は上乗せ無し、11〜20階層が1段目、という具合です。
  - **勝利報酬.** トークン 100 + 10 ×(階層 − 1)と、その段のチップから引いたコマンド1個(1〜10階層はスターターチップ、それ以降はプレミアムチップ)。敗北時の支援はありません。
  - **毎回同じ.** 階層はシードとその番号から組むので、同じシードなら同じ階層は同じ中身です。戦闘から戻っても、中断して再開しても引き直しになりません。保存するのはシードだけです(`RunModeRepository.ChallengeSeed`。タイトルが決める)。
- **戦闘はチャレンジを知らない.** `BattleSceneTransitionData.EnemyStrength`で最大HPを倍率で上げ、`LayerBoostStatusEffect`(階層ブースト。攻撃ダメージを足す)を付けます。筋力とは別の効果にしてあるのは、敵が自分で筋力を積んだときに「大きい方を残す」で打ち消されないようにするためです。敵詳細ウインドウも、上乗せ後のHPと階層ブーストを出します。
- **ランの終わり.** 負けたらResultSceneを直接開きます。マップを経由すると、一度見せてからまた離れることになるためです。階層数のあるチャレンジで最後の階層に勝つと、報酬は無く、すぐに`CompleteCampaign`がResultSceneを開きます。どちらも遷移の前に`FinishChallenge`が`CampaignSaveService.FinishChallengeRun`(そのチャレンジの最高到達階層と踏破の記録、`endless.sav`の削除)を呼び、ResultSceneはそれが作った`ChallengeResultTransitionData`を見せます([docs/ending-and-result-scenes.ja.md](ending-and-result-scenes.ja.md))。あきらめるも敗北と同じ扱いです。
- **中断データはチャレンジ全体で1つ.** どのチャレンジも同じファイル(`endless.sav`)に保存し、タイトルはチャレンジを押したときに再開を聞きます([docs/title-scene.ja.md](title-scene.ja.md))。
- **ページ.** `MapPresenter`は部屋を一直線の経路の座標に置きます(部屋*i*は`startX + i × step`)。10部屋で1ページです(`MapPresenter.PageSize`)。`MapRouteScrollView`(`Canvas/RouteScroll`)が`LinkParent`・`CellParent`・`PlayerParent`をそろって横へずらし、今のページを画面の真ん中に置きます。出しておくのは今のページとその前後だけで(`MapPageWindow`)、範囲から外れた部屋と配線はプールへ戻して使い回します。300階層でも1階層でも部屋は30個です。ページの境目をまたぐ移動(10 → 11部屋)では、パケットが飛んでいる間に経路が格子に吸着しながら1ページ分流れます(`MapPlayerView.PlayTravel`の`onDeparted`、同じ時間)。10部屋のストーリーは1ページなので見た目は今まで通りです。チャレンジでは16:9の画面の右端に、次のページの最初の部屋がのぞきます。
- **とても深い階層でもあふれない**(2026-10-04)。普通に遊んで届く値ではありませんが、デバッグの階層移動や書き換えたセーブでは届きます。階層の番号は`CampaignProgressRepository.MaxStageIndex`(`int.MaxValue - 1`。「番号 + 1」も収まる)で止まり、そこから計算する値はどれも、あふれて戻らずに上限で止まります。`EndlessBalance.CreateStrength`・`VictoryTokensOf`・`StageReward.WithTokenBonus`・`LayerBoostStatusEffect`・`MapPageWindow.Of`は`long`で計算して`int`に収め、`MapRunRule.LayerText`と再開の要約は +1 を`long`で行い、部屋のアドレスは下4桁だけを出します(`MapCellView.FormatAddress`。256階層ごとに`0x0000`へ戻る)。部屋とプレイヤーの位置は、マップを開いたときのページを原点にした相対の座標で置くので(`MapPresenter._originIndex`)、値が大きくならず、floatの精度が落ちて経路がずれることもありません。`EndlessOverflowTests`がそれぞれを上限で確かめます。
- **デバッグ.** `Map: Jump to Layer`(両モード)は指定した階層でマップを開き直します。`Map: Clear All`はストーリーだけです。

## シーンの構成

- `Views/MapCellView.cs` — マス1つ分。番号と、踏破済み/現在地/未到達の色だけを持ちます。マスは実行時に生成するのでプレハブ(`Assets/Prefabs/MapCellView.prefab`)です。
- `Views/MapSceneViewContainer.cs` — マスの置き場所とプレハブ、2つのボタンとそのラベル。マス自体はシーンに置きません。
- `Presenters/MapPresenter.cs` — 部屋をページごとに、プールから使い回して並べ(上の「チャレンジ」参照。位置はスクリプトで置きます。Layout Groupを使わない理由は[docs/battle-scene.ja.md](battle-scene.ja.md)の手札と同じ)、Repositoryの値で状態を塗り、ボタンのラベルを`UiTextKey`から流し込み、押下を注入された`Action`で報告します。踏破後は**ステージ開始**を押せなくします。
- `FirstBuild/Presenters/FirstBuildPresenter.cs`(ウインドウ) — 切り替えの覆いが晴れてから、共有の`ChipInstallWindow`でファーストビルドを見せ、閉じたらデッキに自動でセットしたことをトーストで伝えます。見せるだけで、引きとデッキはInitializerが決めて保存済みです。
- `Initializer/MapSceneInitializer.cs` — コンポジションルート。以下のモデル・ユースケース・ウインドウ・Presenterを作って配線するだけで、流れは持ちません。
- `UseCase/MapRunRuleFactory.cs` — `CampaignBalance` / `EndlessBalance`から`MapRunRule`(ストーリー / チャレンジ)を選びます。
- `UseCase/MapRunUseCase.cs` — マップでのランの状態。新しいランのデッキ・トークン・ユニットの用意、戦闘結果の反映(`ApplyBattleResult` → `Data/MapArrival`: どこから来たか、報酬、いまランを終えたか)、デッキの保存・購入・強化の反映、そして保存(`SaveIfAllowed`: マップのチュートリアル中と完了後はしない)。
- `UseCase/MapStageTransitionUseCase.cs` — マップから出ていくこと。ステージ開始、エンディング、中断。戦闘へ渡す静的関数`ReturnToMap`(チャレンジでは負けでランを終える)もここです。
- `UseCase/MapTutorialUseCase.cs` — チュートリアルを流すかどうか、スキップと再生、見たことの記録。
- `Initializer/MapDebugCommands.cs` — 0707パネルのマップ用コマンド(デバッグビルドのみ)。
- `Presenters/MapRouteLayout.cs` — 部屋とプレイヤーの位置をページ単位で計算します(`MapPresenter`が使う。`MapRouteLayoutTests`)。

## 未実装・割り切り

- **デッキ**: 今は`MapSceneInitializer`のInspector配列です。デッキ編集ができたら、進行状況と並べてデッキ用のRepositoryから取るべきで、そのTODOをフィールドに書いてあります。
- **プレイヤーの状態**: マップが毎回新しい`UnitStatusModel`を作るので、どのステージも全快から始まります。ダメージを持ち越すならモデルをRepositoryに置くだけで、`BattleSceneTransitionData`は既に`UnitStatusModel`ごと受け取る形なので戦闘側の変更は不要です。
- **デッキ確認**は「未実装です」のトーストを出すだけです。
- 新しくキャンペーンを始めるとトークンは消えますが`CampaignDeckRepository`は消えないので、同じ起動中なら前のキャンペーンで持っていたカードが残ります。

## 会話パート(`Common/Conversation/`、`Prefabs/ConversationWindow.prefab`)

ストーリーは、マップに重ねる短い会話で語ります。画面下のテキストボックス(`// TRANSMISSION`、話し手は黄色、台詞は1秒40文字で打ち出す)で、後ろのマップはほかのウインドウと同じくぼかします(`WindowTransition`。重ねたときのぼかしの動きもほかと同じ)。ボックスでも背景でも、クリックすると、打ち出している途中ならまず全文を出し、出し終えていれば次へ進みます。最後の行の次で閉じます。

- **流すとき。** 2か所。どちらも場面の切り替えの幕が上がってから流します(`MapSceneInitializer.PlayWhenShownAsync`)。
  - エピソードを始めたとき: `CampaignData.OpeningConversation` → チュートリアルの確認(@docs/tutorial.md)。
  - ステージに勝って戻ってきたとき: そのステージの`StageData.ClearConversation` → 報酬ウインドウ。`MapPresenter`を`holdReward`付きで作り、`ShowReward()`が呼ばれてから報酬を開きます。負けたときは流しません。
- **データ。** `ConversationData`(ScriptableObject、`Create > Campaign/Conversation`)。`_id`と`ConversationLine[]`(話し手と台詞)。エピソード1の分は`Assets/ScriptableObjects/Conversations/Episode1/`(`Episode1_Opening`、`Episode1_StageNN_Clear`)にあり、今はどれも3行の仮テキストです。台詞はアセットを書き換えて差し替えます。参照が空、または行が無い会話は飛ばし、すぐ次へ進みます。
- **台詞はローカライズしない。** アセットに直接書くので`FontCoverageTests`は見ません。代わりに`ConversationDataTests`が、全文字がDotGothic16にあること、IDが重ならないこと、空の行が無いこと、エピソード1に始まりの会話と全ステージのクリア後の会話があることを確かめます。
- **ぼかしを途切れさせない受け渡し。** `ConversationPresenter`は、次の手順(次のウインドウを開く)を呼んで**から**自分を閉じます。会話と次のウインドウの間でぼかしが抜けません。

## メニュー(共通の`Window/Menu/`、`Prefabs/MenuWindow.prefab`)

`Canvas/MenuButton`(右上。チュートリアルボタンを左右反転した位置の0.6倍、ラベルは`UiTextKey.Menu`)が、`PopupCanvas`上の共通のメニューウインドウ(`PopupCanvas`の下に生成。`MenuWindowView` / `MenuWindowPresenter`、`// MENU`、メニュー。ルートの「ボタンを並べるだけのウインドウ」を参照)を開きます。中身は**中断**・**オプション**と閉じるで、背面のクリックでも閉じます。`MapSceneInitializer`は`MenuWindowInitData`を組み立てるだけです。ほかのマップのウインドウと同じく`WindowTransition`で開き、マップをぼかします。2026-10-06からはバトルのメニューも同じウインドウです。

- **オプション**は、バトルとタイトルと同じ`GameOptionsWindow`をメニューの上に重ねて開きます(`GameOptionsPresenter.Open`。`onDeleteUserData`は渡さないので、ユーザーデータの行は出ない)。閉じるとメニューに戻ります。
- **中断**は共用の`ConfirmDialog`で「中断してタイトルに戻りますか？」(詳細「進み具合は保存され、タイトルから再開できます」、中断する / 戻る。`UiTextKey.SuspendConfirm` / `SuspendConfirmDetail` / `SuspendOk`。項目の`MenuConfirmData`)と確かめます。OKならメニューを閉じ、`MapStageTransitionUseCase.Suspend`が進み具合を保存し(ほかの節目と同じく、マップのチュートリアル中と最終ステージの後は保存しない)、タイトルへ戻ります。タイトルのストーリーかチャレンジから再開できます。
- 写したボタンにはチュートリアルボタンの`TutorialAnchor`が付いていたので外しました。チュートリアルが指すのはチュートリアルボタンだけのままです。

## チュートリアル

`CampaignTutorialRepository.Stage == Map`の間、マップはファーストビルドの代わりにチュートリアルの説明を流し、保存もしません。練習の戦闘から戻ってきた結果は捨て、そこでチュートリアルを終えます。左上の**チュートリアル**ボタンでいつでも見直せます。docs/tutorial.ja.md参照。

## 見た目

[docs/tone-and-manner.ja.md](tone-and-manner.ja.md) に沿って、**サイバー空間を奥へ進む**見立てで装飾しています。

- **部屋。** 各ステージは部屋です(`MapCellView`: 暗い中身、状態で色が変わる縁、番号`01`…、飾りのアドレス`0x0100`…)。ジグザグの経路に並びます。踏破済みはライム、現在地は黄色で脈打ち、未到達は暗いシアンです。
- **配線。** `MapLinkView`が隣り合う部屋をつなぎ、通ってきた区間を光らせます。
- **見出し。** 上部に`// NETWORK DEPTH` / `LAYER nn / NN`。
- **ボタン。** 3つのボタンは波紋付きの斜めのボタンで、画面の下端を基準にし、主操作のステージ開始(`>>>`の飾り付き)を中央に置いています。ステージ開始は目立つよう1.2倍にしています。斜めのスライス画像は縦に伸ばすと折れ目が出るので(docs/art.ja.md参照)、枠の高さではなく`localScale`で大きくしています。画面上のボタンの文字は**接続開始 / Connect**(ステージ開始)、**コマンドビルド / Command Build**(デッキ編集)、**マーケット / Market**(チップ購入)で、このドキュメントでは機能の名前で呼びます。文字は自動で縮み(上限は元の大きさ、左右28pxは内側に空ける)、長い名前でも斜めの面からはみ出しません。
- **背景。** `bg_map.png`は地平線を低くして、経路が夜空に浮かぶようにしています。
- **敵詳細**(`Canvas/EnemyDetailButton`。コマンドビルドの上に置いた0.8倍の`CyberButton`、ラベルは`UiTextKey.EnemyDetail`。キャンペーンを踏破したら押せない)で`PopupCanvas`の`Prefabs/EnemyDetailWindow.prefab`を開きます。受け持つのは`EnemyDetailPresenter`(`Window/EnemyDetail/`) → `EnemyDetailWindowView`です。ウインドウはユニット強化のウインドウを写して作ったので、開閉・ぼかし・電子の散り方は他のウインドウと同じ共通の`WindowTransition`です。閉じるボタンか背面のクリックで閉じます。ウインドウのView(`EnemyDetailWindowView`・`EnemyDetailSlotView`・`EnemyCommandRowView`・`EnemyDetailInfo`)は`Common/Views/EnemyDetail/`に、文の組み立ては`Common/Presenters/EnemyDetailInfoBuilder`にあります。タイトルのエネミーコレクションが同じウインドウを使うためです([docs/title-scene.ja.md](title-scene.ja.md)参照)。デッキ編集のウインドウにも、同じウインドウを開く2つ目のボタンがあります(同じHandlerの`Show`を呼ぶ。[docs/deck-edit-window.ja.md](deck-edit-window.ja.md)参照)。
  - **左: 敵の一覧。** 今いるステージ(次の戦闘)の敵1種類につき`EnemyDetailSlot.prefab`を1枠(同じ敵が2体なら`x2`)。敵のアイドルアニメ(その`UnitVisualDefinition`。戦闘と同じく左向きに反転)と名前を出します。クリックで選び、選んでいる枠には黄色の光る枠と、横に揺れる黄色のドットのカーソル(`UI/ui_cursor.png`)が付きます。開いたときは先頭を選びます。
  - **右: 選んだ敵。** 名前、`HP`と毎ターンの行動回数(`UiTextKey.EnemyActionCount`)、無効化など最初から持つ効果(黄)、特徴(`EnemyDescription`テーブル)、そして`UiTextKey.EnemyCommands`の下に、使うコマンドを1つずつ`EnemyCommandRow.prefab`で並べます。絵・名前・**コマンドの効果の説明**(`CardDescription`。実際の数値入り)です。特徴の文以外は`EnemyData` / `CardData`から引くので、敵やコマンドを調整すればウインドウも追いつきます。
  - **何も切らない。** `MapEnemyInfoLayoutTests`が、敵の特徴と、敵が使うコマンドの説明(数値を差し込んだもの)を、全言語で枠に収まるか測ります。測るのは**実体化した**プレハブで、途中の非アクティブな親も有効にしてからです。プレハブのアセットのままや、ウインドウの非アクティブな`Dialog`の下ではTMPが初期化されず、どの文も同じ高さに測れてしまい、テストが何があっても通ってしまうためです。
  - 1行の文字は`Overflow`にしています。DotGothic16は行の高さが大きく、低い枠で`Ellipsis`にするとエラー無しに全部の文字が消えるためです。

**進む演出。** 勝って進んだとき、`MapRunUseCase.ApplyBattleResult`は1つ前の部屋を返し、`MapPresenter`はそれを`arrivedFromIndex`として受け取ります。Presenterは*前の部屋*を現在地として見せ、ステージ開始を押せなくし、`MapPlayerView`に移動を再生させます。

1. シーン遷移の覆いが明けてから、さらに`_startDelay`だけ待つ。
2. プレイヤーが横一線に潰れ、電子の粒になって弾ける。
3. 光るパケットが、軌跡を残しながら格子に沿って配線の上を飛ぶ。
4. 次の部屋でまた弾け、開いてプレイヤーに戻る。`onArrived`で本当の現在地に塗り直す。

部屋・配線・プレイヤーは、**位置もアンカーも同じ**3つの親(`LinkParent`、`CellParent`、`PlayerParent`。この順に描く)の下にあるので、Presenterは部屋の位置からそのままプレイヤーの位置を計算できます。1つを動かすときは、ほかもそろえてください。

# マーケットのウインドウ(日本語版)

マップのマーケット(チップ購入)ウインドウの詳細ドキュメントです。[docs/card-shop-window.md](card-shop-window.md)(英語版、`CLAUDE.md`から`@`importされる方)の対訳。まずルートの[CLAUDE.ja.md](../CLAUDE.ja.md)で一般的なレイヤー/配線の規約を、開く側の画面については[docs/map-scene.ja.md](map-scene.ja.md)を読んでください。

以前は独立したシーン(`CardShopScene`)で、`CardShopSceneParameter`を持って入り`CardShopSceneResult`を持って戻っていました。2026-10-01にデッキ編集([docs/deck-edit-window.ja.md](deck-edit-window.ja.md))と同じくマップ上のウインドウになり、シーン・その Initializer・引き継ぎの型・`SceneName.CardShop`は削除しました。2026-10-06からは、コードはウインドウ専用のフォルダ`Assets/Scripts/Window/CardShop/`(`Live2DCardRogue.Window.CardShop.*`)にあり、マップは`MapSceneInitializer`から使います。`MapScene`のものは何も参照しません:

| ファイル | 役割 |
| --- | --- |
| `Common/Models/CardShopModel.cs` | トークン・タブ・選択・個数と、今回買ったもの |
| `Window/CardShop/Presenters/CardShopPresenter.cs` | マップのマーケットボタンとウインドウ |
| `Window/CardShop/Views/CardShopWindowView.cs` | ウインドウ(`Assets/Prefabs/CardShopWindow.prefab`。MapSceneの`PopupCanvas`に置く) |
| `Window/CardShop/Views/CardPackView.cs`・`SingleCommandView.cs`・`CardShopPurchaseView.cs` | チップの行・シングルの行・購入パネル |

ウインドウは、売っているチップを一覧し、選んだものの購入パネル(個数の−/+・合計価格・購入する)を出し、下の中央に**閉じる**ボタン(`UiTextKey.Close`)があります。ほかのウインドウと同じく共通の`WindowTransition`で開閉し、閉じるのは閉じるボタンだけです(背面のクリックでは何も起きない)。

## パックはアセット

`Common/ScriptableObjects/CardPackData.cs`が、パックの`_id`・トークン価格`_price`・1パックから出る枚数`_cardCount`・封入カード`_cards`・レアリティごとの重み`_rarityWeights`を持ちます。どのパックを並べるかは`MapSceneInitializer`のInspector配列(`_shopPacks`)なので、パックを増やす作業は「アセットを作って1行足す」だけです。

レアリティはカード側にあります: `CardData._rarity`(`CardRarity.Normal` / `Rare` / `Epic`)。重みは相対値なので、`70 / 25 / 5`と`14 / 5 / 1`は同じ意味です。

## 売っているチップ(2026-09-29)

売り物のチップは、どれも1つで**5コマンド**(`_cardCount`)です。ファーストビルドのチップは10個開けるので3のままです。並び順(`MapSceneInitializer._shopPacks`):

| チップ | 中身 | ノーマル / レア / エピック | 値段 |
| --- | --- | --- | --- |
| スターター(`StarterPack`) | プレイヤーのコマンド全部、バランスよく | 70 / 25 / 5 | 100 |
| アタック(`AttackChip`) | 攻撃・ウイルス・強化 | 60 / 30 / 10 | 150 |
| リペア(`RepairChip`) | ブロック・回復・リジェネ | 60 / 30 / 10 | 150 |
| ハック(`HackChip`) | メモリー・バッファ・停止・ループバック・データ破損・エクスプロイトチェーン… | 50 / 35 / 15 | 180 |
| プレミアム(`PremiumPack`) | プレイヤーのコマンド全部、レア寄り | 30 / 45 / 25 | 300 |

チップの絵は`Cards/Packs/<Id>.png`で、種類ごとに色と紋章が違います(docs/art.ja.md参照)。

## シングル購入

左のパネルには**チップ**と**シングル**の2つのタブがあります(`ShopTab`)。シングルは、カードショップのシングル買いのように、コマンドを1つずつ選んで買います。

- **売っているもの**は、ショップに並べたチップの中身すべてです(`CardShopModel.Singles`。重複なし、レアリティの低い順)。敵専用のコマンドはどのチップにも入っていないので、売られません。
- **値段はレアリティで決まり**、チップの1コマンドあたりより少し高めです: ノーマル40、レア90、エピック180(チップと同じ係数がかかる)。値段と上限は`CampaignBalance`(「シングル購入」)にあり、`SinglePurchaseRule`としてモデルへ渡ります(渡されなければ`CardShopModel`は`SinglePurchaseRule.Default`を使います)。
- **1種類につき、キャンペーンを通して3個まで**(`LimitPerCommand`)。マーケットを閉じて開き直しても戻りません。開くたびに`CampaignDeckRepository.PurchasedSingles`から始め(`PriorSinglePurchases`)、シングルで買うたびにマップへ伝え、マップがリポジトリに足して保存します(`purchasedSingles`、セーブの版2)。チップから出たコマンドは数えません。
- **モデルが決める**のは`RemainingOf(card)`で、個数は残り数とトークンの両方で止まります。売り切れの行は「売り切れ」と出て暗くなりますが、選んで中身を読むことはできます。
- **一覧の行**(`SingleCommandView.prefab`。チップの行の複製)は、レアリティの色(ノーマル シアン、レア マゼンタ、エピック 金)の枠に入ったコマンドの絵、名前、「残り n/3」、値段(買えない・売り切れならマゼンタ)。購入パネルにはコマンド名、「レア  コスト3  残り 2/3」(`UiTextKey.SingleInfo`)、効果の説明を出し、何も選んでいないときの案内もタブに合わせます(`// SELECT A CHIP` / `// SELECT A COMMAND`)。
- **所持数。** 一覧の行と購入パネルには、そのコマンドをいくつ持っているか(所持 n)も出します。開くたびに`CampaignDeckRepository.OwnedCards`から始め、`CardShopModel.OwnedCountOf`が今回買った分(チップから出た分も)を足します。
- **デッキの上限。** `CardData.MaxCopiesInDeck`が0より大きいコマンド(1枚までのエクスプロイトチェーンの3つ)は、行と購入パネルの絵の右上に、その数を書いた黄色い丸のバッジが付きます。バトルの外のカードに付くものと同じです([docs/deck-edit-window.ja.md](deck-edit-window.ja.md)参照)。購入パネルでは、絵の下端に「デッキに1個まで」の札も重ねます。
- **買うと**、チップと同じインストールのウインドウで、買ったコマンドを見せます。
- `CardShopModelTests`が、一覧・値段・上限・トークン、それにチップがシングルの残りを減らさないことを確かめます。

## 抽選の仕組み

`Common/Models/Cards/CardPackOpener.cs`が1枚ごとに2段階でやります: **重みでレアリティを決めてから、そのレアリティの封入カードから等確率で1枚**。この順番にしてあるので、「Normalを20枚追加したせいでEpicの確率が黙って下がる」ことが起きません。重みが表示どおりの意味を保ちます。候補になるのは「重みが正」かつ「実際にそのレアリティが封入されている」レアリティだけなので、入れ忘れたレアリティに重みが残っていても無視されるだけで、空の抽選にはなりません。

Openerを`System.Random`を受け取る素のstaticクラスにしてあるのは、重みの効き方をテストできるようにするためです(`CardPackOpenerTests`が乱数を固定して分布を検証します)。`CardPackData.Open`は乱数を用意して渡すだけの薄い呼び出し口です。封入が空・重みが全部0・null要素、といった不正なアセットでも例外にせず空配列を返します(`EnemyData.PickCards`と同じ約束)。

## トークンを受け取り、カードを返す

**開くたびに`CardShopModel`を作り直します。** Presenterに渡す`Func<CardShopModel>`(`MapRunUseCase.CreateCardShopModel`。チップ・今のトークン・チップの係数・シングルの決まり・これまでにシングルで買った分・所持コマンド)で作ります。**買ったものはその都度伝えます。** `onPurchased(コマンド, そのうちシングル, 残りトークン)`を受けた`MapSceneInitializer.HandleShopPurchased`が、所持リストへ加え、シングルを記録し、トークンを書き戻し、ユニット強化ウインドウのトークンを合わせ(`UnitUpgradeModel.SetTokens`)、進捗を保存します(マップのチュートリアル中とキャンペーン踏破後は保存しない)。マップのトークン表示の数え下げはPresenterが行います。だからウインドウを開いたまま終了しても買ったものは失われず、Presenterはリポジトリに触れません。

**トークンはキャンペーンのものです。** モデルは`CampaignTokenRepository.Tokens`から始まり、買うたびに残りを書き戻すので、消費は残り、ステージ報酬で得たトークンもここで使えます([docs/map-scene.ja.md](map-scene.ja.md)の「報酬とトークン」)。以前はポイントと呼んでいて、UIのラベルは`UiTextKey.Tokens`です。

## モデルが決めること

`Common/Models/CardShopModel.cs`がトークン・選択中のパック・個数を持ち、UIは次の3つを表示するだけです:

- `CanBuy` — パックが選ばれていて`TotalPrice <= Tokens`。購入ボタンの活性はこれで決まるので、「押せるのに買えない」が起きません。
- `CanIncreaseQuantity` — **もう1個増やしても**払えるか。払えない個数を組み立てられないよう、+ボタンが手前で止まります。
- `CanDecreaseQuantity` — 個数は1未満にならない。

購入するとトークンを引き、出たカードを`PurchasedCards`に積み、個数を1に戻します(同じ個数を続けて買えることは稀なため)。パックを選び直したときも1に戻ります。

## 見た目

[docs/tone-and-manner.ja.md](tone-and-manner.ja.md) に沿って、**データマーケット**の見立てで装飾しています。左に`// CHIPS`、右に`// CHECKOUT`、所持トークンは右上の`// TOKENS`の札(トークンのアイコン付き。価格は`100 TKN`の形)、閉じるは下の中央です。ウインドウは1860×1010(ユニット強化ウインドウの枠を広げたもの)です。専用のBGMはなく、マップの曲が流れ続けます(`BgmId.Shop`と`Backgrounds/bg_shop.png`は今は使われていません)。

- **パックの絵**は`CardPackData._illustration`(未設定でもよい)です。`CardPackView`が行に出し、`CardShopPurchaseView.Show`は引数で受け取ります。
- **一覧の行**(`CardPackView.prefab`)は、白い縁に選択状態の色を乗せ、選んでいる行にはシアンの`SelectGlow`を点けます。1パックも買えないトークンなら価格をマゼンタにします(`Bind`の`isAffordable`。Presenterが`Price <= Tokens`で決める)。これは目安で、購入ボタンを押せるかは引き続き`CanBuy`が決めます。
- **チップが何のためのものか。** 購入パネルのチップの絵の下に、買うと何が起きるかをシアンで出します(`UiTextKey.ChipInstallInfo`。「インストールすると、コマンドを{0}個獲得してデッキを強化できる」、`{0}`は`CardPackData.CardCount`)。その下に、`CardPackDescription`の表からチップごとの説明を出します。自動の折り返しだと最後の1文字だけが次の行に落ちたことがあるので、説明は文の区切りで文中に改行を書いてあります。`CardPackLocalizationTests`が、すべての`CardPackData`に名前と説明が全言語そろっているかを確かめます。
- **購入パネル。** `_root`は子の`Content`なので、パネル自体は出たままです。何も選んでいないときは`_emptyHint`を出します。個数の−/+は六角形のボタン、購入は`>>>`の飾りを点けた`CyberButton.prefab`です。

## 購入結果ウインドウ

購入すると、トーストではなく`ChipInstallWindowView`(`// DOWNLOAD COMPLETE`、見出しは`UiTextKey.AcquiredCards`)を開きます。ウインドウとそのViewは`Common/Views/ChipInstall/`と`Prefabs/ChipInstallWindow.prefab`にあります。マップがキャンペーンのファーストビルドを同じウインドウで見せるためです([docs/map-scene.ja.md](map-scene.ja.md)参照)。ウインドウは何を開けたかを知らず、見出しとボタンの文字は呼び出し側が入れます。`CardShopWindow.prefab`の中に置いたプレハブのインスタンス(`ChipInstallWindow`。`CardShopWindowView._resultView`)です。開くと一番手前のウインドウになるので、後ろのマーケットのウインドウはマップと一緒にぼけます(ルートの「くっきりしているのは一番手前のウインドウだけ」参照)。開閉はゲーム共通の`WindowTransition`で行い([docs/tone-and-manner.ja.md](tone-and-manner.ja.md)の「動き」参照)、背後のぼかしのかけ外しもそちらが受け持ちます。このウインドウが足しているのはカードの動きだけで、開くと1枚ずつ届き、閉じると逆順に潰れて消えます(`Close()`。`Hide()`は起動時に使う、動きなしの版)。電子の粒(`WindowDissolve`)はプレハブの常に有効なルートの下、`ResultView`の隣(ウインドウの外)に置いてあり、ウインドウを非表示にしても消えません。ウインドウの下の半透明の`Scrim`がクリックを受け止めるので、**閉じる**(`UiTextKey.Close`)を押すまでマーケットは操作できません。

- **カード**(`ShopResultCardView.prefab`、`ChipInstallCardView : CardView`)は、スクリプトで`CardArea`に並べます。列数を1から順に試し、枠に収まる中で一番大きくなるものを選び(上限は`_maxCardScale`)、行ごとに中央揃えにします。`_minDescriptionScale`より小さくなるときは、読めない大きさなので説明文を隠します。カードは1枚ずつ(`_cardInterval`おき)横一線から開き、届くまではカーソルに反応しません。
- **開封演出**は見せ方だけで、何が出たかは`CardPackOpener`が既に決めています。
  1. **インストール**(`ChipInstallView`、`CardArea/Install`の下): チップの形をしたパックが格子に沿ってソケットへ差し込まれ、進み具合のバーが最後の方ほどゆっくり埋まります。光はシアンから始まり、中身の一番高いレアリティへ**昇格**します。レアがあれば40%でマゼンタ、エピックがあれば75%で金になります。金は虹色に揺らぎ、揺れと光の粒が出て、バーが**99%で`_epicHoldDuration`だけ止まって**から完了します。完了するとチップが光になって弾けます。
  2. **めくる**: カードが裏向きで届き、**レアリティの低い順**に並ぶので、一番良いカードは必ず最後にめくれます。ノーマルはすぐめくれます。レアとエピックは、まずその色で光を溜め(`_rareCharge`・`_epicCharge`)、めくった後も薄く光る縁が残ります。めくるたびに、カードの色で`RevealBurst`(カードの位置へ動かして出す、ワールド座標の粒)が散ります。
  3. すべて表になってから閉じるボタンが出ます。表になるまで、カードはカーソルに反応しません。
  - **飛ばす**: 演出中は`SkipArea`(ウインドウを覆う透明な`ClickArea`)がどこのクリックも「今すぐ終える」に変えます。インストールを隠し、全カードを表にし、閉じるボタンを出します。
- **カーソルで詳細。** カードは乗った・外れたを伝えるだけで、`CardShopPresenter`がそれを`ShowDetail` / `HideDetail`へ渡します。右側で1.4倍の`CardDetailView`(戦闘の詳細パネルと同じ部品)が動きます。閉じるまでの短い待ちを持っているので、カードからカードへ移っても点滅せず中身だけが入れ替わります。


## 未実装・割り切り

- レアリティはデータだけで、カードの表示には出していません。
- パック一覧に出るのは名前と価格だけで、中身のプレビューはありません。
- 購入結果では、新しく手に入ったカードと、既に持っていたカードの区別をしていません。

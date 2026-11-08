# デッキ編集ウインドウ(日本語版)

マップのデッキ編集(**コマンドビルド**)ウインドウの詳細ドキュメントです。[docs/deck-edit-window.md](deck-edit-window.md)(英語版、`CLAUDE.md`から`@`importされる方)の対訳。まずルートの[CLAUDE.ja.md](../CLAUDE.ja.md)で一般的なレイヤー/配線の規約を、開く側の画面については[docs/map-scene.ja.md](map-scene.ja.md)を読んでください。

以前は独立したシーン(`DeckEditScene`)で、`DeckEditSceneParameter`を持って入り`DeckEditSceneResult`を持って戻っていました。2026-10-01にユニット強化と同じくマップ上で開くウインドウになり、シーン・その Initializer・引き継ぎの型・`SceneName.DeckEdit`は削除しました。2026-10-06からは、コードはウインドウ専用のフォルダ`Assets/Scripts/Window/DeckEdit/`(`Live2DCardRogue.Window.DeckEdit.*`)にあり、マップは`MapSceneInitializer`から使います。`MapScene`のものは何も参照しません:

| ファイル | 役割 |
| --- | --- |
| `Window/DeckEdit/Models/DeckSetEditModel.cs` | 編集中のすべてのデッキと、選んでいるデッキ・アクティブなデッキ |
| `Window/DeckEdit/Models/DeckEditModel.cs` | 編集中のデッキ1つ(コマンドごとの所持数とデッキ内の数) |
| `Window/DeckEdit/Presenters/DeckEditPresenter.cs` | マップのコマンドビルドボタンとウインドウ |
| `Window/DeckEdit/Views/DeckEditWindowView.cs` | ウインドウ(`Assets/Prefabs/DeckEditWindow.prefab`。MapSceneの`PopupCanvas`に置く) |
| `Window/DeckEdit/Views/DeckEditCardView.cs`・`DeckEditCountView.cs`・`DeckEditSubView.cs` | 一覧のカード・デッキの個数・拡大表示 |
| `Window/DeckEdit/Views/DeckSlotTabView.cs` | デッキの番号のタブ1つ(01〜10) |
| `Window/DeckEdit/Constants/DeckEditConstants.cs` | `MaxDeckSize`(20) |

ウインドウは3つのエリアでできています: **所持**リスト、**デッキ**リスト、最後に押したカードを拡大表示する**サブビュー**。その下に**おまかせ**・**キャンセル**・**保存**が並びます。

## デッキは10個、使うのはアクティブな1つ

デッキは`CampaignDeckRepository.DeckSlotCount`(10)個まで持てます。**アクティブな**デッキが戦闘で使うデッキです(`CampaignDeckRepository.Deck`がそれを返す)。

- **所持コマンドはデッキ同士で取り合わない。** どのデッキも所持しているものすべてから別々に組みます。同じコマンドを、それぞれのデッキに所持数まで入れられます。だから`DeckSetEditModel`はデッキごとに`DeckEditModel`を1つずつ、同じ所持リストから作って持ち、所持リストには選んでいるデッキの残り(所持数 − そのデッキに入っている数)を出します。
- **タブ。** `// COMMAND DECK`のパネルの上に番号のタブ(`DeckTabs`、01〜10)を並べます。押すとそのデッキを選び、両方の一覧・個数・見出し(`デッキ 03`)が切り替わり、サブビューの選択が外れ、一覧は先頭に戻ります。選んでいるタブは明るく、アクティブなデッキのタブは番号が黄色で黄色の下線が付きます。選んでいるデッキがアクティブなときは、見出しの横に黄色の**アクティブ**の札(`UiTextKey.ActiveDeck`)が出ます。開いたときはアクティブなデッキを選んでおきます。
- **アクティブにする**(`UiTextKey.SetActiveDeck`)は、選んでいるデッキをアクティブにします。デッキの見出しの横、アクティブの札と同じ場所にあり、どちらか一方だけを出します。選んでいるデッキがアクティブなら札、そうでなければボタン(`ActivateTag`。札と同じ形でシアン)です。そのため、デッキのパネルを隠す「所持」表示では見えません。個数を問わずアクティブにします(足りないデッキを断ると、デッキによって押したときの動きが違ってしまうため)。個数は保存のときに確かめます。
- **保存**が確かめるのは**アクティブな**デッキだけです。ほかのデッキは足りなくても超えていても保存できます。アクティブなデッキがちょうど`MaxDeckSize`個でなければ、そのデッキと個数を伝えるトーストを出し、開いたままにします。
- **全部で1つの編集。** デッキの切り替え・いくつものデッキの編集・おまかせ・アクティブにするは、どれも保存までモデルの中だけで持ちます。保存で`onSaved(デッキの一覧, アクティブな番号)`を`MapRunUseCase.ApplyDeckSaved`へ渡し(`CampaignDeckRepository.SetDecks`のあと保存)、キャンセルですべて捨てます。

## 3つの表示

タイトルの左の小さなタブ(**すべて / デッキ / 所持**。`UiTextKey.DeckEditLayoutAll` / `Deck` / `Owned`。マーケットのチップ / シングルのタブと同じ0.55倍の`CyberButton`で、選んでいるものが黄色)で、ウインドウの並べ方(`DeckEditLayout`)を切り替えます。リストが見づらいとき用です:

- **すべて(Both):** 所持リスト・デッキリスト・拡大表示(今まで通り)。
- **デッキ(DeckOnly):** 所持パネルを隠し、デッキのパネルを2つのパネルを合わせた範囲(幅1230)に広げるので、デッキリストが3列から6列になります。まだデッキに無いコマンドを入れるときは、ほかの表示に切り替えます。
- **所持(OwnedOnly):** デッキのパネルを隠し、所持パネルを同じように広げます。デッキリストが見えないので、デッキの番号のタブはパネルの上に残し、所持パネルの見出しの右に選んでいるデッキと個数を出し(`DeckSummary`。`デッキ 02  12/20`。デッキのパネルの見出しと個数をゲージ抜きで写したもの)、拡大表示にも「−」を出します。切り替えなくてもデッキから抜けます。

置き方は`DeckEditWindowView.SetLayout`が受け持ちます。最初にすべて表示のときの位置を覚え、パネルを動かして大きさを変え、デッキの番号のタブを見えているパネルの上へ寄せ、`CommonScrollList.FitColumnsToWidth`(表示枠の幅に収まる列数にする)を呼びます。Presenterは表示のしかたをフィールドに持つので、マップにいる間は開き直しても前回のままです(保存はしません)。

## 開閉

- **開く。** `Canvas/DeckEditButton`(ラベル`UiTextKey.DeckEdit`。このPresenterが入れる)で、ほかのウインドウと同じく共通の`WindowTransition`で開きます。横一線から開き、背後のマップはぼけて暗くなります。
- **敵詳細**(`EnemyDetailButton`。ウインドウの右上、表示切り替えのタブと同じ0.55倍の`CyberButton`、ラベルは`UiTextKey.EnemyDetail`)は、マップの敵詳細ウインドウをデッキ編集の上に重ねて開きます。デッキを組みながら次の相手を確かめられます。マップのボタンと同じウインドウです。`DeckEditPresenter`は`DeckEditInitData`で敵詳細のHandlerの`Show`と`CanOpenEnemyDetail`を受け取り、ラベルを自分で付け、全ステージ踏破後は開くときにボタンを押せなくします。閉じると編集中のままのデッキ編集に戻ります。
- **閉じるのはキャンセルと保存だけ。** 背面のクリックでは何も起きません(暗幕が入力を受け止めるだけ)。編集中の誤クリックで編集を捨てないための意図的な仕様です。
- **確認**はマップで共用の`WindowHandler<ConfirmShowData>`で行います(接続開始・ユニット強化と共用。どの問いか(`Confirming`)はOKのコールバックが持つ):
  - **キャンセル**は、変更が無ければすぐ閉じ、あれば「変更を破棄しますか？」(破棄する / 戻る)と確かめます。
  - **保存**は、アクティブなデッキの個数の確認を通ったうえで、変更が無ければすぐ閉じ、あれば「変更を保存しますか？」(保存する / 戻る。`UiTextKey.SaveChangesConfirm` / `SaveChangesOk`)と確かめます。
  - **おまかせ**は毎回「おまかせでデッキを組み直しますか？」と、置き換わるデッキ(`デッキ 03`)を添えて確かめます(OKは組み直す。`UiTextKey.AutoBuildConfirm` / `AutoBuildOk`)。
  - **デッキを空にする**(ゴミ箱のアイコン、`ClearButton`。アクティブの札 / アクティブにするの右)は、「デッキを空にしますか？」とデッキを添えて確かめ(OKは空にする。`UiTextKey.ClearDeckConfirm` / `ClearDeckOk`)、選んでいるデッキのコマンドをすべて抜きます(何も入れずに`ReplaceDeck`)。デッキが空のときは押せません。ほかの編集と同じく、確定するのは保存です。
  - 戻るは、いつも編集中の中身を残したままウインドウに戻ります。
- **何を変更とみなすか**は`DeckSetEditModel.IsDirty`です。アクティブなデッキが変わったか、どれかのデッキの中身が開いたときと違うか(`DeckEditModel.IsDirty`は個数で比べるので、同じコマンドを入れて抜いたら変更なし。タブを切り替えただけでも変更なし)。
- **保存**はアクティブなデッキを確かめます(上記)。ボタンを活性のままにしてあるのは意図的で、押せないボタンを眺めさせるより理由を伝えるためです。
- 閉じている途中の押下は受け付けません(`DeckEditWindowView.IsOpen`)。

## コマンドの保存場所は知らない

Presenterには所持コマンド・デッキの一覧・アクティブな番号の`Func`と、`Action<CardData[][], int> onSaved`が渡されます。`MapSceneInitializer`がそれを`CampaignDeckRepository`につなぎ、`MapRunUseCase.ApplyDeckSaved`がデッキを書き戻して進捗を保存します(ほかの節目と同じく、マップのチュートリアル中とキャンペーン踏破後は保存しません)。

**開くたびに、その`Func`から`DeckSetEditModel`を作り直します。** これがキャンセルの実装のすべてです。リポジトリは一度も触られていないので巻き戻しも「元のデッキ」の保持も要らず、キャンセルした編集が次に開いたときに残ることもありません。モデルは閉じても捨てず、次に開くまで持っています(閉じる動きの間にリストをスクロールすると行の描き直しが走るため)。編集中はデッキが上限を**超えても**かまいません(入れ替えるときに、先に入れてから抜けるように)。

所持コマンドは重複ありの平らなリストで渡します(1種10個なら10要素)。デッキと同じ形なので、型が1つで両方を扱えます。

## 計算はモデル1箇所に閉じている

`DeckEditModel`がカードごとの所持数とデッキ枚数を持ち、他は全部そこから導出します:

- **所持リストの個数 = 所持数 − デッキに入っている数**。0より大きいカードだけを所持リストに出します。「デッキに入っているものは所持リストに出さない」はこれで実現していて、表示側では何も引き算しないので**2つのリストが食い違う余地がありません**。
- `CanAddToDeck`は`残り > 0`で、`CardData.MaxCopiesInDeck`が0より大きいコマンドは、デッキ内の数がそれ未満であること(エクスプロイトチェーンの3つは1個まで)。プラスボタンは、どちらかを使い切ると非活性になります。上限はここでは見ず、保存のときに判定します(`IsDeckValid`)。
- 表示順は構築時に確定(最初に見つかった順)。増減のたびに並びが入れ替わって押し間違える、ということが起きません。
- 所持数より多い枚数がデッキに入ったデータを渡された場合は、例外にせず所持数の方を引き上げて辻褄を合わせます。呼び出し側の不正データでウインドウが落ちるべきではないためです。

`DeckEditPresenter`は`OnChanged`1本だけを見て、2つのリストとデッキの個数表示とサブビューをまとめて同期します。だから増減後にどこか1箇所だけ古いまま、ということが起きません。

## リスト間のドラッグ

**すべて**表示のときは、コマンドを一方のリストからもう一方のパネルへドラッグでき、**1回のドロップで1個ずつ**移ります(`x3`を1回ドラッグすると`x2`が残る)。所持→デッキは`TryAddToDeck`、デッキ→所持は`TryRemoveFromDeck`で、ほかの編集と同じく保存するまで確定しません。

- **役割分担。** `DeckEditCardView`は、自分のいるウインドウ(`GetComponentInParent<DeckEditWindowView>`)にドラッグを伝えるだけです。ウインドウが、始めてよいか(`TryBeginCardDrag`:開いている、`Both`表示、行がどちらのリストの下にあるか)を決め、ポインタに付いて動くゴーストを出し、落とせる側のパネルを光らせ、離した場所を判定します。もう一方のパネルの上で離すと`OnCardDroppedToDeck` / `OnCardDroppedToOwned`を出し、Presenterが`CanAddToDeck` / `CanRemoveFromDeck`を確かめて1個移し、そのコマンドをサブビューに出します(移した先のリストから選んだ扱い)。それ以外の場所で離しても何も起きません。
- **ドラッグとスクロール。** ドラッグを受ける行は、そのままでは`ScrollRect`のドラッグを奪ってしまうので、最初の動きで決めます。主に**横**ならカードのドラッグ、主に**縦**なら親の`ScrollRect`へ渡します(initialize / begin / drag / end を`ExecuteEvents.ExecuteHierarchy`で)。デッキだけ・所持だけの表示では、どのドラッグもスクロールです。
- **入らないとき。** `MaxCopiesInDeck`に達したコマンドは`Error`を鳴らし、「〇〇はデッキに1個までです」とトーストを出します。移せたときはデッキへは`MemoryWrite`、デッキからは`CardReturn`を鳴らします。
- **ゴースト。** 最初にドラッグした行の写しを、ウインドウの`WindowTransition`のオブジェクトの下に1度だけ作って使い回します(`x1`、不透明度85%、`CanvasGroup.blocksRaycasts`を切って下のパネルを隠さない)。`CancelCardDrag`で隠し、これは表示の切り替え、閉じるとき、行の`OnDisable`でも呼ばれます(ドラッグ中にホイールで見えなくなった行はプールに戻され、`OnEndDrag`が届かないため)。
- **落とし先の光。** `OwnedPanel/DropHighlight`と`DeckPanel/DropHighlight`(`ui_glow_frame`、シアン、レイキャストなし、既定で非表示。`_ownedDropHighlight` / `_deckDropHighlight`)。ドラッグ中は落とせる側の縁がうっすら、ポインタが乗るとはっきり光ります。どちらも未設定でかまいません。
- ドラッグしても行のクリックは起きません(ドラッグが始まると入力モジュールが`eligibleForClick`を落とすため)。

## サブビューの2つの形

どちらのリストから開いたかはPresenterの状態(`_isSelectedFromDeck`)で、モデルには持たせていません:

- **所持リスト**から押した → プラスのみ
- **デッキリスト**から押した → プラスとマイナス

プラスの活性は`CanAddToDeck`、マイナスは`CanRemoveFromDeck`で決まります。増減のたびにサブビューを出し直すので、ボタンの活性も個数表示も追随します。デッキから全部抜いたカードも消えずに残り、マイナスが非活性になるだけです。ウインドウを開くと選択は外れます。

## スクロールは行を使い回す

`Common/Views/UI/CommonScrollList.cs`が共通の仮想化リストです。`ScrollRect`のcontentには、画面に見えている行＋予備1行分しかGameObjectを置きません。`SetItemCount`でcontentの大きさを決めて描き直し、`Refresh`がスクロール位置から表示範囲を計算して、範囲外になった行をプールへ返します。ウインドウは開くたびに`ScrollToStart`を呼ぶので、開き直すと先頭から見えます。

**スクロールバー.** 2つの一覧(とコマンドのコレクションの一覧)には、バトルログと同じ細いシアンの縦スクロールバーがあります(`ScrollRect.verticalScrollbar`、`AutoHide`。全部収まるときは隠れる)。スクロールできることを分かるようにするためです。表示枠の中ではなく、一覧の右の縁にまたがってパネルの余白に置いています。所持の一覧は3列が残り4pxで収まっているので、表示枠を狭めると2列に減るためです。

カードのことは意図的に知りません。`Initialize(Action<int, RectTransform>)`で「空の枠に何番目を描くか」を渡す形なので、中身は呼び出し側の自由です(ここでは`DeckEditCardView.Bind`)。前提は**全行が同じ大きさ**であることで、可変高さには対応していません。

`CommonScrollListPlayModeTests`が肝心の性質を固定しています: 300件でも300個作らないこと、スクロールしても新規生成が増えないこと、件数が減ったら破棄ではなく非アクティブにすること。表示範囲の計算にはレイアウト済みの`RectTransform`が要るのでPlayMode側のテストです。

## 見た目

[docs/tone-and-manner.ja.md](tone-and-manner.ja.md) に沿って、**プログラムエディタ**の見立てで装飾しています。1860×1010のウインドウ(ユニット強化ウインドウの枠・暗幕・潰れるときの光・散る粒を広げたもの)の上部に`// PROGRAM EDITOR`の見出しとタイトル、パネルは3つ(`// LIBRARY`、メモリ基板の画像を使った`// COMMAND DECK`、`// COMMAND DATA`)で、おまかせ・キャンセル・保存は下に1列で中央に並べ、保存を主操作にしています。

- **一覧のカード**(`DeckEditCardView.prefab`)は、戦闘のカードの絵を0.64倍にして`Card`という子に入れたものです。ルートの大きさはスクロール一覧が自由に変えられます。この大きさでは説明文を隠し(実行リストのカードと同じ)、空いた説明欄の上に個数の札を置いています。サブビューに出しているカードには、選んだ側の一覧でシアンの`SelectGlow`が付きます。`Bind`が`isSelected`を受け取り、選ぶたびに両方の一覧を描き直します。
- **デッキの上限のバッジ。** デッキに入れられる数に上限があるコマンド(`CardData.MaxCopiesInDeck`が0より大きい。エクスプロイトチェーンの3つは1)は、カードの右上の角にその数を書いた黄色い丸のバッジが付き、+ボタンが押せなくなる理由が分かります。`CardView`そのものの機能(`_deckLimitBadge` / `_deckLimitText`、未設定でもよい)で、一覧のカード、サブビュー、ショップの結果ウインドウ、報酬ウインドウに付いています。**バトルのカードには付けません**(手札・コマンドメモリー・バトルの詳細パネル)。デッキを組むための情報だからです。大きく見せるカード(サブビュー、ショップの結果の詳細、報酬のカード)には、同じことを言葉でも書きます。絵の下端に重ねた黄色一色の札の「デッキに1個まで」です(`_deckLimitLabel` / `_deckLimitLabelText`、`UiTextKey.DeckLimit`)。
- **デッキの個数**は`DeckEditCountView`です。`12/20`の数字を、ちょうど20個のときだけライム、それ以外はマゼンタで出し(`23/20`もマゼンタ。ゲージは満杯のまま)、20個のメモリーチップを並べた`SegmentGaugeView`を添えます。戦闘のメモリー残量と同じゲージを0.58倍にしたものです。
- **サブビュー。** `_root`は子の`Content`なので、パネル自体は出たままです。何も選んでいないときは`_emptyHint`(うっすらしたカードの裏面と`// SELECT A COMMAND`)を出します。カードは`CardView.prefab`から作った素の`CardView`を1.15倍にしたものです。
- **ボタン**は`CyberButton.prefab`のインスタンスです。
- **専用のBGMはありません。** マップの曲が流れ続けます。`BgmId.DeckEdit`と`bgm_deck_edit`はシーンだった頃のもので、`Backgrounds/bg_deck_edit.png`とともに今は使われていません。

## おまかせ

**おまかせ**(`UiTextKey.AutoBuild`)は、所持しているものすべてから`AutoDeckBuilder`(ファーストビルドと同じ組み方)でデッキを組み直し、`DeckEditModel.ReplaceDeck`で置き換えます(所持数とコマンドごとの投入上限はそのまま効く)。保存はしないので、続けて編集しても、保存してもキャンセルしても構いません。役割ごとの個数はPresenterに渡す`AutoDeckRule`から取り(マップは`CampaignBalance.CreateAutoDeckRule()`を渡す)、無ければ`DeckEditConstants.MaxDeckSize`の5:3:2で組みます。

## 未実装・割り切り

- ウインドウを置いているのはマップだけです(マーケットも同じ。[docs/card-shop-window.ja.md](card-shop-window.ja.md))。`Window/DeckEdit/`にあってどのシーンも参照しないので、別のシーンで使うなら、その`PopupCanvas`に`DeckEditWindow.prefab`を置き、Initializerで`DeckEditPresenter`を組み立てれば足ります。

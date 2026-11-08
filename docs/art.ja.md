# アートアセット

ゲームのアート（`ArtSource/` → `Assets/Art/`）の詳細ドキュメントです。`CLAUDE.md` から読み込まれます（日本語版は [CLAUDE.ja.md](../CLAUDE.ja.md)）。すべてのシーンをこのアートで装飾しています。

## 方向性: サイバー × ポップ × ドット

| 要素 | 表現のしかた |
| --- | --- |
| ベース | ほぼ黒の濃紺 |
| 色 | ネオンの平塗り（シアン、マゼンタ、イエロー、ライム） |
| サイバー | グリッド、角を斜めに切ったパネル、グリッチの帯、シンセウェーブ風の地平線、回路の配線 |
| ポップ | 太い`INK`の縁取り、ずらした影（`hard_shadow`）、網点、ステッカー風の図形 |
| ドット | 全画像を低い解像度で描いて最近傍で拡大。UI の文字はドットフォント DotGothic16 |

色はすべて `ArtSource/palette.py` から取っているので、ここの値を変えて書き出し直せば、生成物の配色がまとめて変わります。

**コマンド(コード上はカード)はカートリッジです。** 下端に金の接点ピンと、差し込む向きを決める切り欠きがあります。実行リストは*命令メモリ*の基板です（接点ピンの付いた受け皿、見出し `// COMMAND MEMORY`、番地 `0x00`〜`0x02` のソケット）。カードをセットすると、命令をメモリに書き込む演出が出ます（下の「演出」を参照）。

## 置いてある絵はすべて仮素材

どの画像もコードで描いています（Python で SVG を組み立て、resvg で PNG にしています）。差し替える前提の素材です。キャラクターは 64×64 のドット絵で、コマ送りで動きます（下の「キャラクター」を参照）。プレイヤーはショートカットの銀髪にヘッドセットのサイバーな女の子、敵は車輪付きのトロイの木馬（マゼンタ）、根を張るフードのルートキット（オレンジ）、浮かぶ単眼ドローンのスパイウェア（黄）、子機を従えたロボットのボットネット（紫）です。

## 背景と画面の形

背景は、16:9・16:10・21:9 のどれでも引き伸ばさずに済むよう、2560×1440 で描いています。中央の 1920×1080 が基準の構図です。`background.py` は、意図して配置するもの（太陽、ステッカー、網点）をこの安全領域（`OX`、`OY`）を基準に置き、空・ビル街・床のグリッドは画像の端まで続けて描きます。警告色のバーは背景には**入れていません**。どの比率でも画面の端に貼り付く必要があるので、別の 9-slice の画像にしてあります。この決まりの元は `CLAUDE.ja.md` の「uGUI の組み方」です。

## 書き出し直す

```powershell
pip install -r ArtSource/requirements.txt
python ArtSource/build.py                 # 全部
python ArtSource/build.py cards ui        # 一部だけ: background ui cards characters effects
python ArtSource/preview.py UI out.png    # Assets/Art/ 以下の1フォルダを1枚に並べて目視確認
```

`build.py` は `Assets/Art/` 以下の**決まったファイル名**に書き出します。上書きしても `.meta` は残るので、GUID（その画像へのすべての参照）もインポート設定も書き出し直しの前後で変わりません。`ArtSource/assets/` の下はグループごとに1モジュール、画像ごとに1関数です。

画像はすべて**表示サイズの2倍**で描いています。そのため 9-slice の Image は `Image.pixelsPerUnitMultiplier = 2` を使い、Simple の Image は RectTransform のサイズで大きさを決めます。

**ドット感。** 各グループのモジュールには `PIXEL` 定数があり、ドット1粒が書き出し画像の何画素になるかを表します。値は 背景 4、UI 6、カード 6、エフェクト 8 です。キャラクターはドット単位で直接描くので（下の「キャラクター」）`PIXEL` を持ちません。`svgkit.render(..., pixel=N)` は、1/N の解像度でアンチエイリアス無し（`shape_rendering="crisp_edges"`）に描き、`Image.NEAREST` で元の大きさへ拡大します。`PIXEL` を 1 にすると、そのグループはなめらかな絵に戻ります。

低い解像度で描く副作用として、細かい部分が消えます。1画素の線、薄い走査線、細かいグリッドは、消えるか縞模様のむらになります。細部は1ドット以上の太さで描いてください。そうしないと残りません。

## インポート設定（`Assets/Scripts/Editor/ArtImportPostprocessor.cs`）

`Assets/Art/` 以下のテクスチャは、ミップマップ無しの単体の UI スプライトとして取り込まれます。

- **9-slice の枠:** このファイル内の表（キーはファイル名）が持ちます。**`ArtSource/assets/ui.py` の角の大きさと一致させる必要があります**。片方を変えたら、もう片方も直してください。
- **敷き詰める画像:** `TiledSprites` にある名前（今は `Image.Type.Tiled` で並べるメモリの接点 `ui_mem_pins`）は `TextureWrapMode.Repeat` になります。

この処理が働くのは、`.meta` がまだ無い**初回**のインポートだけです。これは意図的なもので、上書きした PNG や、Inspector で誰かが直した設定には手を出しません。その代わり、表を書き換えても、すでに取り込み済みの画像には反映されません。Sprite Editor で直すか、`run_script` からその画像のインポーターに対して `ArtImportPostprocessor.Apply` を呼んでください。

## どの画像がどこに刺さっているか

| 何 | ファイル | 割り当て先 |
| --- | --- | --- |
| 画面の端のバー | `UI/ui_hazard_bar.png`（9-slice：両端のストライプは残し、真ん中を画面幅まで伸ばす。下のものは上下反転） | `BackCanvas/TopBar`（両シーン）、`BackCanvas/BottomBar`（TitleScene） |
| バトル背景 | `Backgrounds/bg_battle.png`（2560×1440。下記参照） | BattleScene の `BackCanvas/Background` |
| タイトル背景 | `Backgrounds/bg_title.png`（同じ世界で、地平線を上げ、ロゴの後ろに大きな太陽） | TitleScene の `BackCanvas/Background` |
| レアリティ別のカード枠 | `Cards/Frames/card_frame_{normal,rare,epic}.png`（シアン/マゼンタ/ホログラムの黄） | `ScriptableObjects/Visuals/DefaultCardFrameSet.asset`（`CardFrameSet`）。`CardView.prefab` / `CommandCardView.prefab` がこれを参照 |
| カード裏面（山札・捨て札のアイコン） | `Cards/Frames/card_back.png` | BattleScene の `DeckCountView` / `DiscardCountView` |
| カードイラスト | `Cards/Illustrations/<CardData.Id>.png` | 各カードの `CardData._illustration` |
| 敵の立ち絵（アイコン） | `Characters/<EnemyData.Id>.png`（アイドルの1コマ目） | `EnemyData._portrait`。対象の候補一覧とログのアイコンにも使う |
| プレイヤーの立ち絵（アイコン） | `Characters/Player.png`（アイドルの1コマ目） | BattleScene の `PlayerUnitView/Portrait` と MapScene のプレイヤー |
| キャラクターのアニメ | `Characters/<Name>/<Name>_{idle,action,hit}_<n>.png` | `ScriptableObjects/UnitVisuals/<Name>.asset`（`SpriteSheetUnitVisualDefinition`）。`EnemyData._visual` と `BattleSceneInitializer._playerVisual` が参照する |
| UI パーツ | `UI/ui_*.png` | BattleScene と各プレハブの Image に直接 |
| 命令メモリの受け皿 | `UI/ui_tray.png`、`ui_slot.png`、`ui_mem_pins.png` | `Canvas/CommandTray`（+ `Slot1..3`、`Pins`、`Header`） |
| 書き込み演出 | `UI/ui_write_scan.png`、`ui_glow_frame.png` | `CommandCardView.prefab` の `WriteScan` / `WriteFlash` |
| 実行ボタン | `UI/ui_button_primary.png`、`ui_button_outline.png`、`ui_button_mask.png`、`ui_ripple_ring.png` | `Canvas/ExecuteButton`（+ `Echo1..3`、`RippleMask`） |
| 対象の候補一覧 | `UI/ui_panel.png`、`ui_slot.png`、`ui_glow_frame.png` | `Canvas/TargetPicker`、`Prefabs/TargetOptionView.prefab` |
| メモリー残量ゲージ | `UI/ui_mem_cell.png` | `Prefabs/MemoryCellView.prefab`（`Canvas/PlayerStamina/Gauge` の `SegmentGaugeView` が使う） |
| ブロックのバリア | `UI/ui_barrier.png`、`ui_barrier_crack_{1,2,3}.png`、`ui_block_badge.png` | `UnitView.prefab` の `Barrier` |
| マップ背景 | `Backgrounds/bg_map.png`（地平線が低い） | MapScene の `BackCanvas/Background` |
| マップの部屋 | `UI/ui_map_room_frame.png`（白。状態の色を乗せる）、`ui_map_room_fill.png` | `Prefabs/MapCellView.prefab`。配線は `Prefabs/MapLinkView.prefab` |
| シーン遷移 | `UI/ui_transition_slice.png` | `Resources/SceneTransitionEffect.prefab` |
| デッキ編集・カード購入の背景 | `Backgrounds/bg_deck_edit.png`、`bg_shop.png`（地平線をかなり低くして、パネルが空に乗るように） | デッキ編集とマーケットがマップ上のウインドウになったので、今は使われていない |
| チップ（コード上はカードパック） | `Cards/Packs/<CardPackData.Id>.png`（ICチップ。両側に金のピン、光るダイ、チップごとの色。ダイの紋章はスターター・プレミアムが稲妻、アタックが刃、リペアがプラス、ハックが `</>`） | `CardPackData._illustration`、パックごとに1枚。`CardPackView` と `CardShopPurchaseView` が表示する |
| 一覧の行 | `UI/ui_row_fill.png`（暗い地。伸ばすので模様なし）、`ui_row_frame.png`（白。選択状態の色を乗せる） | `Prefabs/CardPackView.prefab` |
| ゴミ箱のアイコン | `UI/ui_icon_trash.png`（白。色は `Image.color` でシアンを乗せる） | `Prefabs/DeckEditWindow.prefab` の `DeckPanel/ClearButton` |
| 札と見出しの線 | `UI/ui_tag.png`（枚数・価格・所持トークン）、`ui_section_line.png`（パネルの見出しの下） | `DeckEditWindow.prefab`、`CardShopWindow.prefab`、`DeckEditCardView.prefab`、`CardPackView.prefab` |
| 主なボタン | 実行ボタンと同じ画像をプレハブにしたもの | `Prefabs/CyberButton.prefab`（`DeckEditWindow.prefab`、`CardShopWindow.prefab`、ConfirmDialog） |
| パックのインストール | `UI/ui_install_socket.png`、`ui_install_chip.png`、`ui_install_glow.png`(白。レアリティの色を乗せる)と、`ui_ripple_ring`、`ui_bar_*` | `Prefabs/ChipInstallWindow.prefab`(`CardShopWindow.prefab`とMapScene)の`CardArea/Install`。カードの裏面と、レアリティの縁(`ui_glow_frame`)は`Prefabs/ShopResultCardView.prefab` |
| 確認ウインドウ | `UI/ui_panel.png`、`ui_section_line.png`、`CyberButton` 2つ | `Prefabs/ConfirmDialog.prefab`（MapScene） |
| トークンのアイコン | `UI/ui_token.png`（金の六角形のデータトークン。文字なし） | MapSceneの`Canvas/Tokens/Icon`、`CardShopWindow.prefab`の`Window/Credit/Icon`、`Prefabs/StageRewardWindow.prefab`の`TokenPanel/Icon` |
| 報酬の放射光 | `UI/ui_reward_rays.png`（白。レアリティ色・トークンの金・支援の緑を乗せる） | `Prefabs/StageRewardWindow.prefab`の`Window/Rays`（MapScene）。カードの縁は`ui_glow_frame` |
| トースト | `UI/ui_toast_panel.png`、`ui_toast_icon.png` | `Resources/MessageToast.prefab` |
| パーティクルのテクスチャ | `Effects/fx_*.png` + `Effects/Materials/*.mat` | `UnitView.prefab` の `DamageBurst` / `HealBurst`、`BackCanvas/AmbientSparkles` / `AmbientShards` |
| ポストエフェクト | `Assets/Settings/CyberVolumeProfile.asset`（ブルーム、色収差、ビネット） | BattleScene と TitleScene の `PostProcessVolume`。かかるのは Main Camera、つまり背景の層だけ |
| タイトルロゴのずらし影 | `Assets/Fonts/DotGothic16/DotGothic16 PopShadow.mat`（フォントのマテリアル＋マゼンタの Underlay をずらしたもの） | TitleScene の `Canvas/TitleText` |

`CardView`・`BattleUnitStatusView`・`BattleUnitEffectView`・`BattleCommandCardView` は、これらの割り当て先をすべて省略可能として扱います。未設定のところは、その装飾が出なくなるだけです。

## フォント

すべての文字は `Assets/Fonts/DotGothic16/DotGothic16-Regular SDF.asset` を使っています。TMP の既定フォントもこれです。

- **フォント:** Google Fonts のドットフォント DotGothic16 で、ライセンスは SIL OFL（`OFL.txt` を同じ場所に置いてあります）。
- **アセットの設定:** 動的生成、複数アトラス対応で、**フォールバックはありません**。無い字は空白で表示されるので、ローカライズテーブルについては `FontCoverageTests` が確かめています（`CLAUDE.md` 参照）。
- **マテリアルの Face Dilate を 0.2 にしてあります。** DotGothic16 には、線の太さが1ドットしかない字があります（`I`、`ー`、`6` の横棒など）。縮小表示するとその線が1画素を割り、エラーも出ずに消えます（実際に「6ダメージ」が「1ダメ ジ」と表示されていました）。Dilate はすべての線を太らせるための設定です。このフォントで字が欠けて表示されたら、まずここを疑ってください。
- **カードの説明文は折り返し**（`TextWrappingModes.Normal`）、14〜22 の範囲で自動で縮めます。上限を 22 にしてあるのは、短い説明（「6ダメージを与える」）の最後の1文字だけが次の行に落ちず、1行に収まるようにするためです。
- **太字は使いません。** ドットフォントに疑似太字をかけると潰れるので、各テキストの `Bold` は外してあります。
- **最小サイズ。** ドットフォントは画面上でおよそ 16px 以上ないと読めません。そのため `CommandCardView.prefab`（0.6倍で表示）は**説明文を非表示**にして、名前とイラストだけを見せています。説明の全文は手札のカードで読めます。

## キャラクター（`ArtSource/assets/characters.py`、`ArtSource/pixelkit.py`）

キャラクターは 64×64 のキャンバスに `pixelkit.Sprite` で**ドット単位で**描き（四角・楕円・1ドット、アンチエイリアス無し）、最近傍で 8 倍の 512×512 に拡大します。SVG のグループと違って図形からラスタライズしないので、ポーズを変えると動かしたいドットだけが動きます。コマ送りにはこれが要ります。

- **層。** `Sprite` が本体で、`outline(INK)` が外周に 1 ドットの `INK` のフチを付けます。`Sprite.fx`（ビーム・輪・火花）はフチの後に描くのでフチが付きません。`Sprite.under` は足元のホログラムの台座です。
- **ポーズは引数。** キャラクター 1 体が 1 つの関数（`girl(eyes, arm, bob, lean, hair, mouth)`、`trojan(...)` など）で、各コマは引数を変えた呼び出しです。被弾のコマは `glitch_hit` が共通で作ります。1 コマ目はのけぞり、行の帯を 2 か所横にずらし、白く飛ばします。2 コマ目は戻りかけです。
- **全員右向き。** 敵は画面上で `Portrait` の x スケール -1 によって反転します。
- **書き出し。** 1 体ごとに `Characters/<Name>/<Name>_idle_<n>.png`（ループ）、`_action_<n>` と `_hit_<n>`（1 回再生）、それにアイコン用にアイドルの 1 コマ目を写した `Characters/<Name>.png`。今はアイドルがプレイヤー 8 コマ・敵 4 コマ、行動 4 コマ、被弾 2 コマです。
- **コマ数を変えたら** **Tools > Art > Rebuild Unit Visuals**（`Editor/UnitVisualDefinitionBuilder.cs`）を実行します。各 `Characters/<Name>/` のコマを `ScriptableObjects/UnitVisuals/<Name>.asset` に並べ直し、`Id` が `<Name>` の `EnemyData` に見た目が未設定なら割り当てます。

**見た目の差し替え。** バトルが触るのは `IUnitVisual`（`Common/Views/UnitVisuals/`：`PlayAction`、`PlayHit`、`SetDefeated`、`SetColor`）だけで、それを `UnitVisualDefinition` アセット（`Common/ScriptableObjects/UnitVisuals/`）が作ります。コマ送りは `SpriteSheetUnitVisualDefinition` → `SpriteSheetUnitVisual` で、時間の進め方は素の C# の `FrameAnimationPlayer`（EditMode テストあり）です。アイドルはループ、行動と被弾は 1 回再生してアイドルへ戻り、被弾は行動を上書きし、倒れたユニットは被弾の最後のコマで止まります。本番のスプライトシートも同じアセットに差し込めます。Live2D など別方式にするときは、自分のオブジェクトを組み立てて `IUnitVisual` を返す `UnitVisualDefinition` のサブクラスを作り、`EnemyData._visual` / `_playerVisual` をそれに向けるだけで、ほかは変わりません。見た目は `Portrait` の枠の中に作られるので、左右反転と被弾の揺れはそのまま効きます。色（被弾の点滅・倒れたときの灰色）は `SetColor` で届きます。

## 演出

**HPの増減（`BattleScene/Views/BattleUnitEffectView.cs`）。** `UnitPresenter` がHPの増減量をそのユニットの View に渡し、View が4つの演出を出します。揺れ、フラッシュ、浮かび上がる数字（`FloatingNumberView.prefab`）、パーティクルです。どれも `UnitView.prefab` にあります。被弾時のフラッシュは**色ではなく透明度の点滅**です。`Image.color` はスプライトに掛け算されるので、色を乗せると絵の色によっては濁るためです。

**命令の書き込み（`BattleScene/Views/BattleCommandWriteEffectView.cs`、`CommandCardView.prefab` に付いています）。**

- **カードが実行リストに入ったとき**（使い回しの View が有効化された `OnEnable`）は「差し込み」を見せます。1.3倍から縮みながらフェードインし、走査線が上から下へ走り、最後に縁がシアンに光ります。
- **カードが別の枠へ移ったとき**（並べ替えや、実行・手札へ戻したあとの前詰め）は、走査線と発光だけを出します。これが「書き換え」です。番地が変わったことを知っているのは`BattleCommandListPresenter`だけなので、Presenterが`MoveToSlot`から呼びます。

この演出が触るのはスケール、`CanvasGroup`、自分の子オブジェクトだけで、**`anchoredPosition` には触りません**。カードの位置は Presenter が決めるためです。

**カードの動き（`BattleScene/Views/BattleCardMotionView.cs`、`CardView.prefab` と `CommandCardView.prefab` の両方に付いています）。** Presenter が目標を渡し、そこまでの動きはこのコンポーネントが担当します。動きはすべて「デジタル」です。移動中の位置は `_step` ピクセルの格子に吸着し、登場と退場はブラウン管のような横一線を経由して、明滅と横ぶれを伴います。

| 動き | 見え方 |
| --- | --- |
| 移動・並べ替え | 格子に沿ってカクカクと新しい位置へ滑る |
| 選択 | カードが浮き、シアンの `SelectGlow` の縁が脈打つ。切り替わった瞬間に一瞬揺れる |
| 登場（引いた・戻ってきた） | 山札（または実行リスト）から線に潰れた状態で明滅しながら飛んできて、着いた場所で開く |
| 退場 — `Upload` | 潰れて実行リストへ吸い込まれる |
| 退場 — `Discard` | 潰れて捨て札へ飛ぶ |
| 退場 — `Return` | 潰れて手札の方へ落ちる |
| 退場 — `Execute` | `ExecFlash` の板が白く光ってカードが膨らみ、潰れて上へ抜ける |

飛んでいる途中のカードは押せません（`CanvasGroup.blocksRaycasts`）。退場し終えた View は、コールバックで Presenter のプールへ戻ります。

**ウインドウ（`Common/Views/UI/WindowTransition.cs`）。** すべてのウインドウが使う、共通の開閉の動きです（決まりは [docs/tone-and-manner.ja.md](tone-and-manner.ja.md)）。必須なのはウインドウの `RectTransform` と `CanvasGroup` で、暗くする板、ノイズの帯（ウインドウの下に置いた素の `Image` の帯。`_glitchDuration` が 0 より大きいときだけ使い、既定では使わない）、`CollapseFlash`（ウインドウ全体を覆う薄いシアンの `Image` を一番手前に置いたもの。潰れた枠が光る線に見えるのはこれのおかげ）、電子の粒は任意です。粒はマップのプレイヤーの粒を複製し、形をウインドウ幅の `SingleSidedEdge`、ソーティングレイヤーを `UI_Popup_Effect` にしたもので、ウインドウの GameObject の**外**に置く必要があります。

**ボタン（`Common/Views/UI/ButtonRippleEffect.cs`）。** 実行ボタンは、半透明のシアンの平行四辺形（`ui_button_primary`）です。このコンポーネントはどの `CommonButton` にも付けられ、同じ反応が得られます。

- **カーソルを合わせている間:** ボタンと同じ形の輪（`ui_button_outline`）が、繰り返し外へ広がって消えます。
- **押したとき:** 押した位置から円の波紋（`ui_ripple_ring`）が広がり、`ui_button_mask` を使った `Mask` でボタンの形に切り抜かれます。同時に面が光り、外へ強めの輪が出ます。
- **押せない間:** `CanvasGroup` でボタン全体を暗くします。そのため Button 自身の「押せないときの色」は白にしてあります。両方を効かせると二重に暗くなるためです。

時間は実時間で進め、1フレームあたりの上限を付けています。`timeScale` で UI の反応が遅くならないようにするためです。

斜めのスプライトは、斜めの辺が収まるよう左右の枠を広く取って 9-slice にし、表示するボタンの高さぴったり（216px ＝ 表示 108px）で描いています。**縦には伸ばさないでください。** 9-slice で中段のセルが伸びると、斜めの線がそこで折れ曲がります。同じ理由で、カーソルを合わせたときの輪は大きさを変えずに、全体を等倍に拡大（`localScale`）しています。

**パーティクル。** Screen Space - Camera の Canvas の下に置いたパーティクルは普通のレンダラーなので、ルートドキュメントの Canvas のルールがそのまま当てはまります。

- GameObject のレイヤー（`UI_Default`、`UI_Back`）で、どのカメラに映るかが決まります。
- Sorting Layer（`UI_Default_Effect`、`UI_Back_Effect`）で、その層の UI より手前に出ます。
- すべて `ParticleSystemScalingMode.Hierarchy` なので、大きさや速度の単位は Canvas のピクセルです。

## 未対応

- BattleScene・TitleScene・MapScene のボタンは `CyberButton.prefab` を作る前の複製なので、プレハブを変えてもそちらには反映されない。
- プレイヤーにはデータアセットが無い。見た目は `BattleSceneInitializer._playerVisual` とシーンの `Portrait` のスプライトで決まる。
- マップのプレイヤーは静止画（アイドルの1コマ目）のままで、動かない。

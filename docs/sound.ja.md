# サウンド

ゲームの音(`SoundSource/` → `Assets/Audio/`、鳴らすのは `Common/Audio/`)の詳細ドキュメントです。`CLAUDE.md` から読み込まれます(日本語版は [CLAUDE.ja.md](../CLAUDE.ja.md))。音が*どうあるべきか*は [docs/tone-and-manner.ja.md](tone-and-manner.ja.md) の「音」にあり、ここはその作り方とつなぎ方です。

## すべて仮素材で、コードで生成している

アートと同じく、どの音も Python(`numpy` だけ)で合成し、**決まったファイル名**で書き出しています。本番の音に替えるときは、そのファイルを上書きするだけです。

```powershell
pip install -r SoundSource/requirements.txt
python SoundSource/build.py            # 全部
python SoundSource/build.py sfx        # 効果音だけ
python SoundSource/build.py music      # BGM だけ(20秒ほどかかる)
```

| ファイル | 中身 |
| --- | --- |
| `SoundSource/synth.py` | 部品: 矩形波・三角波・ノコギリ波・スーパーソウ・ノイズ、エンベロープ、FFT のローパス/ハイパス、短い音用の時間で動くローパス、エコー、小さなリバーブ、ビットクラッシュ、WAV の書き出し |
| `SoundSource/sfx.py` | 効果音ごとに1関数。`SOUNDS` のキーから `Assets/Audio/SE/se_<キー>.wav` を書き出す |
| `SoundSource/music.py` | 曲ごとに1関数。`SONGS` → `Assets/Audio/BGM/bgm_<キー>.wav` |

キーは `SeId` / `BgmId` の名前をスネークケースにしたもので(`CardSelect` → `se_card_select.wav`)、`SoundLibrary` はこの決まりで埋めています。

**BGM** は、4小節で一巡するコード進行に、パッド・ベース・アルペジオ・ドラム・リードを重ねて作ります。メロディはコードからシード固定で作ります。強拍にコードの音、その間を音階の音でつなぎ、A B A B′ のフレーズに並べます(B′ は主音で終わる)。書き出しは後ろに3秒はみ出させ、その分を曲の頭に重ねてからループの長さで切るので、残響がループの継ぎ目をまたいで続きます。既存の曲からの引用やサンプリングはしていません。

| 曲 | テンポ・調 | 雰囲気 |
| --- | --- | --- |
| `bgm_title` | 100、イ短調 | 夢っぽいパッドとチップのアルペジオ。リードは後半だけ |
| `bgm_map` | 92、D ドリアン | 探索: ベルのメロディ、軽いビート |
| `bgm_battle` | 150、ニ長調(I→V→vi→IV)、32小節 | トランス寄りで明るい。Aメロ(16分で転がるベース、16分のアルペジオ、リード)→ 控えめなクラップとオフビートの和音が加わる → ビルドアップ(ベースは伸ばし、小さめのスネアの連打が4分から16分へ詰まり、ノイズが上がり、キックが抜け、最後に半拍の無音)→ サビ(シンバル1回、キックに合わせて`sidechain`でうねるスーパーソウの和音、1オクターブ上のリードをノコギリ波で重ねる)。ドラムは伴奏にとどめる |
| `bgm_deck_edit` | 88、ハ長調 | 落ち着いた作業: maj7 のパッド、ゆっくりした三角波のアルペジオ。デッキ編集がマップ上のウインドウになってからは未使用(マップの曲が流れ続ける)。`bgm_shop`もマーケットがウインドウになって同じく未使用 |
| `bgm_shop` | 116、ヘ長調 | 根音と5度で弾むベース、手拍子、明るいリード |

`BgmId.BossBattle`(ボス戦)は**生成しません**。曲はユーザーが用意して`SoundLibrary`で割り当てます。割り当てるまでは`SoundLibraryTests`が未設定として報告します。

**取り込み設定**は `Editor/AudioImportPostprocessor.cs` が初回だけ行います(アートと同じ)。SE は読み込み時に展開(ADPCM)、BGM は Vorbis のストリーミングです。このファイルを変えても取り込み済みの音には効かないので、`run_script` から `AudioImportPostprocessor.Apply` を呼んでください。

## 鳴らすのは `AudioUseCase` だけ

`Common/Audio/` は `Message/` と同じ形の機能フォルダです。

- **入口は `AudioUseCase.PlaySe(SeId)` / `AudioUseCase.PlayBgm(BgmId)` だけです。** View・Presenter・UI の部品はここを呼び、再生の仕組みには触れません。再生中でなければ何もしないので、EditMode のテストが音を鳴らす経路を通っても `DontDestroyOnLoad` で落ちません。
- その裏の `SoundPlayer`(`internal`)は、自分で生成して `DontDestroyOnLoad` で残るオブジェクトです。`Resources/SoundLibrary.asset` を読み、効果音用の10本を順番に使い回し、BGM は2本でクロスフェードします。同じ効果音は `SoundLibrary._seMinInterval`(0.04秒)より短い間隔では鳴らし直さないので、カーソルを滑らせても音が団子になりません。
- `SoundLibrary` は、それぞれの id に音・音量・(効果音なら)少しの高さの揺らぎを割り当てます。SE / BGM 全体の音量とクロスフェードの長さもここにあります。**音の設定はすべてこの1アセットで行います。**
  - **行は enum の値の名前で持ちます**(数値ではありません)。`SeId` / `BgmId` の途中に値を足しても割り当てはずれません。(`CommonButton`・`WindowTransition`・`SceneBgm` などは今も数値で持っているので、新しい値はやはり末尾に足します。)
  - **Inspector**(`Editor/SoundLibraryEditor.cs`)は全 id(`None` を除く)を enum の順に並べ、用途ラベル・音・音量・高さの揺らぎと、▶ 試聴ボタン(Edit モードでも鳴り、アセットの音量で再生)を出します。音の無い id は赤く表示され、音を入れるとその行が追加されます。enum に無い名前の行や同じ名前が重なった行は警告に並び、ボタンで削除できます。
  - **重ねて鳴らす2つ目の音**(`Clip2`・`Volume2`・`Delay2`)を効果音ごとに設定できます。入っていれば `SoundPlayer` が1つ目と一緒に別の声で鳴らします。ピッチの揺らぎは同じ値で、`Delay2` 秒遅らせて鳴らせます(`AudioSource.PlayDelayed`)。`StopSe` は両方を止め、0.04秒の連打の間引きも id 単位なので、2つがずれることはありません。重ねた効果音は10本の声のうち2本を使います。▶ の試聴でも両方が鳴ります。
  - **選んだら試聴.** 上部の**選んだら試聴**(マシンごとに`EditorPrefs`で覚える)がオンなら、音・音 2 の欄で音を選ぶと(アセット一覧でもドラッグ＆ドロップでも)、その音だけをその行の音量で鳴らします。「なし」を選ぶと止めます。最後に選んだ・試聴した行は、ショートカット **A**(`Live2DCardRogue/SoundLibrary 重ねて試聴`。Edit > Shortcuts で変更可)で、重ねる音(音 2 と遅延)ごと鳴らし直せます。効くのは Inspector かアセット一覧にフォーカスがあるときだけで、文字の入力中(一覧の検索欄など)は効きません。Timeline・Animation・Curve Editor・Scene ビューの A はその画面だけの割り当てなので、そちらでは向こうが優先されます。
  - **用途ラベル**は enum の各値に付けた `[SoundUsage("…")]`(`SoundUsageAttribute`)です。id を足したら書きます。
  - `SoundLibraryTests`(EditMode)は、音の無い id、enum に無い名前や重複した行、用途ラベルの無い id で失敗します。どれも実行時には「鳴らない」だけでエラーにならないためです。
- **BGM はシーンごとに `SceneBgm` 部品で決めます**(各シーンの `SceneBgm` という名前のルートオブジェクト)。`Start` で自分の曲を頼み、同じ曲が流れていれば何もしないので、同じ曲のシーンを行き来しても頭から鳴り直しません。
  - **戦闘の曲はステージが決めます。** `StageData._bgm`(既定は`Battle`。エピソード1のStage10、ボスのランサムウェアは`BossBattle`)が`StagePlan.Bgm`と`BattleSceneTransitionData.Bgm`で渡り、`BattleSceneInitializer`がシーンの`SceneBgm`に`Override`で渡します(単体再生では`_bgmMock`)。チャレンジのランダムなステージはいつも`Battle`です。`SceneBgm`は実行順を遅らせてあり(`[DefaultExecutionOrder(100)]`)、Initializerの`Start`が差し替えてから鳴らします。既定の曲を先に鳴らして次のフレームで差し替えると、前のシーンの曲がクロスフェードの途中で切れるためです。

**音を止める。** `AudioUseCase.StopSe(id)`は、その効果音を鳴らしているものをすべて止めます。要るのは長い効果音(チップのインストールの溜め)だけで、演出を飛ばしたときに鳴り残らないようにしています。

## どこで鳴らしているか

決まり: 受け取った**入力そのもの**の音は View が、自分が決めた**結果**の音は Presenter が鳴らします。

| 音 | 鳴らすところ |
| --- | --- |
| ボタンのホバー・クリック、押せないボタンを押したときの `Error` | `CommonButton` 自身(`_hoverSe`・`_clickSe`。`Editor/CommonButtonEditor.cs` で Inspector から変えられる)。役割のあるボタンは名前で設定済み: 実行・OK・保存・購入・キャンペーン → `Confirm`、キャンセル・戻る → `Cancel`、デッキ編集の + / − → `MemoryWrite` / `CardReturn` |
| ウインドウの開閉 | `WindowTransition`(`_openSe`・`_closeSe`)。すべてのウインドウで同じ |
| シーン切り替えのグリッチ | `SceneTransitionEffect`(遷移が始まるとき) |
| 手札を選んだ(`CardSelect`)、使えないカードを指した(`Hover`)、クリックを断った(`Error`)、何も無い所のクリックで選択を取り消した(`Cancel`) | `BattleCardHandPilePresenter` |
| メモリーへ書き込んだ(`MemoryWrite`)、戻した(`CardReturn`)、実行した(`CommandExecute`) | `BattleCommandListPresenter`(カードの消え方を選ぶのと同じ場所) |
| 敵が生成された(`MemoryWrite`) | `EnemySummonPresenter`(BattleScene)。Viewを出すとき |
| 敵がコマンドを使った(`EnemyAction`。濁って下がるブズッという音で、プレイヤーの`CommandExecute`とは別) | `BattleActionPresenter`(`BattleCommandExecuteUseCase.OnCardExecuting`で、使ったのがプレイヤー以外のとき) |
| ユニットのHPが減った(`Hit`。種別が`TakePoisonDamage`、つまりウイルス・ワームの継続ダメージなら`VirusTick`)、増えた(`Heal`。リジェネも) | `UnitPresenter`(浮かぶ数字と一緒に) |
| 継続効果が付いた・重ねがけされた(`StatusApply`。ブロックは専用の音があるので除く) | `UnitPresenter`(浮かぶ効果名と一緒に) |
| ブロックを張った・重ねた(`BlockGain`)、攻撃を防いで残った(`BlockAbsorb`)、0 になって割れた(`BlockBreak`) | `BattleBlockBarrierView`(壁の演出を選ぶのと同じ場所) |
| 一覧のカードをクリックした(`CardSelect`) | `DeckEditCardView`、`CardPackView` |
| 確認ウインドウの背面クリック(`Cancel`) | `ConfirmDialogView`(ボタンは自分で鳴る) |
| チップのインストール: 読み込みの間に上がっていく溜め(`InstallCharge`、1.9秒。チップが弾けたときと演出を飛ばしたときに`AudioUseCase.StopSe`で切る)、光の色が上がるたびの音(レアは`InstallPromote`、エピックは`InstallPromoteEpic`) | `ChipInstallView` |
| コマンドをめくる音(`CardFlip`。音程のあるブリップは入れず、ノイズだけのシャカッという擦れ)、開封演出を飛ばした(`Click`) | `ChipInstallWindowView` |
| ステージ報酬のファンファーレ(`Reward`)、敗北時のバックアップブースト(`BackupBoost`)、それを飛ばした(`Click`) | `StageRewardView` |
| ランクのデコード(`RankReveal`)、確定(`Confirm`。A以上は`Reward`)、リザルトの行がすべり込む(`TokenTick`) | `ResultView` |
| トークンの数え上げの1刻み(`TokenTick`) | `TokenCounterView`(`_tickSe`)。報酬ウインドウとマップのトークン表示 |

## 未対応

- あるのは操作の音だけです。戦闘の結果(攻撃・ブロック・ウイルス)、パックの開封演出、勝敗の瞬間はまだ無音です。
- BGM は短い手続き生成のループで、仮の曲です。

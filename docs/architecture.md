# 🏛️ 設計資料

このプロジェクトのコード設計の要点。仕様の詳細と設計判断の経緯は [CLAUDE.md](../CLAUDE.md)（日本語版 [CLAUDE.ja.md](../CLAUDE.ja.md)）と各シーンの資料（[docs/](./)）にある。

## 🗺️ シーン構成

```
TitleScene ──▶ MapScene ⇄ BattleScene
                  │
                  └──▶ EndingScene ──▶ ResultScene ──▶ TitleScene
```

| シーン | 役割 |
| --- | --- |
| 🏠 Title | モード選択（ストーリー / チャレンジ / コレクション / オプション） |
| 🧭 Map | ステージ進行。デッキ編集・マーケット・強化はこの上のウインドウ |
| ⚔️ Battle | ターン制カードバトル。単体で再生しても動く |
| 🎬 Ending / 🏆 Result | クリア演出とスコア・ランク |

## 🧱 レイヤー（MVP + UseCase）

```
Model ──event──▶ Presenter ──▶ View
  ▲                               │
  └──── UseCase ◀──── 入力 ───────┘
```

- 📦 **Model** — 状態と `event Action<...>` だけを持つ。**UnityEngine に依存しない**。
- ⚙️ **UseCase** — プレイヤー操作に応じて Model を変更する手順。
- 🔌 **Presenter** — Model のイベントを View に流す。すべて `PresenterBase` を継承し、
  コンストラクタは保持のみ → `Initialize()` で購読 → `Dispose()` で解除。言語切替は `OnLocaleChanged()`。
- 🖼️ **View** — `MonoBehaviour`。表示と入力の転送だけ。ルールは持たない。
- 🧩 **ViewContainer** — シーンの View 参照を Inspector で束ねる。
- 🏗️ **Initializer** — シーンのコンポジションルート。すべてを `new` して配線し、`OnDestroy` で破棄する。

## 📁 フォルダ

```
Assets/Scripts/
├── Common/        シーンに依存しない共通コード
│   ├── Models/ Views/ Presenters/ ScriptableObjects/   ← レイヤー
│   └── SceneTransition/ WindowSystem/ Save/ Audio/ …   ← 自己完結した機能
├── Scenes/<Scene>/   シーン固有（Models/ UseCase/ Presenters/ Views/ Initializer/ …）
├── Window/<Name>/    ウインドウ 1 つにつき 1 フォルダ
├── Editor/           エディタ拡張
└── Tests/            EditMode / PlayMode
```

🚦 **Scene も Window も Common にだけ依存する**。
シーン同士、Window からシーン、Common から Scene / Window への参照は禁止で、`SceneDependencyTests` が検出する。
シーンはウインドウを `Common/WindowSystem/` の InitData とハンドラ越しに扱うので、Window の型を知らずに済む。

## 🔀 シーン遷移

- `SceneTransitionUseCase.LoadScene(data)` だけを使う（`SceneManager` を直接呼ばない）。
- 遷移データ（`SceneTransitionData`）が**行き先を自分で持つ**。1 型 = 1 行き先。型は `Common/SceneTransition/TransitionData/` に置き、どのシーンにも属さない。
- 受け取り側の Initializer は `SceneInitializerBase<TData>` を継承し、`Initialize(TData)` で受け取る（単体再生時は `null` → モックで代替）。
- 戻り値は「どう包んで返すか」を呼び出し側が関数で渡す（`CreateReturnData`）。呼ばれた側は戻り先を知らない。
- 🧊 シーンをまたぐ ScriptableObject は **ID で渡し**、`GameAssetRepository.Hold` でアンロードされないよう保持する。
- 💾 シーンをまたいで残る状態は `Common/Repositories/` に置く。
- ✨ 切り替え中は `SceneTransitionEffect` のグリッチが画面を覆い、フリーズを見せない。

## 🪟 ウインドウ

ウインドウはシーンに配置せず、**生成する**。

```
InitData ──▶ WindowUseCase.Create ──▶ Handler.Show(showData)
                │
                └─ WindowCatalog から prefab を引き、PopupCanvas に生成
```

- 📝 **InitData** — 開くのに必要な全部（コールバック、Model、確認ダイアログのハンドラ）。
- 📬 **ShowData** — 開くたびに渡すもの。型で縛られ、間違えるとコンパイルエラー。
- 🎛️ **Handler** — `Show` / `Hide` / `Dispose`。シーンの Initializer が `CreateWindows()` でまとめて作る。
- ✅ 確認ダイアログは 1 つを共有し、答えは `ConfirmShowData` のコールバックで返る。
- 📋 ボタンが並ぶだけのウインドウは `MenuWindowInitData` で作る（新しい View 不要）。
- 🌫️ 開閉アニメとブラーは `WindowTransition` に統一。最前面のウインドウだけがくっきり見える。

## 💡 設計の約束ごと

- 🔁 **相互参照は作らない**。片方に `event` / `Action` / `Func` を持たせ、両方を知るのは Initializer だけ。
- 📣 デリゲートは `.Invoke(...)` で呼ぶ。
- 🧮 数値と仕様はアセットに。キャンペーンの調整値は `CampaignBalance.asset` に集約。
- 🌐 表示テキストはすべてローカライズテーブル経由。キーはデータの ID そのもの。
- 🔤 UI は `CommonText` / `CommonButton` を使う（生の TMP / Button は禁止、テストで検出）。
- ✂️ 1 クラス 1 責務。200 行を超えたら分割を検討（行数だけで分割はしない）。
- 💬 コメントは「何を」ではなく「なぜ」を書く。

## 🧪 テスト

`Assets/Scripts/Tests/` の NUnit テスト（`com.unity.test-framework`）。Model / UseCase は EditMode、Play モードが必要なものだけ PlayMode。
エディタを開いたまま実行するには：

```powershell
unity command run_tests --mode all --filter "Live2DCardRogue.Tests" --filter_type assembly
```

## 🎨 アセット

- アート（`ArtSource/`）とサウンド（`SoundSource/`）は Python で生成した**オリジナルのプレースホルダー**。
- 見た目・動き・言葉のルールは [tone-and-manner.md](tone-and-manner.md)。
- 第三者素材とライセンスは [CLAUDE.md](../CLAUDE.md) の「Art, fonts and copyright」を参照。

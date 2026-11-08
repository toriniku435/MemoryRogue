# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

A Unity 6 (6000.3.24f1) 2D deck-building roguelike prototype. Five scenes exist: `Assets/Scenes/TitleScene.unity` (the entry scene — title plus four mode buttons; see @docs/title-scene.md), `Assets/Scenes/MapScene.unity` (the campaign map that Campaign mode opens — one cell per stage, see @docs/map-scene.md), the deck edit and the market are windows on the map itself, not scenes (see @docs/deck-edit-window.md and @docs/card-shop-window.md), `Assets/Scenes/BattleScene.unity`, where the fighting happens (documented in detail in @docs/battle-scene.md), and `Assets/Scenes/EndingScene.unity` / `Assets/Scenes/ResultScene.unity`, reached by clearing the campaign's last stage (see @docs/ending-and-result-scenes.md). Campaign (Story on screen) and Challenge are wired up. Challenge (Normal 30 / Hard 60 / Master 100 layers, or Endless) runs on the same MapScene under another rule (random enemies that grow stronger every ten layers, no story, one defeat ends the run; see "Challenge mode" in @docs/map-scene.md). Campaign goes title → map → battle → back to the map, advancing a cell on a win, with deck editing reachable from the map; clearing the last stage goes on to the ending, the result screen (score and rank) and back to the title. A new campaign opens with a story conversation and can then open with a tutorial (map walkthrough plus a scripted practice battle, replayable from the map; see @docs/tutorial.md); every stage win plays another conversation before its reward (see "Conversations" in @docs/map-scene.md). BattleScene can also be opened and played on its own, which its Inspector fallback supports, implementing a turn-based deck-building card battle: draw a hand, drag cards onto a target to spend stamina and apply their effect, end turn, repeat until either HP reaches 0.

## Conversation style

**Reply to the user in Japanese, in an お嬢様口調 (refined young-lady speech: 〜ですわ, 〜ますの, 〜でしてよ, 〜かしら, わたくし).** This applies to chat replies only. Code, code comments, commit messages, docs (including this file) and in-game text keep their usual style. Keep the replies as clear and precise as they would otherwise be; the tone is on top of the content, never in place of it.

## Commands

There is no CLI build/lint pipeline in this repo (no npm/dotnet scripts). Development happens through the Unity Editor. There is a small NUnit test suite (`com.unity.test-framework`) — see "Tests" below.

### Driving a live Unity Editor from the terminal

The `unity` CLI (installed under `~/AppData/Local/Unity/bin`, not on PATH by default — prepend it: `$env:Path += ";C:\Users\<user>\AppData\Local\Unity\bin"`) can control an already-open Editor for this project via the `com.unity.pipeline` package (already added to `Packages/manifest.json`):

```powershell
unity status                              # confirm a connected Editor (state "ready")
unity command                             # list every command the Editor exposes
unity command editor_play                 # enter Play mode
unity command editor_stop                 # exit Play mode
unity command recompile                   # force a recompile; poll recompile_status until "completed"
unity command console --tail 30           # read recent console output / check for errors
unity command run_script --file "AgentScripts/Foo.cs" --entry "Foo.Run" --dry_run true   # compile-check first
unity command run_script --file "AgentScripts/Foo.cs" --entry "Foo.Run"                  # then execute
```

`run_script` (compile a project C# file in memory and run one static method, no domain reload) is the reliable way to build/inspect scenes, prefabs, and ScriptableObjects programmatically — prefer it over hand-editing `.unity`/`.prefab` YAML. Scratch scripts for this go outside `Assets/` (e.g. `AgentScripts/`, git-ignored) so writing them never triggers a reimport — **delete them (or the whole directory) once the task they were written for is done**; do not leave throwaway `run_script`/debug scripts in the repo for a future session to trip over.

**A `run_script` call that removes/replaces a `[SerializeField]`-bearing component (e.g. swapping a class for a subclass) breaks existing Inspector references** — a component reference is serialized by instance (fileID), not by GameObject, so `DestroyImmediate` + `AddComponent<NewType>` leaves every field that pointed at the old instance null (in the edited object itself, *and* in any other object's `[SerializeField]` that referenced it, e.g. a ViewContainer). Read the old field values via `SerializedObject` before destroying, and after re-adding, re-check (and if needed re-assign) every *other* object that might reference this GameObject/component — don't assume the swap alone is complete.

**`EditorSceneManager.NewScene`/`OpenScene` invalidates asset references you captured before the call.** A `CardData`/`CampaignData`/prefab reference read into a local before switching scenes writes through as `null` when you later assign it to a component in the new scene, and the failure is silent — the field just ends up empty and the game breaks at runtime. Re-load anything you need **after** the scene switch, by path (`AssetDatabase.LoadAssetAtPath`), and verify the assignment by reading it back before you trust it.

**Gotcha discovered repeatedly while building this project:** the Editor only notices external file changes (new/edited `.cs` files, `Packages/manifest.json` edits) when its window regains focus. If `unity status`/`unity command` can't connect or a `run_script` call doesn't see a just-added field/type, the fix is almost always "ask the user to click into the Unity Editor window," not a deeper problem — confirm this before assuming the Editor isn't running (see the CLI's own `unity-cli` skill for the fuller "sandboxed agent" and "Safe Mode" diagnostics). `unity command recompile` (works even while unfocused) plus polling `recompile_status` is the reliable way to confirm a change actually compiled.

**Play start scene (toolbar).** `Editor/PlayModeStartSceneToolbar.cs` puts a scene dropdown next to the Play button (Unity 6.3's `MainToolbarElement`, docked `Middle`). Picking a Build Settings scene sets `EditorSceneManager.playModeStartScene`, so Play starts there while the edited scene stays open; "開いているシーン" restores normal behaviour. The choice is kept per project in `EditorPrefs`. **It also affects `unity command editor_play`**: when verifying a specific scene from the terminal, check or reset it first — a leftover start scene makes Play open a different scene than the one you opened. If the dropdown does not show on the toolbar, it may need enabling from the toolbar's overflow/context menu, since custom toolbar elements can be user-hidden.

### Running tests

With a live Editor connected, run tests through the Pipeline command rather than the standalone `unity test` CLI command — `unity test` launches its own batch-mode Editor process, which fails outright (`COMMAND_FAILED`, project already open) while this project's Editor window is open, and that's the normal state while working on this repo:

```powershell
unity command run_tests --mode all --filter "Live2DCardRogue.Tests" --filter_type assembly --timeout 180
```

EditMode results come back synchronously in that call's response. PlayMode results do not — the response says `"PlayMode tests started asynchronously"`; poll `unity command test_status` until its `status` is `"completed"` to get the PlayMode summary/results. `--mode editor` / `--mode playmode` restrict to one or the other; `--filter_type testName` (the default) or `category` are the other filter modes besides `assembly`.

## Architecture

### Namespace / folder split

- `Live2DCardRogue.Common.*` (`Assets/Scripts/Common/`) — scene-agnostic model, view, and use-case code, reusable by any future scene. Documented below. Its top level is **the layers** (`Models/`, `Views/`, `Presenters/`, `ScriptableObjects/`) **plus one folder per self-contained facility** (`Localization/`, `SceneTransition/`, `WindowSystem/`, `Message/`, `Audio/`, `DebugMenu/`, `Save/`, `Tutorial/`, `Conversation/`, `Settings/`, `Interaction/`) — a facility being a small set of types with a single entry point that callers use without knowing its internals, whose own layers are too few to be worth separating (`Message/` is one use case wrapping one view). Keep to those two shapes; a facility's folder is not an excuse to put scene-specific code in `Common/`. Splitting `Common/` feature-first (`Cards/`, `Units/`, …) is a later move, and one to make only together with an asmdef that has `noEngineReferences: true` — the `Models/` folder is currently the only thing making "a Model must not depend on UnityEngine" visible, and a feature-first tree would hide it without that compile-time guard.
- `Live2DCardRogue.<SceneName>.*` (`Assets/Scripts/Scenes/<SceneName>/`) — everything specific to one scene. Every scene's folder sits under `Scripts/Scenes/`, so the top-level folders under `Scripts/` read as what they are: `Common/`, `Scenes/`, `Window/` (below), `Editor/`, `Tests/`. The extra level is only for that grouping — the namespace stays `Live2DCardRogue.<SceneName>.*` with no `Scenes` segment. **A scene never references another scene** — not even its entry parameter: what crosses a scene boundary (parameters and results) lives in `Common/SceneTransition/TransitionData/` (below). `SceneDependencyTests` fails on a `Live2DCardRogue.<OtherScene>` reference.
- `Live2DCardRogue.Window.<Name>.*` (`Assets/Scripts/Window/<Name>/`) — **one folder per window**, laid out like a scene (`Models/`, `Presenters/`, `Views/`, `Data/`, `Constants/`, `Enum/` as needed): `DeckEdit`, `CardShop`, `UnitUpgrade`, `EnemyDetail`, `FirstBuild`, `Reward`, `BattleLog`, `BattlePile`, `CampaignResume`, `EpisodeSelect`, `ChallengeSelect`, `CommandCollection`, `EnemyCollection` and the shared `Menu`. The namespace has no `Scenes` segment and keeps the old layer suffix (`Live2DCardRogue.Window.DeckEdit.Models`). **The dependency points one way: a scene uses windows, a window never references a scene**, and `Common/` references neither (both checked by `SceneDependencyTests`). A window is created and driven by the scene's Initializer through its init data (callbacks and `Func`/`Action`s; see "Windows are generated, not placed"); the window's own Model/Data types stay with it unless that data needs them, in which case they live in `Common/` (`BattleLogModel`, which BattleScene's `BattleLogRecorder` writes and the log window shows). Name a window folder so it does not collide with a type name (`Reward`, not `StageReward`, which is a `Common` type — inside `namespace …Window.StageReward` the identifier `StageReward` would resolve to the namespace).
- **Button-list windows share `Window/Menu/`.** A window that is only a title, a column of buttons and Close (the map's Menu, the battle's Menu, the title's Collection) is one prefab, `Assets/Prefabs/MenuWindow.prefab`, driven by `MenuWindowPresenter` with a `MenuWindowInitData`: the title (`UiTextKey`) and `MenuItemData`s, each a label key, an `Action`, a style (`Normal`, or `Danger` for a magenta label), an optional `canPress` (checked on open and on click; the button greys out) and an optional `MenuConfirmData` (asked through the confirm handler the data carries; OK closes the menu, then runs the action). An item without a confirm runs with the menu left open, which is how Options opens over it. `MenuWindowView` clones its inactive `ItemTemplate` per item, spaces them `_itemSpacing` apart and grows the window by the same amount. A new menu is a `MenuWindowInitData` passed to `Create` in the Initializer's `CreateWindows` (next item) — no new view or presenter.
- **Windows are generated, not placed (`Common/WindowSystem/`).** No window is put in a scene. A scene's Initializer creates every window it may use in one `CreateWindows()` method by handing **init data** to `WindowUseCase.Create(data, popupCanvas)` and gets back a **handler**, which it disposes in `OnDestroy` (the shared confirm dialog last, since the others use it); whoever wants a window open calls the handler's `Show`. The handler fields are declared together in one block at the top of the Initializer. This is the scene-transition shape applied to windows: `WindowInitData` plays `SceneTransitionData`, the window's Initializer plays the scene's.
  - **Init data (`Common/WindowSystem/InitData/`).** One sealed class per window (`DeckEditInitData`, `MenuWindowInitData`, …) carrying its `WindowId` (an abstract property returning a constant) and everything the window needs: callbacks, `Func`s, models, the shared confirm handler. Like `SceneTransitionData`, it lives in `Common/` and is built only from `Common/` types, so no scene names a window type. A window that opens with nothing derives from `WindowInitData` and gets a `WindowHandler` (`Show()`); one that needs something to open derives from `WindowInitData<TShowData>` and gets a `WindowHandler<TShowData>` (`Show(TShowData)`), so passing the wrong show data does not compile.
  - **Show data (`Common/WindowSystem/ShowData/`).** What a window needs each time it opens, deriving from `WindowShowData`: `ConfirmShowData`, `CampaignResumeShowData`, `FirstBuildShowData`, `StageRewardShowData`, `ConversationShowData`, `BattlePileShowData`.
  - **The prefab is the starting point of `Initialize`.** `WindowUseCase` looks the prefab up in `Assets/Resources/WindowCatalog.asset` by the data's `WindowId`, instantiates it under the scene's `PopupCanvas`, finds its `IWindowInitializer` (on the prefab root) and calls `Initialize(data)`. The window's Initializer (`WindowInitializerBase<TData, TPresenter>`, one per window, e.g. `Window/DeckEdit/Initializer/DeckEditWindowInitializer`; Common windows' in `Common/WindowSystem/Windows/` and `Common/Conversation/`) holds its views as `[SerializeField]`s and builds its presenter in `protected abstract TPresenter Initialize(TData data)`. The base resolves the data to `TData`, initializes the presenter and **always calls `HideImmediate` right after**, so every window starts closed. It is called before the prefab's `Start`; a window Initializer does all its work in `Initialize`.
  - **Presenters promise what the handler calls** by implementing `IWindowPresenter` (`Open()`) or `IWindowPresenter<TShowData>` (`Open(TShowData)`), both on top of `IWindowPresenterBase` (`IsOpen`, `Close`, `HideImmediate`). `WindowUseCase` refuses (throws) when a presenter does not match its data's kind.
  - **Handlers (`WindowHandlerBase`, `WindowHandler`, `WindowHandler<TShowData>`) live in `Common/WindowSystem/` and know no scene or window type.** `IsOpen`, `Hide` (animated close) and `Dispose` (disposes the presenter and destroys the instance) are shared.
  - **Scene buttons that open a window** are the scene's: `Common/Presenters/WindowOpenButtonPresenter` (button, label, `UiTextKey`, the handler's `Show`, optional `canPress`) sets the label, follows the locale and calls `Show`. A window presenter does not know its open button. A button *inside* a window that opens another one is handed an `Action` through the init data (the deck edit's Enemy Info button gets the Enemy Info handler's `Show`).
  - **The shared confirm dialog** is `WindowHandler<ConfirmShowData>`: `Show(new ConfirmShowData(message, detail, ok, cancel, onOk, onCancel))` answers only through that data's callbacks, so the presenters sharing it need no "is this my question?" flag. While another question is open it asks nothing; a caller that must know checks `IsOpen` first (the tutorial skip treats that as "not skipped"). It replaced `ConfirmPrompt` and the presenters' direct `OnOkClicked` / `OnCancelRequested` subscriptions.
  - **A new window** is: a `WindowId` (added at the end; the catalog stores the number), a catalog entry, an init data class (and a show data class if it needs one), `IWindowPresenter` on its presenter, a `WindowInitializerBase` subclass on the prefab root with its view assigned, and a line in the scene's `CreateWindows`. `WindowCatalogTests` checks every id has a prefab, that the prefab's root Initializer takes the init data of that id, and that its references are assigned.
  - **Types the init data needs moved to `Common/`** so it could stay scene-independent: `BattleLogModel` / `BattleLogEntry` / `BattleLogKind` (`Common/Models/BattleLog/`), `BattleLogFormatter` / `BattleLogLine` (`Common/Presenters/BattleLog/`), `CardShopModel`, `UnitUpgradeModel`, `EpisodeState` (`Common/Models/`), and the menu's `MenuItemData` / `MenuConfirmData` / `MenuItemStyle` (`Common/WindowSystem/Menu/`; `MenuWindowData` became `MenuWindowInitData`).
  - **Opening, closing, blur and stacking are unchanged**: they are the prefab's own `WindowTransition` / `PopupLayerMember`.
  - Introduced 2026-10-08 for every window of TitleScene, MapScene and BattleScene. Until then each window was a prefab instance in its scene, wired through the ViewContainer, and window presenters wired their own open buttons.
- Folder nesting does not always mirror the namespace (e.g. a scene's `Presenters/CardPile/*.cs` files can still declare `namespace Live2DCardRogue.BattleScene.Presenters`) — grouping directories by concern is purely file organization, and enums/plain-data types for a given area live in `Enum/` / `Data/` subfolders next to what uses them (e.g. `BattleScene/Models/Enum/BattlePhase.cs`, `BattleScene/Data/CardDropCandidate.cs`).

### Layering

Model → UseCase → Presenter → View, plus a per-scene ViewContainer and a composition-root Initializer:

- **Model** (`Common/Models/*Model.cs`, `<Scene>/Models/*Model.cs`) — plain C# classes holding state and exposing C# `event Action<...>` for changes (`UnitStatusModel.OnHpChanged`, `CardPileModel.OnCardAdded`, ...). No UnityEngine dependency.
- **UseCase** (`<Scene>/UseCase/`, and a facility's own folder in `Common/` such as `Common/Message/MessageUseCase.cs`) — orchestrates model mutations for a player-triggered action and raises its own events (or returns a result, e.g. a `bool` success flag) for anything another layer needs to react to.
- **Presenter** (`<Scene>/Presenters/`) — bridges Model events to Views. **Every presenter derives from `Common/Presenters/PresenterBase.cs`** (`IDisposable`): constructor only stores dependencies, `Initialize()` subscribes and does the first sync, `Dispose()` unsubscribes. This split (rather than subscribing in the constructor) exists so a presenter never reacts to a model event before its own `Initialize()` has run.
  - `PresenterBase.Initialize()`/`Dispose()` are **not virtual** — they are template methods that handle the locale subscription and then call the abstract `OnInitialize()`/`OnDispose()`, which is what a subclass implements. Making them virtual would mean a subclass that forgets `base.Initialize()` silently loses that subscription; this shape makes that impossible. Both are idempotent (a second `Initialize()`/`Dispose()` is a no-op).
  - `protected abstract void OnLocaleChanged()` is the third hook: `PresenterBase` subscribes to `LocalizationSettings.SelectedLocaleChanged` for you and calls it. It is **abstract, not virtual, on purpose** — a virtual no-op default would let a presenter that does display localized text be written with no refresh and no prompt to think about it, and the bug would only surface when someone actually switches language. An empty implementation is fine and is the honest answer for a presenter with no localized text (`UnitPresenter` shows only numbers); an intermediate base can also implement it once for its whole family and `sealed` it (`CardPilePresenterBase` does — for a pile presenter the answer is always "re-run `Sync`").
- **View** (`Common/Views/`, `<Scene>/Views/`) — `MonoBehaviour`s that only display data and forward raw input as plain C# delegates/events — no business rules. A base View in `Common/Views/` is left **not sealed** when a scene needs scene-specific behavior (e.g. drag-and-drop) layered on top via a subclass in `<Scene>/Views/`; the base stays a pure display component so it's still reusable if a future scene wants the display without the scene-specific behavior. If nothing outside one scene ever needs the plain version, don't pre-split — fold the behavior straight into the scene's own View class instead (this project did both at different times; see @docs/battle-scene.md for the current split).
- **UI wrapper components** (`Common/Views/UI/`) — also the home of `CommonScrollList`, the virtualized scroll list every long list should use (it keeps only the visible rows alive; see @docs/deck-edit-window.md). Nothing in the project uses `TextMeshProUGUI` or `UnityEngine.UI.Button` directly. `CommonText : TextMeshProUGUI` and `CommonButton : Button` exist as extension points, so a behavior every text/button should share (a default font, click-spam protection, an SFX on press) is added in one place instead of re-pointing every component in every scene and prefab. `CommonButton` now carries the first such behavior — its hover and click sounds (see @docs/sound.md) — and `Editor/CommonButtonEditor.cs` adds those two fields under the stock Button Inspector. Both are **not sealed**. `CommonText` keeps the stock Inspector (TMP's editor is declared with `editorForChildClasses: true`); `CommonButton` extends uGUI's `ButtonEditor` rather than replacing it, so everything stock is still there. `ButtonRippleEffect` (same folder) is the optional hover/press ripple for any `CommonButton` — see @docs/art.md. New UI goes through them too: `CommonUiComponentTests` fails if a raw `TextMeshProUGUI`/`Button` shows up in any prefab or scene, since a raw one looks and behaves identically and is otherwise invisible. Swapping an existing component's class is a `m_Script` swap via `SerializedObject` (see "Driving a live Unity Editor from the terminal" above) — do it **before** retyping the `[SerializeField]` fields that point at it, or the references null out on the next deserialize.
- **ViewContainer** (one per scene, e.g. `BattleSceneViewContainer.cs`) — a `MonoBehaviour` exposing every View/UI reference a scene's presenters need, wired in the Inspector.
- **Initializer** (one per scene, e.g. `BattleSceneInitializer.cs`) — the composition root. It derives from `Common/SceneTransition/SceneInitializerBase<TData>`, whose `Start` consumes the scene's transition data and calls `Initialize(TData)` (null when the scene was opened directly); `Initialize` `new`s up every Model/UseCase/Director/Presenter for the scene and wires the few cross-references between them, and `OnDestroy()` disposes everything created there. This is the only place allowed to reference both a UseCase and the Director/Presenters that depend on it. It only assembles: a flow or a state change it would otherwise carry (applying a battle result, saving, the tutorial's questions) goes into a UseCase it creates (`MapRunUseCase`, `TitleRunUseCase`, …), per the class-size rule.

### Cross-class wiring — prefer one-directional dependencies

Where two classes would otherwise need to reference each other, the codebase consistently resolves it by giving one of them a C# `event`/`Action`/`Func` field instead of a direct type reference, so only the Initializer (or the dependent class's own constructor, when it already legitimately depends on the other) needs to know about both types. When one side needs to *pick among several* possible collaborators (not just fire-and-forget), prefer a **register/evaluate** pattern over a single event: expose `Register<T>(Func<TArg, TResult> evaluator)` and let the registering side hand back a small result value (a plain class/struct, not just a bool) so the caller can compare candidates — this is what lets a new collaborator be added later with a one-line `Register` call and no changes to the class doing the choosing. When adding new inter-class notifications, follow one of these two shapes rather than adding a reference from a lower-level class back up to whatever calls it. The event/callback shape is used throughout BattleScene (see @docs/battle-scene.md); the register/evaluate shape currently has **no live example** — it was how drag-and-drop picked which unit a card had been dropped on, and it went away with the drag-and-drop itself, since a clicked unit reports itself and nothing has to choose between candidates any more. Reach for it again when something genuinely has to compare several collaborators' answers.

### Delegate calls

Always call `Action`/`Func` fields via `.Invoke(...)` (e.g. `_canDrag.Invoke()`, `OnHpChanged?.Invoke(...)`), never the shorthand `_delegate(...)` call syntax — it's more greppable and reads unambiguously as "invoking a stored delegate" rather than "calling a method."

### Comments

The codebase has too many comments; write fewer from here on. A comment records the **intent, constraints and invariants** someone changing the code later needs: **why**, not **what**.

**Write one for:**

- a "why" the code alone cannot tell you;
- something that looks safe to change or simplify but would break the spec if changed;
- an unusual implementation forced by Unity or an external library;
- a lifecycle assumption that matters;
- an implicit contract with another class;
- a place that deliberately behaves differently by type or state;
- a design decision a future refactor is likely to remove or change by mistake.

**Don't write one for:**

- the code restated in prose;
- what the method or variable name already says;
- what an `if` or a loop visibly does;
- a plain getter, setter or assignment;
- the implementation's steps narrated one by one.

**When changing code (AI included):**

- Treat the intent, constraints and invariants in existing comments as spec.
- Don't delete or shorten a comment that looks unnecessary without first finding out why it is there.
- Conversely, where code alone could lead a future change astray, leave a short comment with the design intent.

### Class size and responsibilities

- **One responsibility per class.** A class does one job; when it does several, split it along those jobs: a UseCase for model mutations and flows, a Helper or Service for shared or I/O work, a small pure class for calculations (which can then be tested).
- **Over 200 lines, consider a split.** Look at what the class does, not at the count.
- **Do not split a class whose responsibility is clear and cohesive**, and never for line count alone. A window presenter or an animation-heavy view that does one thing stays whole.
- **A documented decision not to split stands** until it is revisited on purpose (e.g. `BattleCommandQueueUseCase` keeps choosing a command and queueing it together; see "Command use cases" in @docs/battle-scene.md).

### Adding a new scene

`BattleScene` is the reference implementation for how a scene should be structured — mirror its shape (documented in @docs/battle-scene.md) rather than inventing a new one. `TitleScene` (@docs/title-scene.md) is the same shape at its smallest: a scene with no state of its own drops the Model and UseCase layers and keeps just Initializer + ViewContainer + Presenter.

**Scenes are loaded through `Common/SceneTransition/SceneTransitionUseCase.LoadScene(data)`**, never `SceneManager` directly. `data` is a `SceneTransitionData` (`Common/SceneTransition/SceneTransitionData.cs`, an abstract class), which **carries its own destination** (`Destination`, set by each subclass's constructor from `SceneName`), so a call site passes the data and nothing else and cannot pair it with the wrong scene. **Each type goes to exactly one scene; where data can go to several scenes, there is one type per destination** (below). Even a transition with nothing to hand over has a type (`TitleSceneTransitionData`, `MapSceneTransitionData`), so the list of types is the list of ways into each scene. It is an abstract class rather than an interface so that "every transition has a destination" is enforced in one constructor. The data is handed over by `Common/SceneTransition/SceneTransitionDataStore` and read once by **`Common/SceneTransition/SceneInitializerBase<TData>`, the base class of every scene's Initializer**: its `Start` calls `TryConsume<TData>` and hands the result to the abstract `Initialize(TData data)`, which is where the Initializer builds the scene (`data` is null when the scene was opened on its own). A successful read clears the slot so nothing leaks into a later transition. The type argument is how the scene is entered, so **one scene takes one type**; a scene entered with different data from different places gets an abstract base for them and switches on the subtype (`ResultSceneTransitionDataBase` → `ResultSceneTransitionData` from the ending, `ChallengeResultTransitionData` from a challenge). An Initializer must not declare its own `Start`: Unity would call only that one, and the read would be skipped silently (`SceneInitializerTests` checks this). Scene names live in `Common/SceneTransition/SceneName.cs`, and every loadable scene has to be in Build Settings. The types live in `Common/SceneTransition/TransitionData/` (`TitleSceneTransitionData`, `MapSceneTransitionData`, `BattleSceneTransitionData`, `EndingSceneTransitionData`, `ResultSceneTransitionDataBase` with `ResultSceneTransitionData` and `ChallengeResultTransitionData`; namespace `Live2DCardRogue.Common.SceneTransition`), so that no scene has to reference another — they describe a scene's entry conditions and are built only from `Common/` types. (Until 2026-10-06 each belonged to the scene being entered, was called `…SceneParameter`, and was passed to `LoadScene` together with a scene name; the caller referenced the callee's `Data` namespace. That was dropped so that scenes are fully independent and a call site names nothing but the data.) An Initializer that can also run standalone should fall back to its own Inspector mock data when `Initialize` gets null, and log that it did (`BattleSceneInitializer.CreateMockParameter`), so opening a scene directly and pressing Play keeps working.

**Every load is covered by `Common/SceneTransition/SceneTransitionEffect`**, a self-instantiating `DontDestroyOnLoad` singleton (`Resources/SceneTransitionEffect.prefab`), so a scene switch never shows a freeze. The sequence:

1. **Cover.** Horizontal slices glitch in from alternating sides.
2. **Load.** The next scene loads with `LoadSceneAsync` under that cover.
3. **Settle.** It waits `_settleFrames` more frames so the new Initializer's `Start` and the first layout happen while covered.
4. **Reveal.** The slices glitch out.

Details:

- **Loading display.** If the cover lasts longer than `_loadingDelay` (0.2 s), a `// LOADING` label and progress bar appear. Short switches never flash it.
- **No overlapping transitions.** While `SceneTransitionEffect.IsTransitioning`, `LoadScene` does nothing (it doesn't even overwrite the parameter), and the effect's canvas swallows input.
- **Screen Space - Overlay.** This is the one canvas that renders in Overlay. The cameras are destroyed and recreated mid-transition, so a camera-bound canvas would lose its camera.
- **Fallback.** Without the prefab, `LoadScene` falls back to a plain `SceneManager.LoadScene`.
- **Scene-side animation.** Anything a scene wants to animate as it appears should wait until `IsTransitioning` is false, or it plays under the cover (`MapPlayerView` does this).

**Returning a result uses the same one-way rule.** A scene that can be entered and left again produces a plain result (`Common/Models/BattleResult`) and is handed, as part of its data, **how to wrap it for the way back** (`BattleSceneTransitionData.CreateReturnData`, a `Func<BattleResult, SceneTransitionData>`). The caller decides where the result goes — the map passes `ReturnToMap` (a `MapSceneTransitionData` carrying the result) or, in a challenge, `ReturnToMapOrEndChallenge` (a defeat becomes `ChallengeResultTransitionData` for ResultScene) — and the callee just invokes it, so it never references the caller and never knows the scene it returns to. The function survives the scene change, so it must not capture the caller's objects: the map's are `static`. A null function means "stay here", which is what keeps standalone play working. (Until 2026-10-06 the battle took the return scene's *name* — `ReturnSceneName` / `DefeatSceneName` — and sent one `BattleSceneResult` to either scene; it was split per destination at the user's request.)

**ScriptableObjects cross scenes as ids; `Common/Repositories/GameAssetRepository` keeps the assets.** Holding a ScriptableObject in transition data does not keep it loaded: if only the previous scene referenced it, the scene switch's unused-asset sweep can free it in a build (the Editor does not reproduce this), even while C# still points at it, and the next scene gets a destroyed object. This is what left the first practice battle after launch on Android without its tutorial (2026-10-17). So `BattleSceneTransitionData` carries `DeckIds` / `EnemyIds`; its constructor still takes the `CardData[]` / `EnemyData[]` and hands each to `GameAssetRepository.Hold`, which marks it `HideFlags.DontUnloadUnusedAsset` and keeps it by id until the app closes (they are small, so nothing is ever released), and the battle resolves the ids with `FindCard` / `FindEnemy`. Holding happens while the sending scene is still alive, which is why it is done when the data is built. The tutorial's steps are not passed at all: `CampaignTutorialRepository` holds them, marked the same way, from the tutorial's start to its finish (@docs/tutorial.md), and the data only says `PlayTutorial`. A new ScriptableObject that has to cross scenes goes the same way (an id plus a `Hold`); plain C# data (models, results) needs none of this.

**State that has to outlive a scene goes in `Common/Repositories/`**, not in a parameter passed back and forth. `CampaignProgressRepository` is the example: the map writes the current cell, the battle knows nothing about it, and the map reads it again on the way back. `CampaignDeckRepository` (up to ten decks with the active one the battle uses, and the owned cards) and `CampaignTokenRepository` (the campaign's tokens, cleared when a campaign starts) sit beside it, `CampaignUnitRepository` (the player unit's upgraded max HP and memory, cleared when a campaign starts), `RunModeRepository` (whether the run is Story or a challenge, and the challenge, its layer count and seed; the four campaign repositories are shared by both modes), and `GameSettingsRepository` holds game-wide settings (battle speed so far) for a future options screen; the shop and deck-edit windows never touch them — the map hands their presenters values and writes the results back. Reach for a repository when the data belongs to one scene across visits; reach for a parameter when it is what another scene needs to start.


- Create `Live2DCardRogue.<SceneName>.*` under `Assets/Scripts/Scenes/<SceneName>/`, with the same sub-folders BattleScene uses as needed (`Models/`, `UseCase/`, `Directors/` or equivalent flow-control class, `Presenters/`, `Views/`, `Initializer/`, `Constants/`, `Data/` for plain result/DTO classes shared across presenters).
- Put anything not tied to this specific scene under `Common/*` instead (models, views, use-cases) so it stays reusable — check there first before writing something scene-specific that duplicates existing Common code.
- Put a window (a modal popup with its own Model/Presenter/View) under `Window/<Name>/`, not in the scene's folder; a column of buttons is a `MenuWindowInitData`, not a new window. A new scene's entry parameter / result types go in `Common/SceneTransition/TransitionData/`.
- Keep the same layering and lifecycle described above.
- Resolve any two-way class relationship per "Cross-class wiring" above — don't let the new scene's classes reach back into each other's concrete types.
- If you write a new per-scene doc, add an `@docs/<scene-name>.md` import line to this file's relevant sections (like the ones above) so it's discoverable, and keep genuinely scene-agnostic material here instead of duplicating it per scene.

### Card system (`Common/Models/Cards/`, `Common/ScriptableObjects/CardData.cs`)

**Players never see the word "card".** Nor "hand": the hand is the **buffer** (バッファ / Buffer) — the commands loaded for this turn. Code keeps `Hand`. On screen a card is a **command** (コマンド), a card pack is a **chip** (チップ), the card shop is the **chip shop** and the deck is the **command deck** (see @docs/tone-and-manner.md, "Words"). The code, asset names and localization keys still say `Card` / `CardPack` — ids are not wording — so new UI text should use the on-screen names while new code follows the existing type names.

`CardData` (a `ScriptableObject`) holds `_id`, `_cost`, `_rarity` (only used by pack draws — see @docs/card-shop-window.md) and `[SerializeReference] CardEffectBase[] _effects` — all private with public read-only accessors. **No display text lives on `CardData`**: `_id` is the card's stable identifier (set to the asset's own name: `Attack`, `Block`, `StrongAttack`), and both the card's name and its description are looked up by that id in the `CardName` / `CardDescription` localization tables — see "Localization" below. So adding a card means adding the asset, adding it to `CardCatalog` (for saves), *and* its entries in both tables (`CardLocalizationTests` fails if you forget; the description is a Smart String template, see below) — plus, for it to have a picture, an illustration assigned to its `_illustration` (see @docs/art.md). `CardEffectBase` → `AttackCardEffectBase` / `BlockCardEffectBase` / `PoisonCardEffectBase` / `StrengthCardEffectBase` / `HealCardEffectBase` / `RegenerationCardEffectBase` → concrete leaves (`BasicAttackCardEffect`, `BasicBlockCardEffect`, `BasicPoisonCardEffect`, `BasicStrengthCardEffect`, `BasicHealCardEffect`, `BasicRegenerationCardEffect`), plus the leaves added with the second card set: `PiercingAttackCardEffect` (overrides the attack base's `DamageType` to `HpChangeType.TakePiercingDamage`, which block does not absorb), `AreaAttackCardEffect`, `HpRatioAttackCardEffect` / `HpRatioBlockCardEffect` (a percent of the user's *current* HP), `PayHpCardEffect` (HP as a cost, `HpChangeType.PayHp`, never below 1), and the timed-status categories `SelfStatusCardEffectBase` (`MemoryBoostCardEffect`, `HandSizeUpCardEffect`, `HandSizeDownCardEffect`) and `OpponentStatusCardEffectBase` (`StunCardEffect`, `HealReversalCardEffect`). A card that attacks and heals is just two effects on one card (`Siphon`). Note which unit each category acts on: attack and poison land on `target`, block, strength, heal and regeneration on `user` — and that split is also published as `ICardEffect.TargetsOpponent`, which `CardData.TargetsOpponent` ORs over a card's effects. Every card makes the player pick a target, so this is what decides **which side** they are allowed to pick (`BattleCommandQueueUseCase.CanSelectTarget` compares it against "is this the player's own unit"). It is `abstract` on `CardEffectBase` and answered once per category base, not per leaf: a new category has to state which side it acts on, and a new leaf inherits the right answer for free. **Area attacks** add `ICardEffect.TargetsAllOpponents` (virtual, `false` by default): the player still picks one enemy, and `BattleCommandExecuteUseCase.ExecuteNext` then calls `Execute` once per living opponent (from the injected `getLivingOpponents`, wired to `EnemyPartyModel.GetLivingStatuses`), so strength and each target's block apply per hit. `ICardEffect.Execute(UnitStatusModel user, UnitStatusModel target)` is the entry point a UseCase calls; each base class's `OnExecute` computes an amount (`CalculateDamage`/`CalculateBlockAmount`) and applies it (`target.TakeDamage(...)`, `user.AddStatusEffect(new BlockStatusEffect(...))`).

**Card descriptions follow the numbers.** A description never contains a number. Each entry in `CardDescription` is a **Smart String** with named placeholders (`{damage}ダメージを与える`, `Deal {damage} damage`), and `LocalizationHelper.GetCardDescription` fills them from `CardData.CreateDescriptionValues()`. That method collects what every effect adds in `ICardEffect.AddDescriptionValues`. It is `abstract` on `CardEffectBase`, so a new effect has to state its values. Each leaf uses its own names so two effects on one card never collide: `damage`, `block`, `heal`, `poison`, `regeneration`, `strength`, `hpCost`, `damagePercent` / `blockPercent`, and for timed effects `memoryTurns` / `memoryAmount`, `handUpTurns` / `handUpAmount`, `handDownTurns` / `handDownAmount`, `stunTurns`, `reversalTurns`. English uses Smart plurals (`{stunTurns} {stunTurns:plural:turn|turns}`). Adjusting a value in the Inspector therefore rewrites the text everywhere. `CardDescriptionTemplateTests` holds the contract for every card and locale:
- every entry is smart;
- no digit is written outside a placeholder;
- every placeholder is provided by the card;
- every value the card provides appears in the text;
- no two effects on a card share a name;
- the rendered text has nothing left in braces.

A plain positional `string.Format` (`{0}`) was rejected, because on a card with several effects nobody can tell which number is which, and the order would have to match in every language.

**Commands added on 2026-09-27** (all in the chips' pools and `CardCatalog`):

| Id | Shown as | Effects | Note |
| --- | --- | --- | --- |
| `Loopback` | ループバック | `LoopbackCardEffect` | the next command returns to the next hand instead of the archive |
| `Multithread` | マルチスレッド | `MultithreadCardEffect` (`_repeat` 3) | the next command runs 3 times and is purged |
| `Mining` | マイニング | `PayHpCardEffect` (20) + `TokenBonusCardEffect` (50 %) | `CardData._purgeOnUse`; a win pays that much more in tokens |
| `ReversePatch` | リバースパッチ | `HealReversalCardEffect` (2 turns) | one enemy |
| `KernelPanic` | カーネルパニック | `HealReversalAllCardEffect` (3 turns) | every unit, the player included |
| `Bypass` | バイパス | `BlockBreakCardEffect` | removes all of the target's block (`UnitStatusModel.RemoveStatusEffects`), no damage |
| `BufferOverflow` | バッファオーバーフロー | `BasicAttackCardEffect` (40) + `BufferLockCardEffect` (1 turn) | changed 2026-09-29, see below |
| `RepairPacket` | リペアパケット | `OpponentHealCardEffect` (15, cost 2) | heals the **target** — damage under heal reversal |
| `Worm` | ワーム | `WormCardEffect` (2) | a `PoisonCardEffectBase` leaf that infects with `WormStatusEffect` instead (`CreatePoisonEffect`) |

**Changes on 2026-09-29.**

| Id | Shown as | Effects | Note |
| --- | --- | --- | --- |
| `ReversePatch` / `RepairPacket` | | cost 2 → 3 / 1 → 2, heal 20 → 15 | the reversal + repair combo was too strong |
| `BufferOverflow` | バッファオーバーフロー | `BasicAttackCardEffect` (40) + `BufferLockCardEffect` (1 turn) | no more purging; the next buffer is empty (`BufferLockStatusEffect`), and using it again stacks the turns |
| `GarbageCollection` | ガベージコレクション | `OverflowAttackCardEffect` (up to 2, 10 each) | the old Buffer Overflow mechanic: purge up to 2 buffer commands, 10 damage each |
| `PortScan` / `PrivilegeEscalation` / `Terminate` | ポートスキャン / 権限昇格 / ターミネート | `ExploitChainCardEffect` (`_step` 1 / 2 / 3) | executed in that order in one turn (0x00, 0x01, 0x02) the last one kills the target (`HpChangeType.InstantKill`). Steps 1–2 are self-target, step 3 picks an enemy. `CardData._maxCopiesInDeck` = 1. Once step 3 fires in order (even into kill immunity), **all three are purged** for the rest of the battle (`IChainCardEffect`, see @docs/battle-scene.md) |
| `CodeInjection` | コードインジェクション | `GlitchInjectCardEffect` (2 turns, 50 %) | before each of the target's actions, 50 % chance of the enemy-only `SystemError` (`PayHpCardEffect` 10 on itself) |
| `SpyMemoryLeak` (enemy) | メモリーリーク | `StaminaDrainCardEffect` (3) | Spyware; the player's next turn has 3 less memory |
| `SystemError` (enemy) | システムエラー | `PayHpCardEffect` (10) | inserted by glitch; `BattleSceneInitializer._glitchCard` |

**Commands added on 2026-10-04** (all in `CardCatalog`; Starter, Premium and the First Build chip hold all ten, the themed chips their share):

| Id | Shown as | Cost / rarity | Effects | Note |
| --- | --- | --- | --- | --- |
| `ZeroDay` | ゼロデイ | 4 / Epic | `AllUnitsPoisonCardEffect` (8) | infects **every** living unit, the player included; self-target (Hack chip) |
| `Redo` | リドゥ | 2 / Rare | `RedoCardEffect` | runs the effects of the last non-Redo command again, on its target (re-aimed if it died) and with its purge count; chain steps are not repeated (Hack chip) |
| `Flush` | フラッシュ | 2 / Normal | `FlushAttackCardEffect` (8, +16) | the bonus only if the buffer is empty when it executes (Attack chip) |
| `SwapOut` | スワップアウト | 1 / Rare | `PurgeBlockCardEffect` (up to 2, 15 each) | `IHandPurgeCardEffect`, chosen like Garbage Collection (Repair chip) |
| `Defrag` | デフラグ | 1 / Rare | `PurgeHealCardEffect` (up to 2, 15 each) | likewise, heals (Repair chip) |
| `CloudBurst` | クラウドバースト | 2 / Epic | `PayTokensCardEffect` (100) + `PiercingAttackCardEffect` (50) | tokens are paid when queued and never refunded by a defeat (Attack chip) |
| `Pipeline` | パイプライン | 1 / Normal | `BufferCountAttackCardEffect` (2 each) | commands left in the buffer when it executes (Attack chip) |
| `BruteForce` | ブルートフォース | 2 / Normal | `ChanceAttackCardEffect` (50 %, 30) | `ChanceAttackCardEffect.Random` is a static `System.Random`, swappable in tests (Attack chip) |
| `Reflection` | リフレクション | 2 / Rare | `BlockStrikeCardEffect` | damage = the user's `BlockValue`; since 2026-11-07 the block is then halved (rounded down; it was not spent before) (Attack chip) |
| `Format` | フォーマット | 3 / Epic | `FormatCardEffect` | removes every status effect from every living unit, block included, except **traits** (`IStatusEffect.IsTrait`: immunities, poison resistance, layer boost) (Hack chip) |
| `TrojanBreach` (enemy) | ファイアウォール突破 | — | `BlockBreakCardEffect` | Trojan |
| `RootkitHijack` (enemy) | リソースハイジャック | — | `TargetHpRatioAttackCardEffect` (20 %) | Rootkit; a percentage of the **target's** current HP, at least 1 |

**Changes on 2026-10-08.**

| Id | Shown as | Cost / rarity | Effects | Note |
| --- | --- | --- | --- | --- |
| `Forensics` | フォレンジック | 3 / Rare | `DefeatCountAttackCardEffect` (6, +10 per defeat) | adds 10 for each enemy defeated this battle (`CardExecutionContext.DefeatedEnemyCount`, counted by `EnemyPartyModel.DefeatedCount`; a summoned enemy counts every time it falls). Starter, Premium, First Build and Attack chips |
| `Interrupt` | インタラプト | | | `CardData._maxCopiesInDeck` = 1 (perma-stunning the boss with several copies was too easy). Decks saved before keep their copies until edited |

**Changes on 2026-11-07** (the boss rebalance, below; every boss-fight direction was given a way to win):

| Id | Shown as | Change | Why |
| --- | --- | --- | --- |
| `Overwrite` (new) | オーバーライト | 6 / Epic, `PiercingAttackCardEffect` (32) | a sink for spare memory; Starter, Premium, First Build and Attack chips |
| `Exploit` | エクスプロイト | piercing 4 → 15 | 4 for 3 memory was never worth playing |
| `Pipeline` | パイプライン | 2 → 6 per buffer command | |
| `Siphon` | サイフォン | cost 4 → 3, 5 / 4 → 10 damage / 8 heal | the sustain deck's attack |
| `Interrupt` | インタラプト | cost 6 → 4 | |
| `GarbageCollection` | ガベージコレクション | 10 → 14 per purge | the purge deck has to win before its deck runs out |
| `CodeInjection` / `SystemError` | コードインジェクション / システムエラー | 2 → 3 turns / 10 → 15 | |
| `Worm` | ワーム | 2 → 4 | |
| `Overclock` | オーバークロック | HP cost 8 → 4 | |
| `RepairPacket` / `ReversePatch` | リペアパケット / リバースパッチ | 15 → 20 / cost 3 → 2 | |
| `Regeneration` | リジェネ | 3 → 5 | |
| `Strength` | 強化 | +1 → +2 | |
| `Forensics` | フォレンジック | 6 + 10 → 10 + 15 per defeat | |
| `StrongAttack` | 強撃 | 16 → 24 | |
| `Multithread` | マルチスレッド | 3 → 2 runs | three runs of a big command decided the boss in 9 turns |
| `Reflection` | リフレクション | now halves the user's block after dealing it (`UnitStatusModel.SpendBlock`) | block carries over, so stacking it and firing every turn beat everything |

These were chosen by simulating the boss fight with the real battle models and 13 decks of different directions (a greedy one-turn-lookahead AI, about 150 fights each): on arrival at HP 220 / memory 12 the directions win about 20–60 % (43 % on average; Reflection and the memory deck lowest, around 20 %), an Attack/Block-only deck loses every time, and fights last about 12–25 turns. **Overcharge** (healing past max HP, see "Status effect system") is what gives the sustain deck its win; it only applies to the player.

- **Effects that need the battle around them.** `ICommandContextCardEffect.ExecuteWithContext(user, target, CardExecutionContext)` is called instead of `Execute` for the player's commands; the context (`Common/Models/Cards/Base/CardExecutionContext.cs`) carries the buffer count at execution, every living unit, player first, and the enemies defeated so far this battle. Played without one (an enemy), the effect falls back to `Execute` (ZeroDay and Format then act on user and target). `BattleCommandExecuteUseCase.ExecuteEffects` dispatches all opponents → hand purge → context → plain `Execute`.
- **Token cost.** `ITokenCostCardEffect.TokenCost`, summed by `CardData.TokenCost`. The battle keeps the tokens in `BattleTokenModel` (from `BattleSceneTransitionData.Tokens`); `BattleCommandQueueUseCase` refuses a command it cannot pay for ("トークンが足りません"), takes them when the command is queued, gives them back if it returns to the buffer, and reports `TokensSpent` in `BattleResult`; the map deducts them win or lose.
- **Redo.** `CardData.RepeatsPrevious` (a `RedoCardEffect` on it). `CommandHistoryModel` remembers the last executed non-Redo command, its target and purge count **for the current turn only** (`BattleTurnEndUseCase.EndPlayerTurn` forgets it), so a Redo executed first in its turn does nothing. A token cost is paid again when Redo repeats the command (counted in `TokensSpent`); without enough tokens the Redo fizzles.

- **Immunities.** `IStatusEffect.BlocksStatusEffect(incoming)` (default `false`) lets an effect refuse a new one; `UnitStatusModel.AddStatusEffect` then adds nothing and raises `OnStatusEffectBlocked`. `VirusImmunityStatusEffect` (virus and worm) and `CorruptionImmunityStatusEffect` (heal reversal) use it; `KillImmunityStatusEffect` turns `HpChangeType.InstantKill` into 0. Their definitions are `VirusImmunityDefinition` / `CorruptionImmunityDefinition` / `KillImmunityDefinition`: Rootkit has virus and kill immunity, Botnet corruption immunity. A status effect whose `Value` is 0 is shown by name only.
- **`StatLimitType.DrawCap`** (base `int.MaxValue`) caps the draw *after* the "at least one" rule, which is how the buffer lock reaches 0. **`StatLimitType.GlitchChance`** (base 0) is the percent `BattlePhaseDirector` rolls before each enemy action.
- **`CardData._maxCopiesInDeck`** (0 = no limit) is enforced by `DeckEditModel.CanAddToDeck` and `AutoDeckBuilder`.

`CardData._purgeOnUse` sends a command to the **purge pile** after use instead of the archive (see @docs/battle-scene.md). The Virus command (`Poison`) now costs 5 and infects for 4 per turn; the enemies' viruses are separate enemy-only assets (`TrojanVirus`, `RootkitVirus`, `SpyVirus`), so tuning the command no longer changes an enemy.

**Effects that need the buffer (hand).** A card effect only sees two units, so an effect that costs buffer commands implements `IHandPurgeCardEffect` (`HandPurgeCount`, `ExecuteWithPurged(user, target, purgedCount)`; `CardData.HandPurgeCount` reads it). The player chooses which ones when queueing (see "Card play flow" in @docs/battle-scene.md); the chosen commands ride on the `CardCommand` (`PurgeCards`), are purged when it executes (each reported through `OnCardDisposed`), and `ExecuteWithPurged` gets their count instead of `Execute` being called. Played without a buffer (an enemy), it falls back to `Execute`. `AttackCardEffectBase.DealDamage` is the shared "strength, then the target's interventions" step such an effect reuses.

**Enemy-only effects.** `HandJamCardEffect` puts `HandSizeDownStatusEffect` on the *target* (the self-cost `HandSizeDownCardEffect` puts it on the user). `AllyHealCardEffect` heals the **target**, and sets the new `ICardEffect.ChoosesAlly` (virtual, `false` by default, ORed by `CardData.ChoosesAlly`): when an enemy plays such a card, `BattlePhaseDirector` hands it the most damaged living ally (`EnemyPartyModel.FindWeakestLiving`, lowest HP ratio) as the target instead of the player.

Because stock Unity gives `[SerializeReference]` fields no type picker, `Assets/Scripts/Editor/SerializeReferenceDrawerBase.cs` is a custom `PropertyDrawer` that renders a dropdown (via `TypeCache.GetTypesDerivedFrom(BaseType)`) to pick/replace the concrete type and inline-edits its fields. Unity resolves a drawer by the field's **declared** type, so each `[SerializeReference]` base type needs its own one-line subclass carrying the `[CustomPropertyDrawer]` attribute: `CardEffectBaseDrawer` (`CardEffectBase`) and `EnemyCardPickerDrawer` (`IEnemyCardPicker`) are the two so far — the attribute works on an interface as well as an abstract class. Any new concrete type shows up in the matching dropdown automatically; only a brand-new `[SerializeReference]` *base* type needs a new (one-line) drawer.

### Enemy data (`Common/ScriptableObjects/EnemyData.cs`, `Common/Models/EnemyCardPickers/`)

`EnemyData` holds `_id`, `_maxHp`, `_minActionCount`/`_maxActionCount`, `CardData[] _cards`, and `[SerializeReference] IEnemyCardPicker _cardPicker` — same shape as `CardData` (private fields, public read-only accessors, `[CreateAssetMenu]`).

**An enemy has no stamina.** Its output per turn is bounded by the action counts instead, and `BattleSceneInitializer` builds its `UnitStatusModel` with `maxStamina: 0`. Cards played by an enemy cost nothing and are not consumed — there is no enemy hand, deck or discard.

`IEnemyCardPicker.Pick(IReadOnlyList<CardData> cards, int minActionCount, int maxActionCount, EnemyPickContext context)` is handed **all** of that enemy's cards, the action-count range and the situation (`EnemyPickContext`: current and max HP, living allies, turn number, turns ended alone; a picker that does not care ignores it), and returns the cards to play **in execution order**. So one call plans the whole turn and `BattlePhaseDirector` just `foreach`es the result (see @docs/battle-scene.md) — the director never decides how many times or in what order the enemy acts. Returning the same card twice is fine and expected, since nothing is consumed.

There are three implementations. `RandomEnemyCardPicker` (most enemies, a placeholder) rolls an action count in `[min, max]` and fills it with uniformly random picks; `FixedOrderEnemyCardPicker` plays the cards in listed order (the tutorial's enemy); `HpPhaseEnemyCardPicker` (the boss) weights each card by whether HP is above or below a threshold, and puts its summon card first when it has no living ally, on turn 1 or after `_summonAfterAloneTurns` turns alone (see "Boss" below). It uses `System.Random` rather than `UnityEngine.Random` so the Model layer stays UnityEngine-free (the same reason `CardPileModel.Shuffle` does). Write a new implementation as a `[Serializable]` class implementing `IEnemyCardPicker` and it appears in the Inspector dropdown with no editor changes.

**Status effects the enemy starts with** are authored onto `EnemyData` as `[SerializeReference] StatusEffectDefinitionBase[] _initialStatusEffectDefinitions`, and `BattleSceneInitializer` adds `CreateInitialStatusEffects()` to the enemy's `UnitStatusModel` before the loop starts.

**A definition is not the effect.** Definitions live in their own folder (`Common/Models/Statuses/Definitions/`) precisely because they are authoring types — they depend on UnityEngine, the effects beside them do not. `StatusEffectDefinitionBase.Create()` builds a fresh `IStatusEffect` every call, so the authored numbers stay read-only: an `IStatusEffect` mutates `Value` and `IsExpired` during a battle (block absorbs, poison stacks), and putting the runtime type in the asset would write those changes back into the ScriptableObject. A definition is only needed for effects a designer actually places on an asset — `PoisonResistanceDefinition` is the only one so far, because `Block`/`Poison`/`Strength` are created in code by card effects. Adding one is a `[Serializable]` class with the `[SerializeField]` parameters and a `Create()`; it shows up in the Inspector dropdown automatically (`StatusEffectDefinitionDrawer`).

Keeping the two apart is also what lets `StatusEffectBase` stay a plain model type with **no UnityEngine dependency** and no parameterless constructor. An earlier iteration did the opposite — it serialized the effects themselves and paid for it with `[Serializable]`/`[SerializeField]` on the model, a `protected` parameterless constructor per effect, `Activator.CreateInstance(nonPublic: true)` in the drawer, and a `Clone()` whose shallow copy would have broken the first time an effect held a reference type. If you find yourself adding machinery of that kind, the definition layer is the thing you are working around.

The layer deliberately has **no `Id`**. An even earlier `IEnemyPassive` carried one that had to be kept in sync with the status effect's, which was its actual flaw; localization is keyed by the created `IStatusEffect.Id` alone.

**Boss: Ransomware (Episode 1, Stage 10, 2026-10-08; rebalanced 2026-11-07).** `Enemies/Ransomware.asset`: 220 HP, always 2 actions, kill immunity, `HpPhaseEnemyCardPicker` (threshold 50 %). The look is Rootkit's, as a placeholder.

| Id | Shown as | Effect | Weight (HP ≥ 50 % / < 50 %) |
| --- | --- | --- | --- |
| `RansomEncrypt` | 暗号化 / Encrypt | 22 damage | 60 / 35 |
| `RansomRestore` | 自己修復 / Self-Repair | heals itself 12 | 15 / 35 |
| `RansomShield` | 暗号シールド / Cipher Shield | 18 block | 25 / 30 |
| `RansomSpawn` | スパイウェア生成 / Spawn Spyware | `SummonEnemyCardEffect` (Spyware, HP 30) | first action on turn 1, and after 3 turns alone |

- **History.** 2026-10-08: 360 HP, 12 / 15 / 12, Botnet HP 25, weights 50/20/30 and 20/50/30. 2026-10-17: raised to 30 / 30 / 25, summon HP 40, high-HP weights 70/10/20; a simulation then found every deck direction but Reflection near 0 %, the shields stacking past 100 and the low-HP self-repair outpacing the player's damage. 2026-11-07: 220 HP and 20 / 10 / 15, with the command changes in "Card system", chosen by the user over a lower-HP-only version (a shorter fight must not be an easier one, so the attack went up as the HP came down); the same day the user asked for about 20 points lower win rates across the board, which became 22 / 12 / 18 (average over the 13 simulated directions: 68 % → 43 % at HP 220 / memory 12, 31 % → 17 % at 160 / 11, 89 % → 77 % at 300 / 15; raising only the attack to 22 barely moved the fully upgraded case).
- **The shield stacks on purpose.** Block carries over for every unit, the boss included; the answer is Bypass (removing enemy block at the start of its turn was tried in the simulation and rejected by the user: stacking makes the fight more strategic).

- **Summoning is the boss's action.** `SummonEnemyCardEffect` only says what to bring and its max HP (`_hp`); `BattleCommandExecuteUseCase.ExecuteEnemyCard` asks `EnemyPartyModel.Summon` (wired in the Initializer). Since every unit's view, presenters, target list and log are built at the start of the battle, a summoned enemy is **not added mid-battle**: `BattleEnemyFactory.CreateReserves` makes one defeated, hidden *reserve* per summon card when the battle starts (in a free enemy view slot), and `Summon` brings it back with `UnitStatusModel.Revive` (HP to max, status effects cleared, `OnRevived` instead of `OnHpChanged`, so no heal number and no heal log; heal reversal does not apply). `EnemySummonPresenter` shows its view. When it is defeated it waits to be summoned again. A defeated reserve counts as defeated, so beating the boss wins even while the summon is down.
- **Which summon.** It was a Spyware at first, then a Botnet (2026-10-08: with the numbers of the time, the Spyware's stacking virus and memory drain decided the fight), and a Spyware again since 2026-11-07 at the user's choice. With today's numbers a Botnet makes the heal-reversal deck stronger (its Bot Repair heals the boss) and the fight easier overall; the Spyware instead attacks the player's buffer and memory.
- **Turns alone** are counted by `BattleEnemy.CountTurnEnd`, which the director calls at the end of each enemy's turn; the picker gets them through `EnemyPickContext.TurnsAlone`.
- **Music.** Its stage (`Stage10`) sets `StageData._bgm` to `BgmId.BossBattle`, a track the user supplies (see @docs/sound.md).
- The aim: a careless deck should lose, and every deck built around a direction (with Bypass for the shield) should be able to win, more often the more the unit has been upgraded. The numbers still need checking in play.

**How an enemy looks** is `EnemyData._portrait` (a still image, also the icon) and `EnemyData._visual`, a `UnitVisualDefinition` that builds the animated `IUnitVisual` (idle / action / hit). The battle only talks to the interface, so switching to Live2D or anything else is a new definition subclass — see "Characters" in @docs/art.md.

`_cardPicker` has **no public accessor on purpose**: `EnemyData.PickCards()` is the only way in. It passes the asset's own cards and action counts to the asset's own picker, so a call site cannot mismatch them. It also absorbs the three ways an Inspector-authored asset can be malformed, so **every caller can `foreach` the result without a null check**: null entries in `_cards` are dropped before the picker sees them, `_minActionCount > _maxActionCount` is clamped, and an unset picker (or a picker that returns `null`) yields an empty list rather than `null`.

### Status effect system (`Common/Models/Statuses/`)

`IStatusEffect` / `StatusEffectBase` expose **one hook per kind of quantity** for value interception, plus `OnTurnStart` / `OnTurnEnd` for side effects. Each enum lives in `Statuses/Enum/`:

| Hook | Enum | Applied by | Meaning |
| --- | --- | --- | --- |
| `OnHpChange` | `HpChangeType` (`TakeAttackDamage`, `TakePoisonDamage`, `Heal`, `TakePiercingDamage`, `PayHp`) | `UnitStatusModel.ChangeHp` | what this unit **receives** |
| `OnStaminaChange` | `StaminaChangeType` (`Gain`, `Consume`) | `UnitStatusModel.ChangeStamina` | what this unit **receives** |
| `OnDealtValue` | `DealtValueType` (`AttackDamage`, `Block`, `Poison`, `Strength`, `Heal`, `Regeneration`) | `UnitStatusModel.CalculateDealtValue` | what this unit **produces** |
| `OnStatLimit` | `StatLimitType` (`MaxStamina`, `HandSize`, `ActionCount`) | `UnitStatusModel.CalculateLimit` (and `MaxStamina`) | a **limit** on this unit — the caller passes the rule's base value |
| `OnCommandStart` / `OnCommandDestination` / `OnCommandEnd` | `CommandDestination` (`Discard`, `ReturnToHand`, `Purge`) | `UnitStatusModel.BeginCommand` / `CalculateCommandDestination` / `EndCommand`, called by `BattleCommandExecuteUseCase.ExecuteNext` for the player's commands only | the **next command** the player executes: where it goes afterwards. How many times it runs is `StatLimitType.CommandRepeat` (base 1) |

The first two mutate the unit and take a **signed delta**; `CalculateDealtValue` mutates nothing and takes a **positive magnitude**, returning the modified magnitude for the caller to apply. Every card effect base runs its own number through the *user's* `CalculateDealtValue` before applying it, which is what makes an attacker-side buff work without each effect class knowing about it — `StrengthStatusEffect` adds its `Value` to `DealtValueType.AttackDamage` and nothing else. Note that a buff on the attacker and a debuff on the target are **different mechanics**: strength rides one attack no matter how many targets there are, while "takes more damage" would be an `OnHpChange` effect on the target. Each base also guards `baseValue <= 0` *before* the interception, so a zero-damage card stays a zero-damage card however much strength is stacked.

**Adding an interception point means adding an enum value, not a method on the interface.** These started as one `OnXxx(uint)` method per point, all with the identical `uint f(uint)` shape and growing with every new kind of effect; they were collapsed into a single generic hook, then split again along resource lines so that **`UnitStatusModel.ChangeHp` cannot be handed a stamina type**. A single flat enum made that a runtime check; one enum per resource makes it a compile error. Split by quantity, not by effect — a further hook is warranted only when a genuinely different quantity starts being intercepted.

An implementation must return `value` untouched for types it does not care about — `StatusEffectBase` does exactly that by default, so an override is normally one `if (type != ...) return value;` followed by its own logic. `TakePoisonDamage` is deliberately separate from `TakeAttackDamage`: a resistance built on the latter could not tell poison from an ordinary hit and would soak everything. The flip side is that `BlockStatusEffect` handles only `TakeAttackDamage`, so **block does not absorb poison**.

**There is no `TakeDamage`/`Heal` pair — both are `UnitStatusModel.ChangeHp(StatusInterventionType type, int amount)`**, one generic HP change. Two things are deliberately separated there:

- **`type` decides only which interventions run**, never the direction. The caller always names it: `AttackCardEffectBase` passes `TakeAttackDamage`, `PoisonStatusEffect` passes `TakePoisonDamage`. An earlier version had poison call `ApplyIntervention(PoisonDamage, ...)` and then `TakeDamage(...)`, which silently ran the damage through **both** poison and attack interventions — passing the kind in makes that mistake unrepresentable. `HpChangeType` contains only HP changes, so a stamina type cannot even be passed — no runtime validation is needed.
- **The sign of `amount` decides the direction** — negative reduces HP, positive restores it — which is why every number in this model is `int`, not `uint`. A status effect can therefore flip the sign (a heal-reversal effect returns `-value` for `Heal`) and `ChangeHp` needs no knowledge of it. Values flowing through `OnIntervene` are HP deltas, so an implementation that only cares about damage must ignore positive values — `BlockStatusEffect` returns early on `value >= 0` for exactly that reason.

Magnitudes authored in the Inspector (damage, cost, block, poison, HP) stay non-negative via `[Min]`; it is the *delta passed to the model* that carries a sign. `UnitStatusModel.StatusEffects` is a flat list and never holds two effects with the same `Id`: when `AddStatusEffect` finds a match via `IsSameEffect`, it hands the incoming value to that effect's `OnDuplicated(value)` and drops the new instance. **What a repeat application does is therefore each effect's own decision** — accumulate, take the larger, ignore — which is why the method is named for the situation (a duplicate arrived) rather than for one particular response to it. Expired effects are purged at both `StartTurn()` and `EndTurn()`.

**Effects last until something removes them.** `IsExpired` starts `false` and only the effect itself flips it, so "permanent" is the default and a one-shot has to opt out — `BlockStatusEffect` sets `IsExpired` once its value reaches 0, which is what gets it purged at the next `StartTurn`/`EndTurn`. There is no separate base class for the two kinds; an earlier `PermanentStatusEffectBase` existed only to supply `OnDuplicated`, and once that moved down to `StatusEffectBase` it had nothing left to carry.

`StatusEffectBase.OnDuplicated` **keeps the larger value by default** (clamped at `MaxValue`). That is the game's rule for **permanent effects**: adding would let repeat applications grow without limit. **Buffs and damage over time** (strength, virus, worm) step one notch past it since 2026-10-04: they keep the larger value **plus 1** (`StatusEffectBase.StackPlusOne`), so stacking pays a little without compounding. Most effects inherit it and override only the one interception hook they care about. Two kinds step outside the rule, each with its own override: block, which is worn down and runs out, so stacking it is the point (it adds, clamped at `MaxValue`, written to avoid overflow); and timed effects, which keep the longer duration (below).

| Effect | Applied by | Intercepts | Duplicate behaviour |
| --- | --- | --- | --- |
| `BlockStatusEffect` | `BlockCardEffectBase` | `HpChangeType.TakeAttackDamage` | **accumulate** (its own override — not a permanent effect) |
| `PoisonStatusEffect` | `PoisonCardEffectBase` | — (acts on `OnTurnEnd`) | the larger value + 1 (`StackPlusOne`) |
| `PoisonResistanceStatusEffect` | `PoisonResistanceDefinition` | `HpChangeType.TakePoisonDamage` | keeps the larger value (inherited) |
| `RegenerationStatusEffect` | `RegenerationCardEffectBase` | — (acts on `OnTurnEnd`) | keeps the larger value (inherited) |
| `StrengthStatusEffect` | `StrengthCardEffectBase` | `DealtValueType.AttackDamage` | the larger value + 1 (`StackPlusOne`) |
| `MemoryBoostStatusEffect` | `MemoryBoostCardEffect` | `StatLimitType.MaxStamina` (+`Amount`) | timed: keeps the longer duration |
| `HandSizeUpStatusEffect` / `HandSizeDownStatusEffect` | `HandSizeUp/DownCardEffect` | `StatLimitType.HandSize` (±`Amount`) | timed: keeps the longer duration |
| `StunStatusEffect` | `StunCardEffect` | `StatLimitType.ActionCount` (→ 0) | timed: keeps the longer duration |
| `HealReversalStatusEffect` | `HealReversalCardEffect` (enemy card `Corrupt`) | `HpChangeType.Heal` (sign flipped) | timed: keeps the longer duration |
| `LoopbackStatusEffect` | `LoopbackCardEffect` | `OnCommandDestination` (`Discard` → `ReturnToHand`) | next command: used up by it |
| `MultithreadStatusEffect` | `MultithreadCardEffect` | `StatLimitType.CommandRepeat` (→ `Value`), `OnCommandDestination` (→ `Purge`) | next command: used up by it; a repeat keeps the larger count |
| `WormStatusEffect` | `WormCardEffect` | — (acts on `OnTurnEnd`: `Value` + half the owner's `BlockValue` (`BlockRatePercent` 50, rounded down; the full block was too strong) as `TakePoisonDamage`, so block counts but never absorbs it) | the larger value + 1 (`StackPlusOne`) |
| `LayerBoostStatusEffect` | `BattleSceneTransitionData.EnemyStrength` (challenge enemies past layer 10) | `DealtValueType.AttackDamage` (+`Value`) | keeps the larger value (inherited); separate from strength so the two do not cancel |
| `TokenBonusStatusEffect` | `TokenBonusCardEffect` (`Mining`) | — (read at the end of the battle into `BattleResult.TokenBonusPercent`) | **accumulate** (its own override; each use is purged, so it is capped by the copies in the deck) |
| `OverchargeStatusEffect` | `UnitStatusModel.ChangeHp` (healing past max HP, player only) | `DealtValueType.AttackDamage` (+1 per `AmountPerBonus` 5); damage is taken from it by `ChangeHp` itself | **accumulate**, capped at `CapPercent` 30 % of max HP |

**Overcharge (オーバーチャージ, 2026-11-07)** is healing past max HP, kept so the sustain deck has a way to win. It is stored and spent by `UnitStatusModel.ChangeHp`, not by its own hook:

- **Storing.** When the final amount is a heal that would go past `MaxHp`, the excess becomes an `OverchargeStatusEffect`, after `OnHpChanged` (so the log reads "healed", then "gained"). A heal turned into damage by heal reversal stores nothing. Only units with `UnitStatusModel.CanOvercharge` store it; `BattleSceneInitializer` sets it on the player. Enemies do not, or the boss's self-repair at full HP would make it stronger.
- **Spending.** Damage is taken from it **after every intervention** (block first) and before HP. Doing it in `ChangeHp` rather than in an `OnHpChange` hook is deliberate: hooks run in the order the effects were added, which would let an overcharge gained before a block absorb ahead of it. `HpChangeType.InstantKill` ignores it. Spent to 0, it expires and goes at the next purge, like block.
- **Turn hooks iterate a copy of the list** (`StartTurn` / `EndTurn`), because a regeneration tick at full HP adds the overcharge from inside the loop. This threw `Collection was modified` before it was fixed; `OverchargeTests` keeps it fixed.

**Next-command effects** derive from `NextCommandStatusEffectBase`: they act on the next command the player *starts* after they were applied, and are used up when that command ends. `OnCommandStart` arms them and `OnCommandEnd` expires only the armed ones, so the command that applied an effect is not the one that spends it, and the effect survives the end of the turn. When several redirect the same command, each decides what it beats: `Multithread` always purges, `Loopback` only turns a discard into a return, so **purge wins over return**.

**Timed effects** derive from `TimedStatusEffectBase`: `Value` is the turns left and `Amount` the size, fixed at application. A turn counts only if the effect was already there when its owner's turn **started** (`OnTurnStart` arms it, `OnTurnEnd` counts it), so "2 turns" means the owner's next two turns whether it was applied during the owner's own turn (hand size) or the opponent's (stun, heal reversal). A repeat keeps the longer duration instead of adding — stacking stuns must not lock an enemy forever. Their numbers show next to the effect name in battle (`UnitPresenter` appends `Value`).

So re-applying a permanent effect never compounds it and never weakens it. For strength, virus and worm each repeat adds one: three strength cards of +2 leave +4 (2, then 3, then 4), and a +5 after that leaves +6. Regeneration and poison resistance only keep the larger value. (Strength and regeneration used to accumulate and were changed on 2026-09-27 at the user's request; on 2026-10-04 the user asked for buffs and damage over time to gain 1 per repeat.) `RegenerationStatusEffect` is poison's mirror image — same `OnTurnEnd` timing, same never-expiring value, opposite sign. Its tick goes through the plain `HpChangeType.Heal`, not a `TakeRegenerationHeal` of its own: poison needed its own type so a resistance could tell it from an ordinary hit, whereas nothing yet wants to boost or block a regen tick without also affecting a direct heal. Add one the day something does. Because a permanent effect's value therefore grows by at most one per application, those effects need no cap and leave `MaxValue` at the default; block's 999 is the cap that matters. `TakePoisonDamage` being separate from `TakeAttackDamage` is also why `BlockStatusEffect` **does not absorb poison**.

### Card pile model (`Common/Models/CardPileModel.cs`)

Backs any pile of cards (deck/hand/discard, all three are just separate `CardPileModel` instances). Fires `OnCardAdded` / `OnCardRemoved` / `OnCleared`. `MoveAllTo` is implemented via `RemoveCard`/`AddCard` (not a direct list splice) specifically so the removal event still fires on the source pile — don't "optimize" this back to a splice. `IReadOnlyList<CardData> Cards` is exposed for reading; all mutation goes through `AddCard`/`RemoveCard`/`Clear`/`MoveAllTo`/`Shuffle`.

### `UnitStatusModel` (`Common/Models/UnitStatusModel.cs`)

Holds HP and stamina for one unit (player or enemy — nothing unit-side is scene-specific). `ChangeHp`/`ChangeStamina` run every status effect's corresponding hook before applying the final amount, then fire `OnHpChanged`/`OnStaminaChanged`. `MaxStamina` is **computed**: `BaseMaxStamina` (the constructor value) run through `CalculateLimit(StatLimitType.MaxStamina, …)`. When an effect that raised it expires, the purge clamps `Stamina` down to the new maximum and fires `OnStaminaChanged`, which is why a memory boost played this turn is felt at the restore that ends this turn and lasts exactly its duration. `StartTurn`/`EndTurn` run the corresponding status-effect hooks and purge expired effects. `ChangeHp` also stores and spends overcharge (see "Status effect system"), and `SpendBlock(amount)` takes block away without damage (Reflection), raising `OnStatusEffectValuesChanged` so the barrier and the log follow it.

Status-effect notifications come in two kinds.

- `OnStatusEffectsChanged` fires when the list gains or loses an entry.
- `OnStatusEffectValuesChanged` fires when an incoming effect is merged into an existing one through `OnDuplicated` — stacked block, for example, where the count stays the same but a value changes.

A value consumed by interception during `ChangeHp` (block absorbing a hit) raises neither. Watch `OnHpChanged` for that: it fires even when the final amount is 0.

### Global toast messages (`Common/Message/`)

`MessageToast` is a self-instantiating `DontDestroyOnLoad` singleton: `MessageToast.Show(string)` lazily instantiates `Resources/MessageToast.prefab` on first use and keeps it alive across scenes. Repeated calls while it is up swap the text and reset the auto-hide timer without re-running the slide-in. It never outlives a scene: on `SceneManager.sceneLoaded` it hides at once with no animation (the load happens under the transition cover, so the cut is not seen), so a message from the previous screen is never carried into the next. `MessageUseCase.ShowMessage(...)` is a thin wrapper around it so pure UseCase classes (which otherwise have no View dependency) can surface a message — e.g. a failed card use shows a reason via this without the UseCase needing to know about any View type.

`MessageToast` is a **non-blocking notification**: it glitches in at the top of the screen, holds for `_displayDuration`, and glitches out. The screen underneath stays fully playable the whole time, which is the requirement it was rebuilt around (the earlier version blurred the screen and sat in the middle of it):

- **No blur.** It does not push `PopupBlurController`.
- **No input.** Its canvas has no `GraphicRaycaster`, the `Slider` has a `CanvasGroup` with `blocksRaycasts = false`, and every Graphic has `raycastTarget` off.

It still draws on the popup tier: every `Show` rebinds its canvas to `UICameraRig.Current.PopupCamera` (falling back to Screen Space - Overlay in a scene that has no rig), on sorting layer and GameObject layer `UI_Popup`, so it is above all other UI.

**The glitch.** Appearing is a slide from above, a CRT-style open (the panel's y-scale), alpha flicker, noise bars, cyan/magenta ghost copies of the text, and a left-to-right "decode" out of scramble characters. The text's `LayoutElement.preferredWidth` is pinned to the final string's width, so the decode never resizes the panel.

**Timing** runs on `Time.unscaledDeltaTime`, capped per frame. Without the cap, one long frame (a hitch, or an unfocused Editor) skips the whole animation and the hold time.

**Layout.** Its background is the one place in the project that sizes itself with a `HorizontalLayoutGroup` + `ContentSizeFitter` instead of script-set positions, because the panel has to hug a string of unknown length. That coexists with the animation because the animation only moves the outer `Slider` and scales the panel. It never sets the position of anything the Layout Group arranges (compare the usual objection to Layout Groups in @docs/battle-scene.md).

### Game balance (`Common/ScriptableObjects/CampaignBalance.cs`)

**Every number that tunes the campaign, and the stages it is played on, live in one asset, `Assets/ScriptableObjects/CampaignBalance.asset`** — not in code constants and not scattered over scene Inspectors. Its sections:
- **Stages:** one `CampaignData` per episode (`_episodes`, Episode 1 first; `GetEpisode(index)` / `HasEpisode(index)`). They are `Assets/ScriptableObjects/Stages/Episode1..3.asset`; for now Episode 2 and 3 are copies of Episode 1 that point at the same `StageData` and have no opening conversation, ready to be tuned separately. The map plays the one for `CampaignProgressRepository.EpisodeIndex` and has one cell per stage in it; the title asks it which episodes have stages.
- **Start:** tokens, max HP, max memory, and owned commands held before the First Build (empty today).
- **First Build:** the chip opened when a campaign starts and how many (`FirstBuildChip.asset` ×10), the commands guaranteed among them (Attack ×5, Block ×4), and how many of each role the automatic deck takes (attack 10, defence 6, support 4; the sum must equal `DeckEditConstants.MaxDeckSize`).
- **Score and rank:** points per token, penalty per Backup Boost, the eight rank thresholds (E..SSS, kept ascending with E = 0 by `OnValidate`), and the ranks where the Best and Normal routes begin.
- **Unit upgrade:** step, limit, base cost and cost per level, for HP and for memory.
- **Chips:** a price multiplier over every chip's own `CardPackData._price` (it also applies to singles).
- **Singles:** the market's one-command-at-a-time prices per rarity (Normal 40, Rare 90, Epic 180) and the limit per command over a campaign (3), handed to the shop as `CreateSinglePurchaseRule()` → `SinglePurchaseRule` (see @docs/card-shop-window.md).

**A stage can make its enemies stronger** with `StageData._enemyHpPercent` (default 100) and `_enemyAttackBonus` (default 0), the same `EnemyStrength` the challenge uses (max HP scaled, attack bonus as `LayerBoostStatusEffect`). The story uses none of it: every stage is 100 % / +0 since 2026-10-08, so the stages can be level-designed from plain numbers.

Each stage's enemies and rewards stay on its `StageData`, and each chip's price and contents on its `CardPackData`. The balance asset only chooses *which* stage list is played.

- **Models never see the asset.** The Model layer must not depend on UnityEngine, so the asset hands out plain rule objects: `CreateScoreRule()` → `CampaignScoreRule` for `CampaignResultCalculator`, `CreateUnitUpgradeRule()` → `UnitUpgradeRule` for `UnitUpgradeModel`, and `CreateAutoDeckRule()` → `AutoDeckRule` for `AutoDeckBuilder`. `OpenFirstBuild(random)` opens the First Build: chip `CardCount` × chip count commands, the guaranteed ones taking slots first and the rest drawn with `CardPackData.OpenCards`. The chip multiplier reaches the market's `CardShopModel` through `MapRunUseCase.CreateCardShopModel`, so the model still knows nothing about the campaign.
- **Users.**
  - `MapSceneInitializer._balance`: the stages, the starting kit, the upgrade rule, the score rule and the chip multiplier.
  - `EndingSceneInitializer._balance` / `ResultSceneInitializer._balance`: only to score their standalone mock results.
- **Tests.** Tests of the calculation itself build their own rule, so tuning the asset cannot break them. Tests of the balance read the assets: `CampaignRankReachabilityTests` walks **every** `CampaignBalance` asset and checks it against its own stages (stages set and non-empty, SSS reachable, Backup Boost penalty larger than any defeat reward); the thresholds test checks the default asset. `FirstBuildBalanceTests` also walks every balance asset and checks every balance asset's First Build (chip set, guaranteed commands present, the role counts adding up to the deck size, a full deck from any draw).
- **Challenge.** Every challenge uses the `EndlessBalance` asset for what differs (enemy candidates, strength per ten layers, victory rewards) and this one for the rest; the layer counts are `ChallengeCatalog`. See "Challenge mode" in @docs/map-scene.md.
- **Difficulties.** One difficulty is one balance asset: duplicate it, point it at a different `CampaignData` (other `StageData` with other enemies and rewards) and change the numbers. Nothing picks between them yet — the map uses the one on `MapSceneInitializer._balance`. Choosing one on the title screen would also need the choice stored in the save data, so a resumed campaign keeps its difficulty.

### Saving (`Common/Save/`)

Campaign progress and clear records are **encrypted files** under `Application.persistentDataPath/save/` (`campaign.sav`, `clear.sav`): `JsonUtility` JSON, sealed by `SaveCipher`. PlayerPrefs was ruled out: on Windows it lands in the registry, is a poor fit for lists like a deck, and is hard to back up, delete or sync.

- **`SaveFileStorage`** reads and writes text files in one folder and knows nothing about their contents. It writes atomically: to `<name>.tmp`, then `File.Replace` / `File.Move`. A crash mid-write therefore never destroys the previous save.
- **`CampaignSaveService`** is the entry point (`Default` uses the real folder; tests construct one on a temp folder). It is a facade over three classes: `SaveFileCodec` (reading and writing one file: encryption, the plaintext migration), `CampaignSaveSnapshot` (repositories ⇄ `CampaignSaveData`, `Create` / `Restore`) and `ClearRecordStore` (`clear.sav`: clears, challenge records, discovered commands and enemies).
  - **`campaign.sav`** (`CampaignSaveData`) is a snapshot of the four campaign repositories: progress, deck and owned commands, tokens, unit upgrades. It also stores `stageCount` and `savedAt` so the title can describe it without the campaign asset.
  - **`clear.sav`** (`CampaignClearData`) gets one `CampaignClearRecord` per clear: date, the episode (`episodeIndex`, 0-based, added in version 2; a version 1 record reads as Episode 1), score, rank and route names, tokens, Backup Boosts. The title unlocks the next episode from it (`HasCleared`, see @docs/title-scene.md).
- **Commands are stored by `CardData.Id`, never as asset references.** `Restore` resolves them through `Assets/ScriptableObjects/CardCatalog.asset` (`CardCatalog`) and skips unknown ids with a warning, so a renamed command drops out instead of breaking the load. **Adding a command means adding it to the catalog** (`CardCatalogTests` fails otherwise).
- **Singles bought** are saved as `purchasedSingles` (ids, one per command bought), added in version 2; a version 1 file reads as "nothing bought yet".
- **Decks** are saved as `decks` (one `SavedDeck { cards }` per slot, since `JsonUtility` cannot write an array of arrays) and `activeDeckIndex`, added in version 3. `deck` still holds the active deck. A version 1 or 2 file reads its single `deck` into slot 1 as the active deck.
- **The episode being played** (`CampaignProgressRepository.EpisodeIndex`, set by the title's `Reset(episodeIndex)`) is saved as `episodeIndex`, added in version 4 (older files read as Episode 1), so a resumed campaign records its clear under the right episode.
- **Challenge** (`endless.sav` — one suspended challenge of any kind; same `CampaignSaveData` with `isEndless` / `endlessSeed` (version 5) and `challengeType` / `challengeLayerCount` (version 6; a version 5 run reads as Endless)) is written by `SaveProgress` while `RunModeRepository.IsChallenge`; `TryLoadProgress` / `DeleteProgress` take a `RunMode` (Story by default), so one Story and one challenge can be suspended. A defeat or a clear calls `FinishChallengeRun`, which updates that challenge's `CampaignClearData.challengeRecords` entry (version 6: best layer, cleared; the old `endlessBestLayer` is read as Endless's) and deletes `endless.sav`. `Restore` sets the run mode from the file.
- **Discovered commands** (the title's command collection) are `CampaignClearData.discoveredCommands` (version 4): every `SaveProgress`, `RecordClear` and `FinishChallengeRun` adds the owned commands (`RecordDiscoveredCommands`, written only when something is new). They survive the run; deleting user data clears them with `clear.sav`. **Fought enemies** (the enemy collection) are `CampaignClearData.discoveredEnemies` (version 5), added by `RecordDiscoveredEnemies` when the map starts a battle. `Restore` sets the run mode from the file.
- **Versioned.** Both files carry `version`. `TryLoadProgress` refuses a corrupted file or a newer version rather than misreading it. When the shape changes, bump `CurrentVersion` and add the old reader there.
- **When it saves.** `MapRunUseCase` (the map's run flow) calls `SaveProgress` after applying a battle result (a win **and** a loss — otherwise quitting would erase a defeat's Backup Boost), a saved deck edit, after every market purchase and every applied unit upgrade, and when the map's Menu suspends the run (`Suspend`). It does not save right after the last stage is won: claiming that reward records the clear and deletes the progress (`LoadEnding`), so quitting before the claim resumes before the last stage.
- **Resuming.** The title's Campaign button (ストーリー on screen) checks `TryLoadProgress` and opens `CampaignResumeDialog` (Resume / Discard / Cancel; Discard asks "really discard?" through the shared `ConfirmDialog` first, then opens the episode selection; with no save the episode selection opens directly; see @docs/title-scene.md). A new campaign or a discard clears all four repositories so the map seeds them fresh.
- **Encryption and tamper detection (`SaveCipher`).** File layout: `L2CR` magic, format byte, a fresh random 16-byte IV per write, AES-256-CBC ciphertext, then an HMAC-SHA256 over everything before it (encrypt-then-MAC). The MAC is checked in constant time **before** decrypting, so an edited or damaged file is refused (logged as an error and treated as no save) rather than half-read. Both keys are derived with SHA-256 from one secret that `BuildSecret` assembles from scattered bytes. **Changing it makes every existing save unreadable.** The limit is honest: an offline game has to carry its key, so this stops hand-editing and catches corruption, but not someone who reverse-engineers the build.
- **Migration.** A plaintext `campaign.json` / `clear.json` from before encryption is still read once when no `.sav` exists; the next save writes the `.sav` and deletes the `.json`.
- **Settings (`settings.json`).** `GameSettingsSaveService` writes `GameSettingsRepository` as plain, readable JSON (`GameSettingsData`, versioned like the others) — settings need no tamper protection. Fields: battle speed; BGM and SE volume (0–1, multiplied over `SoundLibrary`'s own mix by `SoundPlayer` at play time, so they need no applying); language (a `Locale` code, empty = the package's own startup choice); fullscreen or windowed; and whether the tutorial has been seen (version 2; a version 1 file reads them as defaults). It loads once at start-up (`RuntimeInitializeOnLoadMethod`, before the first scene), then `Common/Settings/GameSettingsApplier` sets the frame rate (`ApplyFrameRate`: `Application.targetFrameRate` 60, or the display's refresh rate when that is not a multiple of 60 — mobile ignores `vSyncCount` and runs at 30 fps without it, which made the stepped animation look choppy on Android) and applies the display mode (`Screen.SetResolution`: the display's size in `FullScreenWindow`, or the largest 16:9 of 1920×1080 / 1600×900 / 1280×720 smaller than the display in `Windowed`; nothing in the Editor) while the language is chosen during Localization's own initialisation by `Common/Settings/GameSettingsLocaleSelector`, a startup locale selector placed right after the command-line one in Localization Settings (applying it from the initialisation's completion callback threw "Reentering the Update method", because that callback can fire inside a synchronous wait). Applying options changes `SelectedLocale` directly. It saves when an options window applies, and when the tutorial is first watched. A missing, broken or newer file leaves the current values alone.
- **Deleting user data** (the title's Options) deletes `campaign.sav`, `endless.sav` and `clear.sav` (`DeleteProgress`, `DeleteClearData`) and resets the tutorial-seen flag. Volumes, language, display and battle speed stay: they are the player's environment, not their progress.
- Save failures are logged, never thrown, and the next milestone saves again.

### Debug features (`Common/DebugMenu/`) — never in a release build

**Everything debug is wrapped in `#if UNITY_EDITOR || DEVELOPMENT_BUILD`** — the files themselves, and every line that uses them. A release build (Development Build unchecked) therefore does not compile any of it. This was verified by compiling the player scripts both ways (`PlayerBuildInterface.CompilePlayerScripts`): the release assembly contains no `DebugMenu` types and no `BattlePhaseDirector.DebugForceFinish`. A reference outside the `#if` breaks only the release build, which is not what the Editor compiles, so check new debug code with that same compile before trusting it.

- **Opening it.** Typing **0707** on the number keys (top row or numpad) shows the debug panel; typing it again hides it. `DebugCommandSequence` is the plain matcher (EditMode-tested). After a match it clears its input, so `070707` does not toggle twice. Digits typed into a number field are not counted; clicking outside the panel, or running a command, leaves the field so 0707 works again.
- **Opening it without a keyboard** (Android). Tapping the screen corners **top-left → top-right → top-left → top-right** within 2 seconds does the same (`DebugScreenCorner` maps a tap to `0` / `7` / other, fed to a `DebugCommandSequence` with a `maxDuration`; both EditMode-tested). A corner is a square of 10 % of the screen's shorter side, a little inside the edge for notches and rounded corners. It reads `Pointer.current` straight from the Input System, so a mouse click works too, and a game button under the corner is pressed as well.
- **`DebugMenuOverlay`** is a self-instantiating `DontDestroyOnLoad` object (`RuntimeInitializeOnLoadMethod`). It reads keys through the Input System, which is why the runtime asmdef references `Unity.InputSystem`. It draws with **IMGUI**, deliberately outside the game's uGUI and tone & manner, and has no prefab or `Resources` asset, since those would ship even when the code does not. Labels are plain English and not localized.
- **Adding a debug action.** A scene calls `DebugMenuOverlay.AddCommand(label, action)` (a button) or `AddIntCommand(label, defaultValue, action)` (a number field plus a button; the action gets the field's value, digits only) in its Initializer, keeps the returned `IDisposable`, and disposes it in `OnDestroy`, all inside the `#if`. The panel lists only what the current scene registered.
- **So far:**
  - BattleScene's **Battle: Win / Battle: Lose** (`BattlePhaseDirector.DebugForceFinish`, see @docs/battle-scene.md).
  - BattleScene's **Battle: Speed %** (sets `GameSettingsRepository.BattleSpeed`; 100 = normal).
  - MapScene's **Map: Clear Stage** (`MapDebugCommands.ClearCurrentStage`). It reloads the map with a `MapSceneTransitionData` carrying a victorious `BattleResult`, exactly as if the current stage's battle had been won, so the reward window, the move to the next room, the save and (on the last stage) the ending all run as usual. It refuses once the campaign is complete or during the tutorial.
  - MapScene's **Map: Clear All (score)** (`MapDebugCommands.ClearAllStages`). It marks every stage cleared and goes to the ending with a `CampaignResult` for the typed score (as remaining tokens with 0 Backup Boosts), so rank and route follow the normal rules (see @docs/ending-and-result-scenes.md).
  - MapScene's **Map: Own All Commands** (`MapDebugCommands.SetAllOwnedCount`, default 20). Sets the owned count of every command in the market's chips (the player's commands; `CardCatalog` also holds enemy-only ones) to the typed number, leaving the decks alone.
  - MapScene's **Map: Set Tokens** (`MapDebugCommands.SetTokens`, default 9999). Sets the tokens to the typed number.
  - Both save through `MapRunUseCase.SaveIfAllowed` and reload the map so its displays are rebuilt. Saving also records the owned commands as discovered, so after Own All Commands the title's command collection shows every command.

### Art assets

**Tone & manner is @docs/tone-and-manner.md** — the concept, colour roles, shapes, pixel rules, motion, interaction and wording that every screen follows, plus what was tried and dropped. Read it before touching anything visual or interactive. **Maintain it from the user's feedback:** whenever the user comments on look, motion, interaction or wording, update that doc in the same change as the code (add or adjust the rule; move an overturned choice to its "Tried and dropped" table with the date and reason). Keep it and its `.ja.md` mirror in step.

Art is generated by Python under `ArtSource/` into fixed file names under `Assets/Art/`, and is imported by `Editor/ArtImportPostprocessor.cs`. Everything there is a **placeholder meant to be swapped** — see @docs/art.md for the pipeline, which slot each image plugs into, and how to replace one with real art.

### Sound

**Every sound goes through `Common/Audio/AudioUseCase`** (`PlaySe(SeId)`, `PlayBgm(BgmId)`) — the same shape as `MessageUseCase` wrapping `MessageToast`. Never touch `SoundPlayer` or an `AudioSource` for game sound directly. Each scene picks its BGM with a `SceneBgm` component. Sounds are generated by `SoundSource/` into fixed file names under `Assets/Audio/`; the pipeline, the id ↔ file convention and where each sound is played from are in @docs/sound.md, and what the sound should be is in @docs/tone-and-manner.md.

### Art, fonts and copyright

Every asset that ships must be either **our own original work** or **third-party material whose licence allows it and is recorded below**. Follow these rules for any art you generate or add, and treat them as part of the task, not a review step afterwards.

**Generating art (`ArtSource/`)**

- Draw from primitives — shapes, paths, gradients, patterns — composed for this game. Never trace, copy, recreate or "closely reproduce" an existing work: another game's cards, UI, icons or screen layout; an anime/manga/film character; a logo or mascot; a specific artist's illustration.
- Genre aesthetics are fine: cyberpunk, synthwave, pixel art, pop art, halftone. A general style is not protected; a **specific, recognisable expression** is. If a design would make someone say "that's the X from Y", change it.
- No real brands, trademarks or logos (including ones that are only hinted at), no real people's likenesses, and no names that collide with a known IP.
- Avoid legally protected emblems even when they would be convenient symbols. In particular, **never a red cross or red crescent on a white background** (the Red Cross emblem is protected by the Geneva Conventions and national law). Heal icons use a plus sign in a game palette instead (the current one is lime on dark). Also avoid national flags, coats of arms, and the Olympic rings.
- **Sound follows the same rules as art**: synthesised from oscillators and noise in `SoundSource/`, melodies generated for this game; never sample, re-create or quote an existing track, jingle or recognisable sound logo. Third-party sounds need a permissive licence and a row in the table below.
- Do not embed text in images. Text belongs in `CommonText` so it can be localised; it also keeps fonts out of the images, where their licensing is harder to track.

**Third-party material (fonts, images, sounds, AI-generated images)**

- **Only permissive licences that allow commercial use:** SIL OFL, CC0, CC-BY (with the attribution), MIT/Apache and similar. No "free for personal use", no unclear sources, and nothing ripped from another game or website.
- **Keep the licence file next to the asset** (e.g. `Assets/Fonts/DotGothic16/OFL.txt`), and add a row to the table below with its source URL. OFL fonts must ship with their licence, and a font with a Reserved Font Name must not be renamed while keeping that name.
- **Images from an external generative-AI tool** may only be used if that tool's terms allow commercial use. Record the tool and the date in the table. Never prompt with an artist's name or an existing IP.
- **Note on generated art.** Art generated by code or AI may have weak or no copyright protection of its own in some jurisdictions. It is safe to *use*, but hand-made art is what gives the project something protectable.

| Asset | Licence | Source | Notes |
| --- | --- | --- | --- |
| `Assets/Fonts/DotGothic16/` | SIL OFL 1.1 | https://github.com/google/fonts/tree/main/ofl/dotgothic16 | `OFL.txt` alongside |
| `Assets/TextMesh Pro/Fonts/LiberationSans.ttf` | SIL OFL 1.1 | bundled with TextMesh Pro | `LiberationSans - OFL.txt` alongside |
| `Assets/TextMesh Pro/Sprites/EmojiOne.png` | EmojiOne free licence (CC-BY 4.0, **attribution required**) | bundled with TextMesh Pro | Set as TMP's default sprite asset, so it ships. Unused so far — either add the attribution to the game's credits or clear `m_defaultSpriteAsset` in `TMP Settings.asset` before release |
| everything under `Assets/Audio/` | original — generated by `SoundSource/` | this repo | placeholder sounds and BGM |
| everything under `Assets/Art/` | original — generated by `ArtSource/` | this repo | characters are code-drawn pixel art (placeholder quality) |

A **legal review is still recommended before a commercial release.** These rules reduce the risk; they are not legal advice.

### Building uGUI

For any Canvas / RectTransform / Button / Layout work, load the `unity:ui-ugui` skill and follow it, **with the project overrides below**. Where the two disagree, this section wins. What the UI should *look and behave* like is @docs/tone-and-manner.md; this is how to build it.

**Adopted from the skill:**

- **Inspect before you edit.** Dump the hierarchy (a `run_script`) before touching a scene or prefab.
- **Fix in place; never destroy and rebuild to fix.** Destroyed objects null out every reference to them (see the component-swap warning above).
- **One change at a time, verified.** For anything visual, verification means a `capture_game_view` in Play mode.
- **Order of setup.** Set the anchor preset, then **a pivot that matches the anchor** (a top-right element pivots top-right), then the position. Check the result has non-zero size and sits inside its parent.
- **Raycast Target.** `raycastTarget = false` on every non-interactive Graphic: text, decoration, overlays, particles' neighbours. `true` only on things that take input. Full-screen catchers are deliberate and named (`BattleCancelAreaView`).
- **Interaction readiness.** Exactly one EventSystem per scene, and a GraphicRaycaster on every canvas that takes input. The toast canvas deliberately has none.
- **Contrast.** New text must contrast with what is behind it. Deliberately low contrast (disabled, decoration) is fine.
- **Exact values.** When the user gives exact values (px, colours, spacing), apply them exactly.
- **Layout Group + ContentSizeFitter pitfalls.**
  - A ContentSizeFitter on the same object as a Layout Group that controls child size conflicts with it.
  - A ContentSizeFitter on a child whose parent Layout Group controls that axis is overridden and does nothing.

**Project overrides:**

- **Layout Groups only for static, content-sized UI** (the status-effect list, the toast panel). Anything that moves or animates — hand, execution list, target list, card motion — is **script-positioned** (see @docs/battle-scene.md). The skill's general "prefer Layout Groups" does not apply to those.
- **Short UI type names.** Scripts use `using UnityEngine.UI;` and short type names. Only fully qualify (`UnityEngine.UI.Image`) where a name is actually ambiguous.
- **Two-choice confirmation is `Assets/Prefabs/ConfirmDialog.prefab`** (`Common/Views/UI/ConfirmDialogView`), generated once per scene and shared as a `WindowHandler<ConfirmShowData>`: ask with `Show(new ConfirmShowData(...))` (see "Windows are generated, not placed") — a click on the background counts as cancel, unless the question passes `cancelOnBackground: false` (the map's tutorial prompt), in which case it does nothing and only the two buttons answer. The view only reports; the request's callbacks decide what OK does. The prefab keeps the dialog (`Dialog`, with `WindowTransition`) and its electron burst as siblings under an always-active root, so the burst outlives the closed window. MapScene's Stage Start is the first user.
- **Text and buttons** go through `CommonText` / `CommonButton` (see "UI wrapper components"), never raw TMP / `Button`.
- **Primary buttons are `Assets/Prefabs/CyberButton.prefab`** (the slanted ripple button, with its `>>>` `Deco` child off by default). Place it with `PrefabUtility.InstantiatePrefab` so the look stays in one place. The buttons in BattleScene, TitleScene and MapScene predate the prefab and are plain copies.
- **Prefabs live in `Assets/Prefabs/`**, not per-feature UI folders.
- **Screen sizes.** The base resolution is **1920×1080 (16:9)**, and **16:10 (Steam Deck) and 21:9 (ultrawide) must not break**. Never hard-code a particular aspect ratio in code, scenes or art.
  - **CanvasScaler.** Every canvas, the toast's included, is Scale With Screen Size, 1920×1080, **Screen Match Mode = Expand**. The 1920×1080 reference area is therefore always fully visible, and the canvas grows along whichever axis the screen has extra: sideways on 21:9 (≈2560×1080), vertically on 16:10 (1920×1200). Nothing is ever cropped. (The skill's Match 0.5 was not used: it scales the UI instead of adding room, and can still crop.)
  - **Anchor by role.** Things that belong to the world — units standing on the background's horizon — anchor to the **screen centre**, so they stay on the floor line of the centre-anchored background. HUD — memory gauge, deck, archive, Execute, hand, top panels — anchors to the **edge or corner it belongs to**, with the pivot on that edge. Full-screen things (the cancel area, the target-list root) stretch.
  - **Safe area (notches, rounded corners, gesture bars).** Edge- and corner-anchored HUD goes under a full-stretch container with `Common/Views/UI/SafeAreaFitter`, which sets its anchors to `UnityEngine.Device.Screen.safeArea` at run time (the `Device` one, which the Device Simulator overrides; the serialized anchors stay 0–1, so without insets nothing moves). The container's parent must cover the screen. World things (units, the route, backgrounds) and full-screen catchers stay outside it. So far: BattleScene `Canvas/SafeArea` (deck, memory, archive, purge, Execute, command tray and list, Log, Menu) and `Canvas/SafeAreaHand` (buffer label, empty-buffer notice, hand — a second container so the draw order around `CardDetail` / `LogFeed` / `TargetPicker` is kept); MapScene `Canvas/SafeArea` (every button, Depth, Tokens); ResultScene's `Content` (the fitter on it directly); the tutorial overlay's skip button (`Visual/SafeArea`). Title and Ending have only centred text. Windows are centred and leave enough margin. The toast is top-centre and not fitted yet. The screen is **landscape only** (Auto Rotation with only Landscape Left / Right allowed; `ScreenOrientationSettingsTests`), so turning the device over moves a notch to the other side without changing the screen size — which is why the fitter checks every `Update` instead of waiting for a size change.
  - **Backgrounds** are drawn at **2560×1440**, centred at native size. The middle 1920×1080 is the reference composition (safe area) and the world continues outside it, so wider or taller screens reveal more city, sky and floor instead of stretching. Anything that must hug a screen edge — the hazard bars — is a separate sliced UI piece anchored to that edge, not part of the background image. Ambient particle emitters cover the full 2560-wide area.
  - **Checking.** Use **Tools > Game View** (`Editor/GameViewAspectMenu.cs`) to switch the Game view between 16:9 / 16:10 / 21:9, and capture each in Play mode. Note that `capture_game_view` rescales every capture to 1280×720, so a non-16:9 capture looks squashed; judge placement, not proportions.
- **Canvas binding.** Canvases are Screen Space - Camera bound to the `UICameraRig` camera of their tier (see "Rendering"). Never Screen Space - Overlay, except the toast's no-rig fallback and `SceneTransitionEffect` (see "Adding a new scene").

### Rendering: cameras, canvases and sorting layers

Every scene holds one instance of `Assets/Prefabs/UICameraRig.prefab` - three cameras wired as a URP camera stack, plus a `UICameraRig` component that publishes them as `UICameraRig.Current` for UI that belongs to no scene (the toast). Add that prefab to a new scene rather than hand-placing cameras.

| Camera | Type | Renderer | GameObject layer it culls to | Draws |
| --- | --- | --- | --- | --- |
| `Main Camera` | Base, post-processing **on** | `UniversalRenderer` (index 0) | `Default` + `UI_Back` (8) | the world and anything that should be affected by post-processing |
| `UI Camera` | Overlay | `UniversalRenderer` (0) | `UI_Default` (9) | ordinary UI - lands on top of the post-processed image |
| `Popup Camera` | Overlay | `UniversalRenderer_Popup` (1) | `UI_Popup` (10), `UI_PopupTop` (11) | popups. `UI_Popup` holds every open window but the front one and is drawn **before** the blur; `UI_PopupTop` holds the front window, the toast and the tutorial and is drawn **after** it |

The stack order on the Base camera *is* the draw order: `UI Camera`, then `Popup Camera`. Post-processing runs on the Base camera only, which is what puts "before post" and "after post" on different cameras rather than on different passes of one camera.

Sorting layers are `UI_Back`, `UI_Back_Effect`, `UI_Default`, `UI_Default_Effect`, `UI_Popup`, `UI_Popup_Effect` - each tier, plus a layer for effects that must sit above that tier but below the next. **A sorting layer orders drawing within one camera; the GameObject layer decides which camera sees the object at all.** Both have to be set, so a canvas *and everything under it* carries the layer of its tier - use `SetLayerRecursively` when building one from script.

Canvases stay per-scene, one per tier the scene needs: `BackCanvas` (Screen Space - Camera on `Main Camera`, layer `UI_Back`, so far only a placeholder colour) and `Canvas` (on `UI Camera`, layer `UI_Default`), plus a `PopupCanvas` (on `Popup Camera`, sorting layer and GameObject layer `UI_Popup`) in a scene that has a modal window — so far MapScene and BattleScene. Every window prefab's root is a **nested Canvas with a `PopupLayerMember`**, which moves the window between `UI_Popup` and `UI_PopupTop` as windows open and close (see "Only the front window is sharp" below). Bind a canvas to the rig's camera for its tier - never to `Camera.main`, and never leave it on Screen Space - Overlay, which bypasses the cameras (and therefore the blur) entirely.

**All the cameras use the Universal Renderer, not the 2D Renderer, and that is load-bearing.** With `Renderer2D`, the Overlay cameras do not composite into the Base camera's colour target - each camera overwrites what the one below drew, and anything a renderer feature writes into the camera colour is lost with it. Nothing in the project uses `Light2D`, so the whole stack moved to `UniversalRenderer`; `Assets/Settings/Renderer2D.asset` is still on disk but is no longer registered in `UniversalRP.asset`.

**Blur behind popups** is `PopupBlurRendererFeature` (`Common/Rendering/`), attached to `UniversalRenderer_Popup` and configured there (injection point, iterations, offset). It blurs whatever is already in the colour target *before* the Popup camera draws its own content, using `Assets/Shaders/DualKawaseBlur.shader` (Dual Kawase: blur while halving the resolution, then blur again on the way back up). It only enqueues while `PopupBlurController.IsActive`, so a popup opts in with `PopupBlurController.Push()` and **must** `Pop()` when it closes. Pair the `Pop` with `OnDisable`, so a scene change or a `SetActive(false)` cannot leave the counter stuck on. **Windows get it through `Common/Views/UI/WindowTransition`**, which pushes in `OnEnable`, pops in `OnDisable`, and fades `PopupBlurController.Strength` (0..1) in while the window opens and out while it closes; the feature's last upsample pass (`DualKawaseUpBlend`) blends the blur over the original image by that strength. The strength goes back to 1 when the last popup pops. Every window in the game uses it (the map's deck edit, market, purchase result, unit upgrade and enemy info windows, the confirm dialog, …). `MessageToast` used to blur too and deliberately dropped it: a notification must not stop play, a window the player has to read may. The feature costs nothing while inactive.

**Only the front window is sharp; the blur runs once.** `Popup Camera` draws in three steps, the last two inside `PopupBlurRendererFeature`:

1. the renderer's normal drawing draws `UI_Popup` - every open window except the front one;
2. the blur (`_injectionPoint`, `AfterRenderingTransparents`) blurs everything so far: the scene below and those windows;
3. the feature's `DrawLayerPass` draws `_frontLayers` (`UI_PopupTop`): the front window, the toast and the tutorial.

However many windows are stacked, there is one blur, and everything but the front window is under it. Steps 2 and 3 are ordered in code (`_injectionPoint` and `+ 1`), not by the order of features on the renderer.

**Which window is in front** is `Common/Views/UI/PopupStack`: `WindowTransition.PlayOpen` brings the window to the front, and `OnDisable` (closing finished, hidden, scene unloaded) removes it. On every change it walks the stack (the plain `Common/Models/PopupStackModel`) from back to front and calls each window's `PopupLayerMember.Apply(isFront, order)`, found as `GetComponentInParent` of the `WindowTransition`:

- it moves the whole subtree to `UI_PopupTop` (front) or `UI_Popup` (others);
- it sets its nested Canvas to Override Sorting, sorting layer `UI_Popup`, order `10 + stack index`.

The order is what makes the front window take clicks first, since every window is on the same camera; it also orders the back windows among themselves. A window nested in another window's prefab (the market's purchase result) is handled by the back-to-front walk: the front one is applied last and wins for its subtree. Only the Canvas's and particles' layers matter for drawing, so rows a list instantiates later need nothing. The toast and the tutorial overlay move themselves to `UI_PopupTop` (order 100) when they bind to the camera.

**The blur fades for the first and the last window; in between, only the window just behind fades.** `WindowTransition` fades `PopupBlurController.Strength` on open only when no other blurring window is open (`OpenCount`), and on close only when it is the last; otherwise the scene's blur would dip every time a window opens over another. When a window opens over another (or closes off one), it instead fades a **region**: the screen rectangle of the window just behind it (`PopupStack.Below`, its `WindowTransition.WindowRect` converted to the camera's viewport) gets its own strength through `PopupBlurController.SetRegion`, which the blur's last pass (`DualKawaseUpBlend`, `_BlurRegion` / `_BlurRegionStrength`) uses inside that rectangle instead of the global strength. Opening ramps it 0 → 1, so that window blurs in while the scene stays fully blurred; closing ramps it 1 → 0, so at the moment the closing window is gone and the one behind moves back to the front layer it is already sharp and nothing jumps. The region is cleared when the transition ends (`EndRegionFade`, also in `OnDisable`). Only one region exists at a time. The part of the scene seen through the behind window's translucent panel follows the same ramp, which is barely visible under that window's own scrim.

**A new window** needs its prefab root to have a `Canvas` (Override Sorting on), a `GraphicRaycaster` and a `PopupLayerMember`, plus its `WindowInitializerBase` subclass, a `WindowId`, a `WindowCatalog` entry and an init data class ("Windows are generated, not placed"). When adding these from a script, set Override Sorting through `SerializedObject` (`m_OverrideSorting`): in a prefab opened alone the root has no parent canvas, so the property setter silently does nothing.

`PopupLayerSetupTests` pins the asset side, since none of it fails loudly:

- `UI_PopupTop` is out of the popup renderer's Opaque/Transparent Layer Masks (left in, it is also drawn before the blur);
- it is the feature's `_frontLayers`;
- `Popup Camera` sees both layers;
- every window prefab has its `PopupLayerMember` and `GraphicRaycaster`.

Two earlier versions were dropped on 2026-10-01:

- **A fourth camera.** An extra camera per scene, a second renderer asset, and the upper window had to leave its prefab.
- **Fixed blur levels per window.** Two blurs when stacked, and every window had to be assigned a level by hand; the battle's confirm dialog over the options window was left unblurred.

Two things cost nearly all the time when this was built, and neither reports itself as an error:

- A blit material used with `Blitter.BlitTexture` must include **`Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl`**. That file defines `Vert` (the vertex-ID fullscreen triangle Blitter actually draws), `Varyings`, `_BlitTexture` and `_BlitScaleBias`. There is no `ShaderLibrary/Blit.hlsl` in either render-pipeline package, and with a wrong include path the pass is excluded from compilation and the blit draws **nothing at all** while `Shader.isSupported` still returns `true`. `ShaderUtil.ShaderHasError` / `ShaderUtil.GetShaderMessages` is what tells the truth - check those first when a custom blit has no visible effect. (Core's `Blit.hlsl` already declares `_BlitTexture_TexelSize`, which is why the feature passes its own `_BlurTexelSize` instead.)
- The `Both vertex and fragment programs must be present in a shader snippet` warnings left on that shader come from the `HLSLINCLUDE` block, not from the passes. They are benign.

The blur is recorded as explicit `AddRasterRenderPass` steps with `builder.AllowPassCulling(false)` rather than `RenderGraphUtils.AddBlitPass`, so a write into the camera colour can never be dropped as "nothing reads this", and so the per-pass texel size is handed to the material at execution time instead of record time.

### Japanese text and the TMP font atlas

Every text in the game uses the pixel font `Assets/Fonts/DotGothic16/DotGothic16-Regular SDF`, which is also TMP's **default font** (`TMP Settings.asset`), so a newly added text starts in it. Its material has **Face Dilate 0.2** because some of its glyphs are one dot thick, and those strokes vanish when scaled down — see @docs/art.md.

**It has no fallback font.** NotoSansJP used to sit behind it and was removed once every scene was styled. A character DotGothic16 does not have therefore renders as **blank space with no error**. `FontCoverageTests` (EditMode) looks up every character in every localization table in DotGothic16's source `.ttf` and fails on anything missing; text written in code (toast messages) or typed into a scene is not covered by it, so check new characters there by hand. If a character really is missing, the fix is a fallback font asset on DotGothic16 (with its licence added below), not switching the text to another font.

The asset is **dynamic**: glyphs are rasterised into its atlas the first time a character is shown.

Its atlas is 1024x1024 at a 64pt sampling size, which holds only on the order of **150 CJK glyphs per texture** — so `Multi Atlas Textures` must stay **on**, or the first atlas fills up and every character after that renders as **blank space with no error in the console**. That is the failure to suspect whenever new Japanese text shows up empty while older text still draws. If it happens, check the font asset's multi-atlas flag first, then whether the character can be added at all (`TMP_FontAsset.TryAddCharacters` reports what it could not fit).

### Localization (`Assets/Localization/`, `com.unity.localization`)

Localized so far: a card's name and description (`CardView.SetCardData` reads both through `LocalizationHelper`), a status effect's name and description (resolved by `UnitPresenter`), and the UI labels listed in `UiTextKey` (the deck/discard captions and the Execute button). Still raw strings: `MessageToast` text (including the failure messages `BattleCommandQueueUseCase` shows) and any label authored directly in a scene.

**Re-reading localized text on a locale change is the presenter's job, and `PresenterBase` already wires it up** — it subscribes to `LocalizationSettings.SelectedLocaleChanged` and calls each presenter's `OnLocaleChanged()` (see "Layering" above). Views stay dumb: `CardView` has no subscription and no MonoBehaviour lifecycle of its own, it just gets `SetCardData` called again. Don't hand a View a `LocalizedString`/`StringChanged` binding instead — besides putting localization lifecycle in the View, `BattleCardHandPilePresenter` pools and reuses its card views, so a per-field binding would need re-binding on every reuse. There is no in-game language-switch UI; switching is an Editor-only affordance (below).

**Switching language at runtime** is done with the Localization package's own Editor UI — the locale dropdown it adds to the Game view, and `Window > Asset Management > Localization Scene Controls`. Changing it in Play mode is what exercises the `OnLocaleChanged()` path above. A custom main-toolbar dropdown was built for this and then removed as redundant, so don't add one back without a reason the package's UI doesn't cover.


**`Common/Localization/LocalizationHelper.cs` is the only place that names a table or builds a key** — never call `LocalizationSettings.StringDatabase` from a View or Presenter. Each table gets one `public const string <Name>Table` plus one `public static string Get<Name>(...)` method; both go through the private `GetLocalizedString(tableName, key)`.

**A table entry's key is the id of the data it describes, verbatim** (`CardData.Id` for card names — the card assets' `_id` is set to the asset's own name, so the keys are the asset names: `Attack`, `Block`, `StrongAttack`, `Poison`, `Strength`, `Heal`, `Regeneration`, …). **An id is not wording**: the `Poison` card and status are shown as ウイルス / Virus, and the id stays `Poison` — rename the text in the tables, not the key. No prefix: the table already scopes the key, and a prefix only adds a second thing to keep in sync. To keep that rule from leaking, **a helper method takes the data object, not the key or the id** — `GetCardName(CardData cardData)`, not `GetCardName(string cardId)` — so the call site can't pick the wrong field and "which field is the key" is decided in exactly one place.

**One table per text field, keyed by whatever identifies the row:**

| Table | Keyed by | Helper |
| --- | --- | --- |
| `CardName` | `CardData.Id` | `GetCardName(CardData)` |
| `CardDescription` | `CardData.Id` | `GetCardDescription(CardData)` |
| `CardPackName` | `CardPackData.Id` | `GetCardPackName(CardPackData)` |
| `CardPackDescription` | `CardPackData.Id` | `GetCardPackDescription(CardPackData)` — the chip's description in the shop's checkout panel; line breaks are written into the text at sentence boundaries |
| `EnemyName` | `EnemyData.Id` | `GetEnemyName(EnemyData)` — shown above the enemy's portrait in battle |
| `EnemyDescription` | `EnemyData.Id` | `GetEnemyDescription(EnemyData)` — the enemy's traits, shown in the map's Enemy Info window for the next layer. Plain text without numbers; HP, action counts, commands and immunities are shown from the data beside it |
| `StatusEffectName` | `IStatusEffect.Id` | `GetStatusEffectName(IStatusEffect)` |
| `StatusEffectDescription` | `IStatusEffect.Id` | `GetStatusEffectDescription(IStatusEffect)` |
| `StatusEffectLogDetail` | `IStatusEffect.Id` | `GetStatusEffectLogDetail(IStatusEffect)` — a format string with the numbers (`{0}` value, `{1}` a timed effect's size), used by the battle log |
| `UIText` | a `UiTextKey` enum member's name | `GetUiText(UiTextKey)` |

`CardName` and `CardDescription` share their key source on purpose — a `StringTable` holds exactly one string per key per locale, so two texts about the same card *have* to live in two tables. Think of a table as one column, the key as the row. So: **a new text field about an existing subject gets a new table; a new label of an existing kind does not.**

That is why `UIText` is the home for **every** general UI label (`DeckPile`, `DiscardPile`, `Execute`, `StageStart`, `DeckEdit`, `OwnedCards`, `DeckCards`, `Cancel`, `Save`, `Back`, `CardShop`, `Buy`, `Tokens`, the stage-result labels `VictoryRewardTitle` / `VictoryRewardMessage` / `DefeatRewardTitle` / `DefeatRewardMessage` / `RewardClaim` / `BackupAccept`, the battle log's `BattleLog` and `Log*` format strings, and the four mode names `Campaign` / `Endless` / `Collection` / `Options` so far) rather than one table per screen or widget: those are all the same kind of text, just more rows. A table is not free here — each one costs a folder, `2 + <locale count>` assets, and an Addressables group per locale — so splitting one kind of label across screens multiplies that overhead for nothing.

UI labels have no data object to pass, so `UiTextKey` (`Common/Localization/UiTextKey.cs`) carries the type safety instead: the enum member's name *is* the table key, and callers never write a raw string.

Two EditMode tests keep the tables and their key sources in step, because the failure mode in both directions is only visible as "No translation found" on screen: `UiTextKeyTests` walks the `UiTextKey` enum, `CardLocalizationTests` walks every `CardData` asset and checks its `Id` against **both** card tables, and `EnemyLocalizationTests` walks every `EnemyData` asset against `EnemyName`, and `StatusEffectLocalizationTests` reads the `public const string EffectId` off every `IStatusEffect` implementation found by `TypeCache` (they have no parameterless constructor to instantiate) and checks it against all three status-effect tables — all in every locale. It also fails if an effect omits that constant, which is what keeps the id reachable from the type.

A View never calls the helper: the presenter resolves the string and pushes it in (`CardPileCountView.SetLabel(string)` is fed by `BattleCardDeckPilePresenter`/`BattleCardDiscardPilePresenter` from inside `Sync`, which is also what `OnLocaleChanged` re-runs — so the label follows a language change for free). `CardView` is the exception that proves the rule: it reads both card texts through the helper itself, because it is handed a `CardData` and nothing else.

`LocalizationHelperPlayModeTests` covers this end to end (it asserts the ja/en entry for a card id, so a broken key convention or a missing entry fails there). It is a PlayMode test because `LocalizationSettings` needs its Addressables-backed initialization, which Edit mode can't resolve. Renaming a key means renaming it in `SharedTableData` too, so treat the helper and the table keys as one unit that changes together.

Asset layout — keep new localization assets inside this shape:

```
Assets/Localization/
├── Localization Settings.asset      (LocalizationSettings; registered in ProjectSettings/EditorBuildSettings.asset under com.unity.localization.settings)
├── Locales/                         (one asset per locale: "English (en)", "Japanese (ja)")
└── Tables/
    └── <TableCollectionName>/       (one folder per table collection)
        ├── <Name>.asset             (StringTableCollection)
        ├── <Name> Shared Data.asset (SharedTableData — the keys, shared by every locale)
        └── <Name>_<code>.asset      (StringTable, one per locale)
```

- **One folder per table collection** — a collection is always `2 + <locale count>` files, so a flat `Tables/` folder stops being readable as soon as either number grows. When creating a new collection in the Localization Tables window, point its folder dialog at a new `Assets/Localization/Tables/<Name>/`; the package then writes all of that collection's assets there, and adding a locale later drops the new `<Name>_<code>.asset` into the same folder automatically. Don't organize by locale instead (`Tables/ja/...`) — the collection and shared-data assets belong to no single locale, and the package would fight the layout.
- **Moving any of these assets must go through `AssetDatabase.MoveAsset`** (via `run_script`), never a plain file move — see "Driving a live Unity Editor from the terminal" above. The Addressables entries and `EditorBuildSettings` reference these assets by GUID, so a proper move keeps every reference; the folder layout itself has no effect on the build.

`Assets/AddressableAssetsData/` is **required, package-generated, and must stay committed**. `com.unity.addressables` is an indirect dependency of `com.unity.localization` (it is not in `manifest.json`), and Localization loads locales and string tables exclusively through it. Its `Localization-*` groups are created and maintained by the package — string tables are grouped per locale (`Localization-String-Tables-<locale>`), independent of the folder layout above. Nothing in `Assets/Scripts/` uses Addressables directly. Leave the (empty) `Default Local Group` alone: `AddressableAssetSettings.m_DefaultGroup` points at it, and anything newly marked addressable needs it.

### Tests (`Assets/Scripts/Tests/`)

Scripts under `Assets/Scripts/` compile into the `Live2DCardRogue.Runtime` assembly (`Assets/Scripts/Live2DCardRogue.Runtime.asmdef`); `Assets/Scripts/Editor/` has its own `Live2DCardRogue.Editor.asmdef` (Editor-only, references Runtime) so the `CardEffectBaseDrawer` custom drawer still only builds for the Editor. Neither existed before a test suite did — a custom test `.asmdef` can only reference *other* `.asmdef`-defined assemblies, never the implicit `Assembly-CSharp`, so adding tests to a project with no existing `.asmdef` means giving the main code one first. This is a one-time structural change, not something to repeat per test.

- `Assets/Scripts/Tests/EditMode/Live2DCardRogue.Tests.EditMode.asmdef` — the default home for a new test. Covers plain Model/UseCase/status-effect/card-effect logic with no Unity Play-mode dependency (`CardPileModelTests`, `CardCommandModelTests`, `UnitStatusModelTests`, `BlockStatusEffectTests`, `CardEffectTests`, `BattleCommandUseCaseTests`). It also references `Unity.Localization` and `Unity.Localization.Editor`, because `UiTextKeyTests` and `CardLocalizationTests` inspect the localization table assets through `LocalizationEditorSettings` — checking that every `UiTextKey` and every `CardData.Id` has a non-empty translation in every locale. `FontCoverageTests` reads the same tables to check every character in them exists in DotGothic16 (see "Japanese text and the TMP font atlas"). Those are Editor-only APIs, and a PlayMode assembly (`includePlatforms: []`) cannot reference an Editor-only assembly at all, so tests that touch them have no other home. `Unity.TextMeshPro` and `UnityEngine.UI` are referenced for the same kind of reason: `CommonUiComponentTests` names the raw `TextMeshProUGUI`/`Button` types it wants to keep out (see "UI wrapper components" above). It checks prefabs by loading them and inspecting the components, but checks scenes through `AssetDatabase.GetDependencies` instead — opening a scene from an EditMode test would disturb whatever scene is open in the Editor.
- `Assets/Scripts/Tests/PlayMode/Live2DCardRogue.Tests.PlayMode.asmdef` — only for a case that genuinely needs Play mode. `LocalizationHelperPlayModeTests` and `PresenterBasePlayModeTests` are here because `LocalizationSettings` needs its Addressables-backed initialization (see "Localization" above; this asmdef therefore also references `Unity.Localization` and `Unity.ResourceManager`). `PresenterBasePlayModeTests` drives a spy subclass of `PresenterBase` to check that a locale change reaches `OnLocaleChanged()` and that `Dispose()` really unsubscribes — a test for the base class's own contract, with no View or scene involved. Note that a test which changes `LocalizationSettings.SelectedLocale` must restore it in teardown (it is global state) and must switch to a *different* locale than the current one, since assigning the same locale raises no event. The remaining ones are in `BattleCommandUseCasePlayModeTests`: `TrySelectCard`'s two refusal paths (not enough stamina, execution list full) call `MessageUseCase.ShowMessage` → `MessageToast`, which calls `Object.DontDestroyOnLoad`, and that throws `InvalidOperationException` outside Play mode. If a new test's only obstacle is a `MonoBehaviour` singleton doing something Play-mode-only like this, that's the signal to add it here instead of EditMode — don't work around it by mocking away the dependency.
- `CardData` and the `CardEffectBase` subclasses expose their data only via `[SerializeField]` with no public setter (Inspector-only by design). Tests construct them with `ScriptableObject.CreateInstance<CardData>()` / `new BasicAttackCardEffect()` and set the private fields via reflection (see the `SetPrivateField` helper duplicated in `BattleCommandUseCaseTests.cs` and the PlayMode test) rather than adding test-only constructors/setters to production code.
- Run tests via the live-Editor `run_tests` Pipeline command — see "Running tests" above, not the standalone `unity test` CLI command.
- Add or update tests here as new Model/UseCase/status-effect logic is added or changed — this suite is expected to grow with the codebase, not stay a one-time snapshot.

### Writing/updating these docs

When writing or revising this file, its `@`-imported per-scene docs, or the `.ja.md` mirrors, actually read the current code for what you're documenting rather than trusting an earlier pass of the doc (or of your own memory) — this codebase gets refactored often, and a description here can go stale the same session it was written. If you notice a contradiction — the doc says one thing and the code does another, a naming convention is broken somewhere (e.g. a private field not in `_camelCase`), two docs disagree with each other, or a described behavior no longer matches its own comment — say so explicitly rather than silently documenting around it or silently "fixing" the code to match the doc. Report what you found and let the user decide which side (doc or code) is wrong; don't assume the doc is the source of truth.

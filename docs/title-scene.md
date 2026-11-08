# TitleScene

Detail doc for `Live2DCardRogue.TitleScene.*` (`Assets/Scripts/Scenes/TitleScene/`), imported from the root `CLAUDE.md`. Read that file first for the general layering/wiring conventions, and @docs/battle-scene.md for the scene the modes will eventually lead to.

`Assets/Scenes/TitleScene.unity` is the entry scene (index 0 in Build Settings). It shows the game title and four mode buttons: **Story (Campaign in the code) / Challenge / Collection / Options**.

**No window is placed in TitleScene.** `TitleSceneInitializer` generates all of them at start-up with `WindowUseCase.Create` under `PopupCanvas` (confirm, resume, episode and challenge selection, the collection menu and both collections, options) and opens them through their handlers' `Show` (see "Windows are generated, not placed" in the root doc). Where this doc says a window is "on the title's `PopupCanvas`", that is where it is generated.

**All four are wired up.** **Collection** opens the shared menu window (`Prefabs/MenuWindow.prefab` through a `MenuWindowInitData`, `MenuWindowView` / `MenuWindowPresenter`; see "Button-list windows" in the root doc) titled コレクション (`UiTextKey.Collection`) with two buttons — コマンド / エネミー (`UiTextKey.CollectionCommands` / `CollectionEnemies`) — and Close (a background click also closes). `TitleSceneInitializer` only builds its `MenuWindowInitData`: **Commands** and **Enemies** open their collections over it (below; each through its handler's `Show`). Until 2026-10-06 it had its own window (`CollectionMenuWindow.prefab`, `CollectionMenuPresenter`, `CollectionCategory`). An Achievements button was planned and removed on 2026-10-04.

### Enemy collection (`Prefabs/EnemyCollectionWindow.prefab`)

The map's Enemy Info window, reused: `EnemyDetailWindowView` (moved with `EnemyDetailSlotView`, `EnemyCommandRowView` and the display data `EnemyDetailInfo` from `MapScene` to `Common/Views/EnemyDetail/` on 2026-10-04 so the title can use it) driven by `EnemyCollectionPresenter`. `// ENEMY DATABASE`, エネミーコレクション (`UiTextKey.EnemyCollection`).

- **Left:** every enemy in `Assets/ScriptableObjects/EnemyCollection.asset` (`EnemyCollectionData`; Trojan, Spyware, Rootkit, Botnet — not the tutorial's), top to bottom with its idle animation. Unlike the map's column it scrolls: `SlotScroll` (`ScrollRect`, a `RectMask2D` viewport wide enough for the slot's cursor and glow, the `Slots` layout group with a `ContentSizeFitter`) and the thin cyan scrollbar (`AutoHide`). `EnemyDetailWindowView._slotScroll` is optional; when set, every `SetEnemies` scrolls back to the top.
- **Opening** selects the top enemy; a click selects another (yellow frame and cursor) and shows it on the right.
- **Enemies never fought are hidden.** Their slot shows the idle animation as a black silhouette (`IUnitVisual.SetColor(black)`) with a cyan **?** over it (`EnemyDetailSlotView._unknownMark`) and the name ？？？; selecting one shows ？？？, `HP ???` and 「まだ遭遇していない敵です。戦うとデータが記録されます。」 (`UiTextKey.EnemyUnknown`) with no commands (`EnemyDetailInfoBuilder.CreateUnknown`, `EnemyDetailInfo.IsDiscovered`).
- **What counts as fought:** `MapStageTransitionUseCase.StartStage` records the stage's enemies (`CampaignSaveService.RecordDiscoveredEnemies` → `CampaignClearData.discoveredEnemies`, version 5) in Story and Endless alike. On open, `TitleScene/Models/EnemyDiscoveryResolver` adds what older saves imply: every stage of a cleared episode, the stages before the current one in a suspended Story, and the layers already cleared in a suspended challenge (rebuilt from its seed with `TitleSceneInitializer._endlessBalance`).
- **Right:** the same details as the map (HP, actions, starting effects, traits, every command with its text), built by `Common/Presenters/EnemyDetailInfoBuilder` — the map's `EnemyDetailPresenter` uses it too. The collection shows each enemy's own numbers (`EnemyStrength.None`).
- `EnemyCollectionTests` checks that every enemy met in play (any `StageData`, any `EndlessBalance` candidate) is listed.

### Command collection (`Prefabs/CommandCollectionWindow.prefab`)

`CommandCollectionWindowView` / `CommandCollectionPresenter` / `CommandCollectionModel` (`Window/CommandCollection/`), on the title's `PopupCanvas`, opened over the collection menu (closing it returns there; Close or a background click closes it). The prefab is a trimmed copy of Command Build: `// COMMAND DATABASE`, コマンドコレクション.

- **Filter tabs** above the list: すべて, then one per chip (`UiTextKey.CollectionFilterAll`, then each chip's `CardPackName`). The view clones `Filter` once per tab (`_filterSpacing` apart); the selected tab's label is yellow. A tab re-lists the commands and scrolls to the top.
- **The list** (`// COMMAND INDEX`, a `CommonScrollList` of `CollectionCardView.prefab`, whose component is the shared `Common/Views/CardList/CardListItemView`) shows a command **only if it has ever been owned** in Story or Endless; any other is a face-down card with a **?** (`_lockedRoot`). The header shows 獲得 26 / 32 (`CollectionProgress`, counted over すべて). A thin cyan scrollbar on its right (as in Command Build, hidden when everything fits) shows that it scrolls.
- **Hovering** a command shows it enlarged on the right (`// COMMAND DATA`, the shared `CardListDetailView`; the battle's archive and purge lists use both) and gives it the cyan glow in the list. A ? card can be hovered too: the panel then shows the same face-down ? card (`_lockedCard`), so the name and text stay hidden.
- **What is listed** is `Assets/ScriptableObjects/CommandCollection.asset` (`CommandCollectionData`): `_packs` are the tabs, in the market's chip order, and `_otherCards` are commands no chip gives (the stage reward Repair). すべて sorts the union by rarity, then by first appearance. Enemy-only commands are never listed. `CommandCollectionTests` checks that every obtainable command (any chip, any stage reward, starting kit) is listed and no enemy-only one is.
- **What counts as owned** is `CampaignSaveService.LoadDiscoveredCommandIds`, re-read on every open: `clear.sav`'s `discoveredCommands` plus the owned commands of a suspended Story or Challenge run (so a run from before the record existed counts too).

 Campaign starts a new run, or — when `CampaignSaveService` finds a saved campaign (`campaign.sav`) — first opens **`CampaignResumeDialog`** on the title's `PopupCanvas` (`// SUSPENDED SESSION`: the message, a one-line summary `LAYER 04 / 10   TKN 820   HP 160   MEM 12`, and Cancel / Discard / Resume, with a background click as Cancel). `CampaignResumePresenter` reports the choice and `TitleSceneInitializer` acts on it:
- **Resume** restores the repositories through `CampaignSaveSnapshot.Restore` (commands resolved via its `_cardCatalog`) and loads the map.
- **Discard** first asks through the shared `ConfirmDialog` (also on the title's `PopupCanvas`): "本当に破棄しますか？ / 中断したストーリーは元に戻せません", Cancel / 破棄する. Only OK deletes the progress file and opens the episode selection; Cancel goes back to the three-choice dialog.
- **Episode selection** (`Prefabs/EpisodeSelectWindow.prefab`, `EpisodeSelectWindowView` / `EpisodeSelectPresenter`; `// SELECT EPISODE`, エピソード選択) opens when there is no saved campaign, or right after Discard. On screen the mode is **ストーリー / Story** and the episodes are **エピソード1–3 / Episode 1–3** (`UiTextKey.EpisodeName`). Each episode's stages are its `CampaignData` in `CampaignBalance` (`TitleSceneInitializer._balance`, the same asset as the map's); choosing one starts a new campaign with `CampaignProgressRepository.Reset(episodeIndex)` and goes to the map, which plays that episode. **Unlocking:** Episode 1 is always open; Episode N (2 and up) opens once Episode N-1 has a clear record in `clear.sav` (`TitleScene/Models/EpisodeUnlockRule`, `CampaignClearData.HasCleared`, re-read each time the window opens). A locked episode is drawn disabled, and pressing it toasts the condition through `CommonButton.OnDisabledClicked` (エピソード1をクリアすると選べるようになります). An unlocked episode whose `CampaignData` is missing or empty (`TitleRecordUseCase.HasEpisodeContent` → `CampaignBalance.HasEpisode`) would be pressable and toast エピソード2は準備中です; all three have stages now, so this only guards against a misconfigured asset. Each episode's state is an `EpisodeState` (`Locked` / `NotReady` / `Playable`). Pressing a playable episode asks エピソード1を始めますか？ (始める / 戻る) through the shared `WindowHandler<ConfirmShowData>`, then starts the new campaign below. Close or a background click closes the window.
- **Clear and rank.** A cleared episode shows the lime `CLEAR` tag (the same as the challenge selection's) and its best rank in the result screen's rank colours to the right of its button (`EpisodeSelectWindowView._clearMarks` / `_rankTexts`; `CampaignClearData.BestRank`, the best `CampaignClearRecord.rank` of that episode, read through `TitleRecordUseCase.GetBestRank` each time the window opens).
- A **new campaign** sets `RunModeRepository.StartCampaign()` and clears all four campaign repositories — deck included, which it used to leave behind — so the map seeds them again. See "Saving" in the root doc. It then flags `CampaignTutorialRepository.RequestPrompt()` and loads the map, which asks whether to watch the tutorial on arrival (see @docs/tutorial.md).

## Challenge (`Window/ChallengeSelect/Presenters/ChallengeSelectPresenter.cs`, `Prefabs/ChallengeSelectWindow.prefab`)

The **Challenge** button (`GameMode.Challenge`, `UiTextKey.Challenge`; it was Endless until 2026-10-04) runs the map without a story, with random enemies that grow stronger every ten layers (see "Challenge mode" in @docs/map-scene.md).

- **Resume first.** All challenges share one suspended run (`endless.sav`). When it exists, pressing Challenge opens the same `CampaignResumeDialog` as Story (`ChallengeResumeMessage`; summary `ノーマル   LAYER 12 / 30   TKN …`, or just `LAYER 37` for Endless; discard asks with `ChallengeDiscardConfirmDetail`). Resume restores that challenge; Discard deletes it and opens the selection.
- **The selection** (`ChallengeSelectWindowView`, `// SELECT CHALLENGE`, チャレンジ選択) lists `ChallengeCatalog` in its order: ノーマル 30階層, ハード 60階層, マスター 100階層 and エンドレス (`UiTextKey.ChallengeButtonLabel` `{0}  {1}階層`; a challenge with no end shows only its name; names through `LocalizationHelper.GetChallengeName`). The labels shrink to fit the slanted face. Pressing one asks 「ノーマルを始めますか？」 (始める / 戻る) through the shared `WindowHandler<ConfirmShowData>`, then `StartNewChallenge` clears the campaign repositories, sets `RunModeRepository.StartChallenge(type, layerCount, seed)` and loads the map, which opens with the First Build (no tutorial question). Close or a background click closes the window.
- **Clear mark.** A challenge with a layer count that has ever been cleared (`ChallengeRecord.cleared`, read by `TitleRecordUseCase.IsChallengeCleared` on every open) shows a lime `CLEAR` tag to the right of its button (`ChallengeSelectWindowView._clearMarks`, `ClearMark` under each button). Endless has no end, so it never shows one. Endless shows its best layer instead: a cyan `LAYER 37` tag (`_bestLayerMarks` / `_bestLayerTexts`, `BestLayerMark` on the Endless button; `TitleRecordUseCase.GetChallengeBestLayer`), hidden until it has been played.
- **Layer counts** are `Assets/ScriptableObjects/ChallengeCatalog.asset` (`TitleSceneInitializer._challengeCatalog`): change them there. `ChallengeCatalogTests` pins the current values (30 / 60 / 100 / no end), so update it with the asset.

## Options (`Common/Presenters/GameOptionsPresenter.cs`, `Prefabs/GameOptionsWindow.prefab`)

The **Options** mode button opens `GameOptionsWindow` (`// SYSTEM CONFIG`, オプション) on the title's `PopupCanvas`. **The same window, model and presenter are used in battle** (see "Options" in @docs/battle-scene.md); only the title passes `onDeleteUserData`, which shows the User Data row. Edits go into `GameOptionsModel` (`Common/Models/`), **Apply** writes `GameSettingsRepository`, saves `settings.json` and applies display and language (`GameSettingsApplier`), **Reset to Default** resets everything but the language, and **Cancel** (or the background) asks 変更を破棄しますか？ through the shared `WindowHandler<ConfirmShowData>` when something changed.

| Row | Control | Notes |
| --- | --- | --- |
| BGM / 効果音 | slider, 5 % steps | heard live while editing (`AudioUseCase.PreviewVolumes`, cleared on close); moving the SE slider plays a click |
| 戦闘スピード | slider, x0.25 steps | |
| 言語 | `OptionSelectorView` ◀ name ▶ | the locales in Localization Settings, by their own name (日本語, English; `LocalizationHelper.GetAvailableLocales`) |
| 画面モード | `OptionSelectorView` | フルスクリーン / ウインドウ; no effect in the Editor |
| ユーザーデータ | 削除する (magenta) | asks ユーザーデータを削除しますか？ (detail: 中断したストーリーとクリア記録が消えます。元に戻せません), then `TitleSceneInitializer.DeleteUserData` and a toast. The options window stays open. **Title only**: without `onDeleteUserData` the row is hidden and the window shrinks by its height (`GameOptionsWindowView.SetUserDataVisible`) |

`OptionSelectorView` (`Common/Views/UI/`) is the reusable ◀ value ▶ row: two `CommonButton`s with the `ui_cursor` arrow (the left one mirrored) and a value text; it only reports -1 / +1.

## What it is made of

The title's windows are not in this folder: the resume dialog, the episode and challenge selections and the two collections live in `Assets/Scripts/Window/` (`CampaignResume`, `EpisodeSelect` — with `EpisodeState` and `EpisodeUnlockRule` —, `ChallengeSelect`, `CommandCollection`, `EnemyCollection`; `Live2DCardRogue.Window.<Name>.*`), and the Collection menu is the shared `Window/Menu/`. `TitleSceneInitializer` builds them. What stays here is the mode selection below and `Models/EnemyDiscoveryResolver`.

The scene is deliberately thin — there is **no Model and no UseCase layer here**, because nothing in it holds state: a press is a branch on an enum. It keeps the Initializer/ViewContainer/Presenter shape anyway, so it reads like BattleScene at a glance.

- `Enum/GameMode.cs` — the four modes. The enum's order is not the screen order; which button is which mode is set per button in the Inspector.
- `Views/TitleModeButtonView.cs` — one mode button: it knows its `Mode`, exposes its `CommonButton`, and takes a label. It does not know what pressing it does.
- `Views/TitleSceneViewContainer.cs` — the buttons, in screen order.
- `Presenters/TitleModePresenter.cs` — pushes each button's label from `UiTextKey` (re-pushing on a locale change) and reports the pressed `GameMode` through an injected `Action<GameMode>`. Adding a mode means adding an enum value, a `UiTextKey`, one line in `ToUiTextKey`, and a button in the scene; forgetting the mapping throws rather than silently showing nothing.
- `Initializer/TitleSceneInitializer.cs` — composition root, and **the only place that decides what a mode does** (a `switch` over the mode buttons, opening the windows).
- `UseCase/TitleRunUseCase.cs` — starting, resuming and discarding a run (Story or challenge), and deleting user data.
- `UseCase/TitleRecordUseCase.cs` — reading `clear.sav` for the screen: episode states, best ranks, challenge records, discovered commands and enemies.

Note that the presenter keeps the `UnityAction`s it registered so `OnDispose` can remove exactly those listeners — a lambda can't be unsubscribed unless you held on to the same delegate instance.

## Handing data to the next scene

Nothing passes a parameter yet (mode selection goes nowhere), but the mechanism is in place and BattleScene already consumes it. Two pieces in `Common/` do the transport, and they are scene-agnostic on purpose:

- `Common/SceneTransition/SceneTransitionDataStore.cs` — a static hand-off slot. The sender `Set`s, the receiving Initializer's base class (`SceneInitializerBase<TData>`) `TryConsume`s. **A successful read clears it**, so a parameter can never leak into a later transition; a type mismatch leaves it alone.
- `Common/SceneTransition/SceneTransitionUseCase.cs` — `LoadScene(data)`, which stores the `SceneTransitionData` and then loads its `Destination`. Scene names live beside it in `SceneName.cs`, and each data type picks its own, so a call site never writes one.

The payload types are in `Common/` too, in `Common/SceneTransition/TransitionData/` (`TitleSceneTransitionData`, `MapSceneTransitionData`, `BattleSceneTransitionData`, `EndingSceneTransitionData`, `ResultSceneTransitionData`, `ChallengeResultTransitionData`; each carries its own destination, see "Adding a new scene" in the root doc), so that **no scene references another**: the caller and the callee both depend only on `Common/`, and BattleScene still knows nothing about who launched it. Everything a payload is made of (`CardData`, `EnemyData`, `UnitStatusModel`) is in `Common/` as well. (Until 2026-10-06 each payload lived in the `Data/` folder of the scene it enters, and the caller referenced that scene's namespace.)

`BattleSceneTransitionData.PlayerStatus` is a whole `UnitStatusModel` rather than a max-HP number, so a run can carry damage and status effects from one battle into the next.

## Running BattleScene on its own

`BattleSceneInitializer.Initialize(data)` uses the handed-over data when there is one and, when `data` is null, falls back (`CreateMockParameter`) to its own `_playerDeckMock` / `_enemyDataMocks` / `_playerMaxHpMock` / `_playerMaxStaminaMock`, logging that it did so. With no mode implemented, **that fallback is currently the only way the battle runs at all**, so keep it working: it is what makes "open BattleScene.unity and press Play" the normal way to iterate on the battle, which it has been for the whole project.

## Look

Styled per @docs/tone-and-manner.md:

- **Background.** `bg_title.png` puts the horizon at y 430 with a big sun directly behind the logo, and hazard bars at top and bottom. It shares its ambient particles and `CyberVolumeProfile` post-processing with BattleScene.
- **Logo.** DotGothic16 with the `PopShadow` material (magenta underlay). `TitleLogoGlitchView` glitches it for ~0.2 s every few seconds: jitter, cyan/magenta ghosts, flicker.
- **Mode buttons.** The same slanted translucent button as BattleScene's Execute, with `ButtonRippleEffect`: Echo/RippleMask children, italic label.
  - They are 108 px tall, since the slanted sprite must not be stretched vertically.
  - They are stacked diagonally, each row 34 px further left, to follow the slant.

Nothing in the scene's code changed for this; it is all scene content plus the one view.

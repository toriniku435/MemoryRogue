# Tutorial

Detail doc for the campaign's first-time tutorial and the facility behind it (`Assets/Scripts/Common/Tutorial/`), imported from the root `CLAUDE.md`. Read @docs/map-scene.md and @docs/battle-scene.md for the screens it runs on.

## The flow

1. **Title → map.** Starting a *new* campaign (Episode 1, with no save or after discarding one) flags `CampaignTutorialRepository.RequestPrompt()` and loads the map. The map sees `IsPromptPending`, holds back the First Build and, once the scene-transition cover has lifted and the episode's opening conversation has been read (see "Conversations" in @docs/map-scene.md), asks 「チュートリアルを見ますか？」 through its shared `ConfirmDialog` (`MapScene/Presenters/TutorialPromptPresenter`; Watch / No thanks; a background click does nothing (`cancelOnBackground: false`), so a stray tap cannot answer it). **Watch** starts the map tutorial right there (`Stage = Map`); **No thanks** runs the First Build right there (open the chips, build the deck, save, show the chips). Quitting before answering leaves no save, so the next new campaign asks again. **Once the tutorial has been watched** (Watch here, or the map's replay button; `GameSettingsRepository.HasSeenTutorial`, saved in `settings.json`), later new campaigns skip the question and go straight to the First Build (`MapTutorialUseCase.AskOrSkip`); it can still be replayed from the map. Deleting user data on the title's Options resets it.
2. **Map.** With `Stage == Map`, `MapSceneInitializer` skips the First Build and does not save, and plays `CampaignTutorialRepository.MapSequence`: the rooms, the depth, Enemy Info, Connect, Command Build, tokens, Market, Upgrade and the replay button, one bubble each. The last step loads the practice battle (`Stage = Battle`): `CampaignTutorialData`'s fixed deck and enemy, `shuffleDeck: false`, and `playTutorial: true`. The battle's steps are not handed over: the battle takes `CampaignTutorialRepository.BattleSequence`.
3. **Battle.** The deck is drawn in its listed order, so the script knows each hand. Turn 1 draws Attack, Block, Regeneration, Strong Attack and Heal. The player loads Attack, Block and Regeneration; Strong Attack is then out of memory; then Execute. The Training Trojan attacks and plants a virus. Turn 2 covers the lasting effects (virus and regeneration), the archive and the draw, then Heal and Mining and Execute, then the purge pile. After that the battle is free.
4. **Back on the map.** The `BattleResult` in `MapSceneTransitionData` is **ignored** while `Stage == Battle`: no reward, no progress, no Backup Boost, so losing the practice is fine. The tutorial ends (`Stage = None`), and the campaign starts as usual with the First Build.

- **Skipping.** The Skip button (top-right) first asks チュートリアルをスキップしますか？ (スキップする / 戻る) through the scene's shared `ConfirmDialog`; while it asks, the overlay steps aside (`TutorialOverlayView.SetSuspended`) so the dialog can be clicked, and 戻る brings it back on the same step. Confirmed on the map, the map reloads without the tutorial (so the First Build happens). Confirmed in the practice battle (the dialog adds 練習の戦闘を終えて、マップに戻ります), the battle ends at once and returns to the map, which ignores the battle as usual and finishes the tutorial; played standalone (no return scene) the battle simply continues. Scenes pass the question to `TutorialUseCase.Play(sequence, onEnded, confirmSkip)`; without `confirmSkip` Skip ends at once.
- **Quitting midway** leaves nothing behind: the repository is not saved, and no campaign save exists yet before the First Build, so the next new campaign asks again and starts from the beginning.
- **Replay.** The map's **チュートリアル** button (top-left, `MapTutorialButtonPresenter`) first asks チュートリアルを最初から見ますか？ (見る / 戻る), then sets `Stage = Map` and reloads the map, running the whole tutorial again. A campaign in progress is untouched, since the tutorial saves nothing and its battle result is ignored.
- **Asking once, through a shared dialog.** One-off questions go through the scene's `WindowHandler<ConfirmShowData>` (`Common/WindowSystem/`): `Show(new ConfirmShowData(message, detail, ok, cancel, onOk, onCancel))` answers only through that data's callbacks, since the same dialog serves other presenters (Stage Start, Unit Upgrade, Command Build). While another question is open it asks nothing, so the skip checks `IsOpen` first and counts that as not skipped.

## Built to change

| Change | Where |
| --- | --- |
| Wording | the `TutorialText` table, keyed by each step's `_textKey` (`LocalizationHelper.GetTutorialText`) |
| Add, remove or reorder a step | the sequence assets `Assets/ScriptableObjects/Tutorial/MapTutorial.asset` / `BattleTutorial.asset` (`TutorialSequence`: `TutorialStep[]`) |
| Move or rebuild a UI element | nothing, as long as the new object keeps its `TutorialAnchor` with the same `_anchorId`; the overlay reads the anchor's current rect every frame |
| The practice battle | `CampaignTutorial.asset` (`CampaignTutorialData`: deck in draw order, enemies, player HP / start HP / memory). The battle steps are written against the deck order, so review them together. The steps themselves are `CampaignTutorialData`'s `MapSequence` / `BattleSequence` |
| New kinds of "wait until…" | a name in `TutorialEventNames` and a raise where it happens (in a scene's bridge, never in the game logic) |

**A step** (`TutorialStep`) is:

- **An anchor id**: a scene `TutorialAnchor`, or a dynamic id `Prefix:Name` that a scene resolves at runtime.
- **A text key.** Empty means a wait-only step: nothing is shown and input is free, e.g. while commands execute.
- **A bubble side**: `Auto` picks below → above → right → left.
- **How it advances:**
  - `Click`: a click anywhere continues, the bubble itself included (it has its own `ClickArea`, since it sits in front of the full-screen catcher), and the screen underneath cannot be touched.
  - `Event` with an event name: only the highlighted element can be used, and the step continues when that event is raised.

**Pieces (`Common/Tutorial/`).**

- **`CampaignTutorialRepository` holds the steps from start to finish.** `StartFromMap(mapSequence, battleSequence)` (Watch, or the replay button, while the map that references `CampaignTutorialData` is alive) keeps both and marks them `HideFlags.DontUnloadUnusedAsset`; `Finish` (back from the practice battle, a skip, or a new campaign on the title) clears the marks and lets go. The map plays `MapSequence`, the battle `BattleSequence`. The mark is what keeps the battle's steps alive: only the map scene references them, and in a build the switch to the battle can free a ScriptableObject that only C# points at, which on Android left the first practice battle after launch without its tutorial (2026-10-17). The Editor does not reproduce this.
- **`TutorialUseCase.Play(sequence, onEnded)`** is the entry point. It spawns `Resources/TutorialOverlay.prefab` and returns an `IDisposable`, which the scene disposes in `OnDestroy`.
- **`TutorialSession`** (a `PresenterBase`, so the text follows a language change) joins a `TutorialRunner` (plain state, EditMode-tested) to the `TutorialOverlayView`.
- **`TutorialOverlayView`**:
  - Dims four panels around a hole over the target, with a pulsing yellow frame.
  - Places the bubble on the free side and clamps it to the screen.
  - Shows the step counter, the "click to continue" hint and Skip.
  - Draws on the popup camera, sorting layer `UI_Popup` order 100, and waits until the scene-transition cover has lifted.
- **`TutorialAnchorRegistry`** finds anchors. Scenes register dynamic prefixes: `HandCard:<command id>` (the leftmost hand card with that id, `BattleCardHandPilePresenter.FindCardTransform`) and `MapRoom:Current` (`MapPresenter.CurrentCellTransform`).
- **`TutorialEvents`** carries the event names: `CardQueued`, `ExecuteRequested`, `PlayerTurnStarted` (the phase became `Player`, after the draw) and `EnemyTurnStarted`. In battle they are raised by `BattleTutorialEventBridge`, which watches the models and the director from outside; the game code knows nothing about the tutorial.

**The practice enemy** is `Enemies/TutorialTrojan.asset`: 24 HP, and every turn Backdoor then a virus, via `FixedOrderEnemyCardPicker` (plays its cards in listed order, up to the max action count).

**Tests.**

- `TutorialRunnerTests` covers how steps advance.
- `TutorialDataTests` checks the assets:
  - every text key exists in every locale;
  - every event step waits for a known event;
  - wait-only steps advance by an event;
  - every scene anchor id exists in its scene file, and every dynamic prefix is a known one;
  - every `HandCard:` command is in the tutorial deck.
- The font and enemy localization tests cover the new texts and the Training Trojan.

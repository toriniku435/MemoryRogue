# MapScene

Detail doc for `Live2DCardRogue.MapScene.*` (`Assets/Scripts/Scenes/MapScene/`), imported from the root `CLAUDE.md`. Read that file first for the general layering/wiring conventions, @docs/title-scene.md for how you get here and @docs/battle-scene.md for where a stage leads.

The map's windows are not in this folder: they live in `Assets/Scripts/Window/<Name>/` (`Live2DCardRogue.Window.<Name>.*`) — `DeckEdit`, `CardShop`, `UnitUpgrade`, `EnemyDetail`, `FirstBuild`, `Reward` (the stage reward window) and the shared `Menu` — and the map uses them from `MapSceneInitializer` / `MapPresenter`. Window paths below are relative to `Window/`; other paths are relative to `Scenes/MapScene/`. What the map hands to other scenes (`BattleSceneTransitionData`, `EndingSceneTransitionData`, `ChallengeResultTransitionData`, …) and reads back (`MapSceneTransitionData.BattleResult`) is in `Common/SceneTransition/TransitionData/`.

`Assets/Scenes/MapScene.unity` is the campaign map: a row of cells, one per stage, with the player's current cell highlighted, plus **Start Stage** and **View Deck** buttons (the latter is still a TODO).

## Stages come from assets, not from code

- `Common/ScriptableObjects/StageData.cs` — one stage: an `_id` and the `EnemyData[]` it fields. `CreateEnemyLineup()` drops null entries, so callers never null-check — the same contract `EnemyData.PickCards()` has.
- `Common/ScriptableObjects/CampaignData.cs` — the stage list in play order. `CreateStages()` likewise drops nulls. The one being played is the current episode's (`CampaignBalance.GetEpisode(CampaignProgressRepository.EpisodeIndex)`, `MapRunRuleFactory.ResolveCampaign`, which falls back to Episode 1 with an error if that episode has no stages), so each episode, and each difficulty's balance asset, can have its own stages.

**The number of cells is `CreateStages().Count`** — there is no stage count anywhere in the code. Adding or removing stages is editing that episode's `Episode1..3.asset`; the map, the progress bounds and the "campaign cleared" check all follow. The shipped campaign happens to have ten (`Assets/ScriptableObjects/Stages/Stage01..10.asset`), which is data, not a rule.

One real limit: a stage can field at most as many enemies as `BattleSceneViewContainer.EnemyStatusViews` has entries (two today). `BattleSceneInitializer.CreateEnemies` hides the spare views when a stage fields fewer, and logs an error when a stage asks for more than can be shown.

## Progress lives in a repository, not in a parameter

`Common/Repositories/CampaignProgressRepository` is a static holding `CurrentStageIndex` (0-based). The map reads it on `Start`, the map writes it after a win, and it survives the map → battle → map round trip untouched — which is the whole point: the progress is not threaded through `BattleSceneTransitionData` and back, because only the map cares about it.

It is `static` for the same reason `SceneTransitionDataStore` is: anything owned by a scene object dies at the scene boundary, and this has to outlive one. It is in-memory only; making a run resumable across app launches means adding save/load here and nowhere else.

## The round trip

1. **Start Stage** first opens the confirm dialog (`ConfirmDialog.prefab`, generated under `PopupCanvas` as the map's shared `WindowHandler<ConfirmShowData>`, message `UiTextKey.StageStartConfirm` plus `LAYER nn / NN`). Cancel or a click on the background just closes it; **OK** closes it and calls `MapStageTransitionUseCase.StartStage()`, which builds a `BattleSceneTransitionData` from the current stage's lineup, the deck and the player values, **including `CreateReturnData` (`ReturnToMap`, or `ReturnToMapOrEndChallenge` in a challenge)**, and loads BattleScene.
2. The battle runs and `BattlePhaseDirector` raises `OnBattleFinished(bool isVictory)` after a short delay that lets the win/lose toast be read. The director does not navigate — `BattleSceneInitializer` subscribes, builds a `BattleResult` and loads what the map's `CreateReturnData` makes of it: `ReturnToMap` wraps it in a `MapSceneTransitionData` (or, in a challenge, `ReturnToMapOrEndChallenge` sends a defeat to ResultScene). Both are `static`, since they run in the battle scene. Without the function the battle stays, which is what happens when BattleScene is played standalone. The map reads `MapSceneTransitionData.BattleResult` once in `Start` (null when it was opened from the title or reopened).
3. Back on the map, `MapRunUseCase.ApplyBattleResult()` consumes that result **before** building the cells. It first hands out the stage's reward (see "Rewards and tokens" below), then a win calls `AdvanceStage()` and a loss changes nothing. So a defeat leaves the player on the same cell and a victory moves them one forward.

BattleScene never learns that a map exists — it only knows a scene *name* it was told to return to. That keeps the dependency pointing one way (map → battle), the same rule the parameter type follows.

## Rewards and tokens

**Tokens** are the campaign's currency (they were called "points" before; the shop spends them). They live in `Common/Repositories/CampaignTokenRepository`, beside the progress and deck repositories, and **last for one campaign**: `TitleSceneInitializer` calls `Clear()` when Campaign is chosen, and the map hands out `CampaignBalance.StartTokens` (500) the first time it opens with the repository uninitialized.

- **Per-stage rewards are data** on `StageData`: `_victoryTokens` + `_victoryCard` (one card) for a win, `_defeatTokens` for a loss (about twice the victory tokens, and no card). `CreateReward(isVictory)` returns a `StageReward` (`Common/ScriptableObjects/Data/`), so the caller never branches on which fields apply.
- **Applied before display.** `ApplyBattleResult` reads the stage the player was on *before* advancing, adds the tokens to the repository and the card to `CampaignDeckRepository.OwnedCards`, and passes the `StageReward` to `MapPresenter` only to be shown. Leaving the map without pressing Claim loses nothing.
- **A defeat is framed as help, not a reward** — a **Backup Boost** with encouraging wording. Losing a stage can be repeated for more tokens; that is intended as a comeback aid.
- **The market** starts each visit from `CampaignTokenRepository.Tokens` (`CreateCardShopModel`), and every purchase writes the remainder back with `Set` (`MapRunUseCase.ApplyShopPurchase`), which also updates the Upgrade window's tokens (`UnitUpgradeModel.SetTokens`).

**Reward window** (`Reward/Views/StageRewardView.cs`, `Assets/Prefabs/StageRewardWindow.prefab` on `PopupCanvas`). It waits for the scene-transition cover to lift, then opens through the shared `WindowTransition`.

| | Victory | Defeat |
| --- | --- | --- |
| Header | `// ACCESS GRANTED :: LOOT DECRYPTED` (cyan) | `// CONNECTION LOST :: BACKUP BOOST` (magenta) |
| Title / message | `VictoryRewardTitle` / `VictoryRewardMessage` (yellow title) | `DefeatRewardTitle` / `DefeatRewardMessage` (cyan title) |
| Content | the card opens from a line with a white flash, its rarity-coloured glow and rotating rays behind it; then the tokens count up | tokens only, with green rays |
| Button | `RewardClaim` (受け取る) | `BackupAccept` (再起動) |

The Claim button appears only when the count-up finishes; a click on the scrim skips to the end. On Claim the presenter closes the window, the token display counts up from the pre-reward amount (`TokenCounterView`, `Common/Views/UI/`), and then the travel animation plays if the player advanced.

**Token display** sits **just above the Market button (`CardShopButton`)** (`Canvas/Tokens`), so it reads as "what you can spend there". It started in the header's top-right and was moved at the user's request.

**Clearing the campaign.** The last stage gives **no reward** (no tokens, no command; changed 2026-10-04): `ApplyBattleResult` only advances the progress and sets `_completedNow`. Back on the map, once the transition cover lifts, the stage's clear conversation plays (if any) and then `MapStageTransitionUseCase.CompleteCampaign` calculates the `CampaignResult` and loads EndingScene — no reward window and no travel animation. The same holds for the last layer of a limited challenge, which goes on to ResultScene. Every defeat also calls `CampaignProgressRepository.RecordBackupBoost()`, which the result screen's score subtracts. See @docs/ending-and-result-scenes.md.

**Saving.** The map is where progress is saved: after a battle result (win or loss), a saved deck edit, each market purchase and each applied unit upgrade; clearing the campaign records the clear and deletes the progress. See "Saving" in the root doc.

**Tokens spent in battle.** `StartStage` hands the current tokens to the battle (`BattleSceneTransitionData.Tokens`), and `ApplyBattleResult` subtracts `BattleResult.TokensSpent` first, before any other branch (the last stage included), so a defeat does not give them back.

**Mining bonus.** A victory's tokens are raised by `BattleResult.TokenBonusPercent` (`StageReward.WithTokenBonus`, rounded down; never on a defeat), and the reward window's detail line adds `MINING +50%`.

**Enemies and stages (2026-09-27).** Three enemies joined the Trojan: **Rootkit** (HP 110; block 8, a 4-damage probe, a 3-per-turn virus — tough and slow), **Spyware** (HP 60, two actions a turn: keylogger −1 hand for 2 turns, a 3-per-turn virus, data corruption, a 4-damage probe — fragile and annoying) and **Botnet** (HP 60; heals the most damaged ally for 12, 5-damage DDoS). The stages are now: 1 Trojan, 2 Spyware, 3 Rootkit, 4 Trojan+Trojan, 5 Trojan+Spyware, 6 Rootkit+Botnet, 7 Spyware+Botnet, 8 Trojan+Botnet, 9 Rootkit+Spyware, 10 Rootkit+Botnet. Rewards changed at 2 (Reverse Patch), 5 (Loopback), 7 (Kernel Panic) and 9 (Multithread).

**Harder enemies (2026-09-29).** At the user's request every enemy was made roughly 30–50 % stronger. The Trojan no longer uses the player's Attack / Strong Attack assets but its own `TrojanBackdoor` (バックドア, 9) and `TrojanPayload` (ペイロード, 20), so tuning an enemy never changes a player command; its virus is 2. Trojan HP 130; Rootkit HP 150, always 2 actions, block 12, probe 7, virus 4 (the probe is shared with Spyware); Spyware HP 80, virus 4; Botnet HP 80, DDoS 8, remote repair 16. The win-rate simulation below predates this and is no longer current.

Simulated with random First Builds (the same simple AI; win %):

| Stage | HP100/M10 | HP160/M11 | HP200/M12 | HP240/M13 |
| --- | --- | --- | --- | --- |
| 1 Trojan | 93 | 100 | 100 | 100 |
| 2 Spyware | 100 | 100 | 100 | 100 |
| 3 Rootkit | 77 | 98 | 99 | 100 |
| 4 Trojan+Trojan | 27 | 68 | 89 | 95 |
| 5 Trojan+Spyware | 47 | 90 | 98 | 98 |
| 6 Rootkit+Botnet | 34 | 75 | 85 | 92 |
| 7 Spyware+Botnet | 64 | 94 | 98 | 99 |
| 8 Trojan+Botnet | 52 | 90 | 97 | 98 |
| 9 Rootkit+Spyware | 39 | 85 | 94 | 96 |
| 10 Rootkit+Botnet | 30 | 75 | 88 | 92 |

The AI plays the most expensive command first and never aims heal reversal at the Botnet on purpose, so a player who does should find the Botnet stages easier than this.

## Deck edit (`DeckEdit/Presenters/DeckEditPresenter.cs`, `Prefabs/DeckEditWindow.prefab`)

The **Command Build** button (`Canvas/DeckEditButton`) opens the deck edit window on `PopupCanvas`, over the map, the same way Upgrade opens its window. Only Cancel and Save close it (no background click). Saving writes the deck to `CampaignDeckRepository` and saves the progress (`MapRunUseCase.ApplyDeckSaved`). Details: @docs/deck-edit-window.md.

## Market (`CardShop/Presenters/CardShopPresenter.cs`, `Prefabs/CardShopWindow.prefab`)

The **Market** button (`Canvas/CardShopButton`) opens the market window on `PopupCanvas`, over the map. Only its Close button closes it (no background click). Every purchase is applied at once: the commands go to `CampaignDeckRepository`, the tokens to `CampaignTokenRepository`, the map's token display counts down, and the progress is saved (`MapSceneInitializer.HandleShopPurchased`). The chips on sale are `MapSceneInitializer._shopPacks`. Details: @docs/card-shop-window.md.

## Unit upgrade (`Common/Models/UnitUpgradeModel.cs`, `UnitUpgrade/Presenters/UnitUpgradePresenter.cs`, `Prefabs/UnitUpgradeWindow.prefab`)

The **Upgrade** button (`Canvas/UnitUpgradeButton`, next to the Market button (`CardShopButton`); the token display sits above the two, since both spend tokens) opens a window where tokens raise the player unit's limits one step at a time:

| Stat | Step | Range | Cost of the next step | All steps |
| --- | --- | --- | --- | --- |
| Max HP | +20 | 100 → 300 (10 steps) | 40 + 10 × level | 850 |
| Max memory | +1 | 10 → 15 (5 steps) | 60 + 40 × level | 700 |

- **State.** The values live in `Common/Repositories/CampaignUnitRepository` (max HP and max memory only — every battle starts full). It lasts one campaign: `TitleSceneInitializer` clears it, and the map seeds it from `CampaignBalance.StartMaxHp` / `StartMaxMemory` (which are also the base the levels count from). `StartStage` builds the battle's `UnitStatusModel` from it.
- **Rules in one place.** The steps, limits and prices come from `CampaignBalance` as a `UnitUpgradeRule`; `UnitUpgradeModel` answers `CanUpgradeHp` / `CanUpgradeMemory`, the costs and the levels, so a button's enabled state and the actual upgrade cannot disagree. On a change the presenter re-syncs both rows (paying for one can make the other unaffordable) and counts the window's token display down.
- **Apply to confirm.** An Upgrade press is only pending: `UnitUpgradeModel` keeps the applied values (`AppliedTokens` / `AppliedHp` / `AppliedMemory`) beside the pending ones and reports `IsDirty`. **Apply** (反映, a `CyberButton` with `>>>`, right of Cancel; enabled only while something is pending) commits (`Apply()`), counts the map's token display down, reports `(tokens, maxHp, maxMemory)` (`MapRunUseCase.ApplyUnitUpgrade` writes them back and saves) and closes. **Cancel** (or a background click) closes at once when nothing is pending; otherwise the map's shared `ConfirmDialog` asks 変更を破棄しますか？ (破棄する discards with `Discard()` and closes, 戻る returns to the window with the pending steps kept). Opening the window always starts from the applied values. The confirm dialog is shared with Stage Start, so each presenter answers only while it is the one that opened it (`_isConfirmingDiscard` / `_isConfirmingStageStart`).
- **Look.** Each row (`UnitUpgradeRowView`) shows `140 → 160`, a `SegmentGaugeView` of levels in memory-chip cells, the cost (yellow, magenta when it cannot be paid, lime `MAX` at the limit) and a small Upgrade button; the value pulses when raised.
- **Trade-off.** Spent tokens are gone from the result screen's score, so how much to upgrade versus keep is the player's choice.

**Why these numbers — a balance simulation (2026-09-27).** Battles were replayed with the real rules (`BattleCommandUseCase`, card and status effects, the Trojan's own `IEnemyCardPicker`) and a simple player AI (queues up to 3 affordable commands, most expensive first; switches to defence and healing below 40% HP; attacks the weakest enemy; never edits the starting deck or uses rewards). Win rates, 2000 battles each:

| Player | 1 Trojan (stages 1–3) | 2 Trojans (stages 4–10) |
| --- | --- | --- |
| HP100 / memory 10 (start) | 98% | **7%** |
| HP140 / memory 10 | 100% | 44% |
| HP100 / memory 15 | 100% | 37% |
| HP160 / memory 12 | 100% | 86% |
| HP200 / memory 10 | 100% | 93% |
| HP200 / memory 13 | 100% | 99.5% |
| HP240 / memory 14, HP300 / memory 15 | 100% | 100% |

Unupgraded, stages 4–10 were close to impossible without many Backup Boost retries. HP matters far more than memory, which is why HP steps are cheap and fine-grained. With the prices above, the tokens available before stage 4 (500 + 320) buy HP200 + memory 12. Stages 4–10 all field the same two Trojans, so the difficulty curve is flat after stage 4; varying the enemies is the real fix for pacing.

## First Build — the starting deck is drawn (2026-09-27)

A new campaign starts with **no commands and no deck**. The first time the map opens, `MapRunUseCase.InitializeDeckIfNeeded` opens the **First Build**, 10 `FirstBuildChip`s, from `CampaignBalance`:

- **What comes out.** 10 × 3 = 30 commands. Attack ×5 and Block ×4 are guaranteed (they take slots first); the other 21 are drawn with the chip's own pool and weights, the same as the Starter Chip (Normal 70 / Rare 25 / Epic 5). The guarantee is what keeps a bad draw from being unwinnable.
- **The deck builds itself.** All 30 go into the owned commands, and `AutoDeckBuilder` (`Common/Models/Cards/`) picks 20 for the deck by role: 10 attack (attack or virus effects), 6 defence (block), 4 support (anything else). Within a role it takes higher rarity first, then lower cost. A role that is short is filled with the best of the rest. It uses no randomness, so the same draw always gives the same deck. The player can rework it in Deck Edit; the other 10 stay owned.
- **Saved at once.** Progress is saved right after the draw, before anything is shown. Quitting during the reveal therefore resumes with the same commands; there is no reroll by restarting.
- **Shown on the map.** `FirstBuildPresenter` opens `Prefabs/ChipInstallWindow.prefab` (the shop's unpack window, shared) once the scene transition has lifted: title `ファーストビルド  チップ10個`, one install, then all 30 flipped lowest rarity first, skippable with a click. Its button is **デッキにセット / Set to Deck**, and closing it shows a toast that 20 commands were set into the deck. A resumed campaign shows nothing.
- **Tokens are unchanged** (500).

**Simulation (2026-09-27, same AI as above, 400 draws, each fought 5 times).** Against one Trojan unupgraded, random First Builds win **92.5%** (the old fixed deck 90.3%), and only 0.8% of draws never won. Against two Trojans: 13.8% unupgraded (old deck 0.8%), 62% at HP160 / memory 11, 82% at HP200 / memory 12, 91% at HP240 / memory 13. A First Build is on average stronger than the old deck, since it brings rares. How hard the later stages should be is to be tuned later, for example with single-command purchases in the shop.

Before this (2026-09-27), a campaign started with a fixed kit: 19 normals and the rares Heal, Strength and Overclock, with a fixed 20-command deck. The balance simulation above used an even earlier, stronger fixed deck.

## Challenge mode — the same map under another rule (2026-10-04)

The title's **Challenge** button (it was Endless until 2026-10-04) opens a choice of **Normal (30 layers), Hard (60), Master (100) and Endless (no end)**, and every one of them runs on this same scene. Nothing is duplicated: what differs from Story is injected as a `MapRunRule` (`Models/MapRunRule.cs`), which `MapRunRuleFactory.Create` picks from `RunModeRepository`.

| | `CampaignMapRule` (Story) | `ChallengeMapRule` |
| --- | --- | --- |
| Rooms | the episode's `StageData`; `StageCount` is their number | `RunModeRepository.ChallengeLayerCount` (30 / 60 / 100), or no end (`StageCount` null) for Endless |
| A room's content | `StageData.CreatePlan()` | `EndlessBalance.CreateStage(seed, index)`, cached per index |
| Story (`HasStory`) | opening and clear conversations, tutorial, ending, Backup Boost | none: no conversation, the tutorial button is hidden |
| Defeat (`EndsOnDefeat`) | back to the map with a Backup Boost | the battle goes straight to ResultScene (`ChallengeResultTransitionData`, recorded and made by `ReturnToMapOrEndChallenge`) |
| Last room won | no reward; clear conversation, then ending and result | no reward; recorded, then ResultScene with `ChallengeResultTransitionData` (Normal / Hard / Master; Endless never ends) |

- **A room is a `StagePlan`** (`Common/ScriptableObjects/Data/`): the enemies, an `EnemyStrength`, the victory and defeat rewards and the clear conversation. The map, the reward flow and `EnemyDetailPresenter` (handed a `Func<StagePlan>`) only see that, so they never ask whether a stage is an asset or random.
- **The layer counts are data:** `Assets/ScriptableObjects/ChallengeCatalog.asset` (`ChallengeCatalog`, its order is the order of the buttons; 0 = no end), on `TitleSceneInitializer._challengeCatalog`. A run keeps the count it was started with (`RunModeRepository.ChallengeLayerCount`, saved), so tuning the catalog never changes a suspended run.
- **Shared with Story:** the First Build, tokens, the market, unit upgrade, Command Build and the four campaign repositories. A new challenge clears them like a new campaign.
- **`EndlessBalance`** (`Assets/ScriptableObjects/EndlessBalance.asset`, on `MapSceneInitializer._endlessBalance`) is every challenge's balance — the name is from when Endless was the only one. The starting kit, upgrades and prices stay on `CampaignBalance`.
  - **Enemies.** Each entry has the layer it starts from and whether it may appear alone. Trojan and Spyware from layer 1, Rootkit from 3, Botnet from 10 and never alone (it only heals allies). From layer 10, 60 % of layers field two (the battle shows at most two).
  - **Strength.** Every 10 layers (`_layersPerTier`), enemies get +25 % max HP and +2 attack damage: none on layers 1–10, one step on 11–20, and so on.
  - **Victory.** 100 + 10 × (layer − 1) tokens and one command drawn from the tier's chip (the Starter Chip on layers 1–10, the Premium chip after that). There is no defeat support.
  - **Deterministic.** A layer is built from the seed and its index, so the same seed always gives the same layer: going to battle and back, or resuming, never rerolls it. Only the seed is saved (`RunModeRepository.ChallengeSeed`, rolled by the title).
- **The battle stays ignorant.** `BattleSceneTransitionData.EnemyStrength` scales max HP and adds a `LayerBoostStatusEffect` (階層ブースト / Layer Boost, + attack damage). It is its own effect rather than strength, so an enemy's own strength does not cancel it under the keep-the-larger rule. The Enemy Info window shows the scaled HP and the boost.
- **Ending a run.** A defeat loads ResultScene directly; going through the map would show it and leave it again. Clearing the last layer of a limited challenge gives no reward and `CompleteCampaign` loads ResultScene right away. Either way `FinishChallenge` calls `CampaignSaveService.FinishChallengeRun` (that challenge's best layer and cleared flag, `endless.sav` deleted) before the transition, and ResultScene shows the `ChallengeResultTransitionData` it built (@docs/ending-and-result-scenes.md). Giving up counts as a defeat.
- **One suspended challenge.** All challenges share one save file (`endless.sav`); the title asks to resume it when Challenge is pressed (@docs/title-scene.md).
- **Pages.** `MapPresenter` lays the rooms out on a straight route (room *i* at `startX + i × step`); ten rooms make a page (`MapPresenter.PageSize`). `MapRouteScrollView` (on `Canvas/RouteScroll`) shifts `LinkParent`, `CellParent` and `PlayerParent` together so that the current page is centred. Only the current page and its neighbours exist (`MapPageWindow`): rooms and traces leaving that window go back to a pool and are reused, so a run at layer 300 has the same 30 rooms as one at layer 1. When the player travels across a page boundary (room 10 → 11), the route scrolls one page, grid-stepped, while the packet flies (`MapPlayerView.PlayTravel`'s `onDeparted`, same duration). A ten-room Story episode is one page, so it looks as before; on a 16:9 screen the first room of the next page peeks in at the right edge in a challenge.
- **Very deep layers never overflow** (2026-10-04). Nobody should get near these limits by playing, but a debug jump or an edited save can. The layer index stops at `CampaignProgressRepository.MaxStageIndex` (`int.MaxValue - 1`, so "index + 1" still fits), and every value computed from it saturates instead of wrapping: `EndlessBalance.CreateStrength` and `VictoryTokensOf` compute in `long` and clamp to `int`, as do `StageReward.WithTokenBonus`, `LayerBoostStatusEffect` and `MapPageWindow.Of`; `MapRunRule.LayerText` and the resume summary add 1 in `long`; the room address shows its low four hex digits (`MapCellView.FormatAddress`, back to `0x0000` every 256 layers). Room and player positions are relative to the page the map opened on (`MapPresenter._originIndex`), so they stay small and float precision never makes the route jitter. `EndlessOverflowTests` checks each one at the limit.
- **Debug.** `Map: Jump to Layer` (both modes) reloads the map at a given layer; `Map: Clear All` is Story only.

## Scene composition

- `Views/MapCellView.cs` — one cell: its number, and a colour for cleared / current / not reached. It is a prefab (`Assets/Prefabs/MapCellView.prefab`) because the cells are made at runtime.
- `Views/MapSceneViewContainer.cs` — the cell parent and prefab, plus the two buttons and their labels. No cells are authored in the scene.
- `Presenters/MapPresenter.cs` — lays the rooms out page by page from a pool (see "Challenge mode" above; script-driven positioning, like the battle's hand — see @docs/battle-scene.md for why no Layout Group), paints their state from the repository, pushes the button labels from `UiTextKey`, and reports presses through injected `Action`s. It disables **Start Stage** once the campaign is complete.
- `FirstBuild/Presenters/FirstBuildPresenter.cs` (a window) — shows the First Build in the shared `ChipInstallWindow` once the transition cover has lifted, and toasts the auto-set deck on close. It only shows: the draw and the deck were decided and saved by the Initializer.
- `Initializer/MapSceneInitializer.cs` — composition root: builds the models, use cases, windows and presenters below and wires them; it holds no flow of its own.
- `UseCase/MapRunRuleFactory.cs` — picks the `MapRunRule` (Story or Challenge) from `CampaignBalance` / `EndlessBalance`.
- `UseCase/MapRunUseCase.cs` — the run's state on the map: seeds the deck, tokens and unit on a new run, applies a battle result (`ApplyBattleResult` → `Data/MapArrival`: where the player came from, the reward, whether the run just completed), applies deck saves, purchases and upgrades, and saves (`SaveIfAllowed`: not during the map tutorial, not once complete).
- `UseCase/MapStageTransitionUseCase.cs` — leaving the map: starting a stage, the ending, suspending; `ReturnToMap` (and, in a challenge, ending the run on a defeat) is the static function handed to the battle.
- `UseCase/MapTutorialUseCase.cs` — whether to play the tutorial, skipping and replaying it, and remembering it was seen.
- `Initializer/MapDebugCommands.cs` — the 0707 panel's map commands (debug builds only).
- `Presenters/MapRouteLayout.cs` — where each room and the player stand, page by page (`MapPresenter` uses it; `MapRouteLayoutTests`).

## Known gaps

- **Deck**: the deck is an Inspector array on `MapSceneInitializer`. When deck editing exists it should come from a deck repository beside the progress one; the field is marked TODO.
- **Player state**: every stage starts from full HP, because the map builds a fresh `UnitStatusModel` each time. Carrying damage between stages is a one-line change (keep the model in a repository instead), and `BattleSceneTransitionData` already takes a whole `UnitStatusModel` so the battle needs no change.
- **View Deck** shows a "not implemented" toast.
- Starting a new campaign clears the tokens but not `CampaignDeckRepository`, so cards owned in an earlier campaign carry over within the same app session.

## Conversations (`Common/Conversation/`, `Prefabs/ConversationWindow.prefab`)

The story is told in short conversations overlaid on the map: a text box at the bottom of the screen (`// TRANSMISSION`, speaker in yellow, the line typed out at 40 characters per second), with the map blurred behind it like any window (`WindowTransition`, so it stacks and fades like the others). A click anywhere — box or background — first completes a line still being typed, then advances; after the last line the window closes.

- **When.** Two moments, both after the scene-transition cover has lifted (`MapSceneInitializer.PlayWhenShownAsync`):
  - starting an episode: `CampaignData.OpeningConversation`, then the tutorial question (@docs/tutorial.md);
  - coming back from a won stage: that stage's `StageData.ClearConversation`, then the reward window. `MapPresenter` is built with `holdReward` and opens the reward only when `ShowReward()` is called. A defeat plays nothing.
- **Data.** `ConversationData` (ScriptableObject, `Create > Campaign/Conversation`): an `_id` and `ConversationLine[]` (speaker, text). Episode 1's are in `Assets/ScriptableObjects/Conversations/Episode1/` (`Episode1_Opening`, `Episode1_StageNN_Clear`), placeholders with three lines each. Replace the text in the assets; an empty reference or a conversation with no lines is skipped and the next step follows at once.
- **The text is not localized.** It is authored on the asset, so `FontCoverageTests` does not see it; `ConversationDataTests` checks every character against DotGothic16, unique ids, no empty lines, and that Episode 1 has an opening and a clear conversation per stage.
- **Hand-off without a blur gap.** `ConversationPresenter` calls the next step (which opens the next window) *before* closing itself, so the blur never drops between the conversation and what follows.

## Menu (the shared `Window/Menu/`, `Prefabs/MenuWindow.prefab`)

`Canvas/MenuButton` (top-right, the tutorial button's mirror image: 0.6x, label `UiTextKey.Menu`) opens the shared menu window, generated under `PopupCanvas` (`MenuWindowView` / `MenuWindowPresenter`, `// MENU`, メニュー; see "Button-list windows" in the root doc): **中断 / Suspend**, **オプション / Options** and Close (a background click also closes). `MapSceneInitializer` only builds its `MenuWindowInitData`. Like every other map window it opens through `WindowTransition` and blurs the map. The battle's Menu is the same window since 2026-10-06.

- **Options** opens the same `GameOptionsWindow` as the battle and the title (`GameOptionsPresenter.Open`, built without `onDeleteUserData`, so no User Data row) on top of the menu; closing it returns to the menu.
- **Suspend** asks 「中断してタイトルに戻りますか？」 (detail 「進み具合は保存され、タイトルから再開できます」, 中断する / 戻る; `UiTextKey.SuspendConfirm` / `SuspendConfirmDetail` / `SuspendOk`, the item's `MenuConfirmData`) through the shared `ConfirmDialog`. OK closes the menu and calls `MapStageTransitionUseCase.Suspend`, which saves the progress (not during the map tutorial or after the last stage, like the other milestones) and loads the title, where Story or Challenge offers to resume it.
- The copied button had the tutorial button's `TutorialAnchor`; it was removed, so the tutorial still points at the tutorial button only.

## Tutorial

While `CampaignTutorialRepository.Stage == Map`, the map runs the tutorial walkthrough instead of the First Build and does not save; a result coming back from the practice battle is ignored and ends the tutorial. The **チュートリアル** button (top-left) replays it at any time. See @docs/tutorial.md.

## Look

Styled per @docs/tone-and-manner.md as a walk **deeper into cyberspace**.

- **Rooms.** Each stage is a room (`MapCellView`: dark fill, a frame tinted by state, number `01`…, decorative address `0x0100`…) on a zigzag route. Cleared rooms are lime; the current room is yellow and pulses; unreached rooms are dim cyan.
- **Traces.** `MapLinkView` joins neighbouring rooms, lit for the stretch already travelled.
- **Header.** `// NETWORK DEPTH` / `LAYER nn / NN` at the top.
- **Buttons.** The three buttons are the slanted ripple buttons, anchored to the bottom, with Stage Start (primary, `>>>` deco) in the middle, scaled to 1.2x so it stands out. It is enlarged with `localScale`, not a taller rect, because the slanted sliced sprite kinks when stretched vertically (see @docs/art.md). On screen the buttons read **接続開始 / Connect** (Stage Start), **コマンドビルド / Command Build** (Deck Edit) and **マーケット / Market** (Chip Shop); this doc keeps the functional names. Their labels auto-size, capped at the original size and kept 28 px inside each side, so a long name shrinks instead of spilling off the slanted face.
- **Background.** `bg_map.png` puts the horizon low, so the route floats in the night sky.
- **Enemy Info** (`Canvas/EnemyDetailButton`, a `CyberButton` at 0.8x above Command Build, label `UiTextKey.EnemyDetail`; it is disabled once the campaign is complete) opens `Prefabs/EnemyDetailWindow.prefab` on `PopupCanvas`, run by `EnemyDetailPresenter` (`Window/EnemyDetail/`) → `EnemyDetailWindowView`. The window is a copy of the Unit Upgrade window, so it opens, closes, blurs and dissolves through the shared `WindowTransition` like every other window; Close or a click on the background closes it.  The window's views (`EnemyDetailWindowView`, `EnemyDetailSlotView`, `EnemyCommandRowView`, `EnemyDetailInfo`) live in `Common/Views/EnemyDetail/` and the text is built by `Common/Presenters/EnemyDetailInfoBuilder`, because the title's enemy collection uses the same window (see @docs/title-scene.md). The deck edit window has a second button for the same window (it calls the same handler's `Show`; see @docs/deck-edit-window.md).
  - **Left: the enemies** of the current stage (the next battle), one `EnemyDetailSlot.prefab` per enemy type (`x2` when the same enemy appears twice), each with the enemy's idle animation (its `UnitVisualDefinition`, mirrored to face left as in battle) and name. Clicking one selects it; the selected slot gets a yellow glow frame and a yellow pixel cursor (`UI/ui_cursor.png`) swinging beside it. Opening selects the first.
  - **Right: the selected enemy.** Name, `HP` and actions per turn (`UiTextKey.EnemyActionCount`), its starting effects such as immunities (yellow), its traits (`EnemyDescription` table), and under `UiTextKey.EnemyCommands` every distinct command it uses as an `EnemyCommandRow.prefab`: illustration, name and **the command's effect text** (`CardDescription`, with the real numbers). Everything but the traits comes from `EnemyData` / `CardData`, so tuning an enemy or a command updates the window.
  - **Nothing is cut.** `MapEnemyInfoLayoutTests` measures every enemy trait and every enemy command's rendered description, in every locale, against its box. It measures on an **instantiated** prefab with its inactive parents enabled: on the prefab asset, or under the window's inactive `Dialog`, TMP is not initialised and every text measures the same, so the test would pass no matter what.
  - Single-line texts use `Overflow`: with DotGothic16's tall line height, `Ellipsis` in a short rect drops every character with no error.

**Advancing.** `MapRunUseCase.ApplyBattleResult` returns the room the player came from when a victory advanced them, and `MapPresenter` gets it as `arrivedFromIndex`. The presenter then shows the *previous* room as current, disables Stage Start, and has `MapPlayerView` play the move:

1. It waits for the scene-transition cover to lift, plus `_startDelay`.
2. The player collapses to a line and bursts into electrons.
3. A glowing packet flies, grid-stepped, along the trace, leaving a trail.
4. At the next room it bursts again and opens back into the player; `onArrived` re-syncs to the real current room.

Rooms, traces and player live under three parents with **identical position and anchors** (`LinkParent`, `CellParent`, `PlayerParent` — drawn in that order), so the presenter can compute player positions from cell positions directly. Keep them aligned if you move one.

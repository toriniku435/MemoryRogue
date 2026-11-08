# EndingScene and ResultScene

Detail doc for `Live2DCardRogue.EndingScene.*` and `Live2DCardRogue.ResultScene.*` (`Assets/Scripts/Scenes/EndingScene/`, `Assets/Scripts/Scenes/ResultScene/`), imported from the root `CLAUDE.md`. Read @docs/map-scene.md for how a campaign reaches them.

Clearing the campaign's last stage goes **map (reward window) → EndingScene → ResultScene → TitleScene**. Both scenes are the TitleScene shape at its smallest: Initializer + ViewContainer + Presenter, no Model or UseCase. Their scenes were made by copying TitleScene (UICameraRig, BackCanvas with `bg_title`, EventSystem, post-processing volume, `SceneBgm` on the title track) and replacing the Canvas contents.

## The result is decided before the ending

`Common/Models/CampaignResults/` holds the rules, so the ending can branch on the rank before the result screen ever shows it:

- **`CampaignResultCalculator.Calculate(remainingTokens, backupBoostCount)`** returns a `CampaignResult`: the inputs, the points, the penalty, `Score`, `Rank` and `Route`.
- **Score** = remaining tokens × `PointsPerToken` − Backup Boosts × `PenaltyPerBackupBoost`, never below 0. The penalty is kept larger than any Backup Boost's tokens, so losing on purpose to farm tokens always lowers the score (`CampaignRankReachabilityTests` checks this against each balance asset's own stage rewards).
- **Rank** (`CampaignRank`, E..SSS, ordered so `>=` works) comes from the rank thresholds, and **Route** from the two route boundaries. All of these numbers live in **`CampaignBalance`** (see "Game balance" in the root doc) and reach the calculator as a plain `CampaignScoreRule` (`balance.CreateScoreRule()`). Current thresholds: SSS 1950, SS 1600, S 1200, A 900, B 600, C 350, D 150, E 0. The best possible score is 500 starting tokens + 1470 from winning stages 1–9 = 1970 (the last stage gives no reward since 2026-10-04, which is when SSS moved down from 2000), and stages 4–10 practically require spending on upgrades or chips, so SSS is reachable only by keeping total spending to about 20 tokens. `CampaignRankReachabilityTests` fails if the starting tokens or rewards change so that SSS can no longer be reached.
- **Route** (`EndingRoute`): by default S and above → `Best`, B–A → `Normal`, E–C → `Bad` (`_bestRouteFrom` / `_normalRouteFrom` on the balance asset).

`MapStageTransitionUseCase.CompleteCampaign` calculates it from `CampaignTokenRepository.Tokens` and `CampaignProgressRepository.BackupBoostCount`. The Backup Boost count is incremented by `ApplyBattleResult` on every defeat and cleared by `Reset()` when a campaign starts. The result goes out in `EndingSceneTransitionData`, and EndingScene hands the same object on in `ResultSceneTransitionData`. Neither scene calculates anything. For testing, the map's debug panel (0707) has **Map: Clear All (score)**, which goes straight to the ending with a typed score. Played standalone, both fall back to `_remainingTokensMock` / `_backupBoostCountMock` on their Initializer.

## EndingScene (placeholder)

A big `Ending` and `// TAP TO CONTINUE`, with a full-screen `ClickArea` (`TapArea`) that loads ResultScene. The route is only logged for now. When the real endings exist, branch on `CampaignResult.Route` in the presenter (or one view per route); nothing upstream needs to change.

## ResultScene

`Views/ResultView.cs` plays the whole sequence; the presenter only pushes labels (`Result`, `ResultRemainingTokens`, `ResultBackupBoosts`, `Score`, `Rank`, `BackToTitle`), calls `Play(result)` and reports **Back to Title** (`SceneTransitionUseCase.LoadScene(SceneName.Title)`; the campaign state is reset the next time Campaign is chosen).

1. The two breakdown rows slide in: remaining tokens `+N` (lime), Backup Boosts `-N` (magenta).
2. The score counts up from 0 (`TokenCounterView`, 1.4 s). The score row is shown on its own timer, so a score of 0 — which finishes counting instantly — still appears.
3. After a pause that grows with the rank, the rank **decodes in** (`RankReveal` sound). It opens from a horizontal line, the letters scramble and lock left to right, and cyan/magenta ghost copies and a scan bar jitter sideways. It then settles with a subtle flash.
4. The Back to Title button (`CyberButton`) appears.

**How lavish it is depends on the rank tier** (`ResultView.TierOf`):

| Tier | Ranks | Pause / decode | Glitch | On lock-in |
| --- | --- | --- | --- | --- |
| 0 | E, D | short | small | `Confirm` sound only |
| 1 | C, B | | | + 1 ring, faint flash |
| 2 | A, S | | | + 2 rings, rotating rays, `Reward` fanfare, an occasional idle glitch |
| 3 | SS, SSS | longest | largest | + 3 rings, stronger rays, rainbow text and rays |

Rank colours are `_rankColors` (grey for E/D, cyan C/B, magenta A, gold S/SS, white-rainbow SSS). A click anywhere (`SkipArea`) skips to the decode, but never past it. The first version dropped a huge letter with a screen shake; it was replaced by the decode at the user's request (see "Tried and dropped" in @docs/tone-and-manner.md).

### Challenge result

A challenge ends in ResultScene with a `ChallengeResultTransitionData` (the challenge, the layer reached, whether it was cleared, the best layer and whether that is a new record): a defeat comes straight from the battle (made by the map's `ReturnToMapOrEndChallenge`), and clearing the last layer of Normal / Hard / Master comes from the map (`CompleteCampaign`). **The sender records the run before the transition**, in `MapSceneInitializer.FinishChallenge` (`static`, since on a defeat it runs in the battle scene): it takes the challenge from `RunModeRepository` and the layer from `CampaignProgressRepository` (the last layer when cleared), records it with `CampaignSaveService.FinishChallengeRun` (per challenge in `CampaignClearData.challengeRecords`, version 6: best layer and whether it was ever cleared; the old `endlessBestLayer` is read as Endless's record; the run's `endless.sav` is deleted) and builds the data — the same way the map fixes a Story result before the ending. `ResultSceneInitializer` only hands it to `ChallengeResultPresenter`. (Until 2026-10-06 ResultScene recorded the run itself and built a non-transition `ChallengeResult`.) The same `ResultView` plays it through `PlayEndless`:

- the title reads 「ノーマル リザルト」 (`UiTextKey.ChallengeResult`, `{0}` = the challenge's name);
- the two rows are **Layers Cleared** (`ChallengeClearedLayers`) and **Best Record** (`ChallengeBestLayer`, `LAYER 42`, with a yellow 新記録！ / NEW RECORD! when beaten);
- there is no score row and no count-up;
- instead of the rank, the **layer reached** decodes in, scrambling through digits, captioned 到達階層 (`ChallengeReachedLayer`), or 踏破 / Cleared (`ChallengeCleared`) with the last layer when the challenge was cleared. Its tier is one per ten layers (`EndlessTierOf`: 1–10, 11–20, 21–30, 31+), borrowing the D / B / S / SSS colours and effects.

Once it settles, **a tap anywhere** returns to the title (`OnTapAfterFinished`), as does the Back to Title button. Played standalone, `_endlessMock` shows a challenge result (`_challengeMock`, `_endlessReachedLayerMock`, `_challengeClearedMock`) without writing a record.

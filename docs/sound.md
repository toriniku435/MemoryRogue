# Sound

Detail doc for the game's sound (`SoundSource/` → `Assets/Audio/`, played through `Common/Audio/`), imported from the root `CLAUDE.md`. What the sound should *be* is in @docs/tone-and-manner.md ("Sound"); this file is how it is made and wired.

## Everything is a placeholder, generated in code

Like the art, every sound is synthesised by Python (`numpy` only) and written to a **fixed file name**, so a real sound can replace it later by overwriting the file:

```powershell
pip install -r SoundSource/requirements.txt
python SoundSource/build.py            # everything
python SoundSource/build.py sfx        # effects only
python SoundSource/build.py music      # BGM only (takes ~20 s)
```

| File | What |
| --- | --- |
| `SoundSource/synth.py` | the parts: square / triangle / saw / supersaw / noise, envelopes, FFT low/high-pass, a time-varying low-pass for short sounds, echo, a small reverb, bitcrush, WAV writer |
| `SoundSource/sfx.py` | one function per effect; `SOUNDS` maps a key to it and writes `Assets/Audio/SE/se_<key>.wav` |
| `SoundSource/music.py` | one function per track; `SONGS` → `Assets/Audio/BGM/bgm_<key>.wav` |

The keys are the `SeId` / `BgmId` member names in snake case (`CardSelect` → `se_card_select.wav`); `SoundLibrary` is filled by that convention.

**BGM** is built from a 4-bar chord loop plus pad, bass, arpeggio, drums and a lead. The melody is generated from the chords with a fixed seed: chord tones on strong beats, scale tones between them, as phrases A B A B′ (B′ lands on the tonic). The render has a 3-second tail, which is added back onto the start before cutting to the loop length, so the reverb carries across the loop point. Nothing is sampled or quoted from existing music.

| Track | Tempo, key | Feel |
| --- | --- | --- |
| `bgm_title` | 100, A minor | dreamy pad, chip arpeggio, lead only in the second half |
| `bgm_map` | 92, D dorian | exploring: bell melody, light beat |
| `bgm_battle` | 150, D major (I–V–vi–IV), 32 bars | trance-leaning and bright. Verse (rolling 16th bass, 16th arpeggio, lead) → verse with soft claps and off-beat stabs → build-up (held bass, a quiet snare roll from quarters to 16ths, noise riser, the kick drops out, a half-beat of silence) → drop (one crash, supersaw off-beat stabs pumping with the kick via `sidechain`, lead an octave up doubled by a saw). The drums stay an accompaniment |
| `bgm_deck_edit` | 88, C major | calm work: maj7 pad, slow triangle arpeggio. Unused since deck editing became a window on the map (the map's loop keeps playing); `bgm_shop` likewise since the market did |
| `bgm_shop` | 116, F major | bouncy root–fifth bass, claps, bright lead |

`BgmId.BossBattle` (boss battles) is **not generated**: the user supplies that track and assigns it in `SoundLibrary`. Until a clip is set there, `SoundLibraryTests` reports it as missing.

**Import settings** come from `Editor/AudioImportPostprocessor.cs` on first import only (like the art): SE decompress on load (ADPCM), BGM streams as Vorbis. Changing that file does nothing for clips already imported — call `AudioImportPostprocessor.Apply` on their importer from a `run_script`.

## Playing sound: `AudioUseCase` only

`Common/Audio/` is a facility like `Message/`:

- **`AudioUseCase.PlaySe(SeId)` / `AudioUseCase.PlayBgm(BgmId)` is the only way in.** Views, presenters and UI parts call it; nothing else touches the player. It does nothing outside Play mode, so an EditMode test that reaches a sound cannot trip over `DontDestroyOnLoad`.
- `SoundPlayer` (`internal`) is the self-instantiating `DontDestroyOnLoad` object behind it. It loads `Resources/SoundLibrary.asset`, keeps ten effect voices round-robin, and two BGM sources for a crossfade. The same effect is not restarted within `SoundLibrary._seMinInterval` (0.04 s), so a sweep of hovers does not pile up.
- `SoundLibrary` maps each id to a clip, a volume and (for effects) a small pitch jitter; it also holds the master SE / BGM volumes and the crossfade length. **Every sound is set in this one asset.**
  - **Rows are keyed by the enum member's name**, not its number, so adding an id in the middle of `SeId` / `BgmId` cannot shift the assignments. (`CommonButton`, `WindowTransition`, `SceneBgm` and the like still store the id as a number, so add new ids at the end anyway.)
  - **Its Inspector** (`Editor/SoundLibraryEditor.cs`) lists every id (except `None`) in enum order, with its usage label, clip, volume, pitch jitter and a ▶ preview button that plays in Edit mode at the asset's own volume. An id with no clip is red; giving it a clip adds its row. Rows whose name is not in the enum, or that repeat a name, are listed in a warning with a button that removes them.
  - **A second, layered sound** per effect (`Clip2`, `Volume2`, `Delay2`): when set, `SoundPlayer` plays it on a second voice together with the first, with the same pitch jitter, `Delay2` seconds later (`AudioSource.PlayDelayed`). `StopSe` stops both, and the 0.04 s retrigger guard is per id, so the two never drift apart. A layered effect uses two of the ten voices. The ▶ preview plays both.
  - **Preview on pick.** With **選んだら試聴** on (a toggle at the top, kept per machine in `EditorPrefs`), choosing a sound for 音 or 音 2 — in the asset picker, by drag-and-drop, anywhere — plays that sound alone at its row's volume; choosing None stops it. The row last picked or previewed can be replayed with its layer (音 2 and the delay) by the shortcut **A** (`Live2DCardRogue/SoundLibrary 重ねて試聴`, rebindable in Edit > Shortcuts). It only works while the Inspector or the asset picker has focus, and not while typing in a text field (such as the picker's search box); the A bindings of Timeline, Animation, the Curve Editor and the Scene view are window-specific and win there.
  - **Usage labels** are `[SoundUsage("…")]` on each enum member (`SoundUsageAttribute`). Write one when adding an id.
  - `SoundLibraryTests` (EditMode) fails on an id with no clip, a row with an unknown or repeated name, or an id without a usage label — all of which would otherwise only show as silence.
- **BGM is chosen per scene by a `SceneBgm` component** (a root object named `SceneBgm` in every scene). It asks for its track in `Start`; asking for the track already playing does nothing, so going back and forth between scenes with the same track does not restart it.
  - **The battle's track comes from the stage.** `StageData._bgm` (default `Battle`; Episode 1's Stage 10, the Ransomware boss, is `BossBattle`) rides on `StagePlan.Bgm` and `BattleSceneTransitionData.Bgm`, and `BattleSceneInitializer` hands it to the scene's `SceneBgm` with `Override` (its `_bgmMock` when played on its own). A challenge's random stages always play `Battle`. `SceneBgm` runs late (`[DefaultExecutionOrder(100)]`) so that the Initializer's `Start` has overridden it before it plays: playing the default first and switching a frame later would cut off the previous scene's track mid-crossfade.

**Stopping a sound.** `AudioUseCase.StopSe(id)` stops every voice playing that effect. Only long effects need it (the chip install's charge), so a skipped sequence does not leave it running.

## Where each sound comes from

The rule: a view plays the sound of **raw input** it receives; a presenter plays the sound of an **outcome** it decides.

| Sound | Played by |
| --- | --- |
| button hover / click, `Error` when a disabled button is pressed | `CommonButton` itself (`_hoverSe`, `_clickSe`, editable in the Inspector through `Editor/CommonButtonEditor.cs`). Buttons with a role are set by name: Execute / OK / Save / Buy / Campaign → `Confirm`, Cancel / Back → `Cancel`, deck edit + / − → `MemoryWrite` / `CardReturn` |
| window open / close | `WindowTransition` (`_openSe`, `_closeSe`) — every window, so it stays unified |
| scene change glitch | `SceneTransitionEffect` when a transition starts |
| hand card selected (`CardSelect`), unplayable card inspected (`Hover`), refused click (`Error`), selection cancelled by an empty click (`Cancel`) | `BattleCardHandPilePresenter` |
| card written to memory (`MemoryWrite`), taken back (`CardReturn`), executed (`CommandExecute`) | `BattleCommandListPresenter`, from the same place it picks the card's exit animation |
| an enemy summoned (`MemoryWrite`) | `EnemySummonPresenter` (BattleScene), when the view appears |
| an enemy using a command (`EnemyAction`, a gritty descending buzz — different from the player's `CommandExecute`) | `BattleActionPresenter`, on `BattleCommandExecuteUseCase.OnCardExecuting` when the user is not the player |
| a unit losing HP (`Hit`; `VirusTick` when the type is `TakePoisonDamage`, i.e. virus and worm ticks), gaining HP (`Heal`, also regeneration) | `UnitPresenter`, with the floating number |
| a status effect applied or stacked (`StatusApply`; not block, which has its own) | `UnitPresenter`, with the floating effect name |
| block gained or stacked (`BlockGain`), absorbing a hit with some left (`BlockAbsorb`), breaking at 0 (`BlockBreak`) | `BattleBlockBarrierView`, from the same place it picks the barrier's animation |
| a list card clicked (`CardSelect`) | `DeckEditCardView`, `CardPackView` |
| confirm dialog background click (`Cancel`) | `ConfirmDialogView` (the buttons sound on their own) |
| chip install: a rising charge while it loads (`InstallCharge`, 1.9 s, cut with `AudioUseCase.StopSe` when the chip is released or the sequence is skipped), a sting on each colour promotion (`InstallPromote` for Rare, `InstallPromoteEpic` for Epic) | `ChipInstallView` |
| each command flipping (`CardFlip`, a noise-only shaka swish with no pitched blip), skipping the unpack sequence (`Click`) | `ChipInstallWindowView` |
| stage reward fanfare (`Reward`), defeat Backup Boost (`BackupBoost`), skipping it (`Click`) | `StageRewardView` |
| rank decode (`RankReveal`), lock-in (`Confirm`, or `Reward` for A and above), result rows sliding in (`TokenTick`) | `ResultView` |
| each step of a token count-up (`TokenTick`) | `TokenCounterView` (`_tickSe`) — the reward window and the map's token display |

## Known gaps

- Only interaction sounds exist. Combat results (hits, block, virus ticks), the pack unpacking sequence and the victory/defeat moments are silent.
- The BGM are short procedural loops meant as placeholders.

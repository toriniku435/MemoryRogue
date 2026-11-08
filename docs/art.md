# Art assets

Detail doc for the game's art (`ArtSource/` → `Assets/Art/`), imported from the root `CLAUDE.md`. Every scene is styled with it.

## Direction: cyber × pop × pixel

| Element | How it shows up |
| --- | --- |
| Base | Near-black navy |
| Colour | Flat neon — cyan, magenta, yellow, lime |
| Cyber | Grids, chamfered (cut-corner) panels, glitch bars, a synthwave horizon, circuit traces |
| Pop | Thick `INK` outlines, hard offset drop shadows (`hard_shadow`), halftone dots, sticker-like shapes |
| Pixel | Every image is rendered at low resolution and scaled up nearest-neighbour; the UI font is the pixel font DotGothic16 |

Every colour comes from `ArtSource/palette.py`; changing a value there and rebuilding recolours everything generated.

**Commands (cards in the code) are cartridges.** Each one has gold contact pins and a key notch along its bottom edge. The execution list is an *instruction memory* board — a PCB tray with pins, header `// COMMAND MEMORY`, and sockets addressed `0x00`–`0x02` — and putting a card in it is shown as writing that instruction into memory (see "Effects" below).

## Everything placed is a placeholder

The images are drawn in code (Python builds SVG, resvg rasterises it) and exist to be swapped out. The characters are 64×64 pixel art with frame animation (see "Characters" below): the player is a cyber girl with short silver hair and a headset, and the enemies are a Trojan horse on wheels (magenta), a hooded Rootkit growing roots (orange), a floating one-eyed Spyware drone (yellow) and a Botnet robot with satellite bots (violet).

## Backgrounds and screen shapes

Backgrounds are 2560×1440 so the game works at 16:9, 16:10 and 21:9 without stretching. The middle 1920×1080 is the reference composition; `background.py` lays out everything designed (sun, stickers, halftone) relative to that safe area (`OX`, `OY`) and continues the sky, skyline and floor grid to the image edges. The hazard bars are **not** in the background: they must hug the screen edge whatever the aspect, so they are a separate sliced sprite. The rules this follows are in `CLAUDE.md` ("Building uGUI").

## Regenerating

```powershell
pip install -r ArtSource/requirements.txt
python ArtSource/build.py                 # everything
python ArtSource/build.py cards ui        # only some groups: background ui cards characters effects
python ArtSource/preview.py UI out.png    # contact sheet of one Assets/Art/ subfolder, for eyeballing
```

`build.py` writes to **fixed file names** under `Assets/Art/`. Overwriting a file keeps its `.meta`, so the GUID (every reference to it) and its import settings survive a rebuild. There is one module per group under `ArtSource/assets/` and one function per image.

Everything is drawn at **2x the size it is displayed at**. Sliced images therefore use `Image.pixelsPerUnitMultiplier = 2`, and simple images get their size from the RectTransform.

**Pixel look.** Each group module has a `PIXEL` constant: the size, in output pixels, of one "dot". The values are background 4, UI 6, cards 6 and effects 8. Characters are drawn dot by dot instead (see "Characters" below) and have no `PIXEL`. `svgkit.render(..., pixel=N)` draws at 1/N resolution with `shape_rendering="crisp_edges"` and scales back up with `Image.NEAREST`. Set a group's `PIXEL` to 1 for smooth art.

A side effect of rendering at low resolution is that thin details vanish: 1-px lines, faint scanlines and fine grids either disappear or alias into bands. Draw details at least one dot thick, or they will not survive.

## Import settings (`Assets/Scripts/Editor/ArtImportPostprocessor.cs`)

Any texture under `Assets/Art/` is imported as a single, mip-less UI sprite.

- **9-slice borders:** kept in the table in that file, keyed by file name. They **have to match the corner sizes in `ArtSource/assets/ui.py`** — change one, change the other.
- **Tiled sprites:** names listed in `TiledSprites` (currently `ui_mem_pins`, the memory pins laid with `Image.Type.Tiled`) get `TextureWrapMode.Repeat`.

The postprocessor only acts on a **first** import, when there is no `.meta` yet. That is deliberate: an overwritten PNG, or a setting someone tweaked in the Inspector, is left alone. The flip side is that editing the tables does nothing for an image that is already imported. Fix it in the Sprite Editor, or call `ArtImportPostprocessor.Apply` on its importer from a `run_script`.

## Where each image plugs in

| What | File(s) | Slot it is assigned to |
| --- | --- | --- |
| Screen-edge hazard bars | `UI/ui_hazard_bar.png` (sliced: the ends keep their stripes, the middle stretches to the screen width; the bottom one is flipped) | `BackCanvas/TopBar` (both scenes), `BackCanvas/BottomBar` (TitleScene) |
| Battle background | `Backgrounds/bg_battle.png` (2560×1440, see below) | `BackCanvas/Background` in BattleScene |
| Title background | `Backgrounds/bg_title.png` (same world, higher horizon and a bigger sun behind the logo) | `BackCanvas/Background` in TitleScene |
| Card frame per rarity | `Cards/Frames/card_frame_{normal,rare,epic}.png` (cyan / magenta / holographic yellow) | `ScriptableObjects/Visuals/DefaultCardFrameSet.asset` (`CardFrameSet`), which `CardView.prefab` / `CommandCardView.prefab` reference |
| Card back (pile icons) | `Cards/Frames/card_back.png` | `DeckCountView` / `DiscardCountView` in BattleScene |
| Card illustration | `Cards/Illustrations/<CardData.Id>.png` | `CardData._illustration`, one per card. Enemy-only commands reuse a player command's drawing under their own file name (`ILLUSTRATIONS` in `cards.py`) |
| Enemy portrait (icon) | `Characters/<EnemyData.Id>.png` (the first idle frame) | `EnemyData._portrait`; also the target list's and the log's icon |
| Player portrait (icon) | `Characters/Player.png` (the first idle frame) | `PlayerUnitView/Portrait` in BattleScene and the player in MapScene |
| Character animation | `Characters/<Name>/<Name>_{idle,action,hit}_<n>.png` | `ScriptableObjects/UnitVisuals/<Name>.asset` (`SpriteSheetUnitVisualDefinition`), referenced by `EnemyData._visual` and `BattleSceneInitializer._playerVisual` |
| UI parts | `UI/ui_*.png` | directly on the Images in BattleScene and the prefabs |
| Instruction-memory tray | `UI/ui_tray.png`, `ui_slot.png`, `ui_mem_pins.png` | `Canvas/CommandTray` (+ `Slot1..3`, `Pins`, `Header`) |
| Write effect | `UI/ui_write_scan.png`, `ui_glow_frame.png` | `WriteScan` / `WriteFlash` in `CommandCardView.prefab` |
| Execute button | `UI/ui_button_primary.png`, `ui_button_outline.png`, `ui_button_mask.png`, `ui_ripple_ring.png` | `Canvas/ExecuteButton` (+ `Echo1..3`, `RippleMask`) |
| Target picker | `UI/ui_panel.png`, `ui_slot.png`, `ui_glow_frame.png` | `Canvas/TargetPicker`, `Prefabs/TargetOptionView.prefab` |
| Memory gauge | `UI/ui_mem_cell.png` | `Prefabs/MemoryCellView.prefab`, used by the `SegmentGaugeView` under `Canvas/PlayerStamina/Gauge` |
| Block barrier | `UI/ui_barrier.png`, `ui_barrier_crack_{1,2,3}.png`, `ui_block_badge.png` | `Barrier` in `UnitView.prefab` |
| Map background | `Backgrounds/bg_map.png` (low horizon) | `BackCanvas/Background` in MapScene |
| Map rooms | `UI/ui_map_room_frame.png` (white, tinted by state), `ui_map_room_fill.png` | `Prefabs/MapCellView.prefab`; traces are `Prefabs/MapLinkView.prefab` |
| Scene transition | `UI/ui_transition_slice.png` | `Resources/SceneTransitionEffect.prefab` |
| Deck edit / card shop backgrounds | `Backgrounds/bg_deck_edit.png`, `bg_shop.png` (very low horizon, so the panels sit on the sky) | unused since deck editing and the market became windows on the map |
| Chip (a card pack in the code) | `Cards/Packs/<CardPackData.Id>.png` (an IC chip: gold pins on both sides, a glowing die, coloured per chip, with an emblem on the die: a bolt for Starter / Premium, a blade for Attack, a plus for Repair, `</>` brackets for Hack) | `CardPackData._illustration`, one per pack; shown by `CardPackView` and `CardShopPurchaseView` |
| List rows | `UI/ui_row_fill.png` (dark, plain so it can stretch), `ui_row_frame.png` (white, tinted by selection) | `Prefabs/CardPackView.prefab` |
| Trash icon | `UI/ui_icon_trash.png` (white, tinted cyan by `Image.color`) | `DeckPanel/ClearButton` in `Prefabs/DeckEditWindow.prefab` |
| Tags and section lines | `UI/ui_tag.png` (count, price, tokens), `ui_section_line.png` (under a panel heading) | `DeckEditWindow.prefab`, `CardShopWindow.prefab`, `DeckEditCardView.prefab`, `CardPackView.prefab` |
| Primary button | the Execute button's sprites, as a prefab | `Prefabs/CyberButton.prefab` (`DeckEditWindow.prefab`, `CardShopWindow.prefab`, ConfirmDialog) |
| Pack install | `UI/ui_install_socket.png`, `ui_install_chip.png`, `ui_install_glow.png` (white, tinted by the rarity colour), plus `ui_ripple_ring`, `ui_bar_*` | `CardArea/Install` in `Prefabs/ChipInstallWindow.prefab` (`CardShopWindow.prefab` and MapScene); the card back and a `ui_glow_frame` rarity frame are in `Prefabs/ShopResultCardView.prefab` |
| Confirm window | `UI/ui_panel.png`, `ui_section_line.png`, two `CyberButton`s | `Prefabs/ConfirmDialog.prefab` (MapScene) |
| Token icon | `UI/ui_token.png` (a gold hexagonal data token, no text) | `Canvas/Tokens/Icon` in MapScene, `Window/Credit/Icon` in `CardShopWindow.prefab`, `TokenPanel/Icon` in `Prefabs/StageRewardWindow.prefab` |
| Reward rays | `UI/ui_reward_rays.png` (white; tinted by rarity / token gold / backup green) | `Window/Rays` in `Prefabs/StageRewardWindow.prefab` (MapScene); the card glow is `ui_glow_frame` |
| Toast | `UI/ui_toast_panel.png`, `ui_toast_icon.png` | `Resources/MessageToast.prefab` |
| Particle textures | `Effects/fx_*.png` + `Effects/Materials/*.mat` | `DamageBurst` / `HealBurst` in `UnitView.prefab`, and `BackCanvas/AmbientSparkles` / `AmbientShards` |
| Post-processing | `Assets/Settings/CyberVolumeProfile.asset` (bloom, chromatic aberration, vignette) | `PostProcessVolume` in BattleScene and TitleScene. It only affects the Main Camera, i.e. the background tier |
| Title logo pop shadow | `Assets/Fonts/DotGothic16/DotGothic16 PopShadow.mat` (the font material + a magenta underlay offset) | `Canvas/TitleText` in TitleScene |

`CardView`, `BattleUnitStatusView`, `BattleUnitEffectView` and `BattleCommandCardView` treat every one of these slots as optional. An unset slot only drops that piece of decoration.

## Fonts

Every text uses `Assets/Fonts/DotGothic16/DotGothic16-Regular SDF.asset`, which is also TMP's default font:

- **Font:** the Google Fonts pixel font DotGothic16, licensed under the SIL OFL (`OFL.txt` sits next to it).
- **Asset settings:** dynamic, multi-atlas, **no fallback**. A character it lacks renders as blank space; `FontCoverageTests` checks the localization tables for that (see `CLAUDE.md`).
- **Face Dilate 0.2 on its material.** DotGothic16 draws some glyphs with strokes one dot wide — `I`, `ー`, the bar of `6`. Scaled down, those strokes fall below a pixel and vanish with no error, which is how "6ダメージ" once rendered as "1ダメ ジ". The dilate thickens every stroke. If text in this font renders with pieces missing, suspect this first.
- **Card descriptions wrap** (`TextWrappingModes.Normal`) and auto-size between 14 and 22. The upper bound is what keeps a short description ("6ダメージを与える") on one line instead of stranding its last character.
- **No bold.** Faux bold smears a pixel font, so the texts have `Bold` removed.
- **Minimum size.** The pixel font needs roughly 16 screen px or more to read. That is why `CommandCardView.prefab` (drawn at 0.6x) **hides its description text** and shows only the name and illustration; the full description is on the hand card.

## Characters (`ArtSource/assets/characters.py`, `ArtSource/pixelkit.py`)

Characters are drawn **dot by dot** on a 64×64 canvas with `pixelkit.Sprite` (rectangles, ellipses and single pixels, no anti-aliasing), then scaled up 8× nearest-neighbour to 512×512. Unlike the SVG groups, nothing is rasterised from shapes, so a pose changes by exactly the dots that were meant to move — which is what frame animation needs.

- **Layers.** `Sprite` is the body; `outline(INK)` puts a one-dot `INK` rim around it. `Sprite.fx` (beams, rings, sparks) is drawn after the outline and gets none; `Sprite.under` holds the hologram pad under the feet.
- **Poses are parameters.** Each character is one function (`girl(eyes, arm, bob, lean, hair, mouth)`, `trojan(...)`, …) and every frame is a call with different arguments. The hit frames share `glitch_hit`: the first leans back, shifts two bands of rows sideways and bleaches toward white; the second is half-way back.
- **Everyone faces right.** Enemies are mirrored on screen by the `Portrait`'s `-1` x-scale.
- **Output.** Per character: `Characters/<Name>/<Name>_idle_<n>.png` (loop), `_action_<n>` and `_hit_<n>` (played once), plus `Characters/<Name>.png`, a copy of the first idle frame used as the icon. Currently idle 8 frames for the player and 4 for enemies, action 4, hit 2.
- **After changing frame counts**, run **Tools > Art > Rebuild Unit Visuals** (`Editor/UnitVisualDefinitionBuilder.cs`). It re-lists the frames of every `Characters/<Name>/` folder into `ScriptableObjects/UnitVisuals/<Name>.asset`, and assigns the asset to the `EnemyData` whose `Id` is `<Name>` if that enemy has none yet.

**Swapping the look.** The battle only talks to `IUnitVisual` (`Common/Views/UnitVisuals/`: `PlayAction`, `PlayHit`, `SetDefeated`, `SetColor`), created by a `UnitVisualDefinition` asset (`Common/ScriptableObjects/UnitVisuals/`). Frame animation is `SpriteSheetUnitVisualDefinition` → `SpriteSheetUnitVisual`, whose timing is the plain `FrameAnimationPlayer` (EditMode-tested): idle loops, action and hit play once and return to idle, a hit overrides an action, and a defeated unit holds the last hit frame. Real sprite sheets drop into the same asset. A Live2D (or any other) look is a new `UnitVisualDefinition` subclass whose `Create` builds its own object and returns an `IUnitVisual`; point `EnemyData._visual` / `_playerVisual` at it and nothing else changes. The visual is created inside the `Portrait` rect, so it inherits the mirroring and the hit shake; colours (hit blink, defeated grey) reach it through `SetColor`.

## Effects

**HP changes (`BattleScene/Views/BattleUnitEffectView.cs`).** `UnitPresenter` passes each HP delta to the unit's view, which plays four effects: a shake, a flash, a floating number (`FloatingNumberView.prefab`) and a particle burst. All of them live in `UnitView.prefab`. The damage flash is an **alpha blink, not a colour tint**, because `Image.color` is multiplied into the sprite and a tint muddies whatever colour the art is.

**Writing an instruction (`BattleScene/Views/BattleCommandWriteEffectView.cs`, on `CommandCardView.prefab`).**

- **A card entering the list** (its pooled view becoming active, i.e. `OnEnable`) is "pressed in": it scales down from 1.3x while fading in, then a scan bar runs top to bottom, then the frame flashes cyan.
- **A card that moves to another slot** (a reorder, or the list closing up after an execution or a return) plays only the scan and flash — a "rewrite". `BattleCommandListPresenter` triggers this through `MoveToSlot`, because only it knows a command changed address.

The effect touches scale, a `CanvasGroup` and its own children, **never `anchoredPosition`**, because the presenter owns the card's position.

**Card motion (`BattleScene/Views/BattleCardMotionView.cs`, on both `CardView.prefab` and `CommandCardView.prefab`).** The presenters give a card targets; this component makes it travel there. Everything is "digital": positions snap to an `_step`-pixel grid while moving, and enters and exits pass through a CRT-style horizontal line with flicker and sideways jitter.

| Motion | What it looks like |
| --- | --- |
| Move / reorder | stepped slide to the new position |
| Select | the card lifts, a cyan `SelectGlow` frame pulses, a short jitter on the change |
| Enter (drawn / returned) | flies in from the deck or the command list collapsed to a line, flickering, then opens in place |
| Exit — `Upload` | collapses and zips to the command list |
| Exit — `Discard` | collapses and zips to the discard pile |
| Exit — `Return` | collapses and drops toward the hand |
| Exit — `Execute` | the `ExecFlash` board flashes white and the card swells, then it collapses and rises away |

Cards in flight are not clickable (`CanvasGroup.blocksRaycasts`), and a view that has finished exiting is handed back to the presenter's pool by the callback.

**Windows (`Common/Views/UI/WindowTransition.cs`).** The one open/close motion every window uses (rules in @docs/tone-and-manner.md). It needs the window's `RectTransform` and `CanvasGroup`; the scrim, the noise bars (plain `Image` strips under the window; only used when `_glitchDuration` is above 0, which it no longer is by default), the `CollapseFlash` (a light-cyan full-window `Image` drawn last, which is what makes the collapsed frame read as a glowing line) and the electron burst are optional. The burst is a copy of the map's player burst with a `SingleSidedEdge` shape as wide as the window, sorting layer `UI_Popup_Effect`, and it must live **outside** the window's GameObject.

**Buttons (`Common/Views/UI/ButtonRippleEffect.cs`).** The Execute button is a translucent cyan parallelogram (`ui_button_primary`). Add the component to any `CommonButton` for the same feedback:

- **Hover:** outline copies of the button shape (`ui_button_outline`) repeatedly scale outward and fade.
- **Press:** a ring (`ui_ripple_ring`) spreads from the pressed point, clipped to the button's shape by a `Mask` with `ui_button_mask`; the face flashes; and a stronger outline pulse goes out.
- **Not interactable:** the whole button is dimmed through a `CanvasGroup`, which is why the Button's own disabled tint is white. Leaving the tint on as well would dim it twice.

It runs on unscaled time (capped per frame) so UI feedback is not slowed by `timeScale`.

The slanted sprites are sliced with left/right borders that contain the slant, and are drawn exactly as tall as the button is displayed (216 px = 108 display px). **Do not stretch them vertically**: a sliced diagonal gets a kink wherever the middle cells are stretched. For the same reason the hover pulse scales the outline uniformly (`localScale`) instead of growing its size.

**Particles.** Particles under a Screen Space - Camera canvas are ordinary renderers, so the canvas rules in the root doc apply:

- The GameObject layer (`UI_Default`, `UI_Back`) picks the camera.
- The sorting layer (`UI_Default_Effect`, `UI_Back_Effect`) puts the particles above that tier's UI.
- All systems use `ParticleSystemScalingMode.Hierarchy`, so sizes and speeds are in canvas pixels.

## Known gaps

- The buttons in BattleScene, TitleScene and MapScene are copies made before `CyberButton.prefab` existed, so a change to the prefab does not reach them.
- The player has no data asset: its look is `BattleSceneInitializer._playerVisual` and the scene's `Portrait` sprite.
- The map shows the player as a still image (the first idle frame); it does not animate there.

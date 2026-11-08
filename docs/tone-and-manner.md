# Tone & manner

The look, feel and wording of the game, imported from the root `CLAUDE.md`. **Every screen, image, animation and label follows this doc.** How images are produced and where each one plugs in is in @docs/art.md. This file is *what they should be*, not how they are built.

**This doc is maintained from the user's feedback.** Whenever the user comments on look, motion, interaction or wording, update this doc **in the same change** as the code:

- Add or adjust the rule.
- If the feedback overturns an earlier choice, move the old choice to "Tried and dropped" with the reason and the date.

Date format: `YYYY-MM-DD`. Last updated 2026-11-07.

## Concept

**Cyber × pop × pixel.**

- **World:** a night-time neon city on a synthwave horizon, with a perspective grid floor.
- **Metaphor:** the player is programming.
  - What the player plays are **commands**, never "cards" (コマンド / Command). Each is drawn as a cartridge with gold contact pins and a key notch on the bottom edge.
  - The execution list is **instruction memory** (a PCB board with sockets addressed `0x00`, `0x01`, …).
  - Putting a card there is **writing an instruction into memory**.
  - Deck editing is a **program editor**: the owned commands are a `// LIBRARY`, the deck is the `// COMMAND DECK` being written, and the deck count is a row of memory chips that fills toward the limit.
  - The shop is a **data market** that sells **chips** (on screen: **マーケット / Market**), never "packs": a chip holds several commands, and buying one makes them available to build into the deck. Chips are drawn as IC chips (gold pins on both sides, a glowing die); the currency is **tokens** (`// TOKENS`, a gold hexagonal data token, prices in `TKN`).
  - A campaign starts with a **First Build**: ten chips are installed on the map and the program writes its own deck from them (`// DOWNLOAD COMPLETE`, ファーストビルド).
  - Stage results are **access**: a win is `// ACCESS GRANTED` and its loot is *decrypted*; a loss is `// CONNECTION LOST` followed by a **Backup Boost**, support tokens transferred to you.
  - The campaign map is **cyberspace**: stages are **rooms (layers) with addresses**, joined by data-bus traces, going deeper to the right. Advancing turns the player into **electrons**, a packet flies along the trace, and they re-assemble in the next room.

## Colour

Dark base, flat neon accents. All values live in `ArtSource/palette.py`.

| Token | Use |
| --- | --- |
| `BG_DEEP` / `BG_NAVY` / `BG_PURPLE` / `PANEL` | backgrounds and panels (translucent dark) |
| `CYAN` | primary: borders, selection, player-side, "you can press this" |
| `MAGENTA` | accent and **warnings**: damage numbers, cost you cannot pay, pop drop shadows |
| `YELLOW` | **focus**: the unit or option currently pointed at; hazard stripes |
| `LIME` | healing, HP |
| `INK` | outlines (near-black navy, never pure black) |
| `TEXT` `#E8FBFF` | body text on dark backgrounds |

- **Text is light on dark.** Dark `INK` text only goes on a neon fill (the card name band).
- **Never put text over a shifting colour.** Numbers sit next to a bar, not on it.

## Shape

- **Corners:** panels, cards and frames have **chamfered (cut) corners**, not rounded ones.
- **Primary buttons:** a **slanted parallelogram** — translucent cyan face, neon edge, white corner brackets. Their label is *italic* to match the slant. Stack several of them diagonally, offset to follow the slant (the title menu).
- **Gems and badges** (cost, memory, block value) are **hexagons**.
- **Pop details:** thick `INK` outlines, hard offset drop shadows (no blur), halftone dots, small sticker-like shapes (triangles, zigzags, rings).
- **Cyber details:** grids, circuit traces, `//`-prefixed headers (`// COMMAND MEMORY`, `// SELECT TARGET`), hazard stripes, glitch bars.

## Pixel

- **Everything is rendered as pixel art**: low resolution, no anti-aliasing, scaled up nearest-neighbour. Details must be at least one dot thick, or they vanish or band.
- **Font:** DotGothic16 for everything, with no fallback font.
  - No bold — faux bold smears it.
  - The material has Face Dilate 0.2, so one-dot strokes survive scaling.
  - **Minimum readable size is about 16 screen px.** Below that, hide the text rather than show something unreadable — the command cards drawn at 0.6× show only name and illustration.
- **Characters** are 64×64 pixel art, animated frame by frame: an idle loop, an action when they use a command, a hit when they take damage (a lean back with a glitch and a white flash). The player is a **cyber girl with short hair** (silver hair with cyan tips, a headset, a white oversized jacket); the enemies are the bad things of the metaphor drawn as creatures and machines. Everyone is drawn facing right; enemies are mirrored.

## Motion

Motion is **digital and snappy**, and never gets in the way of play.

- **Movement** snaps to a pixel grid while it travels (stepped), with ease-out.
- **Appearing and disappearing:**
  - things open from, and collapse into, a **CRT-style horizontal line**;
  - cards leave as a line that zips to where they went (memory, archive, hand); an executed card flashes, swells, then rises away.
- **Glitch** (sideways jitter, cyan/magenta ghost copies, alpha flicker, noise bars) is short, and on idle elements occasional (the title logo, every few seconds).
- **A status effect announces itself.** When an effect is applied, its name and value float up as text only (yellow); when an immunity refuses it, 「ウイルス 無効」 floats in grey.
- **A number says what caused it.** Every damage or heal number carries a small tag underneath with its cause — the command (アタック, システムエラー) or the lasting effect (ウイルス, リジェネ); a reversed heal names the reversal too. This is separate from the log and sits right at the number.
- **Hits:**
  - damage blinks the target's alpha (never a colour tint, which muddies the art) and sprays shards;
  - the block barrier cracks in stages and shatters.
- **Switching content in place does not replay an entrance.** When a panel is already open, swap its content instantly. Hide it only after a short grace (~0.12 s), so moving from one card to the next doesn't blink the panel shut.
- **Throwing away changes asks first.** An options window's Cancel (and Unit Upgrade's, where an Upgrade press stays pending until Apply, and Command Build's) closes at once when nothing changed; after a change it asks 変更を破棄しますか？ (破棄する / 戻る) through the same confirm window.
- **Overwriting or committing an edit asks first.** Command Build's Auto Build always asks before replacing the selected deck (naming it), and its Save asks before committing changes; with nothing changed, Save simply closes.
- **The tutorial points, it does not lecture.** One short bubble at a time, next to the thing it talks about; the rest of the screen is dimmed and the thing itself is lit with a pulsing yellow frame. Steps that ask for an action let only that element be used. It can always be skipped (top-right) and replayed from the map's チュートリアル button; both ask first. Skipping the practice battle ends it and returns to the map.
- **Editing and shopping stay on the map.** Command Build and the Market open as windows over the map, like Upgrade, not as separate screens. A click outside does nothing: Command Build closes only with Cancel or Save, so a stray click never throws an edit away, and the Market with its Close button. A purchase counts at once — the map's tokens count down behind the window.
- **Commands can be dragged between the lists.** In Command Build's All layout, a command dragged sideways from the library onto the deck panel (or back) moves one copy per drop; the panel it can land on glows cyan, brighter under the pointer. A drag that starts vertically scrolls the list instead. A refused drop (the deck limit) answers with the error sound and a toast.
- **Settings are heard and seen before they are kept.** In Options, volumes play at the new level while the slider moves (the SE slider clicks), but nothing is kept until Apply; Cancel after a change asks first. Deleting user data always asks, says what is lost, and is magenta.
- **A tutorial is offered once.** After it has been watched, a new story starts without asking again; the map's チュートリアル button still replays it.
- **Giving up asks first.** The battle's Menu holds Options and Give Up (magenta); Give Up opens the confirm window (あきらめますか？, あきらめる / 戻る) and only OK ends the battle as a defeat.
- **Suspending asks first.** The map's Menu (top-right) holds Suspend and Options; Suspend asks 中断してタイトルに戻りますか？ (中断する / 戻る) and says the progress is kept, then returns to the title. Options is the same window as in battle and on the title.
- **The story is a transmission.** Conversations come in a text box at the bottom of the screen (`// TRANSMISSION`, speaker in yellow), over the blurred map, typed out a character at a time. One click finishes the line, the next one moves on; nothing advances by itself. They come at the start of an episode (before the tutorial question) and after each won stage (before the reward).
- **Leaving for a battle asks first.** A two-choice confirm window (`// CONFIRM`, Cancel left, OK right as the primary with `>>>`). Clicking anywhere outside the window is the same as Cancel, so backing out never needs aiming. The exception is a question with no "backing out": the tutorial question at the start of a story (見る / 見ない) ignores clicks outside, so a stray tap cannot answer it.
- **Expectation is shown as light, and it only goes up.** Rarity has one colour everywhere — Normal cyan, Rare magenta, Epic gold (with a rainbow drift). When something is being revealed (a pack install), the light starts at the lowest colour and is promoted step by step to the real result, each step louder (ring, flash, then shake and particles); the very best gets a brief hold before the payoff. Reveal the best thing last. Never fake a promotion that the result does not back up.
- **Long reveals can always be skipped** with a click anywhere.
- **A verdict is decoded, not dropped.** The result rank appears the digital way: it opens from a line, scrambles and locks left to right, with cyan/magenta ghosts and a scan bar. No screen shake, only a subtle flash. The better the rank, the longer the pause and decode and the more on lock-in (rings, rotating rays, rainbow for SS+); the worst ranks just settle quietly. The score counts up first, and skipping never skips the reveal itself.
- **Rewards feel like rewards; a loss feels like help.** The reward window opens the card from a line with a white flash, its rarity glow and slowly rotating rays behind it, then counts the tokens up with ticks and a pulse of the token icon, over a short fanfare. A loss never scolds: cyan title, green rays, a rising "power back on" sound, and wording that points to the shop and a retry. The button (Claim / Reboot) appears only once the count-up is done; the count continues on the map's token display after closing.
- **Every window opens and closes the same way** — use `Common/Views/UI/WindowTransition`, never a window-specific animation:
  - **open:** the frame opens from a horizontal line with a little flicker, while the blur and dim behind it fade in;
  - **close:** immediately, with no lead-in — the frame collapses into a glowing cyan line, the line bursts into electrons and shrinks away, while the blur and dim fade out (about a quarter of a second). Nothing just blinks off.
  - **Only the front window is sharp.** A window opened over another sends the one behind under the blur with the screen; the one behind blurs in over the open motion, and blurs back out over the close motion, so nothing jumps. The blur on the screen itself does not change when windows stack.
  - What is *inside* a window may add its own motion on top (the result cards arrive one by one and collapse in reverse), but the frame, blur and timing are always the shared ones.
- **Scene changes never show a freeze.** Glitch slices cover the screen, the next scene loads and initialises underneath, then the slices glitch away. If the wait gets long, show `// LOADING` with progress rather than a frozen frame. Input is blocked during the switch. Anything that animates in on a new screen starts only after the cover has lifted.
- **The route turns the page.** On the map, ten rooms are one page. Crossing into the next ten (in a challenge), the whole route slides one page to the left, grid-stepped, while the player flies as a packet, so the current page is always centred and the next page peeks in at the edge.
- **UI animation runs on unscaled time**, capped per frame, so a hitch or slow motion cannot skip it.

## Interaction

The player's hand and eyes should never have to travel far or wait.

- **Click plays a card.** A click always queues the card: on its only possible target, or on the **priority target** when there are several enemies. A card lifts the instant the pointer reaches it, so sweeping across the buffer never lags. Resting on a card that could aim at another enemy (~0.1 s) opens the target panel above it, for a one-off different target; clicking the card with the panel open still goes to the priority target.
  - **The priority target is chosen on the battlefield.** Clicking an enemy makes it the priority target: a yellow cursor with `TARGET` points down at it between its name and its head, and its name turns yellow. It starts on the front enemy, moves to the front-most living one when its enemy falls, and is not shown while only one enemy is left.
  - **On touch there is no hover, so a card takes two taps.** The first lifts it and shows its details (and the target panel when it could aim at another enemy); tapping it again plays it. Tapping empty space puts it back.
  - **On touch, a tap opens a description and it stays** until another command or status effect is tapped — playing the command or tapping empty space does not close it. Pressing Execute does, so the results are not hidden behind it. Only one is open at a time. A queued command works the same way: the first tap shows it and its move arrows, the second returns it to the buffer.
  - **The target panel opens on hover**, right above the card, only when there are several enemies to choose from.
  - **Leaving releases it:** once the pointer is off both the card and the panel (after a short grace to cross the gap), the selection drops. A click on empty space also cancels.
- **A cost that needs choosing is chosen in place.** Garbage Collection asks which buffer commands to purge right after its target: the chosen ones get a magenta `PURGE` mark, a small `// SELECT PURGE` panel above the card counts them and confirms, and a click on empty space backs out. While choosing, the Execute button turns into 決定 and confirms the same way, so the hand can stay where it always ends a turn.
- **The enemies' next moves are shown in advance.** Under each enemy's HP bar, its planned actions for the next enemy turn are listed as instruction-memory rows (`0x00 [icon] name`); pointing at one shows the command's details. Actions a stun will skip are darkened. The two lists under an enemy are labelled (`// 次の行動`, `// 状態効果`) so the plan is never mistaken for an effect already on it; status effects show only their name, and the description opens in a tooltip above the row while pointed at. Chance-based interruptions (a glitch) are not shown — they are the surprise.
- **The next opponents are known before connecting.** The map's **敵詳細 / Enemy Info** button opens a window (`// TARGET SCAN`): the next layer's enemies on the left, each with its idle animation; click one to select it (a yellow cursor points at it and its frame glows) and its details show on the right — HP, actions per turn, immunities, a short trait line and every command it uses with that command's effect text. Every text is shown in full, never cut.
- **The log names the cause.** Damage or healing from a lasting effect says which effect: 「ウイルスで4ダメージ」, never just 「継続効果で」.
- **Targets are picked next to the card**: a small target list opens right above the selected card. The unit on the battlefield still works too.
- **Pointing highlights:**
  - pointing at a target option, or at a queued card, highlights the unit it affects in yellow;
  - pointing at any card shows its details large in the free space at the top centre.
- **A limit is marked on the card.** Outside battle, a command that can be in the deck only once carries a yellow round badge with the number (1) at its top-right corner (deck edit, the market, reward and result windows); when the card is shown enlarged, a yellow plate on the picture also says 「デッキに1個まで」. In battle the mark is not shown: it is deck-building information.
- **Unusable is visible before it is tried.**
  - Unplayable cards are greyed out, with the cost in magenta when memory is the reason.
  - Pointing at one lifts it but opens nothing.
  - Clicking it explains why, in a toast.
  - Outside battle the same rule shows as colour: the deck count stays magenta unless the deck is exactly the required size — short or over (Save refuses until then; going over while editing is allowed), and a pack price turns magenta when the tokens cannot pay for even one.
- **Nothing on screen looks broken.** An empty buffer is never just an empty space: the hand's place shows `// BUFFER EMPTY` with the reason (Buffer Lock, or nothing to load) and what to do (press Execute to end the turn).
- **A list that scrolls says so.** Every scrolling list of cards or lines (Command Build, the command and enemy collections, the battle log) has a thin cyan scrollbar on its right, hidden when everything fits.
- **What happened is always readable.** While actions resolve, a log opens at the top centre (`// EXECUTION LOG`) and closes when the player's turn is back; the **Log** button opens the whole battle's log (`// BATTLE LOG`) any time. The player's side is cyan, the enemies' magenta, turn headers yellow, and each line carries the portrait of the unit it is about. A status effect is explained with its numbers on the line beneath it (「2ターンの間、回復がダメージになる」). Actions are spaced apart so they can be followed; the pace scales with the battle-speed setting.
- **Every pile you can see into, you can open.** Clicking ARCHIVE or PURGED opens the commands in it (the same list and enlarged card as the command collection), without pausing the battle. The command deck stays closed, so the draw order is never given away.
- **Feedback never blocks:**
  - toasts slide in at the top and leave on their own, and never take input;
  - nothing blurs or dims the screen for a notification;
  - no toast on hover.
- **Results the player should look at open a window**, not a toast: the purchase result shows the cards themselves. A window like that is modal — the screen behind it blurs (and dims slightly), and a button closes it. Keep this for results worth reading; notifications stay toasts.
- **A control's size matches the weight of what it does.** Secondary controls (the battle's Log button) are drawn smaller than the primary one beside them.
- **Primary action** (Execute) is the biggest, most distinct control, bottom-right where card games put "end turn". It ripples on hover and press. On the map the primary action is **接続開始 / Connect** (starting a stage), drawn larger than the buttons beside it (1.2x).

## Sound

Halfway between chiptune and synthwave: square and triangle leads, arpeggios and noise drums (chip), over detuned-saw pads, low-pass and echo (synth). Details and where each sound is played: @docs/sound.md.

- **Every interaction answers, briefly.** Hover is a tiny tick, click a short two-note blip. Most effects are under 0.2 s so rapid play never piles them up, and the same effect is not retriggered within 0.04 s.
- **Direction carries meaning.** Confirm rises (a short arpeggio), cancel falls, an error is a low, buzzy, bit-crushed double pulse. A disabled button still answers — with the error.
- **A reveal builds up.** A chip install charges with a low hum that rises (「ぐぐーっ」) while it loads, each colour promotion rings a brighter sting (Epic bigger, with sparkles), and every command flips with a short shaka swish of noise only — no pitched blip, which turned thirty flips into a stream of beeps.
- **Rewards sound like rewards.** A win is a short rising square-lead fanfare with sparkles; a loss is a soft rising swell and chime (encouraging, never a warning); token count-ups tick with a tiny high blip; a result rank decodes with a glitchy burst of noise fragments ending in a latch click, never a boom.
- **Sounds follow the metaphor.** Queuing a card is a stream of data blips ending in a latch click (writing memory); taking it back slides down; executing is a falling zap. An enemy acting is a gritty descending buzz, a hit a short thump, a heal a soft rising chime, and a virus or worm tick a bubbling, bit-crushed gurgle — every action and every lasting effect is heard, not only the player's own. Windows open with a rising whoosh and chime and close as a collapse that crackles into electrons. Scene changes glitch.
- **Every screen has its own loop**, crossfaded on scene changes (the same track carries on uninterrupted). Title dreamy, map exploring, battle driving and uplifting (a trance-leaning build-up into a drop every loop, in a **major key** — a minor battle theme felt too dark — with drums kept to an accompaniment, never showy), deck edit calm, shop bouncy.

## Words

Names follow the computer metaphor. UI labels go through the localization tables. Decorative English headers (`// …`) and hex addresses are part of the art and are not localized.

| Game concept | Shown as |
| --- | --- |
| campaign (the main mode) | **ストーリー / Story** (title button, resume dialog, chip text); the code and docs still say `Campaign` |
| story chapters | **エピソード1 / Episode 1** … (`// SELECT EPISODE`, エピソード選択); each opens by clearing the one before it. A locked one is shown disabled and, when pressed, says what unlocks it (エピソード1をクリアすると選べるようになります) |
| stamina | **メモリー残量** (memory remaining), shown as a gauge of memory-chip cells |
| card | **コマンド / Command** (counted with 個); the code still says `Card` |
| hand | **バッファ / Buffer** — the commands loaded for this turn (「バッファ+1」, 「次のターンのバッファに戻る」); the code still says `Hand` |
| card pack | **チップ / Chip** (スターター, アタック, リペア, ハック, プレミアム); the code still says `CardPack` |
| buying one command | **シングル / Singles** (the market's second tab); 残り n/3, 売り切れ / Sold out |
| card shop | **マーケット / Market** (the map button and the shop's title) |
| starting a stage | **接続開始 / Connect**; confirm 接続を開始しますか？ / Start the connection? |
| deck edit (button and title) | **コマンドビルド / Command Build**; its auto-build button おまかせ / Auto Build; decks 01–10 as tabs, the one used in battle アクティブ / Active, button アクティブにする / Set Active; layout tabs すべて / デッキ / 所持 (All / Deck / Library) |
| deck | **コマンドデッキ / COMMAND DECK** |
| discard pile | **アーカイブ / ARCHIVE** |
| poison | **ウイルス / VIRUS** (the id stays `Poison`) |
| enemies | named after bad things in a computer: トロイの木馬 / Trojan, ルートキット / Rootkit, スパイウェア / Spyware, ボットネット / Botnet; the Episode 1 boss ランサムウェア / Ransomware |
| removed for the rest of the battle | **パージ / Purged** (the pile beside the archive) |
| healing past max HP | **オーバーチャージ / Overcharge** (a status effect; it adds attack damage and is lost before HP) |
| card names | computer terms: ガベージコレクション, ポートスキャン, 権限昇格, ターミネート, コードインジェクション, オーバークロック, エクスプロイト, ファイアウォール, ブロードキャスト, ループバック, マルチスレッド, マイニング, カーネルパニック, バイパス, バッファオーバーフロー, ワーム, ゼロデイ, リドゥ, フラッシュ, スワップアウト, デフラグ, クラウドバースト, パイプライン, ブルートフォース, リフレクション, フォーマット, フォレンジック, オーバーライト, … |
| execution list | **COMMAND MEMORY**, slots `0x00`–`0x02` |
| command details panel | `// COMMAND DATA` |
| target list | `// SELECT TARGET` |
| campaign map | `// NETWORK DEPTH`, `LAYER 03 / 10`; rooms numbered `01`… with an address `0x0100`… |
| scene switch wait | `// LOADING` |
| deck edit | `// PROGRAM EDITOR`; panels `// LIBRARY`, `// COMMAND DECK`, `// COMMAND DATA` |
| chip shop | `// DATA MARKET`; panels `// CHIPS`, `// CHECKOUT`, `// TOKENS` |
| currency | **トークン / Tokens**, prices `100 TKN` |
| stage win | `// ACCESS GRANTED :: LOOT DECRYPTED`; クリア報酬を獲得！; button 受け取る / Claim |
| campaign clear | `Ending` (placeholder), `// TAP TO CONTINUE`; result `// RUN COMPLETE`, `// SCORE BREAKDOWN`, リザルト / Result, ランク E〜SSS, タイトルへ / Back to Title |
| challenge | **チャレンジ / Challenge** (`// SELECT CHALLENGE`, チャレンジ選択): ノーマル / Normal 30, ハード / Hard 60, マスター / Master 100 layers (「ノーマル  30階層」), エンドレス / Endless; `LAYER 12 / 30`, Endless `LAYER 37` (no total); a cleared challenge says 踏破 / Cleared, and its button in the selection carries a lime `CLEAR` tag, Endless a cyan `LAYER 37` tag with its best layer (a cleared Story episode shows the same tag plus its best rank in the rank colour); enemies past layer 10 carry 階層ブースト / Layer Boost; result 「ノーマル リザルト」 / "Normal Result" with 突破した階層 / Layers Cleared, 最高記録 / Best Record, 新記録！ / NEW RECORD!, 到達階層 / Layer Reached |
| unit upgrade | ユニット強化 / Upgrade; `// UNIT TUNING`; rows HP and メモリー上限 / Max Memory, `140 → 160`, `60 TKN`, `MAX`; buttons キャンセル / Cancel, 反映 / Apply |
| battle log | ログ / Log; `// EXECUTION LOG` (live), `// BATTLE LOG` (window); あなた / You; an enemy on screen twice gets A / B |
| archive / purge list | `// ARCHIVE` アーカイブ / `// PURGED` パージ; コマンド, 「5個」 |
| campaign start | **ファーストビルド / First Build** (`ファーストビルド  チップ10個`); button デッキにセット / Set to Deck |
| battle menu | メニュー / Menu (`// MENU`); あきらめる / Give Up; defeat toast 敗北... 接続を切断した |
| map menu | メニュー / Menu (`// MENU`, top-right); 中断 / Suspend, オプション / Options; confirm 中断してタイトルに戻りますか？ (中断する / 戻る) |
| collection | コレクション / Collection (`// DATABASE`); コマンド / Commands, エネミー / Enemies; コマンドコレクション / Command Collection (`// COMMAND DATABASE`, `// COMMAND INDEX`), tabs すべて / All and each chip, 獲得 26 / 32 / Found 26 / 32, an unowned command is a face-down ? card; エネミーコレクション / Enemy Collection (`// ENEMY DATABASE`), an enemy never fought is a black silhouette with ? and the name ？？？ |
| options (title and battle) | オプション / Options; `// SYSTEM CONFIG`; BGM, 効果音 / Sound Effects, 戦闘スピード, 言語 / Language (each language by its own name: 日本語, English), 画面モード / Display: フルスクリーン / ウインドウ; ユーザーデータ / User Data: 削除する / Delete, confirm ユーザーデータを削除しますか？ (title only); buttons デフォルトに戻す / Reset to Default, キャンセル, 反映 / Apply |
| story conversation | `// TRANSMISSION`; クリックで次へ |
| stage loss | `// CONNECTION LOST :: BACKUP BOOST`; バックアップブースト; button 再起動 / Reboot |
| nothing selected yet | `// SELECT A COMMAND`, `// SELECT A CHIP` |
| confirmation | `// CONFIRM`; the target in the detail line (`LAYER 03 / 10`) |
| pack install | `// INSERTING`, `// INSTALLING`, `// OVERFLOW` (the 99 % hold), `// COMPLETE` |
| purchase result | `// DOWNLOAD COMPLETE`; details `// COMMAND DATA`, `// HOVER A COMMAND` |

Two different things are both called "memory": the stamina gauge and the instruction memory board. This is accepted for now; rename one if it causes confusion.

## Layout

- **Hierarchy:** battlefield on top, execution list in the middle, hand at the bottom.
- **Proximity:** the resource sits next to what spends it (memory gauge beside the hand; the map's tokens above the two controls that spend them, Market and Upgrade), and the primary action next to where the turn ends.
- **Consistency:** follow card-game conventions (deck bottom-left, discard bottom-right, cost on the card's top-left).
- **Window buttons sit in one centred row along the bottom**, in every window, in the order they are read (the primary last, on the right). Do not split them between the corners.
- **A control sits next to the state it changes.** Command Build's Set Active is shown in place of the アクティブ tag beside the deck header (tag when active, button when not), not in the bottom row.
- **Draw order:** never let a lifted or selected card slip under another panel. When the hand overlaps, cards stack left to right (each card over the one to its left) and the selected card is always in front.
- **Every screen shape works.** 1920×1080 (16:9) is the base, and 16:10 and 21:9 must look intended, not stretched or cropped. Extra screen space shows more of the world (background) and pushes HUD toward the edges; the battlefield and the hand stay centred. On phones, HUD stays inside the safe area, so a notch or rounded corner never covers a button. How that is built is in `CLAUDE.md` ("Building uGUI").

## Tried and dropped

| Date | What | Why it was dropped |
| --- | --- | --- |
| 2026-09-26 | Pastel yume-kawaii / jirai-kei direction (pink × black, hearts, ribbons, chibi characters) | Replaced by cyber × pop × pixel |
| 2026-09-26 | Illustrated placeholder characters | Flat squares are enough until real art |
| 2026-09-29 | Flat single-colour squares for characters | Replaced by animated 64×64 pixel art (idle, action, hit); the player became a short-haired cyber girl |
| 2026-09-26 | Rounded pastel font / smooth text | DotGothic16 pixel font |
| 2026-09-26 | Toast in the screen centre with a background blur | Felt like waiting; blocked the view. Now top-of-screen, non-blocking, no blur |
| 2026-09-26 | Yellow solid Execute button with drop shadow | Translucent slanted cyber button with ripples |
| 2026-09-26 | Target selection only by clicking the unit | Too far from the hand; added the list above the card |
| 2026-09-26 | Click the selected card to cancel | Conflicts with hover-to-select; cancel is a click on empty space |
| 2026-09-26 | Silently ignoring unplayable cards on hover | Grey them out instead, so the reason is visible |
| 2026-09-26 | Reopening the details panel on every switch | Looked jittery; the frame now stays put |
| 2026-09-26 | "Screen refresh" scan-line wipe when the details switch (also tried on same-card switches) | Not wanted; content swaps instantly |
| 2026-09-26 | A glitch (jitter, noise bars) before a window collapses | Made closing feel slow; the window now collapses at once. `WindowTransition._glitchDuration` can bring it back |
| 2026-09-26 | Closing a window instantly, with the blur switching off at once | Looked like a glitch in the bad sense; windows now glitch out into electrons and the blur fades |
| 2026-09-26 | Purchase result as a toast ("n枚手に入れました") | The player wants to see which cards came out; a result window with the cards replaced it |
| 2026-09-26 | Describing the command card's text at 0.6× | Unreadable at that size in any font; hidden |
| 2026-09-27 | Calling the currency "points" (`// CREDIT`) | "Tokens" fit the computer metaphor better |
| 2026-09-27 | The map's token display in the header's top-right | Its role was unclear; moved next to the Chip Shop button where it is spent |
| 2026-09-27 | The result rank slamming down as a huge letter with a screen shake ("どかっ") | Too heavy for the world; the rank now decodes in quickly and smartly ("ざざっ") |
| 2026-09-27 | The battle theme in a minor key (E minor) | Too dark; now D major with a I–V–vi–IV progression |
| 2026-09-27 | Showy battle drums: a crash every 4 bars and at the loop start, a snare roll up to 32nds, 16th hi-hats, loud claps | Overblown; the drums are now a restrained accompaniment |
| 2026-09-27 | A fixed starting collection and deck | Every run now starts from a drawn First Build, so strategy changes from run to run |
| 2026-09-27 | Plain labels ステージ開始 / Start Stage, チップ購入 / Chip Shop, デッキ編集 / Edit Deck | Renamed to fit the world: 侵入プロトコル開始, マーケット, コマンドビルド (ブラックマーケット was tried first; its label came out too small on the button). The map's button labels now auto-size (capped at their old size) so long names stay inside the slanted face |
| 2026-09-27 | A pitched blip on each command flip, and the colour sting again on Rare / Epic flips | Thirty flips became a stream of beeps (「ピピピ」); the flip is now a noise-only swish |
| 2026-09-27 | Calling the hand 手札 / hand | Out of place in the world; now バッファ / Buffer, with a caption under the row |
| 2026-09-27 | Buffer Overflow purging random buffer commands | The player chooses up to 2, for 25 damage each |
| 2026-09-29 | Buffer Overflow purging chosen buffer commands for damage | Too strong; it now deals 40 and empties the next buffer. The purge mechanic moved to Garbage Collection (2 commands, 10 each) |
| 2026-09-29 | Hover selects a card (sticky selection); every card goes through target selection | Too many steps; a click now plays a card directly unless there are several enemies to pick from, and leaving the card and panel releases the selection |
| 2026-09-29 | Enemy actions revealed only as they happen | The player should plan against them; the next actions are now shown under each enemy |
| 2026-09-29 | 侵入プロトコル開始 / Start Intrusion for starting a stage | Renamed to 接続開始 / Connect at the user's request |
| 2026-09-29 | The next enemies always on the map as a small panel, enlarged while hovered | Too small and cramped; replaced by an Enemy Info button and window with selectable enemies and full command descriptions |
| 2026-10-01 | Calling the main mode キャンペーン / Campaign on screen | Now ストーリー / Story |
| 2026-10-01 | The window behind snapping to blurred / sharp at once when a window opens or closes over it | Fades with the open / close motion (a blur strength for that window's rectangle only) |
| 2026-10-01 | Window buttons split between the bottom corners (deck edit, market) | Centred in one row along the bottom, like the other windows |
| 2026-10-01 | Deck editing and the market as their own screens (a scene change from the map) | Now windows over the map, opened like Upgrade |
| 2026-10-04 | A clicked command always aiming at the front-most enemy | The player picks a priority target by clicking an enemy; it is marked above its head |
| 2026-10-06 | The battle's Menu as a small panel opening above its button, without blur | Every column-of-buttons window (the map's and the battle's Menu, the title's Collection) is now the same menu window; the battle keeps running behind it |
| 2026-09-27 | Calling them cards, card packs, a card shop and the PROGRAM DECK | Now commands, chips, the chip shop and the COMMAND DECK (the `// INSTRUCTION ...` headers became `// COMMAND ...`); pack art redrawn as chips |

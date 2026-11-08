# Market window

Detail doc for the map's market (chip shop) window, imported from the root `CLAUDE.md`. Read that file first for the general layering/wiring conventions, and @docs/map-scene.md for the screen it opens on.

It used to be its own scene (`CardShopScene`), entered with a `CardShopSceneParameter` and left with a `CardShopSceneResult`. On 2026-10-01 it became a window on the map, like the deck edit (@docs/deck-edit-window.md), and the scene, its Initializer, its parameter/result types and `SceneName.CardShop` were deleted. Since 2026-10-06 its code lives in its own window folder, `Assets/Scripts/Window/CardShop/` (`Live2DCardRogue.Window.CardShop.*`), and the map uses it from `MapSceneInitializer`; it references nothing in `MapScene`:

| File | Role |
| --- | --- |
| `Common/Models/CardShopModel.cs` | tokens, tab, selection, quantity and what has been bought this visit |
| `Window/CardShop/Presenters/CardShopPresenter.cs` | the map's Market button and the window |
| `Window/CardShop/Views/CardShopWindowView.cs` | the window (`Assets/Prefabs/CardShopWindow.prefab`, on MapScene's `PopupCanvas`) |
| `Window/CardShop/Views/CardPackView.cs`, `SingleCommandView.cs`, `CardShopPurchaseView.cs` | a chip row, a single row, the checkout panel |

The window lists the chips on sale, shows a purchase panel for the selected one (quantity −/+, total price, Buy) and has a **Close** button (`UiTextKey.Close`, centred at the bottom). It opens and closes through the shared `WindowTransition` like every other window, and only Close closes it: a click on the background does nothing.

## Packs are assets

`Common/ScriptableObjects/CardPackData.cs` holds a pack's `_id`, `_price` in tokens, how many cards one pack yields (`_cardCount`), the cards that can come out (`_cards`) and the per-rarity odds (`_rarityWeights`). Which packs the shop lists is an Inspector array on `MapSceneInitializer` (`_shopPacks`), so adding a pack is an asset plus one entry.

Rarity itself lives on the card: `CardData._rarity` (`CardRarity.Normal` / `Rare` / `Epic`). Weights are relative, so `70 / 25 / 5` and `14 / 5 / 1` mean the same thing.

## Chips on sale (2026-09-29)

Every chip on sale yields **5 commands** (`_cardCount`). The First Build chip stays at 3, since the First Build opens ten of them. The shop lists, in this order (`MapSceneInitializer._shopPacks`):

| Chip | Contents | Normal / Rare / Epic | Price |
| --- | --- | --- | --- |
| Starter (`StarterPack`) | every player command, balanced | 70 / 25 / 5 | 100 |
| Attack (`AttackChip`) | attacks, viruses, strength | 60 / 30 / 10 | 150 |
| Repair (`RepairChip`) | block, healing, regeneration | 60 / 30 / 10 | 150 |
| Hack (`HackChip`) | memory, buffer, stun, loopback, heal reversal, exploit chain… | 50 / 35 / 15 | 180 |
| Premium (`PremiumPack`) | every player command, rare-heavy | 30 / 45 / 25 | 300 |

Each chip's art is `Cards/Packs/<Id>.png`, a colour and an emblem per kind (see @docs/art.md).

## Singles

The left panel has two tabs, **チップ / Chips** and **シングル / Singles** (`ShopTab`). Singles sells one command at a time, like buying singles in a card shop:

- **What is on sale** is every command in the chips the shop lists (`CardShopModel.Singles`, no duplicates, lowest rarity first). Enemy-only commands are never in a chip, so they are never sold.
- **Price by rarity**, a little above a chip's price per command: Normal 40, Rare 90, Epic 180, times the same multiplier as the chips. The prices and the limit live in `CampaignBalance` ("Singles") and reach the model as a `SinglePurchaseRule` (`CardShopModel` falls back to `SinglePurchaseRule.Default` when given none).
- **At most 3 of each command over the whole campaign** (`LimitPerCommand`). Closing and reopening the market does not reset it: each open starts from `CampaignDeckRepository.PurchasedSingles` (`PriorSinglePurchases`), and each single purchase is reported to the map, which adds it to the repository, which is saved (`purchasedSingles`, save version 2). Commands from chips do not count against the limit.
- **The model decides** `RemainingOf(card)`, and the quantity stops at the remaining count as well as at what the tokens can pay for. A sold-out row reads 売り切れ / Sold out and is dimmed, but can still be selected to read it.
- **Rows** (`SingleCommandView.prefab`, a copy of the chip row): the command's illustration in a rarity-coloured frame (Normal cyan, Rare magenta, Epic gold), its name, 残り n/3, and the price (magenta when unaffordable or sold out). The checkout panel shows the command's name, 「レア  コスト3  残り 2/3」 (`UiTextKey.SingleInfo`) and its effect text, and the empty hint follows the tab (`// SELECT A CHIP` / `// SELECT A COMMAND`).
- **Owned count.** Each row and the checkout also show how many of that command the player owns (所持 n): each open starts from `CampaignDeckRepository.OwnedCards`, and `CardShopModel.OwnedCountOf` adds what was bought on this visit, from chips too.
- **Deck limit.** A command with `CardData.MaxCopiesInDeck` above 0 (the exploit chain's three, limited to 1) has a yellow round badge with that number at the top-right of its picture, in the row and in the checkout — the same badge cards show outside battle (see @docs/deck-edit-window.md). The checkout also lays the 「デッキに1個まで」 plate over the bottom of the picture.
- **Buying** plays the same install window as a chip, with the command(s) bought.
- `CardShopModelTests` covers the list, prices, limit, tokens, and that chips do not use up singles.

## How a draw works

`Common/Models/Cards/CardPackOpener.cs` does it in two steps, per card: **pick a rarity by weight, then pick uniformly among that rarity's cards**. Doing it in that order means the odds of an Epic do not quietly drop just because someone added twenty more Normals to the pack — the weights say what they mean. A rarity is only a candidate when its weight is above zero *and* the pack actually contains cards of it, so a weight left on a rarity that was never included is ignored rather than producing an empty draw.

The opener is a plain static class taking a `System.Random`, which is what makes the weighting testable (`CardPackOpenerTests` seeds it and asserts the distribution); `CardPackData.Open` is the thin call site that supplies a fresh `Random`. Malformed assets — no cards, all weights zero, null entries — return an empty array rather than throwing, the same contract `EnemyData.PickCards` has.

## Tokens come in, cards go out

**Every open builds a fresh `CardShopModel`** through the `Func<CardShopModel>` the presenter is handed (`MapRunUseCase.CreateCardShopModel`: the chips, the current tokens, the chip multiplier, the singles rule, the singles bought so far and the owned commands). **Every purchase is reported at once** through `onPurchased(cards, singles, remainingTokens)`, and `MapSceneInitializer.HandleShopPurchased` adds the commands to the owned list, records the singles, writes the tokens back, updates the Upgrade window's tokens (`UnitUpgradeModel.SetTokens`) and saves the progress (not during the map tutorial or after the campaign is complete). The presenter counts the map's token display down itself. So nothing is lost if the game quits with the window open, and the presenter never touches a repository.

**The tokens are the campaign's.** The model starts from `CampaignTokenRepository.Tokens` and each purchase writes the remainder back, so spending persists and stage rewards can be spent here (see "Rewards and tokens" in @docs/map-scene.md). They used to be called points; the UI label is `UiTextKey.Tokens`.

## What the model decides

`Common/Models/CardShopModel.cs` owns tokens, selection and quantity, and answers three questions the UI merely displays:

- `CanBuy` — a pack is selected and `TotalPrice <= Tokens`. This is what greys out the Buy button, so "the button is enabled" and "the purchase will go through" can't disagree.
- `CanIncreaseQuantity` — one *more* pack would still be affordable, so the + button stops at what the player can pay for instead of letting them build an unaffordable order.
- `CanDecreaseQuantity` — quantity stays at 1 or above.

Buying subtracts the tokens, appends the drawn cards to `PurchasedCards` and resets the quantity to 1 (the old quantity is rarely affordable twice in a row). Switching packs also resets it.

## Look

Styled per @docs/tone-and-manner.md as a **data market**: `// CHIPS` on the left, `// CHECKOUT` on the right, the tokens as a `// TOKENS` tag (with the token icon) in the top-right corner, in a 1860×1010 window (the Unit Upgrade window's frame, widened). Prices read `100 TKN`, and Close centred at the bottom. The market has no BGM of its own: the map's loop keeps playing, and `BgmId.Shop` / `Backgrounds/bg_shop.png` are unused now.

- **Pack art** comes from `CardPackData._illustration` (optional). `CardPackView` shows it on the row and `CardShopPurchaseView.Show` takes it as a parameter.
- **Rows** (`CardPackView.prefab`) tint a white frame by selection and light a cyan `SelectGlow` on the selected one. The price turns magenta when the tokens cannot pay for even one pack (`Bind` takes `isAffordable`, which the presenter computes as `Price <= Tokens`). This is only a hint: whether Buy is enabled is still `CanBuy`.
- **What a chip is for.** Under the chip art the checkout panel says what buying it does — `UiTextKey.ChipInstallInfo` ("install it to gain {0} commands and power up your deck", `{0}` = `CardPackData.CardCount`) in cyan — and below that the chip's own description from the `CardPackDescription` table. Descriptions carry their line break at a sentence boundary, since automatic wrapping once left a single character on the second line. `CardPackLocalizationTests` checks every `CardPackData` has both a name and a description in every locale.
- **Checkout.** `_root` is the `Content` child, so the panel stays on screen; with nothing selected it shows `_emptyHint`. Quantity −/+ are hexagon buttons, Buy is a `CyberButton.prefab` with its `>>>` decoration on.

## Purchase result window

A purchase opens `ChipInstallWindowView` (`// DOWNLOAD COMPLETE`, title `UiTextKey.AcquiredCards`) instead of a toast. The window and its views live in `Common/Views/ChipInstall/` and in `Prefabs/ChipInstallWindow.prefab`, because the map shows the campaign's First Build with the same window (see @docs/map-scene.md); the window knows nothing about what was opened, and the caller sets its title and button label. It is an instance of the prefab inside `CardShopWindow.prefab` (`ChipInstallWindow`, `CardShopWindowView._resultView`); opening it makes it the front window, so the market window drops behind the blur with the map (see "Only the front window is sharp" in the root doc); and it opens and closes through the shared `WindowTransition` (see "Motion" in @docs/tone-and-manner.md), which also fades the blur behind it in and out. The window's own addition is the cards: they arrive one by one on open and collapse in reverse on close (`Close()`; `Hide()` is the instant version used at start-up). The electron burst (`WindowDissolve`) sits beside `ResultView` under the prefab's always-active root, outside the window, so it survives the window being deactivated. A translucent `Scrim` under the window takes the clicks, so the market cannot be used until **Close** (`UiTextKey.Close`) is pressed.

- **Cards** (`ShopResultCardView.prefab`, `ChipInstallCardView : CardView`) are laid out by script in `CardArea`: the view tries every column count and keeps the one that gives the biggest card that still fits (capped at `_maxCardScale`), rows centred. Below `_minDescriptionScale` the description is hidden, since it would be too small to read. Each card opens from a line one after another (`_cardInterval`), and cannot be hovered until it has arrived.
- **Unpacking** is a view-only sequence; what came out was already decided by `CardPackOpener`.
  1. **Install** (`ChipInstallView`, under `CardArea/Install`): the pack, drawn as a chip, drops into a socket in grid steps and a progress bar fills, slowing near the end. The glow starts cyan and is **promoted** towards the highest rarity inside — magenta at 40 % if there is a Rare, gold at 75 % if there is an Epic (gold drifts through rainbow hues, the install shakes, particles burst, and the bar **stops at 99 %** for `_epicHoldDuration` before it completes). On completion the chip bursts into light.
  2. **Reveal**: the cards arrive face down, **sorted by rarity, lowest first**, so the best card is always flipped last. Normal cards flip at once; Rare and Epic first charge up in their colour (`_rareCharge`, `_epicCharge`) and keep a faint glowing frame after flipping; each flip bursts `RevealBurst` (world-space particles moved to the card) in the card's colour.
  3. Close appears only when everything is face up. A card cannot be hovered until it is face up.
  - **Skip**: while it plays, `SkipArea` (a transparent `ClickArea` over the window) turns any click into "finish now" — install hidden, every card face up, Close shown.
- **Hover detail.** A card reports enter/exit; `CardShopPresenter` passes that on to `ShowDetail` / `HideDetail`, which drive a `CardDetailView` (the same component as the battle's details panel) at 1.4x on the right. It keeps its short close delay, so moving from card to card swaps the content instead of blinking.


## Known gaps

- Rarity is data only — no card in the game displays it yet.
- The pack list shows a name and price; there is no preview of what is inside.
- The result window does not tell newly acquired cards apart from ones already owned.

# Deck edit window

Detail doc for the map's deck edit (**Command Build**) window, imported from the root `CLAUDE.md`. Read that file first for the general layering/wiring conventions, and @docs/map-scene.md for the screen it opens on.

It used to be its own scene (`DeckEditScene`), entered with a `DeckEditSceneParameter` and left with a `DeckEditSceneResult`. On 2026-10-01 it became a window on the map, opened like Unit Upgrade, and the scene, its Initializer, its parameter/result types and `SceneName.DeckEdit` were deleted. Since 2026-10-06 its code lives in its own window folder, `Assets/Scripts/Window/DeckEdit/` (`Live2DCardRogue.Window.DeckEdit.*`), and the map uses it from `MapSceneInitializer`; it references nothing in `MapScene`:

| File | Role |
| --- | --- |
| `Window/DeckEdit/Models/DeckSetEditModel.cs` | every deck being edited, which one is selected and which is active |
| `Window/DeckEdit/Models/DeckEditModel.cs` | one deck being edited (owned and in-deck counts per command) |
| `Window/DeckEdit/Presenters/DeckEditPresenter.cs` | the map's Command Build button and the window |
| `Window/DeckEdit/Views/DeckEditWindowView.cs` | the window (`Assets/Prefabs/DeckEditWindow.prefab`, on MapScene's `PopupCanvas`) |
| `Window/DeckEdit/Views/DeckEditCardView.cs`, `DeckEditCountView.cs`, `DeckEditSubView.cs` | a list card, the deck count, the enlarged card |
| `Window/DeckEdit/Views/DeckSlotTabView.cs` | one deck number tab (01–10) |
| `Window/DeckEdit/Constants/DeckEditConstants.cs` | `MaxDeckSize` (20) |

The window is three areas: the **owned** list, the **deck** list, and a **sub view** that enlarges whichever card was last clicked. Below them are **Auto Build**, **Cancel** and **Save**.

## Ten decks, one active

The player can keep up to `CampaignDeckRepository.DeckSlotCount` (10) decks; the **active** one is the deck the battle uses (`CampaignDeckRepository.Deck` returns it).

- **Decks share the owned commands.** Each deck is built from everything owned, independently: a command can be in several decks up to the owned count in each. That is why `DeckSetEditModel` holds one `DeckEditModel` per deck, all built from the same owned list, and the owned list shown is the selected deck's *available* (owned − in that deck).
- **Tabs.** A row of number tabs (`DeckTabs`, 01–10) sits above the `// COMMAND DECK` panel. Clicking one selects that deck: both lists, the counter and the header (`デッキ 03`) switch to it, the sub view clears and the lists scroll to the top. The selected tab is bright; the active deck's tab has a yellow number and a yellow underline, and while the selected deck is the active one a yellow **アクティブ / Active** tag (`UiTextKey.ActiveDeck`) sits beside the header. Opening the window selects the active deck.
- **Set Active** (アクティブにする, `UiTextKey.SetActiveDeck`) makes the selected deck active. It sits where the アクティブ tag would be, beside the deck header, and only one of the two is shown: the tag when the selected deck is active, the button (`ActivateTag`, the tag's shape in cyan) when it is not. So it is out of sight in the 所持 layout, where the deck panel is hidden. It activates the deck whatever its count (refusing short decks made the button behave differently from deck to deck); the count is checked by Save instead.
- **Save** only checks the **active** deck: other decks may be short or over and still be saved. If the active deck is not exactly `MaxDeckSize`, a toast names it and its count and the window stays open.
- **Everything is one edit.** Switching decks, editing several, Auto Build and Set Active are all held in the model until Save, which hands `onSaved(decks, activeIndex)` to `MapRunUseCase.ApplyDeckSaved` (`CampaignDeckRepository.SetDecks`, then save). Cancel throws all of it away.

## Three layouts

Three small tabs left of the title (**すべて / デッキ / 所持**, `UiTextKey.DeckEditLayoutAll` / `Deck` / `Owned`; 0.55x `CyberButton`s like the market's Chips / Singles tabs, the current one in yellow) switch how the window is laid out (`DeckEditLayout`), for when a list gets hard to read:

- **すべて (Both):** the owned list, the deck list and the sub view, as before.
- **デッキ (DeckOnly):** the owned panel is hidden and the deck panel widens over both panels' area (1230 wide), so the deck list shows 6 columns instead of 3. Commands not yet in the deck are added from one of the other layouts.
- **所持 (OwnedOnly):** the deck panel is hidden and the owned panel widens the same way. Since the deck list is out of sight, the deck tabs stay above the panel, the owned panel's header shows the selected deck and its count on the right (`DeckSummary`: `デッキ 02  12/20`, a copy of the deck panel's header and counter without the gauge), and the sub view shows minus too, so a command can be taken out without switching back.

`DeckEditWindowView.SetLayout` does the placing: it remembers the Both rects on first use, moves and resizes the panels, moves the tab row over the visible panel and calls `CommonScrollList.FitColumnsToWidth` (columns = what fits the viewport width). The presenter keeps the layout in a field, so reopening the window keeps it during this map visit; it is not saved.

## Opening and closing

- **Open.** `Canvas/DeckEditButton` (label `UiTextKey.DeckEdit`, set by this presenter) opens it through the shared `WindowTransition`, like every other window: it opens from a line, and the map behind it blurs and dims.
- **Enemy Info** (`EnemyDetailButton`, top-right of the window, a 0.55x `CyberButton` like the layout tabs, label `UiTextKey.EnemyDetail`) opens the map's Enemy Info window over the deck edit, so the next opponents can be checked while building. It is the same window as the map's button: `DeckEditPresenter` is handed the Enemy Info handler's `Show` and `CanOpenEnemyDetail` through `DeckEditInitData`, sets the label itself and disables the button on open once every stage is cleared. Closing it returns to the deck edit with the edit intact.
- **Only Cancel and Save close it.** A click on the background does nothing (the scrim still swallows it); this is deliberate, so a stray click cannot throw away an edit.
- **Confirmations** go through the map's shared `WindowHandler<ConfirmShowData>` (also used by Stage Start and Unit Upgrade; each question carries which one it was, `Confirming`, into its OK callback):
  - **Cancel** closes at once when nothing changed; otherwise it asks 変更を破棄しますか？ (破棄する / 戻る).
  - **Save**, once the active deck passes its size check, closes at once when nothing changed; otherwise it asks 変更を保存しますか？ (保存する / 戻る, `UiTextKey.SaveChangesConfirm` / `SaveChangesOk`).
  - **Auto Build** always asks おまかせでデッキを組み直しますか？ with the deck it would replace as the detail (`デッキ 03`), OK 組み直す (`UiTextKey.AutoBuildConfirm` / `AutoBuildOk`).
  - **Empty the deck** (the trash icon, `ClearButton`, right of the アクティブ tag / Set Active) asks デッキを空にしますか？ with the deck as the detail, OK 空にする (`UiTextKey.ClearDeckConfirm` / `ClearDeckOk`), and then removes every command from the selected deck (`ReplaceDeck` with nothing). It is disabled while the deck is empty. Like every edit here it is only committed by Save.
  - 戻る always returns to the window with the edit intact.
- **What counts as a change** is `DeckSetEditModel.IsDirty`: the active deck moved, or a deck's contents differ from when the window opened (`DeckEditModel.IsDirty` compares counts, so adding and removing the same command back is no change, and neither is only switching tabs).
- **Save** checks the active deck (see above). The button stays enabled on purpose, so the player gets told *why* rather than facing a dead control.
- Presses while the window is closing are ignored (`DeckEditWindowView.IsOpen`).

## It does not know where the commands live

The presenter is handed `Func`s for the owned commands, the decks and the active index, and an `Action<CardData[][], int> onSaved`; `MapSceneInitializer` wires them to `CampaignDeckRepository`, and its `MapRunUseCase.ApplyDeckSaved` writes the decks back and saves the progress (not during the map tutorial or after the campaign is complete, like the other milestones).

**Every open builds a fresh `DeckSetEditModel`** from those `Func`s. That is the whole implementation of Cancel: the repository was never touched, so there is nothing to roll back and no "original deck" to keep around, and a cancelled edit never shows up the next time the window opens. The model is kept until the next open (not dropped on close), because scrolling a list during the close animation still re-binds rows. While editing, the deck may go **over** the limit, so a swap can add first and remove later.

Owned commands are passed as a flat list with repeats (ten of each card is ten entries), the same shape a deck has, so one type covers both.

## One model owns the arithmetic

`DeckEditModel` holds owned counts and deck counts per card, and everything else is derived:

- **available = owned − inDeck**, and the owned list shows only cards whose available is above zero. That is the whole answer to "cards in the deck shouldn't appear in the owned list": nothing subtracts anything at display time, so the two lists cannot disagree.
- `CanAddToDeck` is `available > 0` and, for a command with `CardData.MaxCopiesInDeck` above 0, fewer copies in the deck than that (the exploit chain's three are limited to 1) — the plus button greys out when either runs out. The size limit is not checked here; it is checked on Save (`IsDeckValid`).
- Display order is fixed at construction ("first seen" order), so adding and removing never reshuffles the rows under the player's finger.
- A deck holding more copies than the owned list claims is repaired at construction by raising the owned count, rather than throwing — malformed input from a caller shouldn't take down the window.

`DeckEditPresenter` watches one `OnChanged` and re-syncs both lists, the deck counter and the sub view together, which is why an add or remove can't leave one area stale.

## Dragging between the lists

In the **Both** layout a command can be dragged from one list onto the other panel, **one copy per drop** (`x3` dragged once leaves `x2`). Owned → deck is `TryAddToDeck`, deck → owned is `TryRemoveFromDeck`; like every edit here it is only committed by Save.

- **Who does what.** `DeckEditCardView` only reports the drag to the window it sits in (`GetComponentInParent<DeckEditWindowView>`). The window decides whether a drag may start (`TryBeginCardDrag`: open, `Both` layout, and which list the row is under), moves a ghost with the pointer, lights the panel it may be dropped on and tests where it was let go. A drop on the other panel raises `OnCardDroppedToDeck` / `OnCardDroppedToOwned`; the presenter checks `CanAddToDeck` / `CanRemoveFromDeck`, moves one copy, and shows that command in the sub view (as if picked from the list it went to). A drop anywhere else does nothing.
- **Drag vs scroll.** A row that handles drags would otherwise swallow the `ScrollRect`'s drag, so the first movement decides: mostly **horizontal** starts a card drag, mostly **vertical** is forwarded to the parent `ScrollRect` (initialize / begin / drag / end, via `ExecuteEvents.ExecuteHierarchy`). In the DeckOnly / OwnedOnly layouts every drag scrolls.
- **Refused drops.** A command at its `MaxCopiesInDeck` plays `Error` and toasts 「〇〇はデッキに1個までです」. A successful drop plays `MemoryWrite` (into the deck) or `CardReturn` (out of it).
- **Ghost.** A copy of the first dragged row, made once under the window's `WindowTransition` object and reused (`x1`, 85 % alpha, `CanvasGroup.blocksRaycasts` off so it never hides the panel under it). It is hidden by `CancelCardDrag`, which also runs on a layout change, on close, and from the row's `OnDisable` (a row scrolled out of view by the wheel mid-drag is pooled and would never get its `OnEndDrag`).
- **Drop highlight.** `OwnedPanel/DropHighlight` and `DeckPanel/DropHighlight` (`ui_glow_frame`, cyan, no raycast, inactive by default; `_ownedDropHighlight` / `_deckDropHighlight`): the target panel's frame shows faintly while dragging and at full strength while the pointer is over it. Both are optional.
- Dragging does not fire the row's click (the input module clears `eligibleForClick` when a drag starts).

## The sub view's two shapes

Which list the card came from is presenter state (`_isSelectedFromDeck`), not model state:

- clicked in the **owned** list → plus only,
- clicked in the **deck** list → plus and minus.

Plus is disabled by `CanAddToDeck`, minus by `CanRemoveFromDeck`. The sub view is re-shown after every change, so the buttons and counts follow along, and a card that was emptied out of the deck stays on screen with minus greyed out rather than vanishing. Opening the window clears the selection.

## Scrolling reuses its rows

`Common/Views/UI/CommonScrollList.cs` is the shared virtualized list — a `ScrollRect` whose content only ever contains the rows currently on screen plus one spare row. `SetItemCount` resizes the content and re-binds; `Refresh` recomputes the visible index range from the content's scroll offset and hands unused rows back to a pool. `ScrollToStart` is what the window calls on every open, so a reopened window starts at the top.

**Scrollbars.** Both lists (and the command collection's) have a thin cyan vertical scrollbar, the same as the battle log's (`ScrollRect.verticalScrollbar`, `AutoHide`, so it disappears when everything fits), to make it obvious that the list scrolls. It sits on the list's right edge in the panel margin rather than inside the viewport: the owned list fits three columns with 4 px to spare, so narrowing the viewport would drop it to two.

It deliberately knows nothing about cards: `Initialize(Action<int, RectTransform>)` takes a callback that fills a blank frame with whatever the caller wants (here `DeckEditCardView.Bind`). It assumes **every row is the same size** — variable heights would need a different implementation.

`CommonScrollListPlayModeTests` pins the property that matters: 300 items must not create 300 GameObjects, scrolling must not create more, and shrinking the list must deactivate rows rather than destroy them. It is a PlayMode test because the visible-range maths needs a laid-out `RectTransform`, which Edit mode does not give you.

## Look

Styled per @docs/tone-and-manner.md as a **program editor**: a 1860×1010 window (the Unit Upgrade window's frame, scrim, collapse flash and dissolve burst, widened) with the `// PROGRAM EDITOR` header and title at the top, three panels (`// LIBRARY`, `// COMMAND DECK` on the memory-board sprite, `// COMMAND DATA`), and Auto Build, Cancel and Save in one row centred along the bottom, with Save as the primary.

- **List cards** (`DeckEditCardView.prefab`) are the battle card art at 0.64x under a `Card` child, so the scroll list can resize the root freely. The description is hidden at that size, as on the command cards, and the count sits on a tag over the empty description box. The card shown in the sub view gets a cyan `SelectGlow` in whichever list it was picked from — `Bind` takes `isSelected`, and a selection refreshes both lists.
- **Deck limit badge.** A command that can be in the deck only a limited number of times (`CardData.MaxCopiesInDeck` above 0; the exploit chain's three allow 1) shows a yellow round badge with that number at the card's top-right corner, so it is clear why its plus button greys out. It is part of `CardView` itself (`_deckLimitBadge` / `_deckLimitText`, optional): the list cards, the sub view, the shop's result window and the reward window have it. **Battle cards do not** (hand, command memory, the battle's detail panel): it is deck-building information. Enlarged cards (the sub view, the shop's result detail, the reward card) also say it in words on a solid yellow plate over the bottom of the picture, 「デッキに1個まで」 (`_deckLimitLabel` / `_deckLimitLabelText`, `UiTextKey.DeckLimit`).
- **Deck count** is `DeckEditCountView`: `12/20` in magenta unless the deck is exactly 20 (lime then; `23/20` is magenta too, while the gauge simply stays full), plus a `SegmentGaugeView` of 20 memory chips. It is the same gauge as the battle's memory, scaled to 0.58.
- **Sub view.** `_root` is the `Content` child, so the panel itself stays on screen; with nothing selected it shows `_emptyHint` (a faint card back and `// SELECT A COMMAND`). The card is a plain `CardView` built from `CardView.prefab` at 1.15x.
- **Buttons** are `CyberButton.prefab` instances.
- **No BGM of its own.** The map's loop keeps playing. `BgmId.DeckEdit` and `bgm_deck_edit` were the scene's and are unused now, as is `Backgrounds/bg_deck_edit.png`.

## Auto build

**Auto Build** (おまかせ, `UiTextKey.AutoBuild`) rebuilds the deck from everything owned with `AutoDeckBuilder` — the same picker as the First Build — and replaces it through `DeckEditModel.ReplaceDeck` (owned counts and per-command limits still apply). It does not save; the player can keep editing, save or cancel. The role counts come from the `AutoDeckRule` handed to the presenter (the map passes `CampaignBalance.CreateAutoDeckRule()`); without one it uses 5 : 3 : 2 of `DeckEditConstants.MaxDeckSize`.

## Known gaps

- The window is placed only on the map (as is the market, @docs/card-shop-window.md). Since it lives in `Window/DeckEdit/` and references no scene, another scene can use it by placing `DeckEditWindow.prefab` on its `PopupCanvas` and building `DeckEditPresenter` in its Initializer.

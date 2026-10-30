# Decisions (ADRs) for the Avalonia port

Status: **D1–D4 Accepted by Lee on 2026-10-30.** Undo storage strategy in D1 is under review (see D1 point 3). Written 2026-10-30 from feature-matrix section X
(T1.6), its "Observations for D1", and targeted code checks noted inline.

---

## D1 — An editor model in Core; the UI only renders and forwards input

**Context.** Lee's direction (L2): the UI is a thin rendering of a logical model and viewport. Today the
reference app keeps editing logic in WinForms classes (`Canvas.cs`, `CommandController` bound to `Canvas`),
and the Avalonia app keeps selection, viewport, drag state and editing in `Services/CommandService.cs` and the
67 KB `Controls/MapCanvasControl.cs`. Undo/redo is P1 (L3) and needs every mutation to pass through one place;
today `IsDirty` is set by hand at ~20 sites and drags mutate elements directly.

Facts checked for this decision:
- Hit-testing needs **no text measurement**: `Room`, `MapLabel` and `Connection` `Distance`/`UnionBoundsWith`
  use only stored bounds and vertex positions (`src/Trizbort.Core/Domain/Elements/MapLabel.cs:130–145`,
  `Connection.cs:631–643`). So hit-testing can move to Core with no UI abstraction.
- Deleting a room **cascades**: `Project.OnElementRemoved` removes every connection docked to it
  (`src/Trizbort.Core/Domain/Application/Project.cs:192–205`). Undo of a delete must restore those too.
- Ids, `ReferenceRoomId`, ports by `PortId` and paste's `OldId` remapping make per-property undo fragile.
- `Project.Current` is used ~75 times in Core and ~87 times in Avalonia.

**Decision.**

1. Add `src/Trizbort.Core/Editor/` with an **`EditorSession`** that owns:
   - `Project` (wraps `Project.Current`; the static stays for now — removing it is out of scope and would touch
     160+ call sites; the session is the only thing new code talks to);
   - **`Selection`**: an *ordered* list with a primary element (legacy semantics; swap/join/navigation depend on
     order), replacing Avalonia's `HashSet` + `Project.ActiveSelectedElement`;
   - **`Viewport`**: origin, zoom, viewport size, canvas↔client transforms (reuse `CanvasTransform`), fit-to-map,
     zoom-about-point, zoom steps and micro-zoom, wheel inversion setting;
   - **`UndoStack`** (below);
   - **`CanvasInteraction`** (below);
   - one event `Changed(EditorChange flags)` with `Model | Selection | Viewport | Adorners | Dirty`, replacing
     `ProjectChanged`/`SelectionChanged`/`ZoomChanged`/`CanvasInvalidated`.
2. **Every edit is a command executed through the session** (`session.Execute(edit)`), never a direct property
   set from UI code. Dialog OK, paste, delete, nudge, automap batches and drags all go through it.
3. **Undo is a full multi-level stack** (undo and redo, cap ~200 steps), stored behind an `IUndoMemento`
   interface so the storage strategy can change without touching commands.
   - **v1 storage = whole-map snapshot per step** (map XML via `LegacyMapFileEngine`, gzip-compressed, plus selection
     as element ids). Measured on Lee's PC (2026-10-30, `scripts/bench/undo-snapshot-bench.cs`):

     | Map | Rooms / connections | XML | Snapshot (save) | Restore (load) |
     |---|---|---|---|---|
     | space-quest sample | 104 / 137 | 70 KB | 1.4 ms | 11 ms |
     | synthetic | 250 / 468 | 208 KB | 3.2 ms | 24 ms |
     | synthetic | 1,000 / 1,936 | 841 KB | 20 ms | 126 ms |

     So for real maps a step costs ~1–3 ms to record and ~10–25 ms to undo; even a 1,000-room map stays under a frame
     to record and ~0.1 s to undo. Memory is the real cost: uncompressed, 200 steps of a 1,000-room map would be
     ~170 MB, hence gzip (XML typically compresses ~8–10×) and the step cap.
   - Why snapshots first: deletes cascade to connections, paste remaps ids, and dialog OKs touch many properties —
     a snapshot gets all of these right with no per-command undo code, which suits cheap agents.
   - Caveat: restoring recreates element objects, so the UI must drop references (hover, drag state) and reselect by
     id after undo. The session's `Changed(Model | Selection)` event covers this.
   - **Planned v2 = element-level mementos**, only if profiling says so: a transaction records the before-state of
     just the elements it touches (each element already serialises itself) plus added/removed elements (captured from
     `Elements.Added/Removed`, which also catches cascades). Cost then scales with the edit, not the map. To keep that
     upgrade mechanical, commands declare the elements they will change when they open a transaction
     (`session.Begin("Swap names", touched: rooms)`), even though v1 ignores the hint.
   - Rejected: hand-written inverse operations per command (classic command pattern) — fastest, but ~100 commands
     each needing a correct inverse, including cascades and id remapping, is where undo bugs come from; immutable
     model / structural sharing — too large a rewrite of Core.
   - Grouping: one step per discrete command, per dialog OK, per paste, per completed drag (snapshot taken on press,
     committed on `EndDrag(!cancelled)`, discarded on cancel — which also fixes I-010), per automap batch.
   - Dirty flag = "stack position ≠ position at last save".
   - Performance gate (test): recording a step on the 1,000-room synthetic map must stay < 50 ms and restoring < 250 ms.
4. **Input-neutral interaction in Core.** Lift `MapCanvasControl`'s `CanvasDragMode` state machine, hit-testing,
   pending-connection logic and `EndDrag(cancelled)` into `CanvasInteraction`, which consumes neutral events —
   `PointerInput(button, modifiers, clientPos, clickCount)`, `WheelInput`, `KeyInput(KeyGesture)` — and outputs
   commands plus an `AdornerState` (hover element/port/handle/waypoint, marquee rect, pending line, `CursorKind`).
   Keep Avalonia's cancel/restore behaviour (better than legacy). The Avalonia control shrinks to: convert events,
   call the session, draw model + adorners.
5. **UI services Core asks for, via interfaces** (implemented in Avalonia, faked in tests):
   `IClipboard` (async text), `IEditorPrompts` (async message, confirm, save/discard/cancel, edit-element-properties,
   pick file, automap prompts — see D3). Commands that need input ask the interface and await; they never open
   windows themselves. "Add room then edit it" becomes "add room" + `prompts.EditElement(room)`.
6. **Behaviours settled now** (so the move doesn't drift): right-drag pans and a right-click with ≤ 2 px movement
   opens the context menu (Avalonia, approved); hit-testing honours `ZOrder` (legacy); selection modifiers follow
   legacy (Ctrl toggles, Shift adds) unless D4 says otherwise; new rooms/connections use the "new room/new
   connection" defaults as legacy does (Avalonia currently ignores them — matrix G008).
7. **Rendering stays UI-side** (`Drawing/*`, spatial line index, smart-segment recompute, `RenderToBitmap`) and
   reads only model + `AdornerState`.

**Consequences.** Avalonia tests shrink to rendering and wiring; behaviour gets Core unit tests that run on any OS.
Legacy is untouched. `AGENTS.md`'s "No undo/redo exists" line changes when F3 lands.

**Foundation tickets (in order, each ≤ ~1 day, mid tier unless noted):**

| # | Ticket | Notes |
|---|---|---|
| F1 | `Selection` and `Viewport` types in Core, with tests | Ordered selection with primary; viewport maths from `CommandService`/`CanvasTransform`. |
| F2 | `EditorSession` in Core; `CommandService` becomes a thin adapter over it | App keeps working; existing Avalonia tests stay green. |
| F3 | `UndoStack` with snapshot mementos, transactions, dirty flag, perf gate test | Strong-tier review of the diff. |
| F4 | Route existing mutations (add, delete, paste, nudge, dialog OK, automap) through the session | Remove hand-set `IsDirty`. |
| F5 | `CanvasInteraction` extraction with Core tests pinning gestures | L size: split into hit-testing, drag modes, connections. Strong-tier review. |
| F6 | Undo/Redo commands and UI (menu, toolbar, gestures) | Depends on D2 catalogue. |

---

## D2 — One command catalogue drives menus, toolbar, context menus and keys

**Context.** Avalonia wires each surface separately: `MainWindow.axaml` menu items, `Window.KeyBindings`, toolbar
buttons, `MapCanvasControl.OnKeyDown` and `ShowContextMenuAt`. ~150 matrix rows need commands; wiring each in five
places invites the conflicts already found (Ctrl+Shift+A, Ctrl+Shift+S, F1).

**Decision.** `src/Trizbort.Core/Editor/Commands/`:
- `EditorCommand` record: `Id` (e.g. `room.swapNames`), `Title`, `MenuPath` (e.g. `Edit/Rooms/Swap`), `Group` for
  separators, `Gestures` (list of `KeyGesture` = key + `ModifierKeys`, where a new `Primary` flag means Ctrl on
  Windows/Linux and Cmd on macOS), `Scope` (`Window` or `Canvas` — single-letter keys are `Canvas` and only fire
  when the canvas has focus and no text box is editing), `ContextTargets` (`Room | Connection | Label | Empty` for
  the context menu), `CanExecute(session)`, optional `IsChecked(session)`, `ExecuteAsync(session)`.
- `CommandCatalogue` registers all commands in Core. Avalonia **generates** the main menu, macOS `NativeMenu`,
  context menus, toolbar and key bindings from it — no hand-written menu XAML beyond layout containers.
- Tests: duplicate gesture within a scope fails; every catalogue id has a title and menu path or is marked
  hidden; every matrix-referenced command id exists (once tickets add them).
- D4's approved shortcut table becomes the catalogue's gesture data.

**Tickets:** C1 catalogue types + tests; C2 Avalonia menu/context/toolbar/key generation replacing the current
hand-wired items (behaviour-preserving); then every feature ticket is "add command X to the catalogue".

---

## D3 — Prompts from Core are async

**Context.** Automap's same-direction prompt is a synchronous `Func<Room,string,AutomapSameDirectionResult>`
(`src/Trizbort.Core/Automap/Automap.cs:806–811`) and never assigned in Avalonia (matrix E044–E045); room
disambiguation happens inside the synchronous `IAutomapCanvas.FindRoom`. The engine itself is already async
(`Start`, `StartCl`, `ProcessTranscriptText` await I/O and stepping), and `AvaloniaAutomapCanvas` marshals each
call to the UI thread with `Dispatcher.UIThread.Invoke`.

**Decision.** Make the prompts async and await them in the engine:
- Replace `ConflictingRoomChooser` with `IEditorPrompts.ChooseSameDirectionAsync(room, name)` returning
  `Task<AutomapSameDirectionResult>`; add `IEditorPrompts.DisambiguateRoomAsync(candidates, line)` and have the
  engine call it where `FindRoom` currently resolves ambiguity.
- Propagate `await` up the engine's call chain to `ProcessTranscriptText` (already async). No blocking waits on the
  UI thread (no `.GetAwaiter().GetResult()`), so no deadlocks.
- When automap is driven by the `EditorSession` (D1), `AvaloniaAutomapCanvas`'s dispatcher marshalling goes away:
  the engine runs on the UI synchronisation context and awaits I/O.
- Headless/CLI runs pass a non-interactive `IEditorPrompts` that returns today's defaults (`KeepRoom1`, first match).

**Tickets:** A1 async prompt interfaces + engine changes + Core tests with a scripted fake; A2 Avalonia prompt dialogs
from the specs in `dialogs/Automap.md`.

---

## D4 — Shortcut map

**Accepted** — see `shortcut-map.md` section 5 for Lee's answers. Policy L4: keep reference gestures unless an obvious modern
convention exists.

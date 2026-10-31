# Avalonia Feature Parity Review — Plan

Status: Draft 3 (2026-10-30). Progress: Phases 0 and 1 done; D1–D4 accepted (`avalonia-parity/decisions.md`, `shortcut-map.md`); tickets written (T3.1); next: Lee confirms priorities (T3.2), then Phase 4 from wave 1. Owner: Lee. Working folder: `agent-docs/plans/avalonia-parity/`.

## 1. Goal and direction

Find out, with evidence, which features of the original WinForms Trizbort exist in the Avalonia app
(`src/Trizbort.Avalonia/`), which are partial, missing or broken, and turn every gap into a small,
self-contained ticket that a cheap agent can implement.

### Decisions already made by Lee (2026-10-30)

| # | Decision |
|---|---|
| L1 | **Two solutions in one repo.** The refactored WinForms projects are deleted. The original WinForms app from tag `v2.3.1-beta.2` is restored, with its history, into `legacy/` with its own `legacy/Trizbort.Legacy.sln` and test project, so urgent hotfixes and legacy releases stay possible. The root `Trizbort.sln` holds only the new app (Core, Core.Tests, Avalonia, Avalonia.Tests). Done by task T0.2 (approved by Lee). The legacy app is also the **reference** for this review. |
| L2 | **The UI is a thin rendering of a logical model and viewport.** Map, selection, viewport, editing commands, undo history and as much input interpretation as possible live in `Trizbort.Core`. Avalonia translates input events into model calls and draws what the model says. The only UI-side concern should be rendering consistency. (Drives D1.) |
| L3 | **Undo/redo is a P1 feature**, built on the new command pipeline. |
| L4 | **Port all shortcuts and features; prefer the modern convention where an obvious one exists.** Right-mouse-drag to pan (already in Avalonia) is the model of a good change. (Drives D4.) |
| L5 | **Platforms: Windows and Linux must be tested and polished.** macOS code-level support (Cmd key, native menu bar, open-file events) is in scope because it's cheap once D2 exists; macOS packaging, signing and notarisation are documented for a future agent (T3.4), not done. |
| L6 | **Dialogs are reviewed for context, not just fields.** Several Avalonia dialogs changed layout and lost grouping, labels, hints or enable/disable relationships. Reviews use screenshots of both apps (T0.4, T0.5). |

### Deliverables

| File (under `agent-docs/plans/avalonia-parity/`) | Produced by | What it is |
|---|---|---|
| `feature-matrix.md` | Seeded; filled by Phase 1 | One row per feature, with verified status and evidence. |
| `baseline.md` | T0.1, T0.2 | Build/test results for both solutions. |
| `screenshots/winforms/…`, `screenshots/avalonia/…` | T0.4, T0.5 | PNG of every dialog, every tab, key dependent states. |
| `dialogs/<DialogName>.md` | T1.4x | Field-by-field and layout/context comparison per dialog. |
| `decisions.md` | D1–D4 | Short ADRs. |
| `issues.md` | Lee + agents | Teething issues (bugs, not missing features). |
| `tickets/NNN-short-name.md` | Phase 3 | One implementation ticket per file, template in §8. |
| `smoke-test-script.md` | T2.4 | Manual click-through script for Windows and Linux. |
| `macos-future-work.md` | T3.4 | What a future agent needs to do to ship on macOS. |

## 2. Rules for any agent picking up a task (read this first)

These exist to keep cost down. Follow them literally.

1. **Do exactly one task card per session.** Put its ID in the commit message: `parity(T1.2): …`.
2. **The reference source is the legacy app, written `$REF` below; `$REF` = `legacy/`** (repo-relative, e.g.
   `$REF/src/Trizbort/UI/MainForm.cs` = `legacy/src/Trizbort/UI/MainForm.cs`). In it, everything (Domain, Automap, Export, UI) lives in the
   single project `src/Trizbort/`. If T0.2 hasn't landed yet, read the same paths with `git show v2.3.1-beta.2:src/Trizbort/…`
   and `git grep -n <pattern> v2.3.1-beta.2 -- src/Trizbort`.
3. **Read only the files listed under "Read".** For large files use the given line ranges or `grep` commands; never open whole:
   `$REF/…/MainForm.Designer.cs` (98 KB), `$REF/…/Canvas.cs` (100 KB), `$REF/…/Canvas.Designer.cs` (32 KB),
   `$REF/…/RoomPropertiesDialog.Designer.cs` (76 KB), `$REF/…/SettingsDialog.Designer.cs` (86 KB),
   `src/Trizbort.Avalonia/Controls/MapCanvasControl.cs` (67 KB),
   `src/Trizbort.Core.Tests/Fixtures/classic-manual-model-summaries.txt` (770 KB).
4. **Don't read `agent-docs/` wholesale.** Read only the sub-doc the card names.
5. If you need a file the card doesn't list, **stop and write the question in the Notes column / ticket** rather than exploring.
6. **Write outputs in the exact format given.** Don't reformat other rows of shared tables.
7. Phase 1–2 tasks are **read-only on `src/`** (except T2.1, which adds tests). Log problems; fixes happen through tickets.
8. Parity tasks never modify `legacy/` (only T0.4 adds a capture test there). Legacy hotfixes are separate work: open
   `legacy/Trizbort.Legacy.sln`, keep the change minimal, and follow `legacy/README.md`.
9. Follow `AGENTS.md`: build + test pass, then commit immediately. Never commit `*.DotSettings.user`.
10. When done, tick the card's checkbox in §6 of this file.

### Status vocabulary (use only these words in "Verified status")

| Status | Meaning |
|---|---|
| `Implemented` | Present and behaves like the reference (or better), confirmed by code reading and/or a test. |
| `Partial` | Present but missing options, shortcuts, edge cases, fields or context (labels/grouping/hints). Say what in Notes. |
| `Missing` | No equivalent in Avalonia. |
| `Broken` | Present but doesn't work / throws / wrong result. Also add an entry to `issues.md`. |
| `Different` | Works differently. Say how, and whether it looks intentional and better (e.g. right-drag pan) or accidental. |
| `New-in-Avalonia` | No reference equivalent. |
| `Won't-port` | Lee has decided not to carry this over (only Lee sets this). |

Evidence is a `path:line` reference, a test name, a screenshot path, or "manual: <date>, <OS>".

## 3. Model tiers

| Tier | Use for | Examples |
|---|---|---|
| **Cheap** (e.g. Haiku) | Mechanical inventory with given grep commands, filling tables, running builds/tests, setup, summarising. | T0.1, T1.1, T1.2, T1.8, T2.4 |
| **Mid** (e.g. Sonnet) | Judging behaviour from code, dialog reviews with screenshots, writing tests/harnesses, implementing one ticket. | T0.2, T0.4, T0.5, T1.3–T1.7, T2.x, T3.1, most tickets |
| **Strong** (e.g. Opus) | Only D1–D3 and reviewing tickets marked L. Keep sessions short; give them the matrix and named files, not the codebase. | D1, D2, D3 |

## 4. What the static scan already found (seed hypotheses)

From a quick scan on 2026-10-30. **Unverified** — Phase 1 confirms or corrects each point.

- **Menus:** the reference main menu has ~108 entries; Avalonia has ~35. Missing groups: Select Special (7), Rooms submenu
  (rename, region, darkness ×3, start/end, shape ×4, join, swaps ×4), Connections submenu (line styles, up/down/in/out, reverse),
  Validation (4 toggles), export-to-clipboard (9), PDF export, Recent Maps, Smart Save, Open from Web, zoom presets, minimap
  toggle, Map Statistics (+export), Restore Default Map Settings, Online Help.
- **Canvas keys:** the reference handles ~40 key combos in `Canvas.OnKeyDown` (`$REF/src/Trizbort/UI/Controls/Canvas.cs`
  lines 1067–1429); Avalonia handles ~12. Nearly all single-letter keys (R, L, J, K, W, T, A, P, U/D/I/O, V, [, ], ;) and
  Ctrl/Shift+arrow room navigation are missing.
- **Shortcut differences:** Avalonia `Ctrl+Shift+S` = Save As (reference: Smart Save; Save As is `Ctrl+Alt+S`); Avalonia `F1` = About
  (reference: Online Help); Avalonia `Ctrl+0` reset zoom vs reference `0` = select start room.
- **Context menu:** reference has ~18 items; Avalonia has 6.
- **Dialogs with no Avalonia counterpart:** QuickFind, MapStatisticsView, RegionSettings, DisambiguateRooms, AutomapRoomSameDirection
  (documented gap), InputDialog (rename), OnlineHelp. RoomProperties and Map Settings are much smaller than the reference — likely
  missing fields and context.
- **App behaviour:** Avalonia never reads command-line `args`, so opening a map by double-click / file association likely doesn't work.
  No file-watcher reload.
- **Architecture:** editing logic in the reference lives in WinForms classes (`Domain/Controllers/CommandController.cs` takes a
  `Canvas`; `Canvas.cs` has `SelectAllRooms`, `SwapRoomNames`, `JoinSelectedRooms`, `AddOrConnectRoomRelativeToSelectedRoom`, …).
  This branch's `Trizbort.Core` has the domain, persistence, automap and exporters, but only Move/Resize editing (`MapEditing`).
  The Avalonia `MapCanvasControl` (67 KB) and `CommandService` hold selection, viewport, drag state and editing logic UI-side.
  Under L2 this moves into Core (D1).

All of the above is pre-loaded in `feature-matrix.md` as `Suspected-*` rows.

## 5. Phases

```
Phase 0  Setup              T0.1 baseline · T0.2 split solutions (legacy/ + new) · T0.4/T0.5 screenshots
Phase 1  Inventory          ≈13 tasks, cheap/mid, parallelisable (each edits only its own matrix section)
Phase 2  Verification       tests for "Implemented" rows, visual review, issue reproduction, smoke script
Phase 3  Decisions+tickets  D1–D4, then tickets; macOS future-work note
Phase 4  Implementation     editor-model foundation first, then feature tickets, one per session
```

## 6. Task cards

### Phase 0 — Setup

- [x] **T0.1 Record build and test baseline** · Cheap · Depends: none
  - Read: `AGENTS.md`.
  - Steps:
    1. `git rev-parse --short HEAD`; `dotnet --version`; OS.
    2. `dotnet build Trizbort.sln -c Debug` (Linux: add `-p:EnableWindowsTargeting=true`). Record warning count and errors.
    3. `dotnet test src/Trizbort.Core.Tests/Trizbort.Core.Tests.csproj` and `dotnet test src/Trizbort.Avalonia.Tests/Trizbort.Avalonia.Tests.csproj`.
       Record pass/fail/skip counts and failing test names.
    4. `dotnet run --project src/Trizbort.Avalonia`, open each `samples/*.trizbort`; note exceptions/console output.
  - Output: `avalonia-parity/baseline.md`. No source changes.

- [x] **T0.2 Split into legacy and new solutions** · Mid · Windows for step 6 · Depends: T0.1 · Approved by Lee 2026-10-30
  - Context: the original app at `v2.3.1-beta.2` is two projects (`src/Trizbort`, `src/Trizbort.Tests`), `net8.0-windows`, NuGet packages only,
    no dependency on `Trizbort.Core`. On this branch nothing references the refactored `src/Trizbort` except `src/Trizbort.Tests`.
    CI (`.github/workflows/tests.yml`, `release.yml`) currently builds, tests and publishes WinForms on `windows-latest`.
  - Steps (two commits so git sees a clean rename and `git log --follow` works):
    1. **Commit A — restore in place.**
       `dotnet sln Trizbort.sln remove src/Trizbort/Trizbort.csproj src/Trizbort.Tests/Trizbort.Tests.csproj`
       `git rm -r -q src/Trizbort src/Trizbort.Tests`
       `git checkout v2.3.1-beta.2 -- src/Trizbort src/Trizbort.Tests`
       Commit: `legacy: restore original WinForms app from v2.3.1-beta.2`.
    2. **Commit B — move.** `git mv src/Trizbort legacy/src/Trizbort` and `git mv src/Trizbort.Tests legacy/src/Trizbort.Tests`, then
       `dotnet new sln -n Trizbort.Legacy -o legacy` and
       `dotnet sln legacy/Trizbort.Legacy.sln add legacy/src/Trizbort/Trizbort.csproj legacy/src/Trizbort.Tests/Trizbort.Tests.csproj`.
    3. Fix relative paths the move broke, and nothing else: grep `legacy/src/Trizbort.Tests` (especially `TestEnvironment.cs`) and both `.csproj`
       files for `..\`, `../`, `tests/manual`, `samples`, `Docs`, `legacy/vendor` and add one `../` level. The fixtures stay at the repo-root
       `tests/manual/` (shared by both solutions).
    4. Write `legacy/README.md`: what this is, build/test commands, hotfix rules (minimal changes, add a test, bump version, update
       `changelog.md`), release by pushing a tag `legacy-v2.3.x`.
    5. CI:
       - `legacy.yml`: on push/PR with `paths: legacy/**`, `windows-latest`, set up .NET 8, `dotnet test legacy/Trizbort.Legacy.sln -c Release`.
       - `tests.yml`: `paths-ignore: legacy/**`; matrix `ubuntu-latest` + `windows-latest`; `dotnet test Trizbort.sln -c Release`.
       - `release.yml` → `release-legacy.yml` triggered by tags `legacy-v*`, paths updated to `legacy/src/Trizbort/...` (version stamping,
         publish). New `release.yml` triggered by tags `v*` (since renamed to `recast-v*`, decision D5): `dotnet publish src/Trizbort.Avalonia -c Release -r win-x64` and `-r linux-x64`
         (self-contained), version from project properties.
    6. Verify on Windows: `dotnet test legacy/Trizbort.Legacy.sln -c Debug` (needs the .NET 8 runtime) and `dotnet test Trizbort.sln -c Debug`;
       on Linux: `dotnet test Trizbort.sln`. Record counts in `avalonia-parity/baseline.md`.
    7. Check history: `git log --follow --oneline legacy/src/Trizbort/UI/MainForm.cs | tail -3` shows commits from before 2026.
    8. Docs: `AGENTS.md` (project list, both solutions, hotfix pointer), `README.md`, `agent-docs/README.md`, `01-architecture-overview.md`,
       `05-ui-and-canvas.md`, `09-build-test-and-dotnet8-port.md`, `tests/manual/README.md`. Mark `plans/windows-classic-parity-checklist.md`
       as superseded by this plan (keep the file). Commit C: `build: split legacy and new solutions; CI per solution`.
  - Done when: both solutions build and pass tests; history check passes; CI and docs updated.

- [x] **T0.4 WinForms dialog screenshots from the reference** · Mid · Windows · Depends: T0.2
  - Read: `$REF/src/Trizbort.Tests/RoomPropertiesDialogTests.cs`, `$REF/src/Trizbort.Tests/TestEnvironment.cs` (how tests create dialogs).
  - Steps: Add `legacy/src/Trizbort.Tests/DialogCaptureTests.cs`, skipped unless env var `TRIZBORT_CAPTURE_OUTPUT` is set (so the legacy
    test run is unaffected). It opens each dialog off-screen at 100 % DPI with `tests/manual/object-testing.trizbort` loaded and one
    room/connection/label selected, and saves PNGs via `Control.DrawToBitmap`:
    - every `$REF/src/Trizbort/UI/*Dialog.cs`, plus `QuickFind`, `MapStatisticsView`, `RegionSettings`, `InputDialog`;
    - **every tab** of tabbed dialogs;
    - dependent states: for each checkbox/radio that enables/disables other controls (grep `Enabled =` in the dialog's `.cs`), both states.
    Name files `<Dialog>__<tab>__<state>.png`; write them to `agent-docs/plans/avalonia-parity/screenshots/winforms/` with an `index.md`.
  - Done when: test and PNGs committed.

- [x] **T0.5 Avalonia dialog screenshots** · Mid · Depends: none
  - Read: `src/Trizbort.Avalonia.Tests/AvaloniaVisualCaptureTests.cs`, `DialogViewModelTests.cs`, `src/Trizbort.Avalonia/Services/DialogService.cs`.
  - Steps: Add `DialogCaptureTests.cs` to `src/Trizbort.Avalonia.Tests` (skipped unless `TRIZBORT_CAPTURE_OUTPUT` is set) rendering every dialog in
    `Views/Dialogs/` and `AboutDialog` headlessly, with the same map/selection, tabs, dependent states and naming as T0.4. Output to
    `agent-docs/plans/avalonia-parity/screenshots/avalonia/` plus `index.md`.

### Phase 1 — Inventory (fill `feature-matrix.md`)

- [x] **T1.1 Main menu and toolbar** · Cheap · Section M · Depends: T0.2
  - Read:
    - Reference menu text: `grep -oE 'this\._[A-Za-z0-9]+\.Text = "[^"]*"' $REF/src/Trizbort/UI/MainForm.Designer.cs`
    - Reference shortcuts: `grep -nE -A1 'ShortcutKeys =' $REF/src/Trizbort/UI/MainForm.Designer.cs`
    - Reference handlers: `grep -nE 'Click \+=' $REF/src/Trizbort/UI/MainForm.Designer.cs`, then each handler in `$REF/src/Trizbort/UI/MainForm.cs`
      (44 KB — locate with `grep -n`, read only that method).
    - Avalonia: `src/Trizbort.Avalonia/Views/MainWindow.axaml`, `src/Trizbort.Avalonia/ViewModels/MainWindowViewModel.cs`.
  - Steps: For every seeded M row confirm the Avalonia equivalent (bound in axaml **and** a `[RelayCommand]` that does something),
    compare shortcut, set Verified status + evidence. Resolve every `?` shortcut (the expression wraps onto the next line). Add missed rows.
    Note enable/disable differences (reference: grep `Enabled =` in `MainForm.cs`).
  - Done when: section M has no `Suspected-*` in "Verified status" and no `?`.

- [x] **T1.2 Canvas keyboard** · Cheap · Section K · Depends: T0.2
  - Read: `$REF/src/Trizbort/UI/Controls/Canvas.cs` lines 1067–1429 (`OnKeyDown`) and 2677–2681 (`OnPreviewKeyDown`); `CtrlArrowHandler`,
    `ShiftArrowHandler`, `MoveArrowKeyHandler` (grep, read only those). Avalonia: `grep -n 'Key\.' src/Trizbort.Avalonia/Controls/MapCanvasControl.cs`,
    then only `OnKeyDown`/`HandleArrowKey`; and `Window.KeyBindings` in `MainWindow.axaml`.
  - Steps: One K row per distinct key+modifier in the reference (split grouped seed rows). Fill the F-key rows (lines ~1337–1428). For each:
    does Avalonia handle it? Does a window-level `KeyBinding` also claim it (conflict)? Is there an obvious modern convention (Notes, for D4)?
    Reference `Ctrl+E` sets `SquareCorners` — probable reference bug: flag, don't copy.
  - Done when: section K complete with evidence.

- [x] **T1.3 Canvas mouse gestures and context menu** · Mid · Sections C and G · Depends: T0.2
  - Read: `$REF/src/Trizbort/UI/Controls/Canvas.Designer.cs` (grep `Text =` and `Click +=` only); in `$REF/…/Canvas.cs`: `OnMouseClick` (1431),
    `OnMouseDoubleClick` (1445), `OnMouseDown` (1465), `OnMouseMove` (1495), `OnMouseUp` (1505), `OnMouseWheel` (1518), `CtxCanvasMenuOpening` (1830),
    `BeginDrag*`/`DoDrag*`/`EndDrag` (grep). Avalonia: `MapCanvasControl.cs` `OnPointerPressed/Moved/Released/WheelChanged`, `ShowContextMenuAt`;
    `agent-docs/05-ui-and-canvas.md` Avalonia section.
  - Steps: Fill section C including **when** each item shows/enables. Add G rows: click/ctrl/shift select, ctrl-click on reference room (jumps to
    target), marquee, drag room/label, resize handles, draw connection from port, drag from port to empty space, move connection end, move/add
    waypoint, pan, wheel zoom (about cursor?), double-click element vs empty canvas, hover tooltips, cursor shapes. Right-drag pan = `Different`, intentional.

- [x] **T1.4a–g Dialog review (fields + context)** · Mid (must read images) · Section D + `dialogs/<name>.md` · Depends: T0.4, T0.5
  - Per dialog read: its screenshots in both `screenshots/` folders; reference `.cs` (+ `.Designer.cs` via grep for `Text =`, `ToolTip`, `Enabled`,
    `Visible`, control declarations); Avalonia `.axaml` + ViewModel.
    a Room properties (+ `ObjectListEditor`) · b Connection properties · c Label properties · d Map settings (`SettingsDialog`, `RegionSettings`) ·
    e App settings · f Automap (`AutomapDialog`, `AutomapRoomSameDirectionDialog`, `DisambiguateRoomsDialog`) ·
    g No counterpart (`QuickFind`, `MapStatisticsView`, `InputDialog`, `OnlineHelpDialog`): one-paragraph spec each (purpose, inputs, outputs, Core API).
  - Output `dialogs/<Name>.md`:
    1. **Field table**: `Field | Reference (tab, control, label text) | Avalonia (control, binding, label text) | Status | Notes`.
    2. **Context checklist** — each Pass / Lost / Changed + note: grouping (group boxes/headings), label wording and units, tooltips/help text,
       enable/disable dependencies (same control greys out under the same condition?), defaults, order and tab order, which tab a field lives on,
       previews (colour/font samples), validation messages, OK/Cancel/Apply, Enter/Esc, size/resizability.
    3. **Recommendation** for Lee: keep reference layout, keep Avalonia layout, or a specific merge (one paragraph).
  - Done when: every labelled reference control is in the table and every checklist item has a verdict; D row updated.

- [x] **T1.5 Application behaviour** · Mid · Section A · Depends: T0.2
  - Read: `$REF/src/Trizbort/Program.cs`, `$REF/src/Trizbort/Domain/Application/CommandLineOptions.cs`, `$REF/src/Trizbort/UI/MainForm.cs`
    (non-menu methods via grep: load/closing, title, recent files, file watcher, drag-drop, themes), `$REF/src/Trizbort/Domain/StatusBar/*`,
    `agent-docs/08-cli-and-entry-point.md`; Avalonia `Program.cs`, `App.axaml.cs`, `Views/MainWindow.axaml.cs`, `ViewModels/MainWindowViewModel.cs`.
  - Steps: Verify each A row; add rows for anything else at startup/shutdown (settings, window geometry, single instance, backup, CLI export).

- [x] **T1.6 Editing and interaction logic outside Core** · Mid · Section X · Depends: T0.2
  - Read: `$REF/src/Trizbort/Domain/Controllers/*.cs`, `$REF/src/Trizbort/Domain/Commands/*.cs`; in `$REF/…/Canvas.cs` only methods matching
    `Select*`, `Swap*`, `Join*`, `*Handler`, `Paste*`, `ReverseLineDirection`, `ToggleText`, `CopySelectedColor`, `ApplyNewPlainConnectionSettings`,
    `AddOrConnectRoomRelativeToSelectedRoom`, `GetRoomIn*Direction*`, `ResizeRoom`, `DeleteSelection` (grep -n). This branch: `src/Trizbort.Core`
    (grep same names), `src/Trizbort.Avalonia/Services/CommandService.cs`, and non-rendering parts of `MapCanvasControl.cs`
    (`HitTest*`, `OnPointer*`, `EndDrag`, `HandleArrowKey`, `CanvasDragMode`).
  - Steps: One X row per piece of editing/interaction logic that is (a) only in the reference UI or (b) in Avalonia UI code rather than Core.
    Columns: what it does, Core equivalent?, matrix rows needing it, effort to put in Core (S < 1 h, M < ½ day, L larger), UI dependencies to
    abstract (`Keys`, text measurement, `MessageBox`, dialogs).
  - Done when: section X complete. Main input to D1.

- [x] **T1.7 Export and Automap behaviour** · Mid · rows in M/A · Depends: T0.2
  - Read: `agent-docs/06-automap.md`, `agent-docs/07-export-subsystem.md`, `src/Trizbort.Avalonia/Services/StorageService.cs`; reference
    `$REF/src/Trizbort/UI/MainForm.cs` (grep `Export`) and `$REF/src/Trizbort/Export/*.cs`.
  - Steps: Default file names / folder memory per language, overwrite prompts, clipboard variants, PDF (reference uses PdfSharp — is a
    cross-platform PDFsharp usable, or should PDF come from Skia?), image options, automap start/stop, disambiguation, same-direction, status messages.

- [x] **T1.8 Consolidate** · Cheap · Depends: T1.1–T1.7
  - Fill Summary counts; list open questions and remaining `Suspected-*` under "Open questions" at the top of `feature-matrix.md`.

### Phase 2 — Verification

- [ ] **T2.1 Test coverage for "Implemented" rows** · Mid · split T2.1-M, -K, -C/G, -D
  - For each `Implemented` row without test evidence, write a small headless test (`[AvaloniaTest]`; patterns in `MapCanvasControlGestureTests.cs`,
    `DialogViewModelTests.cs`). If it fails: `Broken` + `issues.md` entry; don't fix.
- [ ] **T2.2 Map rendering review** · Mid (images) · 8 batches of ~14 fixtures
  - Read `artifacts/parity/2026-10-19/captures/comparison-summary.csv` and the batch's PNG pairs. Those "classic" captures came from the refactored
    WinForms on this branch: if a difference might be a WinForms regression, open the fixture in the `$REF` app to decide. Log differences in
    `issues.md` (category `render`). Ignore anti-aliasing/hinting.
- [ ] **T2.3 Reproduce Lee's teething issues** · Mid · one card per `issues.md` entry
  - Reproduce (failing headless test if possible), locate cause, record `file:line` and proposed fix. Fix only if flagged `fix-now`.
- [x] **T2.4 Manual smoke-test script** · Cheap · Depends: T1.8
  - `smoke-test-script.md`: ~30-minute checklist per OS (**Windows and Linux**), grouped File / Edit / Canvas / Dialogs / Export / Automap, each
    step with expected result and `[ ] pass / fail / notes`. Use `tests/manual/` fixtures.

### Phase 3 — Decisions and tickets

- [x] **D1 Editor model in Core** — *accepted, see `decisions.md`* · Strong · Input: matrix section X, §7 below, `agent-docs/01-architecture-overview.md`, `02-domain-model.md`,
  `src/Trizbort.Avalonia/Services/ICommandService.cs`, the `CanvasDragMode` part of `MapCanvasControl.cs`.
  Output: ADR in `decisions.md` (≤ 2 pages) plus ticket outlines for an `EditorSession` in `Trizbort.Core` that owns:
  - `Project`, `Selection`, `Viewport` (origin, zoom, size; canvas↔client transforms; fit; zoom-about-point);
  - **every edit as a command** with `Execute`/`Undo` and a grouping rule (one undo step per drag, per dialog OK, per paste); `UndoStack`
    integrated with the dirty flag (L3);
  - **input-neutral interaction**: hit-testing and the drag state machine consume platform-neutral pointer/key events (canvas coordinates,
    buttons, modifiers) and emit commands plus hover/adorner state for rendering; the UI only converts events and draws;
  - abstractions for what Core can't do itself: text measurement, clipboard, async prompts/dialogs, file pickers;
  - migration order that keeps the app working between tickets, and the test strategy (Core tests for commands and interaction;
    Avalonia tests shrink to rendering and wiring).
- [x] **D2 Command catalogue** — *accepted, see `decisions.md`* · Strong · Depends: D1. One registry in Core: id, display name, menu path, default gesture written platform-neutrally
  (`Primary+S`; `Primary` = Ctrl on Windows/Linux, Cmd on macOS), CanExecute, handler → editor command. Avalonia generates main menu, macOS
  `NativeMenu`, context menus, toolbar and key bindings from it. Include a test that fails on duplicate gestures. ADR + skeleton ticket.
- [x] **D3 Async prompts from Core** — *accepted, see `decisions.md`* · Strong · Automap same-direction/disambiguation and any other Core→user question: async callback vs
  background loop + dispatcher. ADR.
- [x] **D4 Shortcut map** — *approved, see `avalonia-parity/shortcut-map.md` §5* · Mid drafts, Lee approves · Depends: T1.1, T1.2. Table `Command | Reference gesture | Proposed gesture | Reason`.
  Policy (L4): keep the reference gesture unless an obvious modern convention exists. Candidates: Save As `Primary+Shift+S` (Smart Save then needs a
  new gesture), Undo `Primary+Z`, Redo `Primary+Y` and `Primary+Shift+Z`, Help `F1`, Find `Primary+F`, Settings `Primary+,`, Zoom
  `Primary+=`/`Primary+-`/`Primary+0`, `Primary+wheel` zoom, `Primary+W` (conventionally Close; reference uses it for Swap Names — flag), Delete
  *and* Backspace delete, right-drag pan (keep). Lee marks each row approve/change.
- [x] **T3.1 Write tickets** — *107 tickets, index in `avalonia-parity/tickets/README.md`* · Mid · Depends: T1.8, D1, D2, D4. One ticket per `Missing`/`Partial`/`Broken` row or tight group, ≤ ~1 day each,
  template §8. Order: editor-model foundation → command catalogue → undo/redo UI → feature tickets → dialog fixes → macOS code-level tickets.
- [ ] **T3.2 Prioritise** · Lee · P1 / P2 / P3 / `Won't-port`.
- [ ] **T3.3 macOS code-level tickets** · Mid · Depends: D2. Cmd mapping (from the catalogue); `NativeMenu` with the standard app menu (About,
  Settings, Quit) generated from the catalogue; files opened from Finder (Avalonia activation / `UrlsOpened`) sharing the command-line open path.
  Expected size: 1–2 small tickets. Mark them "not verifiable without a Mac".
- [ ] **T3.4 macOS future-work note** · Mid · `macos-future-work.md`: `.app` bundle and `Info.plist` (incl. `.trizbort` document type/UTI),
  `dotnet publish -r osx-arm64`/`osx-x64` and universal binary, code signing, notarisation (Apple Developer account needed), `.icns` icon,
  `macos-latest` CI runner, manual test list for a real Mac.

### Phase 4 — Implementation

One ticket per session, in T3.1's order. Size S/M → mid tier; L → mid tier plus a strong-tier review of the diff. After each ticket update the
matrix row(s) with evidence and commit. When undo/redo lands, update the "No undo/redo exists" line in `AGENTS.md`.

## 7. Notes for D1 (so the strong-model session starts informed)

- Avalonia has `ICommandService`/`CommandService` (selection, zoom, origin, add/delete/copy/paste) and a 67 KB `MapCanvasControl` with its own
  hit-testing and `CanvasDragMode` state machine — good first candidates to lift into Core nearly as-is.
- Core already has `CanvasTransform`, `MapEditing.Move/Resize`, `CopyController`, `Project` change events and `IAutomapCanvas` — reuse them.
- Rendering adapters (`src/Trizbort.Avalonia/Drawing/*`) stay UI-side and should read only model + adorner state.
- Undo needs every mutation to go through commands, including dialog OKs (dialog VMs' `ApplyTo` should produce a command) and Automap
  (one undo step per automap batch, or excluded — decide).

## 8. Ticket template

```markdown
# NNN — <Imperative title>

Matrix rows: M031, K005          Priority: P?          Size: S | M | L
Depends on: NNN (or none)        Suggested tier: mid

## What
One or two sentences on the user-visible behaviour, as in the reference app (plus any approved D4 modernisation).

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `SwapRoomNames()` (grep -n to locate)
- `$REF/src/Trizbort/UI/MainForm.cs` — `SwapNamesToolStripMenuItemClick`

## Implement
1. Core: editor command `<Name>Command` in `<file>` (Execute/Undo), registered in the catalogue as `<id>` with gesture `<gesture>`.
2. Avalonia: nothing beyond catalogue wiring, unless: <dialog / special rendering>.
3. Enabled when <condition>.

## Tests
- Core: `src/Trizbort.Core.Tests/<File>.cs` — execute, undo, redo, CanExecute.
- Avalonia (only if UI-specific): `src/Trizbort.Avalonia.Tests/<File>.cs`.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(NNN): <title>`.

## Out of scope
- …
```

## 9. Issue log format (`issues.md`)

```markdown
## I-NNN <short title>
- Reported: <date> by <Lee/agent>   OS: <Windows/Linux>   Category: crash | behaviour | render | usability | dialog-context | performance
- Steps: 1. … 2. …
- Expected: …   Actual: …
- Severity: high | medium | low     Flags: fix-now?
- Investigation (T2.3): cause at `file:line`; proposed fix …
- Ticket: NNN
```

## 10. Suggested order and rough cost

1. T0.1 (cheap), then T0.2 (mid; approved).
2. In parallel: T0.4 (Windows), T0.5, T1.1, T1.2, T1.5, **T1.6** (unblocks D1).
3. D1 → D2 (strong, short). D4 draft once T1.1/T1.2 are done.
4. T1.3, T1.4a–g (after screenshots), T1.7 → T1.8.
5. T2.4 → Lee runs the smoke script on Windows and Linux and logs issues → T2.3 per issue. T2.1/T2.2 alongside.
6. T3.1 → T3.2 → Phase 4, foundation tickets first.

Roughly 30 sessions before implementation; 3 need the strong tier.

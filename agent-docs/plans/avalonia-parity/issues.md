# Avalonia teething issues

Log bugs and usability problems here (not missing features — those go in `feature-matrix.md`).
Format is defined in `../avalonia-parity-review.md` §9. Add `Flags: fix-now` to anything you want fixed straight away.

## I-001 "Load last project on startup" setting has no effect
- Reported: 2026-10-30 by agent (T1.5, code reading)   OS: all   Category: behaviour
- Steps: 1. App Settings → tick "Load Last Opened Project on Startup". 2. Open a map, close the app. 3. Start the app.
- Expected: the last map opens (reference: `legacy/src/Trizbort/UI/MainForm.cs` ~196–199, 652–664).   Actual: empty map.
- Cause: the setting is shown and saved (`src/Trizbort.Avalonia/ViewModels/AppSettingsViewModel.cs:64,86`) but nothing reads it at startup (`App.axaml.cs`). Matrix A014.
- Severity: medium     Flags: —
- Ticket: —

## I-002 Status-bar cursor position never updates
- Reported: 2026-10-30 by agent (T1.5, code reading)   OS: all   Category: behaviour
- Steps: move the mouse over the canvas.
- Expected: X/Y readout follows the cursor.   Actual: always "X: 0, Y: 0".
- Cause: `CursorPositionText` (`src/Trizbort.Avalonia/ViewModels/MainWindowViewModel.cs:52`) is bound in `Views/MainWindow.axaml:172` but never assigned. Matrix A020.
- Severity: low     Flags: —
- **Fixed** 2026-10-30: the session tracks the pointer's map position (`EditorChange.Pointer`, no redraw); the status bar shows it and clears it when the pointer leaves the canvas. Tests: `CursorPositionTests`, `ViewportPointerTests`.
- Ticket: 111

## I-003 Changes to the open map on disk are silently ignored
- Reported: 2026-10-30 by agent (T1.5, code reading)   OS: all   Category: behaviour
- Steps: 1. Open a map. 2. Edit and save the same file in another program.
- Expected: prompt "map modified by another program, reload?" (reference `legacy/src/Trizbort/Domain/Watchers/TrizbortFileWatcher.cs:55–67`).   Actual: nothing happens; the watcher re-arms.
- Cause: Core's `TrizbortFileWatcher` is armed on load, but Avalonia never sets `ConfirmReloadPrompt`, `UIThreadInvoker` or `Project.MapReloader`. Matrix A005.
- Severity: medium     Flags: —
- **Fixed** 2026-10-30: Avalonia wires `ConfirmReloadPromptAsync`, `UIThreadInvoker` and `Project.MapReloader` (`FileWatcherWiring`); the watcher is debounced, ignores the app's own saves, handles save-by-rename, and no longer subscribes twice. Tests: `FileWatcherTests`.
- Ticket: 112

## I-004 Ctrl+Shift+A selects everything instead of the selected rooms' regions
- Reported: 2026-10-30 by agent (T1.2, code reading)   OS: all   Category: behaviour
- Cause: `case Key.A when isCtrl` in `src/Trizbort.Avalonia/Controls/MapCanvasControl.cs:1239` also catches Ctrl+Shift+A. Reference selects all rooms in the selected rooms' regions. Matrix K004.
- Severity: low     Flags: —
- Ticket: —

## I-005 "Show Canvas Tooltips" master toggle does nothing
- Reported: 2026-10-30 by agent (T1.4e, code reading; verified)   OS: all   Category: behaviour
- Steps: App Settings → Tooltips → untick "Show Canvas Tooltips" → OK → hover a room.
- Expected: no tooltips.   Actual: tooltips still shown; the setting reverts next time the dialog opens.
- Cause: `src/Trizbort.Avalonia/ViewModels/AppSettingsViewModel.cs:72` reads `ShowTooltips` in `Initialize`, but `ApplyTo`
  never writes it (it is a computed read-only property in settings). See `dialogs/AppSettings.md`.
- Severity: low     Flags: —
- **Fixed** 2026-10-30: the App settings Tooltips tab has a working Show Canvas Tooltips master (untick saves and clears the children; re-tick restores them) and dependent controls grey out. Smoke S5_45 un-ignored; tests in `DialogViewModelTests`.
- Ticket: 081

## I-006 App settings shown but ignored by the Avalonia app
- Reported: 2026-10-30 by agent (T1.4e, code reading)   OS: all   Category: behaviour
- Settings saved but never read anywhere under `src/Trizbort.Avalonia`: `InvertMouseWheel`, `ShowFullPathInTitleBar`,
  `ApplyStyleToNewRooms` (and `LoadLastProjectOnStart`, see I-001). Details per setting in `dialogs/AppSettings.md`.
- Severity: medium (users will think settings are broken)     Flags: —
- Progress 2026-10-30: `ShowFullPathInTitleBar` is now honoured (ticket 114). `ApplyStyleToNewRooms` → ticket 115; `InvertMouseWheel` still open.
- Ticket: 114 (partial), 115

## I-007 Renaming or deleting a region leaves rooms with the old region name
- Reported: 2026-10-30 by agent (T1.4d, code reading; verified)   OS: all   Category: behaviour (data)
- Steps: Map Settings → Regions → rename a region used by rooms → OK.
- Expected: rooms move to the renamed region (reference updates them: `legacy/src/Trizbort/UI/SettingsDialog.cs:596`;
  delete moves them to the default region: `:359`).   Actual: `MapSettingsViewModel.ApplyTo` just rebuilds
  `Settings.Regions` (`src/Trizbort.Avalonia/ViewModels/MapSettingsViewModel.cs` ~276), so rooms keep a region name
  that no longer exists and lose their colour.
- Severity: high (silently changes the saved map)     Flags: fix-now (Lee, 2026-10-30)
- **Fixed** 2026-10-30: `MapSettingsViewModel` tracks each region's original name and remaps rooms in one pass on OK (rename → new name, delete → default region; swapped names work). Tests: `DialogViewModelTests.MapSettingsViewModel_RenameAndDeleteRegion_UpdatesRooms`, `…_SwappedRegionNames_MoveRoomsCorrectly` (both fail without the fix).
- Ticket: —

## I-008 Automap never asks about ambiguous or conflicting rooms
- Reported: 2026-10-30 by agent (T1.4f, code reading)   OS: all   Category: behaviour (data)
- Expected: reference asks which room was meant (`DisambiguateRoomsDialog`) and what to do when two rooms claim the
  same exit (`AutomapRoomSameDirectionDialog`).   Actual: Core defaults silently pick the first candidate / keep room 1,
  so maps can be merged wrongly without the user knowing. Fix via decision D3 (async prompts). See `dialogs/Automap.md`.
- Severity: medium     Flags: —
- **Fixed** 2026-10-30: `AvaloniaEditorPrompts` shows the same-direction and disambiguation dialogs; smoke S7_63 un-ignored. Tests: `AutomapPromptDialogTests`.
- Ticket: 012

## I-009 Adding elements one by one gets very slow on big maps
- Reported: 2026-10-30 by agent (undo benchmark, measured on Windows)   OS: all   Category: performance
- Evidence: building a 1,000-room / 1,936-connection map with `Project.Elements.Add` took **11 s**, while loading the
  same map from file took **0.13 s** (`scripts/bench/undo-snapshot-bench.cs`). Something in the add path scales badly
  (likely per-add change handling). Affects automap and paste on large maps.
- Severity: medium     Flags: —
- Ticket: —

## I-010 Esc during a room drag does not put the room back
- Reported: 2026-10-30 by agent (smoke test S4_33)   OS: all   Category: behaviour
- Steps: drag a room, press Esc before releasing.   Expected: drag cancelled, room returns (Avalonia already does this
  for connection-end drags).   Actual: drag ends but the room stays where it was dragged. The reference app has no drag
  cancel, so this is an improvement, not parity. Will fall out of undo transactions (D1).
- Severity: low     Flags: —
- Ticket: —

## I-011 Settings are saved to the current folder
- Reported: 2026-10-30 by agent (manual smoke run)   OS: Windows (and all)   Category: behaviour
- Steps: start the app from any folder (here the repo root), change nothing, exit.   Expected: settings in the per-user
  config folder (e.g. `%APPDATA%\Trizbort`, `~/.config/trizbort`).   Actual: `appsettings.json` written into the
  working directory (matrix A012). Pollutes repos and fails in read-only folders.
- Severity: medium     Flags: —
- Ticket: —

## I-012 Avalonia draws only 4 of the 8 second-fill locations
- Reported: 2026-10-30 by agent (ticket 071)   OS: all   Category: render
- The Room dialog now offers all 8 Second Fill locations (as the reference does), but
  `src/Trizbort.Avalonia/Drawing/RoomDrawingAdapter.cs` `GetSecondFillPolygon` only builds polygons for 4 of them, so the
  other 4 render with no second fill. Compare with the reference `legacy/src/Trizbort/Domain/Elements/RoomDrawingExtensions.cs`.
- Severity: medium     Flags: —
- Ticket: —

## I-013 Label dialog Shape list shows raw enum names
- Reported: 2026-10-30 by integrator (ticket 076 screenshot)   OS: all   Category: dialog
- The Label dialog's Shape combo shows `SquareCorners` etc. instead of friendly names ("Square corners").
- Severity: low     Flags: —
- Ticket: —

## I-014 Old menus stay subscribed to the editor session
- Reported: 2026-10-30 by integrator (flaky test)   OS: all   Category: behaviour
- `CatalogueMenuBuilder.Track` subscribes to `EditorSession.Changed` and never unsubscribes, so menus that are rebuilt or
  discarded keep refreshing. A test hit this as a cross-thread update; refreshes are now marshalled to the UI thread, but
  the subscription should be tied to the menu's lifetime (unsubscribe when detached).
- Severity: low     Flags: —
- Ticket: —

<!-- Next ID: I-015 -->

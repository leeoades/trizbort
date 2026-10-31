# Avalonia teething issues

Log bugs and usability problems here (not missing features — those go in `feature-matrix.md`).
Format is defined in `../avalonia-parity-review.md` §9. Add `Flags: fix-now` to anything you want fixed straight away.

## I-001 "Load last project on startup" setting has no effect
- Reported: 2026-10-30 by agent (T1.5, code reading)   OS: all   Category: behaviour
- Steps: 1. App Settings → tick "Load Last Opened Project on Startup". 2. Open a map, close the app. 3. Start the app.
- Expected: the last map opens (reference: `legacy/src/Trizbort/UI/MainForm.cs` ~196–199, 652–664).   Actual: empty map.
- Cause: the setting is shown and saved (`src/Trizbort.Avalonia/ViewModels/AppSettingsViewModel.cs:64,86`) but nothing reads it at startup (`App.axaml.cs`). Matrix A014.
- Severity: medium     Flags: —
- **Fixed** 2026-10-30: `StartupActions` opens the last project when the setting is on (a command-line file wins). Tests: `StartupLastProjectTests`.
- Ticket: 103

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
- Progress 2026-10-30: `ShowFullPathInTitleBar` is now honoured (ticket 114). `ApplyStyleToNewRooms` honoured (ticket 115, `RoomFactory`); `InvertMouseWheel` still open.
- **Fixed** 2026-10-30: all three settings are honoured: full path in title (114), apply style to new rooms (115), invert mouse wheel (`CanvasInteraction.OnWheel`, 051).
- Ticket: 114, 115, 051

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
- **Fixed** 2026-10-30: drags run in Core `CanvasInteraction` inside one undo transaction; Esc or lost pointer capture cancels it, restoring the pre-drag map and recording nothing. Smoke S4_33 un-ignored; tests in `CanvasInteractionTests`, `MapCanvasControlGestureTests`.
- Ticket: 006

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
- **Fixed** 2026-10-31: `RoomDrawingAdapter.GetSecondFillPolygon` builds all 8 locations from the reference geometry (`legacy/src/Trizbort/Domain/Elements/Room.cs`); tests in `DrawingAdaptersParityTests`.
- Ticket: —

## I-013 Label dialog Shape list shows raw enum names
- Reported: 2026-10-30 by integrator (ticket 076 screenshot)   OS: all   Category: dialog
- The Label dialog's Shape combo shows `SquareCorners` etc. instead of friendly names ("Square corners").
- Severity: low     Flags: —
- **Fixed** 2026-10-30: the Shape combo uses `EnumDisplay.Converter`.
- Ticket: 078 batch

## I-014 Old menus stay subscribed to the editor session
- Reported: 2026-10-30 by integrator (flaky test)   OS: all   Category: behaviour
- `CatalogueMenuBuilder.Track` subscribes to `EditorSession.Changed` and never unsubscribes, so menus that are rebuilt or
  discarded keep refreshing. A test hit this as a cross-thread update; refreshes are now marshalled to the UI thread, but
  the subscription should be tied to the menu's lifetime (unsubscribe when detached).
- Severity: low     Flags: —
- **Fixed** 2026-10-30: `CatalogueMenuBuilder` ties each menu's session subscription to its attach/detach (`MenuRefresher`); context menus listen only while open.
- Ticket: 066

## I-015 Status-bar messages gone for catalogue commands
- Reported: 2026-10-30 by integrator (037/038/040 wiring)   OS: all   Category: behaviour
- Copy, cut, paste, delete, select all, add room/label used to post "Copied…", "Deleted…" etc. to the status bar through
  MainWindowViewModel. They now run as catalogue commands and post nothing. `EditorSession.StatusMessage` /
  `StatusMessagePosted` exists (ticket 033 uses it for Cycle Port Detail) but no UI shows it. Wire the status bar to it
  and post messages from the Core commands.
- Severity: low     Flags: —
- **Fixed** 2026-10-30: Core commands post status messages via `EditorSession.PostStatusMessage`; the main window status bar shows them.
- Ticket: 052 batch

## I-016 Toggle Text (Primary+F4) has no visible effect
- Reported: 2026-10-30 by agent (ticket 054)   OS: all   Category: render
- The command flips `DebugDisableTextRendering`, but the Avalonia renderer never reads it.
- Severity: low     Flags: —
- **Fixed** 2026-10-30: the Avalonia adapters skip text when `DebugDisableTextRendering` is set (`Drawing/TextRenderingGate.cs`).
- Ticket: 052 batch

## I-017 Project validation toggles are not undoable
- Reported: 2026-10-30 by agent (ticket 036)   OS: all   Category: behaviour   Flags: needs-decision
- The four Must* flags are project state outside the map snapshot, so toggling them records no undo step and undo
  carries them over. The reference had no undo at all. Decide: leave as is, or add them to the snapshot.
- Severity: low
- Ticket: —

## I-018 Set Region is a picker, not a submenu with the current region ticked
- Reported: 2026-10-30 by agent (ticket 024)   OS: all   Category: menu
- The catalogue has no dynamic child items, so the context menu shows "Set Region…" opening a picker. The reference
  shows a submenu of regions with a tick. Recent Maps (058) solved dynamic items with a dedicated control
  (`Controls/RecentMapsMenu.cs`); generalise that into the catalogue builder and use it here.
- Severity: low     Flags: —
- **Fixed** 2026-10-30: `EditorCommand.DynamicChildren`; "Set Region" is a submenu with the current region ticked. Recent Maps keeps its own control (reads `IRecentFilesService`).
- Ticket: 066

## I-019 Three smoke tests use an outdated harness
- Reported: 2026-10-30 by integrator   OS: all   Category: tests
- S1_08 (reload prompt), S1_09 (load last project) and S7_62 (ambiguous room) check through the old fake dialog service
  or startup path, so they fail although the features work (covered by FileWatcherTests, StartupLastProjectTests and
  AutomapPromptDialogTests). Rewrite them against IEditorPrompts / StartupActions(settings) and remove the [Ignore].
- Severity: low     Flags: —
- **Fixed** 2026-10-31: S1_08, S1_09 and S7_62 rewritten against FileWatcherWiring, StartupActions(settings) and IEditorPrompts; [Ignore] removed.
- Ticket: —

## I-020 Primary+Backspace deletes on every OS; the shortcut map lists it for macOS only
- Reported: 2026-10-31 by agent (ticket 067 conformance tests)   OS: Windows/Linux   Category: keys
- `edit.deleteBackspace` binds Backspace and Primary+Backspace everywhere. Harmless (Ctrl+Backspace is unused on the
  canvas), but it differs from the approved map. Decide: keep everywhere, or restrict Primary+Backspace to macOS.
- Severity: low     Flags: needs-decision
- **Decided** 2026-10-31 (Lee): keep Backspace and Primary+Backspace on every platform; shortcut map updated (K047). No code change.
- Ticket: —

## I-021 17 build warnings to clean up
- Reported: 2026-10-31 by integrator (full rebuild)   OS: all   Category: code quality
- CA1416 on System.Drawing FontStyle.Bold/Italic in MapSettingsViewModel and DialogViewModelTests (harmless enum values, but
  noisy on Linux/macOS builds); CS0618 PdfDocumentOpenMode.ReadOnly in PdfExportTests (use Import); CA2024 reader.EndOfStream
  in async Automap code; CS0219 unused `primary` in CommandCatalogue.View.cs; NUnit1033 in ShortcutMapConformanceTests;
  CS8604 in two smoke tests. Fix them and consider TreatWarningsAsErrors for src/ so agents catch them.
- Severity: low     Flags: —
- Ticket: —

## I-022 Status bar room and connection counts stay at 0
- Reported: 2026-10-31 by integrator (manual run on Windows)   OS: all   Category: behaviour
- Added two rooms with R: the status bar still read "Rooms: 0 | Connections: 0". The counts only refresh on project change.
- Severity: low     Flags: —
- **Fixed** 2026-10-31: MainWindowViewModel recounts on EditorChange.Model (StatusBarCountsTests).
- Ticket: 132

## I-023 Room context menu groups are interleaved
- Reported: 2026-10-31 by integrator (manual run)   OS: all   Category: menu
- Right-click on a room: "Map Settings…" sits between Toggle Darkness and Start Room, "Map Theme…" between Start Room and
  End Room, "Application Settings…" after End Room. Commands from different partials share ContextOrder/ContextGroup values.
  Expected: the reference grouping (room edits, then map/app settings together at the end).
- Severity: medium     Flags: —
- **Fixed** 2026-10-31: settings commands use context group 9-settings, after the room groups (ContextMenuSettingsOrderTests).
- Ticket: 132

## I-024 Room tooltip shows "NoRegion" for rooms in the default region
- Reported: 2026-10-31 by integrator (manual run)   OS: all   Category: tooltip
- The reference header shows the region only when it is not the default (`GetToolTipHeader`, `!IsDefaultRegion()`).
- Severity: low     Flags: —
- **Fixed** 2026-10-31: the tooltip footer omits the default region (RoomTooltipRegionTests).
- Ticket: 132

## I-025 Map rendering differs from the reference (GitHub #22) — found by the export comparison
- Reported: 2026-10-31 by integrator (scripts/compare, 123 maps, legacy vs new PNG/PDF)   OS: all   Category: render
- R1 Region text colour not applied: room text in a region keeps the default colour (e.g. dark blue on blue; basics-159x).
- R2 One-way connections draw several arrowheads (">>>>"); the reference draws one (basics-1591 "1-way from S, to E").
- R3 Dotted connections are drawn dashed; the reference draws fine dots (spiral, basics).
- R4 Map labels get a white box and border by default; the reference draws plain text (spiral "Fall to Carriage").
- R5 Dark-room marker: ignores the map's dark-tab size (dark-tab-48/96) and sits in the wrong corner (top-left vs top-right).
- R6 Room names wrap inside narrow rooms ("Cav / e"); the reference keeps them on one line (dark-tab-96).
- R7 Some connection routes differ (spiral Carriage → Mountainside: reference vertical with square corners, new slanted).
- R8 Maps containing only connections export nothing ("map is empty"); the reference draws them (horizontal/vertical-connector-only).
- Evidence: run the capture tests and `scripts/compare/compare_exports.py`; report index sorted by score (worst: dark-tab-96 18.5%).
- Severity: high (visible on every map)     Flags: —
- **Fixed** 2026-10-31: R1–R8 fixed (RenderingParityTests, ImageExportPlannerTests; R2/R7: removed a port-only special case in Connection.GetSegments that split stalks through their midpoints — segments now match the reference). Re-run of the comparison: worst 18.5% → 15.1%, mean 4.9% → 4.3%, 22 maps better, code-export files differing 636 → 137 (only Unicode and the random Quest game id remain). Remaining systematic offset: I-026.
- Ticket: 132

## I-026 Exported images are 17 px smaller than the reference on most maps
- Reported: 2026-10-31 by integrator (export comparison: 78 of 123 maps differ by exactly 17 px each way)   OS: all   Category: export
- The overhang margin (`ImageExportPlanner.DefaultPadding`) estimates line height as size × 1.2; the reference uses the real
  GDI line height (`Font.GetHeight()`, legacy Canvas.cs:523-526). Measure the actual font line spacing (pass a measurer from
  Avalonia) so exports keep the reference margins. Also shifts the whole picture, inflating comparison scores.
- Severity: low     Flags: —
- **Fixed** 2026-10-31: the planner asks the UI for the real line spacing (`ImageExportPlanner.LineHeightMeasurer`, set to `DrawingAdapters.MeasureLineHeight`). Exported sizes now match on 120 of 123 maps (was 21); comparison mean 4.3% → 0.6%, median 0.2%. Three font-fallback maps (unavailable-font, the two Arial Narrow maps) still differ by 17 px.
- Ticket: —

## I-027 Esc needs pressing twice to cancel a room drag or a connection drag
- Reported: 2026-10-31 by user (manual checklist, Linux laptop, COSMIC)   OS: linux   Category: interaction
- Esc needs pressing twice to cancel a room drag or a connection drag
- Severity: medium     Flags: —
- Ticket: 133

## I-028 Selected dotted/dashed connections draw solid, hiding the style change
- Reported: 2026-10-31 by user (manual checklist, Linux laptop, COSMIC)   OS: linux   Category: rendering
- Selected dotted/dashed connections draw solid, hiding the style change
- Severity: low     Flags: —
- Ticket: 133

## I-029 Ctrl+arrow (add room/connect in a direction) labels every new connection up/down
- Reported: 2026-10-31 by user (manual checklist, Linux laptop, COSMIC)   OS: linux   Category: interaction
- Ctrl+arrow (add room/connect in a direction) labels every new connection up/down
- Severity: medium     Flags: —
- Ticket: 133

## I-030 Map view starts at the top-left on load; reference zooms to fit (Canvas.OnProjectChanged → ZoomToFit)
- Reported: 2026-10-31 by user (manual checklist, Linux laptop, COSMIC)   OS: linux   Category: viewport
- Map view starts at the top-left on load; reference zooms to fit (Canvas.OnProjectChanged → ZoomToFit)
- Severity: low     Flags: —
- Ticket: 133

## I-031 Context menus show the canvas cursor (e.g. move/draw-line) instead of the standard arrow
- Reported: 2026-10-31 by user (manual checklist, Linux laptop, COSMIC)   OS: linux   Category: interaction
- Context menus show the canvas cursor (e.g. move/draw-line) instead of the standard arrow
- Severity: low     Flags: —
- Ticket: 133

## I-032 Pinch on a Linux touchpad does nothing (touchscreen pinch and Ctrl+two-finger scroll work). Avalonia only repo
- Reported: 2026-10-31 by user (manual checklist, Linux laptop, COSMIC)   OS: linux   Category: input
- Pinch on a Linux touchpad does nothing (touchscreen pinch and Ctrl+two-finger scroll work). Avalonia only reports touchpad magnify gestures on macOS; X11/Wayland touchpad gestures are not delivered. Platform limitation — revisit when Avalonia supports it
- Severity: low     Flags: —
- Ticket: —

## I-033 Keys reach the main window while a properties dialog is open (Enter opened a second dialog, Esc did not close 
- Reported: 2026-10-31 by user (manual checklist, Linux laptop, COSMIC)   OS: linux   Category: dialogs
- Keys reach the main window while a properties dialog is open (Enter opened a second dialog, Esc did not close it) — seen on COSMIC after Ctrl+Up; dialogs must take focus and the main window must ignore keys while a modal is open
- Severity: medium     Flags: —
- Ticket: 134

## I-034 Room dialog Objects box: the syntax hint popup shifts the box and Esc closes the whole dialog instead of the h
- Reported: 2026-10-31 by user (manual checklist, Linux laptop, COSMIC)   OS: linux   Category: dialogs
- Room dialog Objects box: the syntax hint popup shifts the box and Esc closes the whole dialog instead of the hint; Tab-to-indent traps focus — decided (user, 2026-10-31): drop Tab indent, Tab moves focus, users type dashes
- Severity: medium     Flags: —
- Ticket: 134

## I-035 App Settings: tabs cannot be reached from the keyboard; Tab from the last field cycles OK/Cancel/last field in
- Reported: 2026-10-31 by user (manual checklist, Linux laptop, COSMIC)   OS: linux   Category: dialogs
- App Settings: tabs cannot be reached from the keyboard; Tab from the last field cycles OK/Cancel/last field instead of back to the top
- Severity: low     Flags: —
- Ticket: 134

## I-036 Colour chooser: text clipped, the More/HSV view too narrow for its controls, only a Cancel button
- Reported: 2026-10-31 by user (manual checklist, Linux laptop, COSMIC)   OS: linux   Category: dialogs
- Colour chooser: text clipped, the More/HSV view too narrow for its controls, only a Cancel button
- Severity: medium     Flags: —
- Ticket: 134

## I-037 App Settings still labels six working settings "(not active yet)"
- Reported: 2026-10-31 by user (manual checklist, Linux laptop, COSMIC)   OS: linux   Category: dialogs
- App Settings still labels six working settings "(not active yet)"
- Severity: low     Flags: —
- Ticket: 134

## I-038 PDF export overwrote an existing file without asking (Linux/COSMIC portal); make sure every save picker asks b
- Reported: 2026-10-31 by user (manual checklist, Linux laptop, COSMIC)   OS: linux   Category: export
- PDF export overwrote an existing file without asking (Linux/COSMIC portal); make sure every save picker asks before overwriting
- Severity: medium     Flags: —
- Ticket: 134

## I-039 Image export "Current zoom" scales the whole map by the zoom factor (same as the reference) but reads as "expo
- Reported: 2026-10-31 by user (manual checklist, Linux laptop, COSMIC)   OS: linux   Category: export
- Image export "Current zoom" scales the whole map by the zoom factor (same as the reference) but reads as "export what is on screen"; relabel
- Severity: low     Flags: —
- Ticket: 134
<!-- Next ID: I-040 -->

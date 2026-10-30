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
- Ticket: —

## I-003 Changes to the open map on disk are silently ignored
- Reported: 2026-10-30 by agent (T1.5, code reading)   OS: all   Category: behaviour
- Steps: 1. Open a map. 2. Edit and save the same file in another program.
- Expected: prompt "map modified by another program, reload?" (reference `legacy/src/Trizbort/Domain/Watchers/TrizbortFileWatcher.cs:55–67`).   Actual: nothing happens; the watcher re-arms.
- Cause: Core's `TrizbortFileWatcher` is armed on load, but Avalonia never sets `ConfirmReloadPrompt`, `UIThreadInvoker` or `Project.MapReloader`. Matrix A005.
- Severity: medium     Flags: —
- Ticket: —

## I-004 Ctrl+Shift+A selects everything instead of the selected rooms' regions
- Reported: 2026-10-30 by agent (T1.2, code reading)   OS: all   Category: behaviour
- Cause: `case Key.A when isCtrl` in `src/Trizbort.Avalonia/Controls/MapCanvasControl.cs:1239` also catches Ctrl+Shift+A. Reference selects all rooms in the selected rooms' regions. Matrix K004.
- Severity: low     Flags: —
- Ticket: —

<!-- Next ID: I-005 -->

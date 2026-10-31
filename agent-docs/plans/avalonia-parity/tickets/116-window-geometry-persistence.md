# 116 — Persist window size, position and maximised state

Matrix rows: A009          Priority: P?          Size: S
Depends on: 100, 101          Suggested tier: mid

## What
The window reopens at the size, position and maximised state it had at exit. The reference stored size only and never restored it (Lee's open question A-3); this implements the useful behaviour.

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `OnClosing` (CanvasWidth/CanvasHeight save)
- `src/Trizbort.Core/Domain/AppSettings/ApplicationSettings.cs` (line ~15, `CanvasWidth/Height`)

## Implement
1. Core: add `WindowState` (`WindowGeometry` record: `X`, `Y`, `Width`, `Height`, `Maximised`) to `ApplicationSettings` (nullable, JSON-compatible, old files still load). Add a pure `WindowGeometry.Fit(saved, screens)` that clamps to visible screen bounds (a monitor was unplugged) and enforces minimum 640x400.
2. Avalonia: `MainWindow` restores in the constructor/`Opened` using `Screens.All`; saves on closing (normal bounds when not maximised — use `RestoreBounds` equivalent). Save only if the window is not minimised.
3. Linux/Wayland ignores positions: restore size and maximised only if `Position` setting has no effect (no special code beyond not throwing).

## Tests
- Core: `src/Trizbort.Core.Tests/WindowGeometryTests.cs` — clamp offscreen, tiny sizes, round-trip JSON, old settings file.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- A009 -> Implemented; committed as `parity(116): window geometry`.

## Out of scope
- Dialog sizes, splitter/panel layout.

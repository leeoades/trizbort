# 055 — Add debug toggles enabled only with --debug

Matrix rows: K062, K063, K064, K067, K069          Priority: P3          Size: S
Depends on: 009, 010, 054        Suggested tier: mid

## What
With the app started with `--debug`: `Ctrl+F1` FPS, `Shift+F1` mouse coordinates, `Ctrl+F2` element rendering, `Ctrl+F3` line rendering toggle; grid polyline toggle has a menu item only (Ctrl+F5 clashes with Start Room). Without the flag none exist (approved Q6).

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnKeyDown` debug cases F1/F2/F3/F5 (~1337-1395)
- `$REF/src/Trizbort/Domain/AppSettings/ApplicationSettings.cs` — `Debug*` properties (grep -n 'Debug')

## Implement
1. Core: `debug.fps|mouseCoords|elementRendering|lineRendering|gridPolyline` with `Hidden` unless `EditorSession.DebugEnabled` (set by start-up from `--debug`; start-up parsing belongs to the start-up ticket, provide a settable property here). Catalogue `CanExecute` returns false and key bindings are not generated when disabled.
2. macOS: Developer menu only.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/DebugCommandTests.cs` — disabled when flag off, flips settings when on.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(055): Add debug toggles enabled only with --debug`.

## Out of scope
- Parsing the command line.

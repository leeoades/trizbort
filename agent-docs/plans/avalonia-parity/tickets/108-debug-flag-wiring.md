# 108 — Wire the `--debug` startup flag and the debug rendering hooks

Matrix rows: K062, K063, K064, K067, K069 (flag and rendering half)          Priority: P?          Size: S
Depends on: 104, 055          Suggested tier: mid

## What
Starting the app with `--debug` turns on the debug features whose commands and gestures are registered by ticket 055 (Ctrl+F1 FPS, Shift+F1 mouse coordinates, Ctrl+F2 element rendering, Ctrl+F3 line rendering; grid polyline menu item only). Without the flag nothing is enabled. This ticket owns the parsing-to-session link and makes each toggle actually change rendering.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnKeyDown` F-key cases (~1337–1395) and where the debug settings are consumed in drawing (grep -n `DebugDraw\|DebugShowFPS\|DebugShowMouseCoordinates`)
- `src/Trizbort.Core/Domain/Application/CommandLineOptions.cs` (extended in 104 with `Debug`)

## Implement
1. Avalonia: `StartupActions` (104) sets `EditorSession.DebugEnabled = options.Debug` before the window is shown (property is added by 055). Do not register commands here (055 does).
2. Avalonia rendering: honour each toggle in `MapCanvasControl` / `Drawing/*`: FPS counter text (frames per second over a 1 s window), pointer coordinates text near the cursor, skip drawing elements (rooms/labels) when element rendering is off, skip connections when line rendering is off, draw the grid polyline when on. Where a toggle has no counterpart in the Avalonia renderer, leave a no-op and record it in the matrix Notes.
3. Toggles read the same `AppSettings`/session properties 055 flips; no new settings.

## Tests
- Avalonia: `src/Trizbort.Avalonia.Tests/DebugRenderingTests.cs` — with debug off the toggles do nothing; with debug on and element rendering off a headless render has no room pixels at a known room position.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- Matrix rows K062–K069 updated (with 055); committed as `parity(108): --debug wiring and rendering`.

## Out of scope
- Command and gesture registration (055). Command-line parsing (104).

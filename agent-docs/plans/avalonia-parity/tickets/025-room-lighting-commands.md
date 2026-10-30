# 025 — Add Toggle Darkness, Force Darkness and Force Lighted commands

Matrix rows: M033, M034, M035, K023, K060, K061, C008          Priority: P2          Size: S
Depends on: 002, 003, 009, 010        Suggested tier: mid

## What
`K` toggles darkness on selected rooms; `Primary+K` forces dark; `Primary+Shift+K` forces lit. Context menu Toggle Darkness shows a check for the clicked room.

## Reference (read only these)
- `$REF/src/Trizbort/Domain/Controllers/CommandController.cs` — `SetRoomLighting`
- `$REF/src/Trizbort/Domain/Controllers/RoomController.cs` — `ToggleDarkness`, `ForceDarkness`, `ForceLight` (RoomController is identical in Core)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnKeyDown` `case Keys.K` (lines ~1323-1331)

## Implement
1. Core: `LightingCommands` registering `room.toggleDark` (`K` Canvas), `room.forceDark` (`Primary+K` Window), `room.forceLit` (`Primary+Shift+K` Window) via existing Core `RoomController`. Menu `Edit/Rooms/...`. ContextTargets Room for toggle.
2. Bare `K` requires no Primary/Alt (shortcut-map section 1); `Primary+K` must not fall through to Toggle.
3. CanExecute: >= 1 selected room. IsChecked (toggle): primary selected room `IsDark`.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/LightingCommandTests.cs` — toggle mixed selection, force, undo, modifier exactness.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(025): Add Toggle Darkness, Force Darkness and Force Lighted commands`.

## Out of scope
- Legacy toggle semantics for mixed selections: copy `RoomController.ToggleDarkness` exactly, do not redesign.

# 022 — Add Select Regions, Select Start Room and select-nearest-to-centre

Matrix rows: K001 (selection half), K003, K004          Priority: P2          Size: S
Depends on: 002, 009, 010, 020        Suggested tier: mid

## What
`Primary+Shift+A` selects every room in the regions of the selected rooms. Bare `0`/`NumPad0` selects the start room and centres it. Bare Enter with nothing selected selects the room nearest the viewport centre (with one element selected Enter opens properties; see 023).

## Reference (read only these)
- `$REF/src/Trizbort/Domain/Controllers/CommandController.cs` — `SelectRegions`, `SelectStartRoom`, `SelectRoomClosestToCenterOfViewport`
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnKeyDown` cases `Keys.A` with Control|Shift, `Keys.D0`/`Keys.NumPad0`, `Keys.Enter` (grep -n 'case Keys.D0' / 'Keys.Enter')

## Implement
1. Core: commands `select.regions` (`Primary+Shift+A`, Window), `select.startRoom` (`0` and `NumPad0`, Canvas, exact modifiers none), and the no-selection half of `edit.enter` handled by 023. Both centre via ticket 020.
2. Fixes Avalonia bug K004: Shift must be checked so `Primary+Shift+A` no longer runs Select All.
3. CanExecute: regions needs >= 1 selected room; start room needs a room with `IsStartRoom`.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/SelectRegionsStartRoomTests.cs` — region selection with default region, no start room, centre called.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(022): Add Select Regions, Select Start Room and select-nearest-to-centre`.

## Out of scope
- Properties on Enter (023).

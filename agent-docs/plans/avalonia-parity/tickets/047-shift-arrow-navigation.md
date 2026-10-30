# 047 — Add Shift+Arrow / Shift+Home/End/PgUp/PgDn / keypad navigation

Matrix rows: K011, K014, K032, K033, K034, K049, K050          Priority: P2          Size: M
Depends on: 002, 009, 010, 020, 005        Suggested tier: strong

## What
`Shift+Arrows` select the connection (else room) in that direction from the selection; `Shift+Home/End/PageUp/PageDown` go NW/SW/NE/SE. Plain `NumPad1-9` (not 5) select the room in that direction incl. diagonals; `Shift+NumPad1-9` behave like Shift+arrows.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `ShiftArrowHandler` (3097), `SelectRoomRelativeToSelectedRoom`, `GetRoomInApproximateDirectionFromRoom`/`GetRoomInExactDirectionFromRoom`, `SelectRoomRelativeToSelectedConnection` (2986-3076), `IndicatedDirection` (2455), `AddOrSelectRooms` (1664)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnKeyDown` numpad block (~1413-1424)

## Implement
1. Core: `EditorSession.Navigate(CompassPoint, mode)` in `src/Trizbort.Core/Editor/Navigation.cs` replacing `Keys`-based `IndicatedDirection` with a Core key-to-compass table (use Core `KeyGesture`). Commands `nav.select.*` (8 directions x Shift/plain) generated from one table; centre via 020.
2. Uses `Room.GetConnections(compassPoint)` and `CompassPointHelper` rotations. Shares logic with the rest of nav; `RoughOpposite` dup in MapStatistics: reuse, do not duplicate.
3. Avalonia bug: today Shift+Arrows move by 2x grid (K014); remove that path.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/NavigationTests.cs` — fixture map with N/E/S/W/diagonal neighbours, connection-first order for Shift, keypad = arrow mapping, no-op at edges.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(047): Add Shift+Arrow / Shift+Home/End/PgUp/PgDn / keypad navigation`.

## Out of scope
- Ctrl variants (048, 049).

# 048 — Add Primary+Arrow / Primary+NumPad add-or-connect room

Matrix rows: K013, K051          Priority: P2          Size: M
Depends on: 002, 003, 009, 010, 020, 030, 037, 047        Suggested tier: strong

## What
`Primary+Arrows` (and `Primary+NumPad1-9`) select the room in that direction; if none, select a connection leaving that way; otherwise connect to a room found at the target point, or create a new room there (copying shape, region, size, darkness) joined by a connection, then select the connection and open the new room's properties.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `CtrlArrowHandler` (1822), `AddOrConnectRoomRelativeToSelectedRoom` (grep -n), `AddOrSelectRooms` (1664), private `AddConnection`
- `$REF/src/Trizbort/Setup/Settings.cs` — `KeypadNavigationCreationModifier` (grep -n 'KeypadNavigation')

## Implement
1. Core: `nav.addOrConnect.*` (8 directions) in `NavigationCommands`, one undo step each including the optional dialog. Uses hit-testing (005) at the target point; new connection takes defaults (030).
2. Gesture is `Primary+...`; reference modifier is a setting — hard-code Primary and keep `Settings.KeypadNavigationCreationModifier` unused (add setting only if Lee asks).
3. Fixes Avalonia bug K013 (currently nudges).

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/AddOrConnectTests.cs` — selects existing, connects to existing at target, creates new with copied traits, undo is one step.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(048): Add Primary+Arrow / Primary+NumPad add-or-connect room`.

## Out of scope
- Alt variant (049).

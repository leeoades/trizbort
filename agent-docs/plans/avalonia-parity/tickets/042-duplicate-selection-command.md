# 042 — Add Duplicate command (Primary+D)

Matrix rows: (new, shortcut-map Q5)          Priority: P2          Size: S
Depends on: 002, 003, 009, 010, 040        Suggested tier: mid

## What
`Primary+D` duplicates the selected elements with the paste offset and selects the copies, without touching the clipboard. Approved by Lee (Q5); there is no reference behaviour.

## Reference (read only these)
- `src/Trizbort.Core/Domain/Controllers/CopyController.cs` — `CreateCopyObject`, `PasteConnections` (reuse the serialise/paste path)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `PasteRooms` (offset and id remap behaviour)

## Implement
1. Core: `edit.duplicate` (`Primary+D`, Window) = serialise selection to an in-memory string and paste it through the same code as 040, with the clipboard untouched. One undo step. Menu `Edit/Duplicate`.
2. Gesture note: bare `D` is label Down (031); `Primary+D` must be matched exactly.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/DuplicateCommandTests.cs` — rooms plus inner connections duplicated, outer connections not, selection moves to copies, clipboard unchanged, undo.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(042): Add Duplicate command (Primary+D)`.

## Out of scope
- Duplicate-drag (Alt+drag).

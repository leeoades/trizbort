# 040 — Move Copy/Cut/Paste into Core commands over IClipboard

Matrix rows: K043, K044, K045, M026          Priority: P1          Size: M
Depends on: 002, 003, 009, 010        Suggested tier: mid

## What
Copy, Cut (Avalonia addition) and Paste work from one clipboard channel. Paste offsets, remaps ids and reference rooms, redocks connections and selects the pasted elements; Paste is greyed out when the clipboard holds no Trizbort data.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `Paste`, `PasteRooms`, `CopySelectedElements` (~538, 720, 2732-2810)
- `src/Trizbort.Core/Domain/Controllers/CopyController.cs` — `CreateCopyObject`, `PasteConnections`, clipboard handlers
- `$REF/src/Trizbort/UI/MainForm.cs` — `EditCopyMenuItemClick` (625), `EditPasteMenuItemClick` (630), enable ~1211

## Implement
1. Core: `edit.copy` (`Primary+C`), `edit.cut` (`Primary+X`), `edit.paste` (`Primary+V`, paste at pointer when invoked from the context menu) over `IClipboard` (async). Paste is one undo step. Remove `_internalClipboard` and the separate control-side `TopLevel.Clipboard` code (X015).
2. Paste creates rooms through the same placement code as 037 (clash avoidance).
3. CanExecute paste: clipboard text parses as Trizbort copy data.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/ClipboardCommandTests.cs` with a fake `IClipboard` — round trip with connections and reference rooms, repeated paste offsets, undo.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(040): Move Copy/Cut/Paste into Core commands over IClipboard`.

## Out of scope
- Copy Color (041).

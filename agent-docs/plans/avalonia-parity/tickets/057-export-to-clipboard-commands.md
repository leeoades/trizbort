# 057 — Add Export to Clipboard commands

Matrix rows: M010, M011          Priority: P2          Size: M
Depends on: 002, 009, 010        Suggested tier: mid

## What
File > Export > To Clipboard has Alan, Adventuron, Hugo, Inform 7, Inform 6, TADS, ZIL, Quest (with header) and Quest (no header); each puts the generated source on the clipboard. Gestures `Primary+Alt+L/A/H/7/6/T/Z`, Quest no-header `Primary+Alt+Q`, Quest with header none (macOS Hugo `Primary+Alt+Shift+H`).

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `HugoToTextToolStripMenuItemClick` (586), `Inform6ToTextToolStripMenuItemClick` (591), `Inform7ToTextToolStripMenuItemClick` (596), `AlanToTextToolStripMenuItemClick` (103), `AdventuronToTextToolStripMenuItemClick` (108), `QuestToTextToolStripMenuItemClick` (746), `QuestRoomsToTextToolStripMenuItemClick` (741), `ZILToClipboardToolStripMenuItemClick` (1337), `ExportCode<T>` (grep -n)

## Implement
1. Core: `export.clipboard.<lang>` commands in `ExportCommands` using the Core exporters and `IClipboard`; disabled when the map has no rooms. Not undo steps. Use existing Core exporter classes only; do not touch exporter output.
2. Avalonia: nothing beyond clipboard implementation (shared with 040).

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/ExportClipboardTests.cs` — fake clipboard receives text equal to the exporter output for the sample fixture, per language.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(057): Add Export to Clipboard commands`.

## Out of scope
- File exports; PDF/image exports.

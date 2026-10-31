# 087 — Map Statistics window and Export

Matrix rows: D011          Priority: P?          Size: M
Depends on: 009, 010, 070           Suggested tier: mid

## What
Tools > "Map Statistics..." shows the report in a read-only monospaced window with Copy and "Save as..."; Tools > "Map Statistic Export..." writes the same report to a log file chosen by a save picker (defaults to `<map name>.log` next to the map, else `trizbort.log` in the user's home) and reports the path.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/NoCounterpart.md` §D011 (report content list, export naming bugs).
- `$REF/src/Trizbort/UI/MapStatisticsView.cs` lines 19-199 (report builder, export).
- `src/Trizbort.Core/Domain/Application/MapStatistics.cs` (all numbers).
- `src/Trizbort.Avalonia/Services/StorageService.cs` (save picker pattern), `Controls/MapCanvasControl.cs:1780` (`ComputeCanvasBounds`) / `MinimapTransform.ComputeCanvasBounds`.

## Implement
1. Core: `src/Trizbort.Core/Domain/Application/MapStatisticsReport.cs` — `static string Build(Project project, double widthWithMargins ...)`: move the report builder from the legacy view, same section order and wording, dimension values passed in as a parameter record (so no UI bounds code in Core). Rules: 0 rooms -> "No rooms to take stats of."; drop the unreachable "Region Links" branch; use `\n`/`Environment.NewLine` consistently.
2. Core commands in catalogue: `tools.mapStatistics` (opens `IEditorPrompts.ShowTextReportAsync(title, text)`) and `tools.mapStatisticsExport` (`IEditorPrompts.PickSaveFileAsync` -> write UTF-8 -> `IEditorPrompts.MessageAsync("Wrote log to <path>", "Log file written")`). Fix legacy name bug: default name `<file>.log` (strip `.trizbort`), never `.trizbort`; always via picker so overwrites are confirmed by the OS.
3. Avalonia: `Views/Dialogs/MapStatisticsWindow.axaml` — title "Map Statistics", 850x666, monospaced `TextBox` read-only fills window, buttons Copy, Save as..., Close (Esc closes). Resizable; scrolls.
4. Menu entries come from the catalogue; no shortcut.

## Tests
- Core: `src/Trizbort.Core.Tests/MapStatisticsReportTests.cs` — empty map text; a fixture map from `samples/` with a stable expected report (golden file); plurals; region section.
- Avalonia: window shows text; Copy puts text on the clipboard fake.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D011 -> Implemented.
- Committed as `parity(087): map statistics`.

## Out of scope
- Grouped/graphical statistics view.

# 090 — Map Theme dialog: export current map theme, infer default room style, no-undo wording, current indicator

Matrix rows: D017          Priority: P?          Size: M
Depends on: 070, 002, 003           Suggested tier: mid

## What
The theme dialog can export the map's current look (as the legacy menu did), has an "Infer default room style from rooms..." action with a summary confirmation, shows which theme matches the current settings, warns clearly about what is replaced and preserved, and applying a theme is one undo step.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/MapSettings.md` — section "MapTheme dialog vs the reference theme menu".
- `$REF/src/Trizbort/UI/MainForm.Themes.cs` (31-142: apply prompt text, export, infer).
- `src/Trizbort.Avalonia/Views/Dialogs/MapThemeDialog.axaml(.cs)`, `ViewModels/MapThemeViewModel.cs` (29, 34-40), `RoomStyleInference` (Core; no caller yet), `MapTheme.Capture`.

## Implement
1. VM/code-behind: "Export Current Map Theme..." button uses `MapTheme.Capture(settings)` and a save picker (existing import/export code in `MapThemeDialog.axaml.cs:43-66`); the existing "Export" stays for the selected theme, relabelled "Export Selected Theme...".
2. "Infer Default Room Style..." button: run `RoomStyleInference` on the project's rooms, confirm with the summary text from the legacy prompt via `IEditorPrompts.ConfirmAsync`, apply to settings; one undo step.
3. Warning text when styles exist: copy legacy wording (what is replaced, what is preserved, "There is no undo" -> change to "You can undo this with Edit > Undo" once 003 lands).
4. Current-theme indicator: compare `MapTheme.Capture(settings)` to each built-in; select the matching one or show "(Current map settings)" entry at the top, selected by default.
5. Apply theme: `session.Execute("Apply theme", ...)`, one step.
6. Layout per 070 (groups "Theme", "Details", "Options"; no clipping).

## Tests
- Core: `EditorSessionDialogTests.cs` — apply theme is one undo step; infer style result.
- Avalonia: `DialogViewModelTests.cs` — current indicator; export current theme produces file equal to Capture.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D017 updated.
- Committed as `parity(090): map theme dialog gaps`.

## Out of scope
- New built-in themes.

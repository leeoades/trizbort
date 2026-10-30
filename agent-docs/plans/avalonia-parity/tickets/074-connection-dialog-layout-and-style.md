# 074 — Connection dialog: restore Style group, colour swatch/clear, multiline description, no clipping

Matrix rows: D002, D016          Priority: P?          Size: M
Depends on: 070, 085           Suggested tier: mid

## What
Connection Properties shows everything at its default size: a "Style" group (Dotted, One Way Arrow, Colour with a visible swatch, "No Color Set" text and a one-click clear), a "Text" group, the Door options, and a multi-line Description at the bottom, with legacy labels.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/ConnectionProperties.md` — whole file.
- `$REF/src/Trizbort/UI/ConnectionPropertiesDialog.cs` (20-35, 96-99, 174-177); `.Designer.cs` grep `Text =` and `Size =` only.
- `src/Trizbort.Avalonia/Views/Dialogs/ConnectionPropertiesDialog.axaml`, `ViewModels/ConnectionPropertiesViewModel.cs`.
- Screenshots: `screenshots/winforms/ConnectionPropertiesDialog__main__default.png`, `screenshots/avalonia/ConnectionProperties__main__default.png`.

## Implement
1. Axaml: restructure using 070 group style: group "Style" (Dotted, One Way Arrow, colour row), group "Text" (see 075), Name at top, Door row ("Door" + four dependants indented, greyed until Door ticked — keep Avalonia's correct initial state), Description last as multi-line (`AcceptsReturn`, `TextWrapping=Wrap`, min 4 lines, stretches). Legacy order: Dotted then One Way Arrow.
2. Labels: "Dotted", "One Way Arrow", "Door", "Color:" — follow the app-wide spelling of `ColorPickerButton`; the dialog must not mix "Colour" and "Color" (decide: use "Color" in this dialog to match legacy and note it in 070's guideline if not already stated).
3. Colour row: use the improved `ColorPickerButton` from 085 (swatch + "No Color Set" text + clear X). Do not rebuild it here.
4. Default size so Colour and all four door options are visible without scrolling (about 520x620; verify with `DialogLayoutAssert.AssertNoClipping`).
5. Enter in the multi-line Description inserts a newline (not OK); make sure `IsDefault` OK still works from other fields.

## Tests
- Avalonia: `DialogViewModelTests.cs` — Description round-trips multi-line text; no clipping assertion at default size in both Door states.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D002 updated; checked in Light and Dark.
- Committed as `parity(074): connection dialog layout`.

## Out of scope
- Preset radios (075); OK through session (075).

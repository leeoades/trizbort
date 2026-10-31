# 076 — Label dialog: Shape enable rule, initial focus, groups, size, OK as one undo step

Matrix rows: D003          Priority: P?          Size: S
Depends on: 070, 002, 003           Suggested tier: mid

## What
Label Properties shows all groups without scrolling, focuses and selects the text on open, lets Shape be changed when Outline OR Background is on, uses access keys, and OK is one undo step.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/LabelProperties.md` — whole file.
- `$REF/src/Trizbort/UI/LabelPropertiesDialog.cs` (30-110: layout, enabling at 78-81, focus 105-108).
- `src/Trizbort.Avalonia/Views/Dialogs/MapLabelPropertiesDialog.axaml`, `ViewModels/MapLabelPropertiesViewModel.cs`, `Services/DialogService.cs:259-272`.

## Implement
1. Window title "Label Properties". Groups "Outline" and "Background" use the 070 group style; Text and Text colour ungrouped above them. Shape row sits in a place enabled by `HasOutline || HasBackground` (move it out of the outline-only grid; a small "Shape" row between the groups or under Background — keep next to the group it affects and say so in a hint "Shape applies to outline and background").
2. Labels: "Enabled" inside each group (legacy), "Line style", "Colour" (this dialog's legacy wording) with `_` keys: `_Text`, `_Enabled`, E`_`nabled, `_Shape`, Line `_`style.
3. Focus Text and `SelectAll()` on `Opened`.
4. Default height to fit (about 480) via `DialogLayoutAssert`; Text box row `*` so it grows.
5. OK through session as one undo step ("Edit label properties").

## Tests
- Avalonia: `DialogViewModelTests.cs` — `ShapeEnabled` true when only Background is on; false when neither; no-clipping assertion; round-trip all 7 properties.
- Core: `EditorSessionDialogTests.cs` — one undo reverts all label changes.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D003 -> Implemented; Light/Dark checked.
- Committed as `parity(076): label dialog`.

## Out of scope
- Font controls for labels (not in reference).

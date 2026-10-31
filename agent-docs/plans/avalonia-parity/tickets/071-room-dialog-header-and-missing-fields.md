# 071 — Room dialog: persistent header, missing fields and friendly option names

Matrix rows: D001          Priority: P?          Size: M
Depends on: 070           Suggested tier: mid

## What
Room Properties regains the fields it lost and the always-visible header of the reference: Name, Subtitle, Reference room, Start/End/Dark stay visible above the tabs. Custom Position (Down/Right), Border Style "None" and all eight Second Fill locations are editable, and dropdowns show readable text instead of enum names.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/RoomProperties.md` — field table and Recommendation (1)-(4).
- `$REF/src/Trizbort/UI/RoomPropertiesDialog.cs` — `LoadFromRoom`/`ApplyToRoom` style methods (grep `_chkCustomPosition`, `_cboBorderStyle`, `_comboBox1`, `_cboHandDrawn`, `_cboDrawType`); lines 25-34 for hand-drawn item text.
- `$REF/src/Trizbort/UI/RoomPropertiesDialog.Designer.cs` — grep only: `Text =` for labels; do not open whole.
- `src/Trizbort.Avalonia/Views/Dialogs/RoomPropertiesDialog.axaml`, `ViewModels/RoomPropertiesViewModel.cs`.
- Model: `Room.ObjectsCustomPosition`, `Room.cs:810-850` (Second Fill locations), `BorderDashStyle.cs`.

## Implement
1. Avalonia axaml: restructure into a header grid (Name, Subtitle, Reference Room, Start/End/Dark checkboxes) above the `TabControl`; tabs become Description & Objects, Shapes & Lines (Border Style, Lines, Shape, corners), Region (or keep Region on the header row — reference put it in its own tab; keep a Region tab with the combo only if 072's swatches need room), Colours. Use `Classes="group"` from 070 for Position and Custom Position.
2. VM: add `UseCustomPosition`, `CustomPositionDown`, `CustomPositionRight` (NumericUpDown, enabled only when `UseCustomPosition`, Position combo disabled when custom is on if the reference does so — check `RoomPropertiesDialog.cs:168`); read/write `Room.ObjectsCustomPosition`. Preserve existing values.
3. VM: add `None` to Border Style list; add the 4 corner locations to Second Fill Location (all 8 in legacy order: Bottom, BottomRight, BottomLeft, Left, Right, TopRight, TopLeft, Top); put the Location combo on the same row as Second Fill colour.
4. Friendly names: introduce `EnumDisplay` helper (`ViewModels/EnumDisplay.cs`) mapping `RoomShape` ("Straight Edges", "Rounded Corners", "Ellipse", "Octagonal" — legacy order), `HandDrawn` ("Map setting (hand-drawn)" / "Map setting (straight)" depending on the current map setting, "Hand-drawn", "Straight"), `BorderDashStyle` (Solid, Dash, Dash-dot, Dash-dot-dot, Dot, None). Bind via `ItemTemplate`, keep enum values as the items.
5. Labels: restore legacy wording where reworded without gain ("Lines:", "Room Fill", "Second Fill", "Room Border", "Room Text", "Object Text", "Subtitle Text"); add `_` access keys per legacy (`_Name`, S`_`ubtitle, Border St`_`yle, `_`Lines, `_`Position, etc.) following 070.
6. Default size: 560x560 (or taller) so no tab needs scrolling; description/objects boxes stretch (`*` rows).

## Tests
- Avalonia: `src/Trizbort.Avalonia.Tests/DialogViewModelTests.cs` — round-trip of custom position, border None, each of the 8 second-fill locations; enum display strings; `DialogLayoutAssert.AssertNoClipping` on the dialog.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D001 notes updated (fields missing: 0).
- Checked in Light and Dark theme.
- Committed as `parity(071): room dialog header and missing fields`.

## Out of scope
- Shape preview, validation, conditional corner group, OK through session (072); Objects editor (073).

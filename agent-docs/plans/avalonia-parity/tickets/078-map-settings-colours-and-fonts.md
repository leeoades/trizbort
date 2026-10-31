# 078 — Map settings: friendly colour names, multi-select edit, fonts with style via font picker

Matrix rows: D004, D016          Priority: P?          Size: M
Depends on: 077, 084, 085           Suggested tier: mid

## What
The Colours tab lists the 11 colours with friendly names (Canvas, Border, Connection, Connection (selected), Connection (hover), Subtitle Text, Object Text, Connection Text, Grid, Start Room, End Room) and the Fonts tab uses the font picker (family list, size, bold/italic, sample) for the four fonts.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/MapSettings.md` rows "Colour list", "Colour multi-select", font rows, "Font style", notes on fonts.
- `$REF/src/Trizbort/UI/SettingsDialog.cs` — colour list edit (477-494), `ShowFontDialog` (569-578); `.Designer.cs` grep `Text =` for 788-799 names and font labels "Room Name", "&Objects:", "&Line:", "&Subtitle:".
- `src/Trizbort.Avalonia/Views/Dialogs/MapSettingsDialog.axaml:96-134`, `ViewModels/MapSettingsViewModel.cs` (250-269 fonts), `src/Trizbort.Core/Setup/Colors.cs` (`ToName`).

## Implement
1. VM: `ColorEntry { Key, DisplayName, Color }`; display-name map in the VM (not in Core `Colors.ToName`, which stays the persistence key). Order as the reference.
2. Axaml: colour list as rows "DisplayName + ColorPickerButton (085)" inside a "Colors" group; swatch beside the name. Multi-select edit is Won't-port unless Lee asks — do NOT implement; record in Notes.
3. Fonts tab: group "Fonts"; four `FontPickerButton`s (084) labelled "Room Name", "Objects", "Line", "Subtitle", each bound to a `FontSpec { Family, Size, Style }`. Write style back (VM:250-269 currently writes only name+size). Show the current font as "Family, 10pt, Bold" text.
4. No clipped spinner columns (070 shared sizes).

## Tests
- Avalonia: `DialogViewModelTests.cs` — friendly names in order; font style round-trips to `Settings` (bold/italic); no-clipping on the two tabs.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D004 updated; Light/Dark checked.
- Committed as `parity(078): map settings colours and fonts`.

## Out of scope
- Colour multi-select; theme dialog (090).

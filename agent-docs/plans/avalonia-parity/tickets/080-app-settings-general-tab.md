# 080 — App settings: restore General tab groups, Port Adjust Detail, hover tips, honest labels

Matrix rows: D006          Priority: P?          Size: M
Depends on: 070           Suggested tier: mid

## What
The General tab regains the "Preferences" and "App defaults" groups, adds the Port Adjust Detail dropdown (NSEW (4), Diagonals (8), All ports (16)), restores the four hover tips and legacy labels, and moves margins back under "App defaults". Settings still ignored by the app are listed as known issues (I-005, I-006 and the other ignored settings) and are not given behaviour here.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/AppSettings.md` — field table, checklist, Recommendation.
- `$REF/src/Trizbort/UI/AppSettingsDialog.Designer.cs` — grep `Text =` and `ToolTip`/`SetToolTip` only (lines ~118-190 General, 291-363 App defaults, 519-540 Map).
- `src/Trizbort.Avalonia/Views/Dialogs/AppSettingsDialog.axaml`, `ViewModels/AppSettingsViewModel.cs` (33,70,92 `PortAdjustDetail`).
- `agent-docs/plans/avalonia-parity/issues.md` — I-005, I-006.

## Implement
1. Axaml: tabs General / Tooltips (081) / Map? Reference tabs are General, ToolTips, Map; keep Avalonia's "Canvas" content in a group "Map" within General or a `Map` tab — follow reference: three tabs General, ToolTips, Map. Margins return to General "App defaults" group (Specify margins, Horizontal, Vertical; spinners enabled only when ticked); delete the "Export Margins" tab.
2. Add `PortAdjustDetail` ComboBox with the three legacy items (`Room.cs:300` consumes it).
3. Group "Preferences": Open last project on start, Invert Mouse Wheel Zoom, Show Full Path in Title Bar, Port Adjust Detail, Default Font Name (free text now; font picker 084 later may replace it). Group "Map": Apply style to new rooms (legacy wording and meaning: "style of the last selected room"), Double click to add room.
4. Tooltips (`ToolTip.Tip`) with legacy text (D:138, 159, 529, 540) on Save images at 100% (see 5), Open last project, Apply style, Double click.
5. Settings that Core saves but the Avalonia app ignores (Open last project, Invert wheel, Full path title, Apply style to new rooms — issues I-005/I-006 family): keep the control, append a muted "(not active yet)" hint ONLY if the owning behaviour ticket has not landed; the behaviour tickets (other agent) remove this hint. Do not implement the behaviour.
6. Smart Save group (Save to PDF, Save to Image, Save TADS to ADV3Lite, Default Image Save Type PNG/JPEG/BMP; no EMF) and "Save images at 100%": add the controls and VM properties (Core already stores them, `ApplicationSettingsController.cs:107-109`), because hiding them loses data editing; mark "(not active yet)" the same way.
7. Margins max back to 4096; labels with `_` keys per legacy; default size about 480x520; remove clipping.

## Tests
- Avalonia: `DialogViewModelTests.cs` — Port Adjust and Smart Save fields round-trip; margin spinners enable; no-clipping on the tabs.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D006 notes: 6 missing fields now present; Light/Dark checked.
- Committed as `parity(080): app settings general tab`.

## Out of scope
- Making the ignored settings take effect (other tickets); tooltip tab (081). No undo: app settings are not map edits.

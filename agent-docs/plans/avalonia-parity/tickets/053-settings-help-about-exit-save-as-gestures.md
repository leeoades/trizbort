# 053 — Apply approved gestures for Save As, Settings, Help, About and Exit

Matrix rows: M005, M013, M065, M066, M071, M072, K024          Priority: P2          Size: S
Depends on: 009, 010        Suggested tier: cheap

## What
Catalogue gestures per shortcut-map section 5: Save As `Primary+Shift+S`; App Settings `Primary+,`; Map Settings `Primary+Shift+,`; Online Help `F1` (Cmd+? on macOS), About no gesture; Exit Alt+F4 (Windows), Primary+Q (Linux), Cmd+Q (macOS).

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `HelpAndSupportMenuItem_Click` (580) opens online help; `AppSettingsToolStripMenuItemClick` (114)
- `$REF/src/Trizbort/UI/MainForm.Designer.cs` — accelerators 1216-1229, 1275-1278

## Implement
1. Core: edit gesture data for existing ids (`file.saveAs`, `app.settings`, `map.settings`, `help.online`, `help.about`, `app.exit`) and add `help.online` (opens URL via `IEditorPrompts.OpenUrl`). Remove F1 from About. Mac native menu supplies Settings/Quit.
2. Remove old Avalonia key bindings once 010 generates them.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/GestureTableTests.cs` — these ids carry the expected gestures per platform profile.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(053): Apply approved gestures for Save As, Settings, Help, About and Exit`.

## Out of scope
- The dialogs; Smart Save (059).

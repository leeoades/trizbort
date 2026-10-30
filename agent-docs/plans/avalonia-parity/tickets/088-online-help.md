# 088 — Online Help: Help menu links and F1

Matrix rows: D014          Priority: P?          Size: S
Depends on: 009, 010           Suggested tier: cheap

## What
Help menu has "Online Help (User Guide)" and "Original Trizbort v1 Help" that open the URLs in the default browser, F1 opens the user guide (reference behaviour), and About keeps its own item with the gesture from the approved shortcut map. A failed launch shows an error message.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/NoCounterpart.md` §D014.
- `$REF/src/Trizbort/UI/OnlineHelpDialog.cs` (URLs lines 10-11, error text 62-83).
- `agent-docs/plans/avalonia-parity/shortcut-map.md` (Help / About gestures).
- `src/Trizbort.Avalonia/Views/MainWindow.axaml:32,93-94` (current F1 = About).

## Implement
1. Core: catalogue commands `help.online` (gesture F1) and `help.originalHelp`; handler calls `IEditorPrompts.OpenUriAsync(uri)` (add this method to the interface if missing; non-interactive implementation returns false).
2. Avalonia: implement `OpenUriAsync` with `TopLevel.Launcher.LaunchUriAsync`; on false/exception show message "Unable to open the link:\n\n<url>\n\n<error>" titled "Online Help".
3. Move About off F1 per shortcut-map; menu item stays under Help.
4. URLs as constants in Core (`HelpLinks.UserGuide = https://github.com/leeoades/trizbort/blob/master/Docs/index.md`, `HelpLinks.OriginalHelp = https://trizbort.genstein.net/help/`).
5. No separate dialog (decision from spec Suggestion); note in matrix.

## Tests
- Core: `src/Trizbort.Core.Tests/HelpCommandTests.cs` — commands call the fake prompts with the right URI; failure path shows message.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D014 -> Different (intentional: menu links instead of dialog).
- Committed as `parity(088): online help`.

## Out of scope
- Bundling help offline.

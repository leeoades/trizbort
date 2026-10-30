# 075 — Connection dialog: preset radios that track the text, access keys, OK as one undo step

Matrix rows: D002          Priority: P?          Size: M
Depends on: 074, 002, 003           Suggested tier: mid

## What
The Text group has the five radio buttons (Up/Down, Down/Up, In/Out, Out/In, Custom) as in the reference: choosing one fills Start/End, typing matching text selects the matching radio (otherwise Custom). All controls have access keys, and OK applies as one undo step.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/ConnectionProperties.md` §1 (Text preset radios), §2 (Enable/disable, Mnemonics), Recommendation (4)-(5).
- `$REF/src/Trizbort/UI/ConnectionPropertiesDialog.cs` — `MatchText` (156-168), `SetText` (169-174), door enabling (127-133).
- `src/Trizbort.Avalonia/Views/Dialogs/ConnectionPropertiesDialog.axaml(.cs)` (15-51 presets/Clear), `ViewModels/ConnectionPropertiesViewModel.cs` (106-111).

## Implement
1. VM: `enum TextPreset { UpDown, DownUp, InOut, OutIn, Custom }`; property `SelectedPreset` (two-way). Setting a non-Custom preset writes Start/End (and leaves Middle untouched, legacy); changing `StartText`/`EndText` re-evaluates the preset via `MatchText` logic (move the pure logic to VM, no UI). Initial value from the connection's texts.
2. Axaml: five `RadioButton`s `GroupName="TextPreset"` bound to `SelectedPreset` via a converter; remove the one-shot buttons. Keep the "Clear" button (new, useful) labelled "Clear text" next to the radios; it empties Start/Middle/End and selects Custom.
3. Access keys per legacy (`_Dotted`, One Way `_`Arrow, `_Style`, `_Text`, `_Custom`, `_Start`, `_Middle`, `_End`, `_Up/Down` ...). Ensure `Alt+D` etc. don't collide; use `DialogLayoutAssert.AssertAccessKeysUnique`.
4. Tooltip on Start/Middle/End boxes (what each is for) — wording new, one short sentence each.
5. OK: `session.Execute("Edit connection properties", () => vm.ApplyTo(connection))` through `IEditorPrompts.EditElement(connection)` (one undo step; Cancel none).

## Tests
- Avalonia: `DialogViewModelTests.cs` — preset selection fills texts; typing "Up"/"Down" selects UpDown; arbitrary text selects Custom; Middle never changed by presets; Clear.
- Core: `EditorSessionDialogTests.cs` — connection OK is one undo step (flow, style, texts, door, colour).

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D002 -> Implemented.
- Committed as `parity(075): connection dialog presets and undo`.

## Out of scope
- Layout (074).

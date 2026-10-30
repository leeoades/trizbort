# 073 — Port ObjectListEditor to Core and wire it to the Objects box; restore Object Syntax help

Matrix rows: D001, D013          Priority: P?          Size: S
Depends on: 071           Suggested tier: mid

## What
In the Room dialog's Objects box, Tab/Shift+Tab indent/outdent the selected lines, Enter continues the list with a bullet at the same nesting, Ctrl+A selects all, and a collapsible "Object Syntax" help lists the flags [m] [f] [p] [!] [s] [u] [c] [h] [w] [1] [2].

## Reference (read only these)
- `$REF/src/Trizbort/UI/ObjectListEditor.cs` (whole file, 173 lines, pure string logic).
- `$REF/src/Trizbort/UI/RoomPropertiesDialog.cs` — `TxtObjectsKeyDown` (~915-939); `_lblObjectSyntaxHelp` handling (~549); legacy `.resx` label text for the syntax help (grep `label9`/`Object Syntax`).
- `legacy/src/Trizbort.Tests` — grep `ObjectListEditor` for existing unit tests to port.
- `src/Trizbort.Core/Domain/Misc/ObjectList.cs` (`TabIndent = 4`).
- `agent-docs/plans/avalonia-parity/dialogs/NoCounterpart.md` §D013.

## Implement
1. Core: `src/Trizbort.Core/Domain/Misc/ObjectListEditor.cs` (copy logic; static `ChangeIndent(text, selStart, selLen, outdent)` and `NewLine(text, selStart, selLen)`; return a small record `{Text, SelStart, SelLength}` or null for default). No UI types.
2. Avalonia: attach in `RoomPropertiesDialog.axaml.cs` a `KeyDown` handler on the Objects `TextBox` (use `AddHandler(..., RoutingStrategies.Tunnel)` so `AcceptsTab`/focus traversal does not consume Tab): Tab/Shift+Tab/Enter with no Ctrl/Alt as in legacy; set `Text`, `SelectionStart/End`, `CaretIndex`, mark handled. Set `AcceptsTab="True"`.
3. Make the Objects box stretch with the dialog (no fixed `Height`); add Ctrl+A select-all if not default.
4. Add an `Expander` "Object syntax" below the box (collapsed by default; style `hint`) with the flag list text copied from legacy. Add a one-line description of bullets/indent to the box tooltip.
5. Optional: none (tree preview is out of scope).

## Tests
- Core: `src/Trizbort.Core.Tests/ObjectListEditorTests.cs` — port legacy tests; indent/outdent multi-line selection; Enter on bulleted, plain-indented, empty-bullet (outdents) and unindented lines (returns null).
- Avalonia: `src/Trizbort.Avalonia.Tests/DialogViewModelTests.cs` or new `ObjectsBoxTests.cs` — raise Tab key event on the TextBox in a headless window and assert indented text.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D013 -> Implemented; D001 note updated.
- Committed as `parity(073): object list editor`.

## Out of scope
- Tree preview of the parsed object list; changing `ObjectList` parsing.

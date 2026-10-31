# 009 — Command catalogue types and tests in Core

Matrix rows: X050, X051 (D2 foundation C1); enables every M, K and C row          Priority: P1          Size: M
Depends on: 002        Suggested tier: mid

## What
Core gets `EditorCommand` and `CommandCatalogue`, the single registry from which menus, toolbar, context menu and key bindings are later generated. This ticket defines the types, the key-gesture model (with a `Primary` modifier), the validation tests, and registers a small seed set that mirrors the commands that already exist in Avalonia. No Avalonia change.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/decisions.md` — D2 (the type shape is specified there; follow it).
- `agent-docs/plans/avalonia-parity/shortcut-map.md` — notation + section 5 table (gesture data is added by feature tickets; seed only existing ones).
- `src/Trizbort.Core/Domain/Enums/ModifierKeys.cs` (Shift/Control/Alt).
- `src/Trizbort.Core/Editor/` — `EditorSession`.
- **This ticket owns the neutral key types** (moved here from 006 so the catalogue doesn't wait for the drag refactor): create `src/Trizbort.Core/Editor/Input/InputKey.cs` (`enum InputKey`: Escape, Space, Delete, Back, Enter, Tab, arrows, Home/End/PageUp/PageDown, D0–D9, A–Z, F1–F12, NumPad0–9, Add, Subtract, OemPlus, OemMinus, OemComma, OemOpenBrackets, OemCloseBrackets, OemSemicolon, Oem5) and `KeyInput.cs` (`readonly record struct KeyInput(InputKey Key, ModifierKeys Modifiers)`), and add a `Primary` flag to Core's `ModifierKeys` enum. Ticket 006 reuses these.
- `src/Trizbort.Avalonia/Views/MainWindow.axaml` lines 15-95 only — the existing menus and key bindings to mirror.
- `src/Trizbort.Avalonia/ViewModels/MainWindowViewModel.cs` — command names (New, Open, Save, SaveAs, Export*, Exit, ShowMapSettings, ShowMapTheme, ShowAppSettings, ShowAbout, ToggleAutomap) to see which are app-level (need dialogs/files, not editor commands).

## Implement
Files in `src/Trizbort.Core/Editor/Commands/` (namespace `Trizbort.Editor.Commands`):
1. Add `Primary = 8` to `Domain/Enums/ModifierKeys.cs` with XML doc: "Ctrl on Windows/Linux, Cmd on macOS. Only used by catalogue gestures; never persisted." Check nothing serialises the enum by number (grep `KeypadNavigation` in `ApplicationSettings`/settings persistence tests) and the existing tests still pass.
2. `KeyGesture.cs`: `public readonly record struct KeyGesture(InputKey Key, ModifierKeys Modifiers)`; `static bool TryParse(string text, out KeyGesture g)` accepting `"Primary+Shift+Z"`, `"Delete"`, `"Alt+Enter"`, `"Primary+0"`; `ToString()` round-trips; `string ToDisplayString(bool macOS)` → `Ctrl+Shift+Z` / `⌘⇧Z`; `bool Matches(KeyInput input, bool primaryIsMeta)` (the platform maps Primary to Control or a Meta flag supplied by the caller: add `Meta` handling by having the Avalonia mapper convert Cmd to `Primary` on macOS and Ctrl to `Primary` elsewhere; so `Matches` is simply equality after that conversion).
3. `CommandScope.cs`: `enum CommandScope { Window, Canvas }`. `[Flags] enum ContextTargets { None=0, Room=1, Connection=2, Label=4, Empty=8, Any=15 }`.
4. `EditorCommand.cs`: `public sealed record EditorCommand` with `string Id`, `string Title`, `string? MenuPath` (e.g. `"Edit/Rooms/Swap"`; null + `Hidden=true` for key-only commands), `string? Group` (separator group inside a menu), `IReadOnlyList<KeyGesture> Gestures`, `CommandScope Scope = Window`, `ContextTargets ContextTargets = None`, `bool ShowInToolbar`, `string? IconKey`, `bool Hidden`, `Func<EditorSession,bool>? CanExecuteFunc`, `Func<EditorSession,bool>? IsCheckedFunc`, `Func<EditorSession,string>? DynamicTitle`, `Func<EditorSession,Task> ExecuteAsyncFunc`. Provide `bool CanExecute(EditorSession)`, `bool? IsChecked(EditorSession)`, `string TitleFor(EditorSession)`, `Task ExecuteAsync(EditorSession)`.
5. `CommandCatalogue.cs`: `public sealed class CommandCatalogue { IReadOnlyList<EditorCommand> All; EditorCommand? Find(string id); void Register(EditorCommand); IEnumerable<EditorCommand> ForContext(ContextTargets target); IEnumerable<EditorCommand> Menu(string rootName); static CommandCatalogue CreateDefault(); }`. `Register` throws on a duplicate id.
   `CreateDefault()` registers, for editor-level (session-only) actions that already exist: `edit.selectAll` (Primary+A), `edit.delete` (Delete), `edit.copy`, `edit.cut`, `edit.paste` (Primary+C/X/V), `view.zoomIn` (Primary+Plus and Primary+NumPadAdd), `view.zoomOut` (Primary+Minus, Primary+NumPadSubtract), `view.resetZoom` (Primary+0, Primary+NumPad0), `view.fitToMap`, `view.toggleGrid`, `project.addRoom`, `project.addLabel`. Use ids exactly like these; their `ExecuteAsyncFunc` calls the `EditorSession` methods from ticket 002 (copy/cut/paste go through the session's internal clipboard string for now). App-level actions (New/Open/Save/Export/settings dialogs/About/Exit/Automap) are registered in ticket 010 from Avalonia with delegates to the view model, because they need services Core doesn't have; give `CommandCatalogue` an `Register` that Avalonia can call.
6. `CatalogueValidator.cs`: `static IReadOnlyList<string> Validate(CommandCatalogue)` returning problems: duplicate gesture within the same scope (Window-scope gestures also clash with Canvas-scope ones, since a window binding would shadow them); command without Title; command without MenuPath that is not `Hidden`; `Hidden` command with `ShowInToolbar`.

## Tests
`src/Trizbort.Core.Tests/CommandCatalogueTests.cs`:
- `DefaultCatalogue_HasNoValidationProblems`.
- `DuplicateGesture_SameScope_IsReported`; `WindowGesture_ClashingWithCanvasGesture_IsReported`; `SameGesture_DifferentScope_NoClash` only when scopes are both Canvas vs Window with different keys (assert the exact rule you implemented).
- `Register_DuplicateId_Throws`; `Find_ReturnsCommand`; `Menu_Edit_OrdersByGroupThenRegistration`.
- `KeyGesture_Parse_Primary_Shift_Z`; `KeyGesture_RoundTrips`; `KeyGesture_DisplayString_WindowsAndMac`; `KeyGesture_Parse_Invalid_ReturnsFalse`.
- `Primary_Modifier_DoesNotChangeExistingModifierValues` (Shift=1, Control=2, Alt=4 still).
- `Execute_SelectAll_SelectsAllElements`; `CanExecute_Delete_FalseWhenNothingSelected`.
- `EveryCommand_HasTitle_AndMenuPathOrIsHidden`. A skipped-later placeholder `EveryMatrixReferencedCommandIdExists` is NOT written now (no list exists yet).

## Done when
- Build + Core tests pass; Avalonia untouched and green.
- Matrix X050/X051 updated "catalogue types: ticket 009"; commit `parity(009): Add command catalogue types and validation tests`.

## Out of scope
- Generating any Avalonia UI (010); app-level commands; feature commands (swap, z-order, …); the full shortcut table (feature tickets add gestures); macOS NativeMenu.

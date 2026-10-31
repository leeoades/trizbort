# 010 — Avalonia generates main menu, context menu, toolbar and key bindings from the catalogue

Matrix rows: X050, X051, C001, C002 (existing items only), M001–M013 existing items, K (existing keys) (D2 foundation C2)          Priority: P1          Size: L (strong-tier review of the diff)
Depends on: 009 (canvas-scope keys: until 006 lands, dispatch them from `MapCanvasControl.OnKeyDown` through the catalogue; after 006, `CanvasInteraction.OnKey` does it)        Suggested tier: mid (implementation), strong (diff review)

## What
Behaviour-preserving refactor: the main menu, canvas context menu, toolbar and window key bindings are produced at runtime from `CommandCatalogue`. All commands that exist today are migrated. Visible menus, shortcuts and enable/disable states stay as they are today (apart from items listed under "Deliberate differences").

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/decisions.md` — D2.
- `src/Trizbort.Avalonia/Views/MainWindow.axaml` (whole file; it contains the hand-written `Window.KeyBindings` lines 15-33, menu lines ~36-100 and the toolbar) and `MainWindow.axaml.cs`.
- `src/Trizbort.Avalonia/ViewModels/MainWindowViewModel.cs` — `ICommand` properties (New/Open/Save/…/ToggleAutomap) and their `CanExecute` rules.
- `src/Trizbort.Avalonia/Controls/MapCanvasControl.cs` — `ShowContextMenuAt` (~1424-1498) and `OnKeyDown` (~1216-1316); after 006 the key handling is in `CanvasInteraction`.
- `src/Trizbort.Core/Editor/Commands/*` (ticket 009).
- `src/Trizbort.Avalonia.Tests/` — grep for tests that look up menu items or `KeyBinding`s (`MenuItem`, `KeyBindings`) so you know what to keep green.

## Implement
1. Catalogue app-level commands: new `src/Trizbort.Avalonia/Services/AppCommands.cs` with `static void Register(CommandCatalogue c, MainWindowViewModel vm)` adding `file.new` (Primary+N), `file.open` (Primary+O), `file.save` (Primary+S), `file.saveAs` (Primary+Shift+S), `file.export.png` and each export entry (ids `file.export.inform7`, `.inform6`, `.tads`, `.hugo`, `.zil`, `.quest`, `.alan`, `.adventuron`), `file.exit` (display-only gesture `Alt+F4`, `Hidden` binding — the OS handles it), `edit.properties` (Alt+Enter, ContextTargets Room|Connection|Label), `edit.mapSettings`, `edit.mapTheme`, `edit.appSettings`, `help.about` (F1), `project.toggleAutomap`. `ExecuteAsyncFunc` calls the view-model `ICommand`/method; `CanExecuteFunc` mirrors the view-model's `CanExecute`. Reuse existing view-model commands; do not duplicate logic.
2. New `src/Trizbort.Avalonia/Controls/CatalogueMenuBuilder.cs`:
   - `IReadOnlyList<MenuItem> BuildMainMenu(CommandCatalogue, EditorSession, Func<EditorCommand,ICommand>)` — splits `MenuPath` on `/`, creates nested `MenuItem`s, groups separated by `Separator` when `Group` changes, header with mnemonic (`_` prefix: add `Mnemonic` to `EditorCommand` or derive from `Title` containing `_`), `InputGesture = KeyGesture.ToDisplayString(...)`, `IsChecked` for toggle items, `IsEnabled` re-evaluated on `session.Changed`.
   - `ContextMenu BuildContextMenu(CommandCatalogue, EditorSession, ContextTargets target)` using `ForContext`.
   - `IReadOnlyList<Control> BuildToolbar(…)` for `ShowInToolbar` commands (buttons with `IconKey` → existing icon resources).
   - `IReadOnlyList<global::Avalonia.Input.KeyBinding> BuildKeyBindings(…)` for `Scope == Window` gestures.
   - `AvaloniaKeyMapper` (in `Controls/InputMapper.cs` from 006): `Primary` → `KeyModifiers.Control` on Windows/Linux, `KeyModifiers.Meta` on macOS (`OperatingSystem.IsMacOS()`), `InputKey` ↔ Avalonia `Key` both ways.
3. `MainWindow.axaml(.cs)`: remove all `MenuItem`s, `KeyBinding`s and toolbar buttons; keep only a `Menu x:Name="MainMenu"`, `StackPanel x:Name="Toolbar"`, and layout. In the window's `Opened`/constructor fill them with the builder. Command execution of catalogue commands goes via a single `RelayCommand`/`AsyncRelayCommand` adapter that calls `command.ExecuteAsync(session)`.
4. `MapCanvasControl.ShowContextMenuAt`: replace its hand-built menu with `BuildContextMenu(catalogue, session, target)` where `target` comes from the hit element (Room/Connection/Label/Empty). Migrate each existing context item into the catalogue with the right `ContextTargets` (Add Room → Empty, Properties → Room|Connection|Label, Delete → Room|Connection|Label, Copy/Cut/Paste etc. as currently shown; read `ShowContextMenuAt` to list exactly the current ones).
5. `CanvasInteraction.OnKey`/`KeyInput` for canvas-scoped gestures: the Avalonia control asks `catalogue.FindByGesture(gesture, CommandScope.Canvas)` (add this lookup to `CommandCatalogue` in this ticket if missing) when no text box is being edited, instead of its own `switch`. Existing canvas keys that are not command-like (Esc, Space-pan, arrow nudge/resize) stay in `CanvasInteraction`.
6. DI: register one `CommandCatalogue` singleton (`CreateDefault()` + `AppCommands.Register`).
7. Deliberate differences, nothing else: F1 and Ctrl+Shift+S conflicts are NOT resolved here (shortcut-map tickets do); Linux/Windows `Primary` is Ctrl exactly as today.

## Tests
- Avalonia `src/Trizbort.Avalonia.Tests/CatalogueMenuTests.cs` (headless window, pattern of `MapCanvasControlSyncTests.cs`): `MainMenu_HasFileEditViewProjectHelpTopLevel`; `MainMenu_FileMenu_ContainsExpectedItemsInOrder` (names from the old axaml, copied into the test as the golden list); `MainMenu_EditMenu_HasGesturesShown` (e.g. Select All shows `Ctrl+A`); `MenuItem_Delete_DisabledWithoutSelection_EnabledWithSelection`; `KeyBindings_ContainPrimaryN_O_S` ; `ContextMenu_Room_ContainsPropertiesAndDelete`; `ContextMenu_Empty_ContainsAddRoomAndPaste`; `Toolbar_ButtonsMatchOldToolbar`; `CtrlA_SelectsAll_ViaGeneratedBinding`; `PrimaryMapsToMetaOnMac` (call the mapper with a flag instead of the OS).
- Core `CommandCatalogueTests`: add `FindByGesture_ReturnsCanvasCommand`.
- All previous Avalonia tests green; update only those that referenced named XAML elements, and list them in the commit message.

## Done when
- Build + Core + Avalonia tests pass on Linux; run the app headless smoke tests in `Smoke/` if they exist.
- `MainWindow.axaml` has no `MenuItem`/`KeyBinding` elements; `ShowContextMenuAt` has no hand-built items.
- Matrix X050/X051 updated; commit `parity(010): Generate menus, toolbar and key bindings from the catalogue`; strong-tier review of the diff.
- If > 1 day: split inside the ticket as 010a (main menu + key bindings + app commands) and 010b (context menu + toolbar + canvas gestures), separate commits.

## Out of scope
- New commands (swap, z-order, selection queries, …), shortcut-map changes/conflict fixes, macOS `NativeMenu`, localisation, customisable shortcuts, icons beyond the existing ones.

# 012 — Avalonia IEditorPrompts: same-direction and disambiguation dialogs

Matrix rows: E044, E045, D008, D009; fixes I-008 (D3 foundation A2)          Priority: P1          Size: M
Depends on: 011        Suggested tier: mid

## What
During automapping the user is asked (a) which room to keep when two rooms claim the same exit and (b) which existing room a transcript room name means, exactly as in the reference app, using two new modal Avalonia dialogs. Closing either window with X behaves as the reference (same-direction → Keep first; disambiguation → "A New Room").

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/Automap.md` — sections 2 (AutomapRoomSameDirectionDialog) and 3 (DisambiguateRoomsDialog): the specs. Screenshots: `agent-docs/plans/avalonia-parity/screenshots/winforms/AutomapRoomSameDirectionDialog__main__default.png`, `DisambiguateRoomsDialog__main__default.png`.
- `legacy/src/Trizbort/UI/AutomapRoomSameDirectionDialog.cs` and `DisambiguateRoomsDialog.cs` (small; Designer files only for control texts via grep, not whole).
- `src/Trizbort.Core/Editor/IEditorPrompts.cs`, `RoomDisambiguation` (ticket 011).
- `src/Trizbort.Avalonia/Services/DialogService.cs` / `IDialogService.cs` — pattern for creating and showing dialogs; `src/Trizbort.Avalonia/Views/Dialogs/AutomapSettingsDialog.axaml(.cs)` for the look and code-behind style.
- `src/Trizbort.Avalonia/App.axaml.cs` — `WireAutomapErrorReporter` (~44-64) and where services are configured; `ViewModels/MainWindowViewModel.cs` ~426-480 for automap start.

## Implement
1. `src/Trizbort.Avalonia/Services/AvaloniaEditorPrompts.cs`: `public sealed class AvaloniaEditorPrompts : IEditorPrompts` taking `Func<Window?> getOwner` (same shape as `WireAutomapErrorReporter`). Both methods run on the UI synchronisation context; if called off-thread use `await Dispatcher.UIThread.InvokeAsync(() => ShowXAsync(...))` (async lambda, **not** `Invoke` + blocking). Dialog `ShowDialog<T>(owner)` returns the result; owner null → fall back to `NonInteractiveEditorPrompts` behaviour.
2. `Views/Dialogs/AutomapSameDirectionDialog.axaml(.cs)`: title "Automapping"; message `Room '{existing.Name}' is already defined in the same direction as room '{newName}'.  What would you like to do?` (fix the legacy missing quote); three equal-width stacked buttons on the right: `Keep '{existing.Name}'`, `Keep '{newName}'`, `Keep Both`; message on the left; modal, centred on owner, no minimise/maximise (`CanResize=false`, `ShowInTaskbar=false`); no Cancel button; Esc and window-close → `KeepRoom1`; default button = `Keep '{existing.Name}'`. Result `AutomapSameDirectionResult`. Add a small `AutomapSameDirectionViewModel` (name, texts) only if the code-behind approach of `AutomapSettingsDialog` uses VMs; otherwise code-behind is fine.
3. `Views/Dialogs/DisambiguateRoomsDialog.axaml(.cs)` + `DisambiguateRoomsViewModel`: title "Disambiguate Rooms"; explanatory label at top (take the text from legacy Designer line ~54 via grep `Text =` / `label` near it, and size the message area to show all lines — the reference screenshot clips it); "When the transcript says:" + read-only multi-line `TextBox` (transcript line, blank line, description; normalise CRLF); "Which room does it mean?" with `ListBox` of candidates (display `Room.Name`) and a read-only `Description` `TextBox` showing the selected room's `PrimaryDescription`; buttons "_This Room" (default; enabled only when a candidate is selected), "A _New Room", "_Auto-Choose the Rest" (access keys via `_`). A single candidate is pre-selected; with several there is no default selection. Focus starts in the context box. Resizable, min size ~ 520x420. Outcomes: This Room → `RoomDisambiguation.Choose(selected)`; New → `CreateNew()`; Auto-Choose → `ChooseFirstAndStopAsking(candidates[0])`; window X / Esc → `CreateNew()`.
4. Wiring: in `App.axaml.cs` where `WireAutomapErrorReporter` is invoked (or in the same method region), add `public static IEditorPrompts WireAutomapPrompts(Func<Window?> getOwner)` which creates `AvaloniaEditorPrompts` and sets `AutomapEngine.Instance.Prompts`; call it at the same place. Update the stale comment in `WireAutomapErrorReporter` ("known parity gap") to point to the new method.
5. Un-skip the smoke test from 011 (`AutomapEngine.Instance.Prompts` must be an `AvaloniaEditorPrompts` after app start).
6. Avalonia headless tests cannot show real modal dialogs easily; make the dialogs testable through their view models and an `IDialogHost`-free design: the view model exposes `Result` and commands; code-behind just closes with the VM's result.

## Tests
`src/Trizbort.Avalonia.Tests/AutomapPromptDialogTests.cs` (view-model level plus headless window construction as in `DialogCaptureTests.cs`):
- `SameDirection_Message_HasBothNamesAndClosingQuote`; `SameDirection_ButtonLabels_UseRoomNames`; `SameDirection_CloseWithoutChoice_IsKeepRoom1`; `SameDirection_EachButton_ReturnsMatchingResult`.
- `Disambiguate_ThisRoom_DisabledUntilSelection`; `Disambiguate_SingleCandidate_PreSelectedAndEnabled`; `Disambiguate_Selection_ShowsPrimaryDescription`; `Disambiguate_NewRoom_ReturnsCreateNew`; `Disambiguate_AutoChooseRest_ReturnsFlag`; `Disambiguate_Close_IsNewRoom`; `Disambiguate_ContextBox_NormalisesNewlines`.
- `AvaloniaEditorPrompts_NoOwner_FallsBackToDefaults`.
- `AutomapEndToEnd_WithScriptedDialogViewModels` — run `AutomapIntegrationTests`-style transcript with an `IEditorPrompts` that returns Keep Both / New room and assert the map (two rooms in the same direction kept; ambiguous name creates a new room), proving I-008.
- `DialogCaptureTests.cs`: add captures for both dialogs if the file has a simple pattern for it (screenshots go where that test writes them; do not commit generated PNGs).

## Done when
- Build + Core + Avalonia tests pass on Linux. Manual check noted in commit: run automap on a transcript that triggers each dialog.
- `issues.md` I-008 Ticket column set to 012; matrix E044, E045, D008, D009 updated (Implemented, test names); commit `parity(012): Avalonia dialogs for automap prompts`.

## Out of scope
- AutomapSettings dialog fixes (grouping, hints, mnemonics: separate dialog tickets); removing `AvaloniaAutomapCanvas` dispatcher marshalling; other `IEditorPrompts` members; localisation; the debug-only "Step with F11" option.

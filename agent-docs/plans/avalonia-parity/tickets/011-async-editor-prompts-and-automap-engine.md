# 011 — Add async IEditorPrompts to Core; the automap engine awaits them

Matrix rows: E044, E045 (engine side) (D3 foundation A1)          Priority: P1          Size: M
Depends on: none        Suggested tier: mid

## What
The same-direction conflict and the ambiguous-room question become async prompts that the automap engine awaits, instead of a synchronous static `Func` (never assigned in Avalonia) and a `FindRoom` that silently returns the first candidate. Headless runs keep today's defaults (`KeepRoom1`, first match) through a non-interactive implementation. No dialogs yet (ticket 012).

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/decisions.md` — D3 (source of truth).
- `agent-docs/plans/avalonia-parity/dialogs/Automap.md` — sections 2 and 3 (when each prompt is shown, inputs, outputs, rules).
- `src/Trizbort.Core/Automap/Automap.cs` — `_chooseConflictingRoom` (line 20), `FindRoom(...)` private wrapper (~315), `Match` (~321-344), `ProcessTranscriptText` (~386-440; same-direction call at ~414), static `ConflictingRoomChooser` / parameterless ctor / 2-arg ctor (~806-826), `InitializeRun` (~828), `Instance` (836), `Start` (~980), `StartCl` (~873).
- `src/Trizbort.Core/Automap/IAutomapCanvas.cs` (27 lines), `AutomapCanvas.cs` — `FindRoom` (~169-181), `AutomapSameDirectionResult.cs` (values `KeepRoom1, KeepRoom2, KeepBoth, Cancel`).
- `legacy/src/Trizbort/UI/Controls/Canvas.Automap.cs` lines 196-260 — the "should we ask" rules and the `_dontAskAboutAmbiguities` flag (reset per run at ~258).
- Tests that touch the old API: `src/Trizbort.Avalonia.Tests/AutomapIntegrationTests.cs` (lines ~31, 49), `Smoke/FileExportAutomapSmokeTests.cs` (~56, 72, 670-673), `src/Trizbort.Core.Tests/AutomapRegressionTests.cs`.

## Implement
1. `src/Trizbort.Core/Editor/IEditorPrompts.cs` (namespace `Trizbort.Editor`):
   ```csharp
   public interface IEditorPrompts {
     Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(Room existing, string newRoomName, CancellationToken token = default);
     Task<RoomDisambiguation> DisambiguateRoomAsync(string transcriptLine, string roomDescription, IReadOnlyList<Room> candidates, CancellationToken token = default);
   }
   public readonly record struct RoomDisambiguation(Room? Chosen, bool NewRoom, bool AutoChooseRest) {
     public static RoomDisambiguation Choose(Room r) => new(r, false, false);
     public static RoomDisambiguation CreateNew() => new(null, true, false);
     public static RoomDisambiguation ChooseFirstAndStopAsking(Room first) => new(first, false, true);
   }
   ```
   (Other prompts named in D1 point 5 — message, confirm, save/discard/cancel, edit element, pick file — are added by later tickets; do not add them now.)
2. `NonInteractiveEditorPrompts` (same folder; `public static readonly … Instance`): `ChooseSameDirectionAsync` → `KeepRoom1`; `DisambiguateRoomAsync` → `Choose(candidates[0])`.
3. `IAutomapCanvas`: **replace** `Room FindRoom(roomName, roomDescription, line, RoomMatcher matcher)` with `RoomSearchResult FindRooms(string roomName, string roomDescription, RoomMatcher matcher)` where `readonly record struct RoomSearchResult(Room? Certain, IReadOnlyList<Room> Maybes)`. Implementation in `AutomapCanvas`: the existing loop — first `true` match → `Certain`; `null` matches → `Maybes` (no prompting, no `list[0]`). Update `AvaloniaAutomapCanvas.FindRooms` (same `Dispatcher` marshalling pattern as its other methods; it stays for now) .
4. `Automap.cs`:
   - Remove static `ConflictingRoomChooser` and the `Func` field/ctor parameter. Add `public IEditorPrompts Prompts { get; set; } = NonInteractiveEditorPrompts.Instance;` (the engine is the singleton `Automap.Instance`; Avalonia assigns it at startup in ticket 012). Keep the parameterless ctor and a ctor `Automap(Action<string,string> reportError, IEditorPrompts prompts)` for tests.
   - `FindRoom` → `private async Task<Room?> FindRoomAsync(string roomName, string roomDescription, string line, CancellationToken token)`: call `_canvas.FindRooms`; `Certain` → return it; `Maybes.Count == 0` → null; if `_dontAskAboutAmbiguities` → `Maybes[0]`; if one candidate and (description empty or candidate has no `PrimaryDescription`) → that candidate (legacy rules, `Canvas.Automap.cs:209-210`); else `await Prompts.DisambiguateRoomAsync(line, roomDescription, Maybes, token)` → `AutoChooseRest` sets `_dontAskAboutAmbiguities = true` and returns `Maybes[0]`; `NewRoom` → null; `Chosen` → it. Add `private bool _dontAskAboutAmbiguities` reset to false in `InitializeRun`.
   - `ProcessTranscriptText`: `var room = await FindRoomAsync(...)`; same-direction: `var decision = await Prompts.ChooseSameDirectionAsync(otherRoom, roomName, token);`. Keep the `token.ThrowIfCancellationRequested()` calls after each await. Handle `AutomapSameDirectionResult.Cancel` like a cancelled token (throw `OperationCanceledException`) instead of falling into `ArgumentOutOfRangeException` (it does today).
   - No `.Result`, `.Wait()` or `.GetAwaiter().GetResult()` anywhere in Core automap (grep to verify).
5. Tests currently assigning `AutomapEngine.ConflictingRoomChooser` are rewritten to set `AutomapEngine.Instance.Prompts` (restore it in teardown; same pattern as their existing `_previousRoomChooser`). The smoke test at ~670 that asserts the chooser is wired is replaced by one asserting `Prompts` is not the non-interactive instance — that assertion only turns green in ticket 012; mark it `[Fact(Skip="enabled by ticket 012")]` here and un-skip in 012.

## Tests
`src/Trizbort.Core.Tests/AutomapPromptTests.cs` with a `ScriptedPrompts : IEditorPrompts` (queues of results + recorded calls):
- `SameDirection_KeepRoom1_ReusesExistingRoom`; `…_KeepRoom2_CreatesNewRemovesOld`; `…_KeepBoth_CreatesNewKeepsOld`; `…_Cancel_StopsRun` (transcript crafted as in `AutomapRegressionTests.cs`: two visits to room A going the same direction with different destination names).
- `Disambiguate_Chosen_UsesChosenRoom`; `Disambiguate_NewRoom_CreatesRoom`; `Disambiguate_AutoChooseRest_AsksOnlyOnce` (two ambiguous lines → one prompt call, both resolved to first candidate); `Disambiguate_SingleCandidateNoDescription_DoesNotAsk`; `Disambiguate_FlagResetsOnNextRun`.
- `Prompts_AreAwaitedWithoutBlocking` (the scripted prompt completes via `TaskCompletionSource` after a delay; engine proceeds only after).
- `NonInteractive_KeepsLegacyDefaults` (existing regression tests keep passing unchanged using the default prompts).
- Run `AutomapRegressionTests` and Avalonia automap tests; they must stay green.

## Done when
- Build + Core + Avalonia tests pass on Linux (one Skip as above).
- `grep -rn "ConflictingRoomChooser" src` is empty; `grep -n "GetAwaiter().GetResult\|\.Wait()" src/Trizbort.Core/Automap` is empty.
- Matrix E044/E045 note "engine awaits prompts: ticket 011"; commit `parity(011): Async IEditorPrompts and awaiting automap engine`.

## Out of scope
- Dialogs (012); removing `AvaloniaAutomapCanvas` dispatcher marshalling (after automap runs through the session on the UI context — separate cleanup); other prompts in `IEditorPrompts`; automap undo (done in 004); changing `Match`.

# 127 — Automap error reporting and recovery

Matrix rows: E046, E047, E048, E049 (error paths)          Priority: P?          Size: S
Depends on: 011, 003          Suggested tier: mid

## What
Errors during automap stop the run cleanly and leave the map in a usable state: the engine reports IO/access errors through `IEditorPrompts` (awaitable, replacing the fire-and-forget `ErrorReporter` static), unexpected exceptions stop automap, restore the "not running" UI state and show one message, and the whole run is one undo step (or is discarded on failure — Lee to confirm; default: keep rooms created before the error, one undo step).

## Reference (read only these)
- `$REF/src/Trizbort/Automap/Automap.cs` — error handling in the transcript open/read path (grep -n `ErrorReporter`, lines ~805–808, 935–949, 1070–1086)
- `src/Trizbort.Core/Automap/Automap.cs` — same locations (lines 942–952, 1077–1086)
- `src/Trizbort.Avalonia/App.axaml.cs` — `WireAutomapErrorReporter` (lines 41–66)
- `src/Trizbort.Avalonia/ViewModels/MainWindowViewModel.cs` — unexpected-exception catch (lines ~468–474)

## Implement
1. Core: `Automap.ErrorReporter` (static `Action<string,string>`) becomes `IEditorPrompts.ShowMessageAsync(title, message)` awaited by the engine; keep the static as a deprecated adapter for tests.
2. Core: engine wraps its run loop: on any exception stop, set status "Automapping halted.", rethrow nothing; surface via `AutomapFailed(Exception)` event.
3. Avalonia: remove `WireAutomapErrorReporter` and the 200 ms poll catch; the VM subscribes to `AutomapFailed` and resets `IsAutomapActive`.
4. Undo grouping: wrap the run in one session transaction (003) closed in `finally`.

## Tests
- Core: `AutomapErrorTests.cs` — missing transcript file, unreadable file, exception thrown by a fake canvas mid-run: stops, status set, one message, one undo step.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- Rows updated; committed as `parity(127): automap error handling`.

## Out of scope
- Disambiguation/same-direction prompts (011, 012).

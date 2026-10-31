# 061 — Add automap run-to-completion and single-step keys

Matrix rows: K068, K071          Priority: P3          Size: S
Depends on: 009, 010        Suggested tier: mid

## What
While automapping, `F5` runs to completion and `F11` performs one step (macOS `Primary+Shift+F11`; both also in the Automap menu).

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnKeyDown` default branch F5 and `Keys.F11` (~1398-1410)
- `src/Trizbort.Core/Automap/Automap.cs` — `RunToCompletion`, `Step` (grep -n)

## Implement
1. Core: `automap.run` (`F5`), `automap.step` (`F11`), `CanExecute = session.Automap.IsRunning`. Wiring only; the automap start/stop session commands come from the automap owner's tickets.
2. Do not copy the legacy 'any modifier except Ctrl' F5 rule; use exact modifiers.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/AutomapKeyCommandTests.cs` — disabled when not running, calls fake automap.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(061): Add automap run-to-completion and single-step keys`.

## Out of scope
- Automap engine.

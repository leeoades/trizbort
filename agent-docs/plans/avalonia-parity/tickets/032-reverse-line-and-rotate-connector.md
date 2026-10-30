# 032 — Add Reverse Line and Rotate Connector commands

Matrix rows: M051, K007, C006, K019, K054, K055, K056          Priority: P2          Size: S
Depends on: 002, 003, 009, 010        Suggested tier: mid

## What
`V` reverses the selected connections. `[` and `]` rotate the single selected connection's connector one way or the other; `Primary+[` and `Primary+]` rotate with the Ctrl flag.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `ReverseLineDirection` (line 744)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `OnKeyDown` bracket branch (`RotateConnector` call, grep -n 'RotateConnector')
- `src/Trizbort.Core/Domain/Elements/Connection.cs` — `Reverse`, `RotateConnector` (grep -n)

## Implement
1. Core: `connection.reverse` (`V`; Connection context target), `connection.rotate.cw|ccw|ctrlCw|ctrlCcw` with gestures `[`, `]`, `Primary+[`, `Primary+]` — match by physical key so non-US layouts work. Note the reference also rotated on `Shift+[`; do not replicate.
2. Bare `V` must not fire with Primary (paste). CanExecute: reverse >= 1 connection; rotate exactly 1 connection.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/ReverseRotateConnectorTests.cs` — reverse swaps source/target, rotate direction mapping, undo.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(032): Add Reverse Line and Rotate Connector commands`.

## Out of scope
- Port adjust (033).

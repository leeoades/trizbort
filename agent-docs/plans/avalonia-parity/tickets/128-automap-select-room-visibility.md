# 128 — Automap canvas ops parity: SelectRoom scrolls into view, dotted connections

Matrix rows: E049          Priority: P?          Size: S
Depends on: 002          Suggested tier: mid

## What
While automapping, the room currently being worked on is selected and scrolled into view (reference `MakeVisible`), and connections created for "dotted" exits (`UseDottedConnection`) are dashed. Verify both in Core's `AutomapCanvas` and fix gaps.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.Automap.cs` — `SelectRoom` (ends with `MakeVisible`, line ~247) and the connection-creation method (dashed style, lines ~86–90)
- `src/Trizbort.Core/Automap/AutomapCanvas.cs`; `src/Trizbort.Avalonia/Services/AvaloniaAutomapCanvas.cs`

## Implement
1. Write failing-first tests for both behaviours against `AutomapCanvas` with a fake session.
2. Core: `EditorSession.EnsureVisible(Element)` (viewport scroll with margin; no zoom change) if missing; call from `SelectRoom`. Respect the setting that disables follow if one exists (grep `Follow`).
3. Set connection style `Dashed` for dotted exits where the reference does.

## Tests
- Core: `AutomapCanvasParityTests.cs` as above.

## Done when
- Build + Core + Avalonia tests pass on Linux; E049 updated; committed as `parity(128): automap canvas parity`.

## Out of scope
- Prompts, status, errors (011, 012, 126, 127).

# 066 — Generate context menu per target with legacy visibility rules

Matrix rows: C015, G016, C019 (clean-up), C001          Priority: P2          Size: M
Depends on: 010, 005, 023, 024, 025, 026, 027, 028, 029, 030, 031, 032, 035, 037        Suggested tier: mid

## What
Right-click first selects the hit element; the menu shows items by target (Room, Connection, Label, Empty) exactly as the reference. Properties appears only when an element is hit, not for empty canvas. The menu is suppressed while panning and over the minimap.

## Reference (read only these)
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `CtxCanvasMenuOpening` (line 1830), designer item order in `Canvas.Designer.cs` 107-420

## Implement
1. Core/Avalonia: ensure each command from 023-035/037 declares `ContextTargets`; add a Core test that builds the menu model for each target. Keep Avalonia-only Cut/Copy/Paste/Delete/Add Map Label/Theme entries (C019) but make Paste CanExecute-driven (grey when clipboard empty).
2. Properties uses hit element, not stale `ActiveSelectedElement`.
3. Remove hand-written `ShowContextMenuAt` item building.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/ContextMenuModelTests.cs` — items per target match the lists in matrix section C.
- Avalonia: `src/Trizbort.Avalonia.Tests/ContextMenuWiringTests.cs`.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(066): Generate context menu per target with legacy visibility rules`.

## Out of scope
- Menu look and feel.

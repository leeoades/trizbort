# 132 — Rendering parity fixes from the export comparison (GitHub #22) + three UI bugs

Matrix rows: rendering (issue I-025), I-022, I-023, I-024          Priority: P1          Size: L
Depends on: —          Suggested tier: mid + strong review

## What
The legacy-vs-new export comparison (scripts/compare, 123 maps) found eight rendering differences (I-025 R1–R8) and a manual run found
three UI bugs (I-022 status-bar counts, I-023 room context-menu grouping, I-024 "NoRegion" in tooltips). Read `../issues.md` I-022..I-025.

## Reference (read only what each fix needs)
- Rooms: `$REF/src/Trizbort/Domain/Elements/Room.cs` `Draw` (text colour per region, dark tab corner and size, text layout/no wrap).
- Connections: `$REF/src/Trizbort/Domain/Elements/Connection.cs` `Draw` (arrowheads per flow/one-way, dotted pen, stalks and routing).
- Labels: `$REF/src/Trizbort/Domain/Elements/MapLabel.cs` `Draw` (default no outline/background).
- Bounds for export: `$REF/src/Trizbort/UI/Controls/Canvas.cs` `ComputeCanvasBounds` (connections count even with no rooms).
- New renderer: `src/Trizbort.Avalonia/Drawing/RoomDrawingAdapter.cs`, `ConnectionDrawingAdapter.cs` (or equivalent), label adapter, `DrawingAdapters.cs`; `src/Trizbort.Core/Export/ImageExportPlanner.cs` (R8).

## Implement
For each of R1–R8: find the reference behaviour, make the new adapter match, and add a focused test (geometry/colour assertions in `DrawingAdaptersParityTests` or pixel checks on a tiny rendered bitmap). R7: first find out why the route differs (stalk length, waypoints, curve/straight setting) and fix the cause; if it is a deliberate model difference, stop and report instead.
I-022: status bar counts update on EditorChange.Model. I-023: give settings commands a context group that sorts after the room-edit groups (reference order). I-024: show the region in tooltips only when it is not the default.

## Verify
After building, the integrator re-runs both capture tests and `scripts/compare/compare_exports.py`; the worst image scores should drop well below the current 18.5% / median 4.7%.

## Done when
- Build + Core + Avalonia tests pass; committed as `parity(132): rendering parity fixes (I-025) and UI fixes`.

# 001 — Add Selection and Viewport types to Core

Matrix rows: X010, X047, X048, X049 (foundation F1; D1)          Priority: P1          Size: S
Depends on: none        Suggested tier: mid

## What
No user-visible change. Create two Core types that become the single owners of selection and view state: an *ordered* `Selection` with a primary element (legacy `_selectedElements` semantics, replacing Avalonia's `HashSet` + `Project.ActiveSelectedElement`), and a `Viewport` (origin, zoom, size, transforms, zoom/fit maths). Nothing in Avalonia uses them yet (ticket 002 does).

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/decisions.md` — D1 points 1 and 6.
- `src/Trizbort.Avalonia/Services/CommandService.cs` — `Select(Element,bool,bool)`, `Select(IEnumerable,bool)`, `Deselect`, `SelectAll`, `ClearSelection` (lines ~167-262), `SetView`, `SetViewportSize`, `ZoomIn/ZoomOut/ResetZoom/FitToMap` (lines ~22-62, 412-436). Copy the semantics exactly, change only the data structure.
- `src/Trizbort.Core/Domain/Misc/CanvasTransform.cs` — reuse (compose, do not copy): `Origin`, `ZoomFactor`, `ViewportWidth/Height`, `CanvasToClient`, `ClientToCanvas`, `ZoomAt` (clamps 0.1–10), `FitToMap`, `GetVisibleWorldBounds`.
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `ZoomIn`, `ZoomInMicro`, `OnMouseWheel` (grep -n; step 1.25, micro 0.01, `InvertMouseWheel`).
- `src/Trizbort.Core/Domain/Application/Project.cs` — `ActiveSelectedElement` (leave as is; session ticket 002 keeps it in sync).

## Implement
Create folder `src/Trizbort.Core/Editor/`, namespace `Trizbort.Editor`.
1. `Selection.cs` — `public sealed class Selection`:
   - `IReadOnlyList<Element> Items`; `Element? Primary` (= last item, `null` if empty); `int Count`; `bool Contains(Element)`.
   - `IEnumerable<Room> Rooms`, `IEnumerable<Connection> Connections` (OfType, keep order).
   - `bool Set(IEnumerable<Element>)`, `bool Add(Element)` (re-adding moves it to the end = becomes primary), `bool Remove(Element)`, `bool Toggle(Element)`, `bool Clear()`; each returns true if the selection changed.
   - `bool Click(Element element, bool toggle, bool add)` — same rules as the current `CommandService.Select(element, toggle, addToSelection)`.
   - `event EventHandler? Changed` raised once per mutating call that changed something.
   - No Avalonia / `Project` dependency. Duplicates are never stored.
2. `Viewport.cs` — `public sealed class Viewport` wrapping a private `CanvasTransform`:
   - `Vector Origin`, `float ZoomFactor`, `double Width`, `double Height` (get; change through methods), `Vector CanvasToClient(Vector)`, `Vector ClientToCanvas(Vector)`, `Rect VisibleWorldBounds`.
   - `void SetSize(double w, double h)`, `void SetView(Vector origin, float zoom)` (clamp zoom 0.1–10), `void Pan(Vector worldDelta)`, `void PanClient(Vector clientDelta)` (divides by zoom).
   - `void ZoomIn()`, `ZoomOut()` (x1.25 / ÷1.25 about the viewport centre; match `CommandService`), `void ZoomBy(float factor, Vector clientPoint)`, `void ZoomWheel(int direction, Vector clientPoint, bool micro, bool invertWheel)` (micro = ±0.01 per notch; `invertWheel` flips direction), `void ResetZoom()`, `void FitToMap(IEnumerable<Element>)` (delegate to `CanvasTransform.FitToMap`), `void CenterOn(Rect worldBounds)` (set Origin to centre, no zoom change; X001).
   - `static Rect ComputeCanvasBounds(IEnumerable<Element> elements, float padding = 0)` — union of `UnionBoundsWith(…, true)` (X049; Avalonia's static `MapCanvasControl.ComputeCanvasBounds` stays until a later ticket).
   - `event EventHandler? Changed`.
3. Do NOT touch Avalonia files or `Project`.

## Tests
- `src/Trizbort.Core.Tests/SelectionTests.cs`: `Add_MakesElementPrimary`, `Add_Existing_MovesToEnd`, `Remove_Primary_FallsBackToPreviousItem` (the ordering bug the HashSet had), `Toggle_AddsThenRemoves`, `Click_Plain_ReplacesSelection`, `Click_Add_AppendsWithoutClearing`, `Click_Toggle_RemovesWhenAlreadySelected`, `Clear_OnEmpty_ReturnsFalseAndDoesNotRaise`, `Changed_RaisedOncePerCall`, `Rooms_Filters_KeepsOrder`.
- `src/Trizbort.Core.Tests/ViewportTests.cs`: `ClientToCanvas_RoundTrips`, `ZoomBy_KeepsWorldPointUnderCursor`, `ZoomIn_ThenOut_ReturnsToOriginalZoom`, `Zoom_ClampsTo_0_1_And_10`, `ZoomWheel_Invert_ReversesDirection`, `ZoomWheel_Micro_Steps_0_01`, `FitToMap_Empty_ResetsOriginAndZoom`, `FitToMap_NeverZoomsInPast1`, `CenterOn_SetsOriginToCentreAndKeepsZoom`, `Changed_RaisedOnPanAndZoom`. Build rooms with `new Room(Project.Current)` as in `MapLabelTests.cs` does for its elements (follow that file's setup).

## Done when
- Build + Core tests + Avalonia tests pass on Linux (Avalonia untouched).
- Matrix rows X010, X047, X048 get a note "types in Core: ticket 001" (status stays until 002/004 consume them).
- Committed as `parity(001): Add Selection and Viewport types to Core`.

## Out of scope
- Using the types from Avalonia (ticket 002). Undo (003). Select-special queries (X008), wheel wiring, zoom presets 50%/200%/±1% (later feature tickets). Removing `Project.ActiveSelectedElement`.

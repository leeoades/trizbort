# 005 — Move canvas hit-testing to Core (ZOrder-aware)

Matrix rows: X034, X035, X036, X046 (hover half), G001, G013, G016 (foundation F5a; D1 points 4 and 6)          Priority: P1          Size: M
Depends on: 002        Suggested tier: mid

## What
Finding "what is under this point" (element, resize handle, port, curve waypoint) becomes a pure Core service taking world coordinates. It honours `ZOrder` (topmost wins among elements within the snap distance), as legacy does; Avalonia currently ignores `ZOrder`. No other UI change.

## Reference (read only these)
- `src/Trizbort.Avalonia/Controls/MapCanvasControl.cs` — `GetVisibleWaypoints` (~529), `GetWaypointBounds`, `HitTestWaypoint` (~548), `HitTestHandle` (~565), `HitTestPort` (~578), `HitTestElement(canvasPos, includeMargins)` (~607-630); also grep `_hoverPort`/`_hoverHandle`/`_visibleHandles` / `RecreateHandles`-like code (~476-605) that computes which handles/ports are active.
- `$REF/src/Trizbort/UI/Controls/Canvas.cs` — `HitTestElement`, `DepthSortElements`, `HitTestHandle`, `HitTestPort`, `HitTestWaypoint`, `RecreateHandles`, `RecreatePorts` (grep -n; lines ~1967, 2177-2223, 2385-2454, 2829-2872).
- `src/Trizbort.Core/Domain/Elements/Element.cs` — `Distance`, `Intersects`, `CompareTo` (ZOrder).
- `src/Trizbort.Core/Domain/Elements/Connection.cs` — `GetCurveWaypoint*`, `CanAddCurveWaypoint`; `ResizeHandle`, `Port`, `MoveablePort` classes (grep `class ResizeHandle`).
- `agent-docs/plans/avalonia-parity/decisions.md` — D1 "Facts checked" (hit-testing needs no text measurement).

## Implement
Create `src/Trizbort.Core/Editor/HitTester.cs` (namespace `Trizbort.Editor`):
1. `public sealed class HitTester { public HitTester(EditorSession session); }` plus pure methods (all in world coordinates; snap distance passed in or read from `Settings.SnapToElementSize`/`Settings.HandleSize`, same as the Avalonia code does today):
   - `Element? HitElement(Vector world, bool includeMargins = false)` — collect elements with `Distance(world, includeMargins) <= snap`; among them return the topmost: sort ascending by `Element.CompareTo` (ZOrder, then list order) and take the last. If two are within snap, nearest does NOT beat topmost (legacy; matrix X034).
   - `IReadOnlyList<Element> HitRect(Rect world)` — elements where `Intersects(world)` (used for the marquee, ticket 006).
   - `IReadOnlyList<ResizeHandle> ActiveHandles()` — 8 handles when exactly one `ISizeable` is selected, else empty (port the existing logic); `ResizeHandle? HitHandle(Vector world)`.
   - `IReadOnlyList<Port> ActivePorts(Element? hoverRoom)` — the ports to show: ports of the hovered/selected room, and movable ports of the single selected connection; `Port? HitPort(Vector world)`.
   - `IEnumerable<CurveWaypoint> VisibleWaypoints(Connection)`, `Rect WaypointBounds(CurveWaypoint)`, `CurveWaypoint? HitWaypoint(Vector world)` (single selected connection only).
   - A record `HitResult(Element? Element, ResizeHandle? Handle, Port? Port, CurveWaypoint? Waypoint)` and `HitResult HitAll(Vector world)` using the **press priority** decided here: waypoint, then handle, then port, then element (legacy order; Avalonia's current order was handle, port, waypoint, element — document in the XML doc comment).
2. No Avalonia types; `Vector`/`Rect` are the Core ones in `Trizbort.Domain.Misc`.
3. Change `MapCanvasControl`: delete its private `HitTest*`/`GetVisibleWaypoints`/`GetWaypointBounds` methods and call `CommandService.Session` → `new HitTester(session)` (field `_hit`). Keep call sites and the `_hoverX` fields as they are. Preserve `includeMargins` use at ~927.
4. If the UnionBoundsWith/Distance of any element calls a text-measuring routine (D1 says not), STOP and write the question in the ticket's Notes rather than adding `ITextMeasurer`.

## Tests
`src/Trizbort.Core.Tests/HitTesterTests.cs`:
- `HitElement_OverlappingRooms_ReturnsHigherZOrder`; `HitElement_EqualZOrder_ReturnsLaterInList`; `HitElement_FarPoint_ReturnsNull`; `HitElement_Label_Hit`; `HitElement_Connection_NearLine_Hit`.
- `HitHandle_OnlyWhenSingleSizeableSelected`; `HitHandle_CornerAndEdgePositions`.
- `HitPort_HoveredRoomPorts`; `HitPort_MovablePortOfSelectedConnection`.
- `HitWaypoint_OnlySingleSelectedConnection`; `HitAll_PrefersWaypointOverHandleOverPortOverElement`.
- `HitRect_ReturnsIntersectingElements`.
Existing `MapCanvasControlGestureTests.cs` must pass unchanged (if one encoded the old press order or non-ZOrder behaviour, update it and say so in the commit).

## Done when
- Build + Core + Avalonia tests pass on Linux.
- `MapCanvasControl.cs` no longer contains `HitTestElement`/`HitTestPort`/`HitTestHandle`/`HitTestWaypoint`.
- Matrix rows X034–X036 updated (Implemented, test names as evidence); commit `parity(005): Move hit-testing to Core`.

## Out of scope
- The drag state machine and cursors (006), tooltips text (stays `BuildToolTipText`), waypoint handle scaling with zoom (legacy `WaypointHandleScale`; later), Ctrl+click reference-room jump (G003).

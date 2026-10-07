# Domain Model: Elements, Project, Geometry

See [`README.md`](README.md) for the doc map and [`01-architecture-overview.md`](01-architecture-overview.md)
for the overall layering.

## `Project` — the whole in-memory document

`Domain\Application\Project.cs`

- Singleton-ish current document: `Project.Current` (static property, raises `ProjectChanged`
  when reassigned).
- **One flat collection holds everything**: `public BoundList<Element> Elements`. There is no
  separate `Rooms`/`Connections`/`Regions` collection — filter `Elements.OfType<Room>()` /
  `.OfType<Connection>()` as needed (this is exactly what the exporter and controllers do).
- Document metadata: `Author`, `Description`, `Title`, `History`, `FileName`, `Name`,
  `HasFileName`, `Version`, `IsDirty`.
- Document-level validation toggles: `MustHaveDescription`, `MustHaveNoDanglingConnectors`,
  `MustHaveSubtitle`, `MustHaveUniqueNames`.
- `IsElementIDInUse(id)` / `FindElement(id)` — ID bookkeeping (see below).
- `GetSelectedElements()` delegates to the UI canvas.
- `Save()`/`Load()` delegate to `MapSaver`/`MapLoader` — see
  [`03-storage-and-persistence.md`](03-storage-and-persistence.md).
- `onElementRemoved` automatically deletes any `Connection` whose vertex referenced the removed
  element — i.e. deleting a room cleans up connections pointing at it. Don't try to duplicate
  this cleanup elsewhere.
- **No undo/redo history lives here** — `History` is just a free-text document property (map
  changelog text), not an edit-history stack. See
  [`04-commands-and-controllers.md`](04-commands-and-controllers.md).

## `Element` — base class for everything drawable

`Domain\Elements\Element.cs`

- Identity is a plain **positive `int` ID**, not a GUID. Setting `ID` is guarded by
  `Project.IsElementIDInUse` so you can't collide with another element. New elements normally
  get `GetNextID()` (first unused positive int); a separate fast-path constructor accepts a
  precomputed ID during file load.
- Holds `Position`-adjacent concerns common to drawables: ports list (`[JsonIgnore]` — runtime
  only, not serialized as JSON; the real persisted form goes through XML, see the storage doc),
  back-reference to owning `Project` (also `[JsonIgnore]`), `Depth`/`ZOrder` (drawing/ordering),
  `Changed`/`Disposed` events.
- Draw order: by `Depth`, then `ZOrder`, then `ID`. `Room` has lower `Depth` than `Connection`
  (rooms draw first, then connections on top).

## `Room : Element, ISizeable`

`Domain\Elements\Room.cs`

- The spatial map node. Key state: `Name`, `Subtitle`, descriptions, `Objects` (free text,
  later parsed by the exporter into structured "things" — see
  [`07-export-subsystem.md`](07-export-subsystem.md)), `Region` (plain string), `IsDark`,
  `IsStartRoom`, `IsEndRoom`.
- Shape/rendering: `Shape`, `Ellipse`, `Octagonal`, `RoundedCorners`, `StraightEdges`, corner
  radii, border styles, fills, colors.
- Geometry: `Position`/`Size` as `Vector` (floats, not pixels — see
  [`05-ui-and-canvas.md`](05-ui-and-canvas.md) for the world↔screen transform), `X`/`Y`/`Width`/
  `Height` accessors, `InnerBounds`.
- Default size/position is derived from `Settings.GridSize` (grid-cell-relative sizing).
- **Gotcha**: setting `Position` clears `ArbitraryAutomappedPosition` as a side effect — code
  that programmatically repositions rooms (e.g. automap layout) should be aware of this flag's
  semantics rather than assuming `Position` is a "dumb" setter.
- Ports are created once per room in the constructor (`addPortsToRoom()`) for directional
  connection attachment (N/S/E/W/NE/etc., up/down/in/out).
- `ReferenceRoomId` + computed `ReferenceRoom`: rooms can alias another room by ID (resolved
  dynamically via `Project.Current.Elements`, not a direct object reference — don't cache the
  resolved reference across structural edits).
- Validation: `ValidationState` (list of `RoomValidationState`, see
  `Domain\RoomValidationState.cs`) populated by `CheckValidation()`.
- Connection-related helpers: `IsConnected`, `GetConnections()`, `DeleteAllRoomConnections()`,
  `AdjustAllRoomConnections()` (re-anchors connector attachment points after the room moves —
  call this, or go through code paths that already call it, after any bulk room repositioning).

## `Connection : Element`

`Domain\Elements\Connection.cs`

- Represents a connector/edge. Stores a `VertexList : BoundList<Vertex>` (the polyline geometry),
  `StartText`/`MidText`/`EndText`, style/flow/color/name, an optional `Door`.
- **Important**: `Connection` has no `Room Source`/`Room Target` properties. Each `Vertex` has a
  `Port`, and the port's owner identifies what's connected. Use the accessor methods instead of
  assuming direct references:
  ```csharp
  connection.GetSourceRoom();
  connection.GetSourceRoom(out CompassPoint sourceCompassPoint);
  connection.GetTargetRoom();
  connection.GetTargetRoom(out CompassPoint targetCompassPoint);
  ```
  This also means a connection can legitimately be dangling (port owner is null/non-room),
  self-looping (both ends on the same room), or attached to a non-room port.
- `VertexPort` (nested in `Connection`) adapts a vertex as a moveable port; its `ID` is the
  **vertex's index in `VertexList`**, not an `Element` ID — don't confuse the two ID spaces.
- Mutating helpers: `Reverse()`, `RotateConnector(...)`, `SetText(...)`,
  `RecomputeSmartLineSegments(...)`.
- Persistence is two-phase on load (`BeginLoad`/`EndLoad`) because a connection's endpoints may
  reference rooms that haven't been loaded yet — see
  [`03-storage-and-persistence.md`](03-storage-and-persistence.md).

## `Door`

`Domain\Elements\Door.cs` — a plain value object (`Lockable`, `Locked`, `Open`, `Openable`), not
an `Element`. It has no ID and no behavior; it's just metadata hung off an optional
`Connection.Door` property.

## `IMoveable` / `ISizeable`

`Domain\Elements\IMoveable.cs`, `ISizeable.cs` — internal interfaces. `IMoveable` requires
`Position`/`X`/`Y`; `ISizeable : IMoveable` adds `Size`/`Width`/`Height`/`InnerBounds`. In
practice only `Room` implements `ISizeable` — connections use vertex/line geometry instead of a
rectangular size.

## Supporting types (`Domain\Misc`, `Domain\Enums`)

- **Geometry/rendering**: `Vector`, `Rect`, `Vertex`, `LineSegment`, `Port`/`MoveablePort`,
  `Drawing`/`DrawingContext`, `TextBlock` (word-wrap, used for on-canvas text, *not* for
  generated export source — see [`07-export-subsystem.md`](07-export-subsystem.md)).
- **`BoundList<T>`**: the observable collection type backing `Project.Elements` and
  `Connection.VertexList`. Its change events drive `Project`'s auto-cleanup of dangling
  connections and UI redraw/binding — don't bypass it with raw list manipulation if you need
  those side effects to fire.
- **Enums**: `CompassPoint`, `Depth`, `BorderDashStyle`, `LightingActionType`,
  `MappableDirection` (full set enumerated by `Domain\Directions.cs:AllDirections`),
  `SelectTypes`, `ValidationType`.
- **`Domain\Cache\Indexer.cs`/`FindCacheItem.cs`**: builds a transient, non-persisted search
  index over room name/description/objects for the `QuickFind` dialog — rebuilt on demand, not
  part of project state.
- **`Domain\Watchers\TrizbortFileWatcher.cs`**: wraps `FileSystemWatcher` on the currently open
  map file; prompts to reload on external changes (warns if the in-memory project is dirty).
- **`Domain\StatusBar\`**: `Status`, `IStatusWidget`, `CapsLockStatusWidget`,
  `NumLockStatusWidget`, `ZoomStatusWidget` — UI status-bar adapters, not domain state.

## Adding a new persisted room/connection property — checklist

If you add a new field to `Room`/`Connection` that should round-trip through saved maps:

1. Add the property to the class.
2. Write it in `Room.Save(XmlScribe)` / `Connection.Save(XmlScribe)`.
3. Read it back in `Room.Load(XmlElementReader)` / `Connection.BeginLoad(XmlElementReader)`
   (or `EndLoad` if it needs other elements resolved first).
4. If it should survive copy/paste, also add it to the corresponding DTO in
   `Domain\Controllers\CopyController.cs` (`CopyRoomObj`/`CopyConnectionObj`).
5. See [`03-storage-and-persistence.md`](03-storage-and-persistence.md) for the full save/load
   pipeline and why there's no automatic serialization here (`[JsonIgnore]` on most of this
   graph — the map format is hand-written XML, not JSON).

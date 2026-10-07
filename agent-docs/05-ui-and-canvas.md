# UI & Canvas

See [`README.md`](README.md) for the doc map. For what `Room`/`Connection`/`Project` mean, see
[`02-domain-model.md`](02-domain-model.md); for the controller layer `MainForm` calls into, see
[`04-commands-and-controllers.md`](04-commands-and-controllers.md).

## `UI\MainForm.cs` / `MainForm.Designer.cs` — the application shell

- Owns the main `Canvas`, status bar, menu/toolbar, and wires `Application.Idle` for periodic
  UI/background processing.
- Creates a `CommandController` bound to the `Canvas` — most menu handlers delegate to it rather
  than mutating domain state directly (see
  [`04-commands-and-controllers.md`](04-commands-and-controllers.md)).
- Owns project lifecycle: `OpenProject()` (file dialog → load), `OnClosing()` (prompt to save
  dirty work, persist window/canvas size + app settings, stop automapping, dispose the file
  watcher). `checkLoseProject()` gates any destructive operation (new/open/exit) on unsaved
  changes.
- Handles command-line actions (`commandLineActions`) — see
  [`08-cli-and-entry-point.md`](08-cli-and-entry-point.md) for the full flag→action mapping.
- Hosts every language/image/PDF export menu handler — see
  [`07-export-subsystem.md`](07-export-subsystem.md).

Top-level menus (from `MainForm.Designer.cs`): **File**, **Edit**, **View**, **Automap**,
**Tools**, **Help**. If you're adding a new command, find the nearest sibling menu item in the
Designer file and follow its wiring (`Click` handler name → method in `MainForm.cs`, usually
calling into `CommandController`/`Canvas`/an exporter).

## `UI\Controls\` — the drawing surface and supporting controls

| File | Role |
|---|---|
| `Canvas.cs` | Core: selection state, zoom/pan/origin, world↔screen coordinate conversion, `OnPaint`/drawing, scrolling, keyboard input, mouse input, dragging, connection drawing. |
| `Canvas.Automap.cs` | Partial class: automap-specific room/connection creation, placement and tidy-layout logic. Implements `IAutomapCanvas` for map operations; `Automap\Automap.cs` still directly depends on WinForms for dialogs and message boxes. |
| `Canvas.Designer.cs` | Generated layout: scrollbars, minimap, corner panel, zoom label. |
| `AutomapBar.cs`/`.Designer.cs` | Automapping progress/status bar + stop button. |
| `Minimap.cs`/`.Designer.cs` | Small overview/navigation map. |
| `TrizbortTextBox.cs` | Customized text box used in property/settings dialogs. |
| `TrizbortToolTip.cs` | Customized tooltip behavior. |

### Rendering loop

`Canvas.OnPaint()` wraps the native `Graphics` with `XGraphics.FromGraphics(...)` (PDFsharp's
drawing abstraction — the same drawing code path is reused for image/PDF export, see
[`07-export-subsystem.md`](07-export-subsystem.md)) and calls `Draw(graphics, ..., Width,
Height)`, which: optionally adjusts zoom/origin for export, draws the grid, applies the world
transform, draws elements in `Depth`/`ZOrder` order, then draws selection handles/ports/marquee
on top. Double buffering and resize-redraw are enabled in the constructor.

### World ↔ screen coordinate transform

Map/world coordinates are `Vector` (floats); screen coordinates are WinForms pixels. The
relationship, centered on the viewport:

```
screen = (world - Origin) * ZoomFactor + (Width/2, Height/2)
world  = (screen - (Width/2, Height/2)) / ZoomFactor + Origin
```

Implemented as `Canvas.CanvasToClient(Vector)` and `Canvas.ClientToCanvas(PointF)`
respectively; sizes scale by `ZoomFactor` alone (no translation). The paint transform
(`Draw()`) applies the equivalent `Translate → Scale → Translate` sequence directly to the
`Graphics`/`XGraphics` object. Mouse-wheel zoom adjusts `Origin` so the world point under the
cursor stays fixed on screen (compute world point before and after the zoom change, offset
`Origin` by the delta).

**If you're debugging "things are drawn in the wrong place"**, check whether the bug is in the
transform (`Origin`/`ZoomFactor`/viewport size) vs. in the element's own `Position`/`Size` — the
two are independent concerns.

### Input → domain mutation: what goes through the controller and what doesn't

- **Discrete, semantic commands** (open properties dialog, change selection, set connection
  style/flow/label, toggle room lighting/shape, delete) are dispatched through
  `CommandController` from `Canvas.OnKeyDown()` and menu handlers.
- **Continuous geometric interaction** (panning, dragging a room, drawing a new connection,
  resizing) is handled directly inside `Canvas.cs` — it converts the mouse point to world space
  via `ClientToCanvas()` and mutates `Room.Position`/`Size` or `Connection.VertexList` directly,
  without going through a controller. This is intentional, not an inconsistency to "fix" —
  controllers are a convenience façade, not a mandatory gate (see
  [`04-commands-and-controllers.md`](04-commands-and-controllers.md)).
- **Automap** (`Canvas.Automap.cs`) similarly mutates `Project.Current.Elements` and
  `Room.Position` directly, driven by `Automap\Automap.cs` rather than user input — see
  [`06-automap.md`](06-automap.md).

## Dialog catalogue (`UI\*.cs`)

| Dialog | Purpose |
|---|---|
| `AboutDialog` | Application/version/about info and links. |
| `AppSettingsDialog` | Edits app-wide preferences backed by `ApplicationSettingsController` (see [`03-storage-and-persistence.md`](03-storage-and-persistence.md)): save behavior, margins, zoom, tooltips, automap defaults. |
| `AutomapDialog` | Configures and starts a transcript automap run — see [`06-automap.md`](06-automap.md). |
| `AutomapRoomSameDirectionDialog` | Resolves "a room already exists in this direction" ambiguity during automap. |
| `ConnectionPropertiesDialog` | Edits a connection's label, direction/flow, door state, color. |
| `DisambiguateRoomsDialog` | Lets the user pick which existing room (or "new room") a same-named transcript room refers to during automap. |
| `InputDialog` | Reusable generic single/multi-field input prompt (OK/Cancel/Yes/No/Save variants). |
| `MapStatisticsView` | Displays room counts, regions, bounds, etc. for the current project. |
| `QuickFind` | Search UI over rooms, backed by `Domain\Cache\Indexer`. |
| `RegionSettings` | Edits region data/colors; regions are just a string property on `Room`, grouped here for editing. |
| `RoomPropertiesDialog` | Edits a room's name/description/objects/colors/shape/region/start-room/reference-room state. |
| `SettingsDialog` | Edits per-map drawing settings (fonts/colors/grid/room/connection defaults/regions) — these are the same settings persisted in the map file's `<settings>` block. Also owns the one `Properties.Settings` usage (`SettingsLastTabIndex`) — this dialog's `FormClosing` handler is the exact code path that was involved in the .NET 8 port's `ConfigurationErrorsException` bug, see [`09-build-test-and-dotnet8-port.md`](09-build-test-and-dotnet8-port.md). |

## `Util\` quick reference

- `XmlScribe.cs` / `XmlElementReader.cs` / `XmlAttributeReader.cs` — map file XML
  read/write helpers (full detail in [`03-storage-and-persistence.md`](03-storage-and-persistence.md)).
- `StringFormats.cs` — shared string/formatting helpers.
- `Smoothing.cs` — smoothing/interpolation helpers used by rendering.
- `PathHelper.cs` — safe path/directory/filename handling (used by `MainForm`'s open/save
  dialogs).
- `KeyboardHelper.cs` — the app's only P/Invoke surface (`user32.dll` `keybd_event`/
  `GetKeyState`) for num-lock/caps-lock/scroll-lock indicators.
- `ClipboardHelper.cs` — clipboard read/write helpers backing `CopyController`.

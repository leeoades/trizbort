# UI & Canvas

> **Layout change (2026-10-30):** the refactored WinForms projects have been removed. The original WinForms app
> (tag `v2.3.1-beta.2`) now lives in `legacy/` with its own `legacy/Trizbort.Legacy.sln` — see `legacy/README.md`.
> WinForms sections of these docs describe the refactored app; for the original, paths are under `legacy/src/Trizbort/`
> and its Domain/Automap/Export code lives inside that project rather than in `Trizbort.Core`.

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
  watcher). `CheckLoseProject()` gates any destructive operation (new/open/exit) on unsaved
  changes.
- Handles command-line actions (`CommandLineActions`) — see
  [`08-cli-and-entry-point.md`](08-cli-and-entry-point.md) for the full flag→action mapping.
- Hosts every language/image/PDF export menu handler — see
  [`07-export-subsystem.md`](07-export-subsystem.md).

Top-level menus (from `MainForm.Designer.cs`): **File**, **Edit**, **View**, **Automap**,
**Tools**, **Help**. If you're adding a new command, find the nearest sibling menu item in the
Designer file and follow its wiring (`Click` handler name → method in `MainForm.cs`, usually
calling into `CommandController`/`Canvas`/an exporter).

`MainForm.Themes.cs` adds **Tools → Themes** immediately after Map Settings at runtime.
It provides Classic, Parchment, Dark, High contrast and Sketch (hand-drawn) presets plus theme import/export.
Maps without individual style overrides apply themes immediately, including room shapes and
label colours. Otherwise, application asks whether to replace individual styles (Yes),
preserve them (No, the default), or cancel. Detection uses `MapTheme.HasIndividualStyles`;
errors are displayed without silently falling back to another font.
**Infer default room style from rooms...** uses `Setup\RoomStyleInference.cs` to preview and
then promote majority room styling to map defaults (see the storage reference).
See the theme format and preservation rules in the storage reference.

## `UI\Controls\` — the drawing surface and supporting controls

| File | Role |
|---|---|
| `Canvas.cs` | Core: selection state, zoom/pan/origin, world↔screen coordinate conversion, `OnPaint`/drawing, scrolling, keyboard input, mouse input, dragging, connection drawing. |
| `Canvas.Automap.cs` | Partial class: automap-specific room/connection creation, placement and tidy-layout logic. Implements `IAutomapCanvas` for map operations; `Automap\Automap.cs` still directly depends on WinForms for dialogs and message boxes. |
| `Canvas.Designer.cs` | Generated layout: docked scrollbars, minimap, zoom label. No separate scrollbar corner panel is needed. |
| `AutomapBar.cs`/`.Designer.cs` | Automapping progress/status bar + stop button. |
| `Minimap.cs`/`.Designer.cs` | Small overview/navigation map. |
| `TrizbortTextBox.cs` | Customized text box used in property/settings dialogs. |
| `TrizbortToolTip.cs` | Customized tooltip behavior. |

Canvas element tooltips use native `ToolTip.SetToolTip` registration rather than immediate
`Show` calls: Windows handles cursor-relative placement and hit testing. Initial and reshow
delays are 500 ms and auto-dismiss is 5 seconds. Hovering another element updates the content;
clicking, leaving the canvas, moving elements or panning dismisses both pending and visible
tips. Tooltip content retains the application description/object preferences and owner-drawn
layout, and hovering connections must not mutate their labels.

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
  resizing) is coordinated by `Canvas.cs`, converting mouse points with `ClientToCanvas()`.
  Selected-element movement and accumulated resize deltas delegate to internal
  `Domain\Misc\MapEditing`; connection drawing and event state remain in Canvas.
  This does not go through a controller and is intentional, not an inconsistency to "fix" —
  controllers are a convenience façade, not a mandatory gate (see
  [`04-commands-and-controllers.md`](04-commands-and-controllers.md)).
- **Automap** (`Canvas.Automap.cs`) similarly mutates `Project.Current.Elements` and
  `Room.Position` directly, driven by `Automap\Automap.cs` rather than user input — see
  [`06-automap.md`](06-automap.md).

Canvas shares selection movement and tooltip dismissal between mouse dragging and arrow keys;
keyboard and mouse panning also dismiss the old tooltip. Regression tests seed tooltip lifecycle
state without native popups and exercise movement of rooms, labels and free connections.
Tooltips are registered with the Canvas as their owner; Windows supplies cursor-relative
placement. Tests check native delay configuration, content and dismissal without showing popups.
`MapEditing.Move` is shared by mouse dragging and arrow-key movement. It moves free vertices
and explicit connection waypoints, keeps docked endpoints attached, and translates unselected
curve waypoints exactly once when both owners move. `MapEditing.Resize` tracks applied
movement (not raw cursor displacement) to preserve grid snapping/minimum-size behavior.
Approximate compass comparisons delegate to `CompassPointHelper.IsSameApproximateDirection`,
also used by statistics without a main-form dependency.

STA integration tests invoke Canvas's real protected mouse/key/wheel handlers through the
`TestCanvas` subclass, without reflection. Canvas is intentionally inheritable for this;
internal read-only tooltip/handle/port diagnostics avoid exposing mutable collections.
They cover resize handles, drag thresholds, movement, selection,
paste and zoom anchoring; off-screen rendering checks exercise the production drawing path.
They do not establish OS capture/cursor behavior or full visual equivalence.

### Connection curve waypoint handles

When exactly one two-vertex `Connection` is selected, `Canvas` draws round waypoint handles
(`DrawWaypointHandles`, hit-tested by `HitTestWaypoint` before resize handles/ports): 2×
`Settings.HandleSize` for set waypoints, 1.5× translucent "insert" handles for addable empty
slots (the midpoint of a straight line; then the 25%/75% points on the curve), scaled so they
never shrink on screen when zoomed out (`WaypointHandleScale`). Dragging uses `DragModes.MoveWaypoint`;
an insert handle only creates a waypoint once dragged past `Settings.DragDistanceToInitiateNewConnection`.
The clicked/dragged waypoint becomes `_selectedWaypoint`, and `DeleteSelection()` (Delete key)
removes that waypoint instead of the connection. Any selection change clears it. Dragging a
selected connection moves its waypoints; dragging/arrow-moving rooms also moves the waypoints of
unselected connections whose *both* ends are docked to moved rooms.

### New-room defaults

`Canvas` remembers the last selected/changed room (`SetRoomDefaultsFrom`, `_newRoomStyleSource`).
When the app setting `ApplyStyleToNewRooms` (*Application Settings → Map → Preferences → Apply style to new rooms*, default off) is enabled, `AddRoom()` (the `R` hotkey / *Add Room* menu) copies that room's styling onto the new room via
`Room.CopyStyleFrom()` (shape, corners, border, colours, region, dark, objects position — not
name/objects/descriptions), and uses its size. When `DoubleClickToAddRoom` (*Map → Preferences → Double click to add room*, default off) is enabled, `OnMouseDoubleClick` on empty canvas (no element/handle/port hit) calls `AddRoom(true)`. `Canvas.Reset()` (new/open project) clears the source.

## Dialog catalogue (`UI\*.cs`)

### Replaceable desktop interaction boundary

`UI\UserInteraction.cs` routes application message boxes, modal forms, common dialogs
(file/font/colour pickers) and clipboard text access through internal `IUserInteraction`.
Its default Windows adapter retains owner, message, buttons, icon, default button and dialog
result behavior. Call this boundary rather than directly using `MessageBox`, native
`Form.ShowDialog`/`CommonDialog.ShowDialog`, or `Clipboard` at new application call sites.
Domain `Element.ShowDialog` methods still prepare/apply properties, but their actual form
presentation uses the boundary. Room/connection dialog owners permit no main form for tests.
Canvas same-name Automap disambiguation and the file-watcher prompt also use this boundary.

`TestEnvironment` installs a fail-fast implementation: unexpected UI/clipboard access raises an
assertion instead of blocking the runner. `UserInteractionTests` substitutes recording/editing
callbacks and covers public persistence errors/warnings, room/connection OK vs Cancel, colour
selection, message choices and clipboard copy/paste without opening windows. Restore the
provider after substitutions; this application still has process-global state and those
tests are nonparallel. Existing layout/focus/keyboard tests explicitly use `TestDialog` to
pump real forms at zero opacity with no taskbar entry; they do not show visible dialogs.
Rendering/control layout stays in WinForms, not behind a synthetic widget abstraction.

Tests access internal types via `InternalsVisibleTo("Trizbort.Tests")`, construct dialogs and
exporters directly, and use typed `RoomPropertiesDialog.LastClosedTab` state and file-watcher
diagnostics. Reflective enumeration remains appropriate for the naming-convention test;
assembly metadata access remains the source of the app's version.

### Map labels

`Canvas.AddLabel` is exposed by **Edit → Add Label**, the canvas context menu and the **L**
key. It places a new annotation at the cursor (keyboard/context menu) or viewport centre
(Edit menu), selects it and opens `LabelPropertiesDialog`. The dialog edits multiline text,
shape, outline style, background visibility and three colours. Outline and Background have
separate groups with Enabled checkboxes and their related controls. Disabling Outline saves
`BorderStyle.None`; its line style/colour controls are disabled. Background colour is enabled
only with its checkbox. Shape is editable when either outline or background is enabled, since
it determines both.
Double-click/Enter/Properties reopens it. OK applies changes; Cancel leaves existing properties
untouched. Labels use the normal selection, movement, resize handles, keyboard resizing,
copy/paste, z-order and delete paths; connectors can be drawn from their compass ports exactly
as for rooms. Their bounds appear in the minimap overview. They appear in image/PDF rendering
but not IF-language source export.

| Dialog | Purpose |
|---|---|
| `AboutDialog` | Application/version/about info and links. |
| `OnlineHelpDialog` | Opened by Help → Online Help or F1; links first to the v2 Markdown user guide (`Docs/index.md` on GitHub), then to the original v1 help it is based on. |
| `AppSettingsDialog` | Edits app-wide preferences backed by `ApplicationSettingsController` (see [`03-storage-and-persistence.md`](03-storage-and-persistence.md)): save behavior, margins, zoom, tooltips, automap defaults. Tabs: General, ToolTips, Map (Preferences: apply style to new rooms, double click to add room). |
| `AutomapDialog` | Configures and starts a transcript automap run — see [`06-automap.md`](06-automap.md). |
| `AutomapRoomSameDirectionDialog` | Resolves "a room already exists in this direction" ambiguity during automap. |
| `ConnectionPropertiesDialog` | Edits a connection's label, direction/flow, door state, color. |
| `DisambiguateRoomsDialog` | Lets the user pick which existing room (or "new room") a same-named transcript room refers to during automap. |
| `InputDialog` | Reusable generic single/multi-field input prompt (OK/Cancel/Yes/No/Save variants). |
| `MapStatisticsView` | Displays room counts, regions, bounds, etc. for the current project. |
| `QuickFind` | Search UI over rooms, backed by `Domain\Cache\Indexer`. |
| `RegionSettings` | Edits region data/colors; regions are just a string property on `Room`, grouped here for editing. |
| `RoomPropertiesDialog` | Edits a room's name/description/objects/colors/shape/region/start-room/reference-room state. Initially opens on Objects with its text box focused, then restores the tab selected when the last room dialog closed (including OK or Cancel), across all rooms and maps in the current process. This state is memory-only and resets to Objects on restart. The region-editing shortcut still opens on Regions with the region selector focused; closing it also updates the remembered tab. |
| `SettingsDialog` | Edits per-map drawing settings (fonts/colors/grid/room/connection defaults/regions) — these are the same settings persisted in the map file's `<settings>` block. Also owns the one `Properties.Settings` usage (`SettingsLastTabIndex`) — this dialog's `FormClosing` handler is the exact code path that was involved in the .NET 8 port's `ConfigurationErrorsException` bug, see [`09-build-test-and-dotnet8-port.md`](09-build-test-and-dotnet8-port.md). |

## `Util\` quick reference

- `XmlScribe.cs` / `XmlElementReader.cs` / `XmlAttributeReader.cs` — map file XML
  read/write helpers (full detail in [`03-storage-and-persistence.md`](03-storage-and-persistence.md)).
- `StringFormats.cs` — shared string/formatting helpers.
- `Smoothing.cs` — smoothing/interpolation helpers used by rendering.
- `PathHelper.cs` — safe path/directory/filename handling (used by `MainForm`'s open/save
  dialogs).
- `KeyboardHelper.cs` — keyboard P/Invoke helpers (`user32.dll` `keybd_event`, exposed as
  `KeybdEvent`, and `GetKeyState`) for num-lock/caps-lock/scroll-lock indicators.
- `ClipboardHelper.cs` — clipboard read/write helpers backing `CopyController`.

# Avalonia frontend (`src/Trizbort.Avalonia/`)

Cross-platform client; paths below are relative to `src/Trizbort.Avalonia/`. The sections above
describe WinForms only.

## Project layout

| Folder | Contents |
|---|---|
| `App.axaml(.cs)`, `Program.cs` | Startup and the DI container (`Microsoft.Extensions.DependencyInjection`). |
| `Views/` | `MainWindow` (menu, toolbar, canvas, minimap, `AutomapBar` overlay), `AboutDialog`, `Views/Dialogs/*` (Room, Connection, MapLabel, MapSettings, MapTheme, AppSettings, AutomapSettings). |
| `ViewModels/` | `MainWindowViewModel` + one VM per dialog. Dialog VMs expose `Initialize(model)` and `ApplyTo(model)`. |
| `Controls/` | `MapCanvasControl`, `MinimapControl`, `MinimapTransform` (pure helper), `AutomapBar`, `ColorPickerButton`. |
| `Services/` | `IDialogService`/`DialogService`, `IStorageService`/`StorageService`, `ICommandService`/`CommandService`, `AvaloniaAutomapCanvas`. |
| `Drawing/`, `Converters/` | Avalonia adapters for Core's drawing abstractions; dialog value converters. |

## DI and scoped dialogs

`App.axaml.cs` registers singletons `IDialogService`, `IStorageService`, `ICommandService`,
`IAutomapCanvas` (one `AvaloniaAutomapCanvas`) and `MainWindowViewModel`; each dialog VM is
**transient**. `DialogService` opens every dialog inside its own `IServiceScopeFactory.CreateScope()`
(`using var scope`), resolves the VM from that scope, shows the dialog modally, and disposes the
scope afterward — the root provider stays usable. `ShowElementPropertiesAsync` dispatches by
element type (Room/Connection/MapLabel). Dialogs edit via `Initialize` → user edits → `ApplyTo`
on accept only; cancel leaves the model untouched.

## `MapCanvasControl` and its state machine

Single custom `Control` that renders the map and handles input. Pan origin/zoom live in
`ICommandService` (`Origin`, `ZoomFactor`); `ViewChanged` fires on pan/zoom so the minimap redraws.
Interaction uses `CanvasDragMode` (`None, Pan, Marquee, MoveElement, MoveResizeHandle, MovePort,
MoveWaypoint`) plus hover state (element/port/handle/waypoint) and pending-new-connection state.
Pointer press picks the mode from the hit test; move updates it; release commits through
`ICommandService`/Core controllers (no undo/redo). Right-drag pans; a right-release with no pan
movement (`_panMoved` false) opens the context menu (Properties...). Double-click on a
Room/Connection/MapLabel calls `ShowElementPropertiesAsync`. Space+drag also pans.
Gesture tests: `MapCanvasControlGestureTests`, `MapCanvasControlSyncTests`.

Scroll, pinch and touch navigation (ticket 131, issue #21). `WheelInput` carries `DeltaX` and `DeltaY`;
`CanvasInteraction.OnWheel` picks zoom or pan, using `ApplicationSettings.ScrollWheelBehavior`
(`ScrollWheelMode`: Automatic / AlwaysZoom / AlwaysPan). An event is "trackpad-like" when
`|DeltaX| > 0` or `DeltaY` is not a whole number (tolerance 0.001); see `CanvasInteraction.IsTrackpadLike`.

| Input | Result |
|---|---|
| Mouse wheel (whole notches, Y only) | `Viewport.ZoomWheel`: one 1.25x step per notch about the pointer (`InvertMouseWheel` applies) |
| Trackpad two-finger scroll (fractional or X delta) | `Viewport.ScrollPan`: content follows the fingers, 40 px per delta unit (`ScrollPanPixelsPerUnit`); `InvertMouseWheel` does not apply |
| Ctrl + trackpad scroll | `Viewport.ZoomSmooth`: `1.25^delta`, delta clamped to +/-1 per event |
| Ctrl + mouse wheel | Micro zoom (+/-0.01) |
| Trackpad pinch | `Gestures.PointerTouchPadGestureMagnifyEvent` to `CanvasInteraction.OnMagnify` (factor `1 + magnification`); on platforms that send pinch as Ctrl + wheel the row above applies |
| Touch, one finger | Treated as the left mouse button |
| Touch, two or more fingers | `MultiTouchTracker` (Core) yields centroid delta and spread ratio; `CanvasInteraction.OnTouchGesture` cancels any active gesture (no undo step), then pans and zooms about the centroid. After a second finger lands, touch input is ignored as mouse input until every finger has lifted. |

Settings: Automatic (default) uses the heuristic; Always zoom zooms for every event; Always pan pans
for every event (Ctrl + scroll still zooms). Already available for pointer-only users: Space +
left-drag, middle-drag, arrow keys with no selection. Tests: `TrackpadNavigationTests` (Core and Avalonia).

Rendering: `Render` sorts visible elements once (cached) and calls the shared static
`DrawElements(context, sortedElements, ...)` (Z-order, spatial line index, `DrawRoom`/
`DrawMapLabel`/`DrawConnection`); adorners (handles, ports, waypoints, marquee) are drawn on top.

## Minimap

`Controls/MinimapControl.cs` draws scaled rooms/connections and the viewport rectangle. All
geometry lives in `MinimapTransform` (`ComputeCanvasBounds`, `WorldToMinimap`, `MinimapToWorld`,
`CalculateOriginFromMinimapPoint`), which is unit-tested (round trip, empty map, drag). Clicking
centers the canvas there; dragging the viewport rectangle updates the canvas `Origin`. It redraws
on map change, pan and zoom; an empty map draws nothing.

## Automap

- Core exposes the narrow public API `Automap.Instance.Start(IAutomapCanvas, AutomapSettings)`
  (`Task`; `StartCl` and parser internals stay internal).
- `ToggleAutomap` (menu/toolbar) → `IDialogService.ShowAutomapSettingsAsync` (seeded from
  `ApplicationSettingsController.AppSettings.Automap`), transcript picked through
  `IStorageService.OpenTranscriptFileAsync`; accepted settings are saved. Cancelling either
  does not start a run (the default `FileName` is empty).
- The run uses the singleton `AvaloniaAutomapCanvas`. `MainWindowViewModel.IsAutomapActive`
  drives `Controls/AutomapBar` (status + Stop). The run is observed in the background; it is
  stopped on explicit Stop, map load, and window close, and `IsAutomapActive` is cleared on
  stop, autonomous completion, or handled I/O failure (a run token guards stale completions).
- Errors go through `IEditorPrompts.ShowMessageAsync` (`AvaloniaEditorPrompts` -> `IDialogService`);
  the engine catches every exception, sets "Automapping halted.", raises `AutomapFailed` and shows one
  message. The static `Automap.ErrorReporter` remains only as a deprecated adapter for tests. The
  status text is event-driven (`Automap.StatusChanged` -> `EditorSession.AutomapStatusChanged`).
- `--debug` (`AppOptions.Debug` -> `DebugOptions.Enabled`, `EditorSession.DebugEnabled`) enables the
  `debug.*` toggles and `automap.step`; the canvas honours them in `MapCanvasControl.Render`.
- **Known parity gap**: same-direction room conflicts keep Core's default `KeepRoom1`; Avalonia has
  no interactive `ConflictingRoomChooser` prompt (a synchronous Core callback vs. async modal).
- Tests (`AutomapIntegrationTests`) use the real singleton with a temp transcript; they serialize
  access and restore static callbacks.

## Image export

File → Export → Image (PNG)... → `IStorageService.ExportImageAsync`. An empty map shows a message
and no picker; otherwise a save picker (PNG) is shown, and the path is saved to
`AppSettings.LastExportImageFileName`. Rendering is Avalonia-side (Core has no image exporter):
`MapCanvasControl.RenderToBitmap(project, scale)` computes the full map extent (with padding),
independent of viewport/zoom, and draws via `RenderMap` → the same `DrawElements` path into a
`RenderTargetBitmap`. Dimensions are capped at **8192×8192**; larger maps get a reduced scale so the
whole extent fits (no cropping). It never touches the live canvas `Origin`/`ZoomFactor`.
Code exporters (File → Export) go through `StorageService.ExportCodeAsync`; theme files via
`OpenThemeFileAsync`/`SaveThemeFileAsync`.

## Avalonia tests (`src/Trizbort.Avalonia.Tests/`)

NUnit + `Avalonia.Headless.NUnit` (Skia). Use `[AvaloniaTest]` for anything that creates controls or
a `RenderTargetBitmap`. Files: gesture/sync tests, `DrawingAdaptersParityTests`,
`DialogViewModelTests` (scope disposal, Room/Connection round-trip), `MinimapTransformTests`,
`AutomapIntegrationTests`, `ImageExportTests` (pixel read-back via `RenderTargetBitmap.CopyPixels`).
Run: `dotnet test src/Trizbort.Avalonia.Tests/Trizbort.Avalonia.Tests.csproj`.

# Storage & Persistence

See [`README.md`](README.md) for the doc map. Covers: the `.trizbort` map file format,
save/load pipeline, and the three *separate* settings systems in this codebase.

## The `.trizbort` map file format (custom XML, not JSON)

Although JSON converters exist in `Domain\SerializeHelpers\` (currently unused; clipboard
copy/paste serializes its own DTOs directly), **the saved map format is hand-written XML**,
produced/consumed via
`Util\XmlScribe.cs` (writer) and `Util\XmlElementReader.cs`/`XmlAttributeReader.cs` (reader).

Shape (see `samples\Zork_770614.trizbort` for a real example):

```xml
<trizbort version="...">
  <info>...</info>
  <map>
    <room id="1" name="West of House" x="-3200" y="-640" w="96" h="64" description="...">
      <objects>Small window</objects>
    </room>
    <line id="3">
      <dock index="0" id="1" port="s" />
      <dock index="1" id="2" port="w" />
    </line>
  </map>
  <settings>...</settings>
</trizbort>
```

- Room geometry is stored as plain XML attributes (`x`, `y`, `w`, `h`).
- A connection is a `<line>` with two (or more) `<dock>` children identifying the room ID and
  compass port it attaches to — this is the on-disk form of the in-memory `Vertex.Port.Owner`
  relationship described in [`02-domain-model.md`](02-domain-model.md).
- Optional curve waypoints on a `<line>` are stored as attributes `curveQuarter`, `curveMiddle`,
  `curveThreeQuarter` with invariant-culture `"x,y"` values. They are deliberately attributes,
  not child elements, because `EndLoad` matches child elements to `VertexList` by position and
  older Trizbort versions ignore unknown attributes (they simply draw the line straight).
- `<settings>` holds per-map drawing/document settings (fonts, colors, grid, validation rules)
  — separate again from the two settings systems below.
- Labels use `<label id="..." text="..." x="..." y="..." w="..." h="..." shape="..."
  borderstyle="..." background="..." textColor="..." borderColor="..." backgroundColor="..."
  ZOrder="..." />`. Connections with any endpoint docked to a label use `<labelLine>` with the
  same contents as `<line>` and the same two-pass docking resolution. Older readers only
  recognize `<room>` and `<line>`, so they ignore both labels and their lines without leaving
  dangling connectors. Saving through an older version **discards** these unrecognized elements.
  Clipboard DTOs also preserve labels and docking to copied labels.

### Save/load call chain

```
Project.Save()                       Domain\Application\Project.cs
  → MapSaver.SaveMap(fileName)       Domain\Application\MapSaver.cs
    → LegacyMapFileEngine.Save(...)  Domain\Application\LegacyMapFileEngine.cs
      → XmlScribe (writer) + Room.Save(scribe) + Connection.Save(scribe)

Project.Load() / Project.Load(uri)
  → MapLoader.LoadMap(fileName)      Domain\Application\MapLoader.cs
    → LegacyMapFileEngine.Load(...)
      → XmlDocument parse
      → per <room>: new Room(id), room.Load(element)
      → per <line>: new Connection(id), connection.BeginLoad(element)   [pass 1]
      → connection.EndLoad(state) for every connection                  [pass 2, resolves ports/rooms]
      → Settings.Load(root["settings"])
```

Key facts:

- `MapFileEngine` (`Domain\Application\MapFileEngine.cs`) is the abstract dispatch base;
  **`LegacyMapFileEngine` is the only engine implemented and is used for both load and save**
  despite the "Legacy" name — there is no newer/alternate current engine.
- `MapSaver`/`MapLoader` only accept the `.trizbort` extension; anything else is rejected.
- An empty/new local file is treated as a blank map: reset drawing settings with
  `Settings.Reset(false)` and clear the supplied project's metadata rather than parsing XML.
  `MapLoader` dispatches by extension before any file access and leaves empty-file handling
  to the engine, so public loading succeeds without clearing the old current project's metadata.
  Unsupported or missing files report failure without resetting the current map's settings.
  Normal loading also uses `Reset(false)` so resetting settings does not erase newly loaded
  title/author/description/history. Public `Settings.Reset()` retains new-document behavior.
- The root element must literally be `<trizbort>`; the `version` attribute feeds
  `Project.SetVersion()` → `Project.CheckDocVersion()`, the hook for any version-specific
  migration behavior (consult that method if you need to introduce a breaking format change —
  add migration logic there, don't just change the writer).
- Connections load in **two phases** (`BeginLoad` then `EndLoad`) specifically because a
  connection's dock may reference a room that hasn't been read yet in document order.
- After opening a *local* file, `Project` installs a `TrizbortFileWatcher`
  (`Domain\Watchers\TrizbortFileWatcher.cs`) that prompts to reload if the file changes on disk
  outside the app (and warns if there are unsaved in-memory changes).
  This includes zero-length maps and relative paths, resolved to an absolute watcher path.
  The engine distinguishes existing local files from URL inputs using `File.Exists`, not
  relative-URI syntax (a plain local filename is also a valid relative URI).

Internal constructors on `LegacyMapFileEngine` accept load-error, version-decision and
duplicate-start/end-warning callbacks; `Room.Load` has the corresponding internal warning
overload. `MapLoader` accepts an internal unknown-extension notification callback and optional
file engine; tests supply the real `LegacyMapFileEngine` with non-interactive callbacks for
supported files too. This avoids test-runner product versions and modal load-error dialogs. Public
entry points still show the existing dialogs. `DocumentVersionPolicy.Compare` contains only
warning classification, preserving the original major/minor/build/**MinorRevision** precedence.
These seams allow negative/legacy tests without blocking UI.

Saved map versions and `Project.CheckDocVersion` use `typeof(Project).Assembly.GetName().Version`,
not `Application.ProductVersion` (which describes the entry/test-runner executable and may
contain a nonnumeric informational build hash). Default load/error/warning dialogs now delegate
to `UI\UserInteraction`; tests can exercise public save/load paths without visible UI.

Clipboard reconstruction uses `CopyController.PasteConnections` to resolve docks only against
copied nodes, leaving omitted endpoints free at their translated positions. Door/corner data
is cloned; Canvas remaps copied room-reference IDs and selects the newly pasted graph. Tests
exercise DTO JSON round-trips and production paste without reading the Windows clipboard.
Room references are retained only when their target room is included in the copied selection.
Otherwise paste clears the reference (including same-map pastes): clipboard data carries no
source-map identity, so retaining source IDs could bind to unrelated or newly created rooms.

Colour clipboard data keeps the existing `Colors` list of `{ Name, Color }` entries and
`SecondFillLocation`. `CopyController` uses an explicit, strongly typed mapping of the six
room colour properties for both capture and `SetRoomColors`, not reflective property access.
Unknown colour keys are ignored for compatibility; partial lists leave unspecified colours
alone, and duplicate entries apply in order.

## Three separate settings systems — don't conflate them

| System | File / location | Scope | Used for |
|---|---|---|---|
| **Map `<settings>`** | Inside the `.trizbort` file itself | Per-document | Per-map drawing settings (fonts/colors/grid/validation rules), via `Settings.Load`/`Settings.Save(scribe)` |
| **`Properties.Settings`** (`Properties\Settings.settings` / `.Designer.cs`) | `.NET` user-scoped `user.config` (standard `ApplicationSettingsBase` mechanism), registered in `app.config` under `<userSettings>` | Per-Windows-user | Currently **only one setting**: `SettingsLastTabIndex` (long) — just remembers the last-selected tab in `SettingsDialog`. Read/written via `Properties.Settings.Default.SettingsLastTabIndex` in `UI\SettingsDialog.cs`. |
| **`ApplicationSettings`** (`Domain\AppSettings\ApplicationSettings.cs` + `ApplicationSettingsController.cs`) | JSON file `.\appsettings.json` (relative to working directory!), via Newtonsoft.Json | App-wide, not per-document | Automap defaults, canvas size, debug flags, default fonts/images, export filenames, last-project filename, load-last-project flag, recent-projects list, save-to-image/PDF options, tooltip options, margins/wrapping, Map preferences (apply-style-to-new-rooms, double-click-to-add-room). |

Things worth remembering:

- `ApplicationSettingsController` also knows how to **migrate a legacy settings file**:
  `%LOCALAPPDATA%\Genstein\Trizbort\Settings.xml`. If `appsettings.json` doesn't exist yet but
  that legacy XML does, it's imported once (`LoadLegacyAppSettings()`) and then written out as
  the new `appsettings.json`. Don't delete that migration path without a deliberate decision —
  users upgrading from old installs depend on it.
- `appsettings.json` is written relative to the **current working directory**, not
  `%APPDATA%`/`%LOCALAPPDATA%`. This was previously generated as a stray file at the repo root
  during manual smoke-testing in a dev checkout — if you see an untracked `appsettings.json` at
  repo root after running the app locally, it's expected working-directory output, not a build
  artifact to commit.
- Automap-specific settings persisted through `ApplicationSettings` (transcript filename,
  verbosity, same-name-room matching, exit guessing, custom object/region commands) are
  described in more detail in [`06-automap.md`](06-automap.md).

## Reusable map themes

`Setup\MapTheme.cs` captures/applies the visual subset of map settings and reads/writes
version-1 JSON `.trizbort-theme` files (`Format: "trizbort-theme"`). Required properties,
palette completeness, region names, enums, finite numeric ranges and fonts are validated.
Fonts and colours are resolved before any map mutation; unavailable fonts fail explicitly.
There is no theme dependency or theme name added to the map format: applying a theme writes
the normal in-memory `Settings` values, which existing map save/load already persists.

Themes exclude content, default room names, snapping, layout distance, editor handle sizes,
keyboard modifiers and app-wide preferences. Region palettes merge by name without removing
map-only regions or changing room membership. `Apply(bool replaceIndividualStyles)` can also
reset room colour/shape/corner/border overrides and connection/label colours, while preserving
geometry, routing, labels, directions, doors and game properties. Capture does not export
individual element overrides. Applying marks `Project.Current.IsDirty` and emits
`Settings.Changed` even when only region/default shape values differ.

`MapTheme.HasIndividualStyles` ignores `Room.StraightEdges`: it is derived (reset from
`IsHandDrawn` on every draw) rather than a user style. It does count
`Room.HandDrawnStyle != MapDefault`.

### Hand-drawn style

`Settings.HandDrawn` (map XML `settings/lines/handDrawn`, false on reset/new map; the old
App Settings `HandDrawnGlobal` option was removed) is the map-wide hand-drawn switch and is part of themes
(optional JSON `HandDrawn`, absent in older theme files → false). Rooms store
`HandDrawnStyle` (`MapDefault`/`HandDrawn`/`Straight`) as the `handDrawnStyle` attribute;
they also write the legacy `handDrawn` yes/no (effective value) for older versions. On load
with no `handDrawnStyle`, legacy `handDrawn="yes"` → `HandDrawn`, otherwise `MapDefault`.
`Room.IsHandDrawn` resolves the effective value. Geometry lives in
`Domain\Misc\Sketch.cs`: a deterministic, length-scaled bow for lines/polylines (exact
endpoints) and normal-displaced outlines for polygons, ellipses and rounded rectangles.
Seeds come from element IDs, so the wobble is stable between redraws. Connections join
contiguous, nearly collinear segments into one sketched stroke (`Sketch.Polyline` adds a bow
plus a few gentle waves). When sketched, chevrons are deferred and snapped onto the nearest
point of the sketched stroke (`Sketch.Nearest`) and drawn as jittered, notched arrowheads via
`Drawing.DrawChevron(..., Random sketch)`. Labels sketch their outline
when the map setting is on. `RoomStyleInference` does not infer hand-drawn style.

`Setup\RoomStyleInference.cs` lifts per-room styling into map defaults so it can be
exported. `Analyze` evaluates each property independently and requires ≥75% agreement
among at least three rooms, using effective colours (override or current default): shape →
`Settings.DefaultRoomShape`; fill/name text per resolved region (case-insensitive, falling back
to `NoRegion`) → region `RColor`/`TextColor`; border/subtitle/object text → `Colors.Border`,
`Colors.Subtitle`, `Colors.SmallText`. `Apply` updates those defaults, clears room colour
overrides equal to the final defaults (so rendering is unchanged), marks dirty and raises
`Settings.Changed`. Room shapes, corners, border styles, hand-drawn overrides and second fills
have no map-level default and are left on rooms.

## JSON serialization — `Domain\SerializeHelpers` converters are currently unused

`Domain\SerializeHelpers\ElementConverter.cs` / `PortConverter.cs` are Newtonsoft.Json
`JsonConverter`s for polymorphic `Element`/`Connection.VertexPort` deserialization. Both are
**read-only** (`CanWrite => false`, `WriteJson` throws `NotImplementedException`). **They are not
currently referenced anywhere** — clipboard copy/paste does *not* use them:
`Domain\Controllers\CopyController.cs` calls `JsonConvert.SerializeObject`/`DeserializeObject`
directly against its own DTOs (`CopyRoomObj`, `CopyConnectionObj`, `CopyVertexObj`,
`CopyColorsObj`, `CopyObject`) with no custom converters registered. Treat these two converters as
dead/unused code unless you find a new call site; don't assume they're load-bearing for
copy/paste. `Element`/`Connection`/`Room` still carry `[JsonIgnore]` on their non-DTO-friendly
properties (the `Project` back-reference, the ports list, etc.) — that's unrelated to these
converters and is just hygiene in case anything *does* serialize the domain types directly.

## Quick checklist: "I need to persist a new piece of state"

1. **Per-map data** (belongs to the drawing, travels with the `.trizbort` file): add to
   `Room`/`Connection`/map `<settings>` and wire through `Save`/`Load` as described in
   [`02-domain-model.md`](02-domain-model.md)'s checklist.
2. **Per-user, trivial, rarely-changed UI state** (e.g. "remember last selected tab"): use
   `Properties.Settings` — add to `Properties\Settings.settings`, regenerate the designer, wire
   through `app.config`'s `<userSettings>` section.
3. **App-wide preference/state that isn't part of any map** (recent files, default
   export/image options, automap defaults): add to `Domain\AppSettings\ApplicationSettings.cs`
   and load/save it through `ApplicationSettingsController`.
4. **Transient/session-only state** (search index, UI hover state): don't persist it at all —
   follow the pattern in `Domain\Cache\Indexer.cs`.

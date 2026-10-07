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
- `<settings>` holds per-map drawing/document settings (fonts, colors, grid, validation rules)
  — separate again from the two settings systems below.

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
- An empty/new local file is treated as a blank map and calls `Settings.Reset()` rather than
  attempting to parse XML.
- The root element must literally be `<trizbort>`; the `version` attribute feeds
  `Project.SetVersion()` → `Project.CheckDocVersion()`, the hook for any version-specific
  migration behavior (consult that method if you need to introduce a breaking format change —
  add migration logic there, don't just change the writer).
- Connections load in **two phases** (`BeginLoad` then `EndLoad`) specifically because a
  connection's dock may reference a room that hasn't been read yet in document order.
- After opening a *local* file, `Project` installs a `TrizbortFileWatcher`
  (`Domain\Watchers\TrizbortFileWatcher.cs`) that prompts to reload if the file changes on disk
  outside the app (and warns if there are unsaved in-memory changes).

## Three separate settings systems — don't conflate them

| System | File / location | Scope | Used for |
|---|---|---|---|
| **Map `<settings>`** | Inside the `.trizbort` file itself | Per-document | Per-map drawing settings (fonts/colors/grid/validation rules), via `Settings.Load`/`Settings.Save(scribe)` |
| **`Properties.Settings`** (`Properties\Settings.settings` / `.Designer.cs`) | `.NET` user-scoped `user.config` (standard `ApplicationSettingsBase` mechanism), registered in `app.config` under `<userSettings>` | Per-Windows-user | Currently **only one setting**: `SettingsLastTabIndex` (long) — just remembers the last-selected tab in `SettingsDialog`. Read/written via `Properties.Settings.Default.SettingsLastTabIndex` in `UI\SettingsDialog.cs`. |
| **`ApplicationSettings`** (`Domain\AppSettings\ApplicationSettings.cs` + `ApplicationSettingsController.cs`) | JSON file `.\appsettings.json` (relative to working directory!), via Newtonsoft.Json | App-wide, not per-document | Automap defaults, canvas size, debug flags, default fonts/images, export filenames, last-project filename, load-last-project flag, recent-projects list, save-to-image/PDF options, tooltip options, margins/wrapping, Map preferences (apply-style-to-new-rooms, double-click-to-add-room). |

Things worth remembering:

- `ApplicationSettingsController` also knows how to **migrate a legacy settings file**:
  `%LOCALAPPDATA%\Genstein\Trizbort\Settings.xml`. If `appsettings.json` doesn't exist yet but
  that legacy XML does, it's imported once (`loadLegacyAppSettings()`) and then written out as
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

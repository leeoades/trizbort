# Architecture Overview

See [`README.md`](README.md) for the doc map. This page is the 30,000-ft view; it intentionally
avoids deep detail — follow the links to the topic docs for specifics.

## Top-level folders

| Folder | Role |
|---|---|
| `Domain\` | Core application/document model: elements (rooms/connections), project state, persistence engines, app-wide settings, controllers/commands, geometry/drawing primitives, enums. No WinForms dependency *in principle*, though in practice some domain code (e.g. `Room`/`Connection` drawing) uses `System.Drawing`/`PdfSharp.Drawing` types directly rather than being UI-framework-agnostic. |
| `UI\` | WinForms forms/dialogs and the `Canvas` control (map drawing surface + input handling). Talks to `Domain` via `Domain\Controllers` and direct `Project.Current` access. |
| `Automap\` | Transcript-parsing engine that turns IF game session text into `Room`/`Connection` objects. Uses `IAutomapCanvas` for map mutations, but `Automap.cs` also directly depends on WinForms: it shows `AutomapRoomSameDirectionDialog` for ambiguity resolution and raises `MessageBox` errors — it is not UI-framework-agnostic. |
| `Export\` | Converts `Project.Current.Elements` into IF-language source (Inform6/7, TADS, Alan, Hugo, ZIL, Quest, Adventuron) via a shared `CodeExporter` base class. Image/PDF export live in `UI\MainForm.cs` instead, not here. |
| `Util\` | Small standalone helpers: XML serialization (`XmlScribe`, `XmlElementReader`, `XmlAttributeReader`), string formatting, path handling, clipboard, keyboard. |
| `Extensions\` | Extension methods (`ColorExtensions`, `FileExtensions`, `RoomExtensions`, `StringExtensions`). |
| `Properties\` | Standard .NET `Settings.settings`/`Settings.Designer.cs` + `Resources.resx` — a *tiny*, almost-unused settings surface (one setting currently: `SettingsLastTabIndex`). Don't confuse with `Domain\AppSettings` (see below). |
| `Setup\` | Installer/setup wizard support (not map-related). |
| `Trizbort.Tests\` | NUnit test project. Not referenced by `Trizbort.sln` — run directly via `dotnet test`. |
| `samples\`, `testing\` | Sample `.trizbort` map files and transcripts, useful for manual verification and as file-format examples. |
| `Docs\` | **User-facing** help/website content (unrelated to this `agent-docs/` folder). |

## Layering / call direction

```
UI (MainForm, dialogs, Canvas)
   │  menu/keyboard/mouse events
   ▼
Domain.Controllers (CommandController, CanvasController, RoomController, CopyController, ElementController)
   │  thin façade, some direct Project/Canvas mutation too
   ▼
Domain (Project, Element/Room/Connection, AppSettings, MapLoader/MapSaver)
   │
   ▼
Util (XmlScribe/XmlElementReader for persistence; misc helpers)
```

Two things cut across this layering rather than flowing through it:

- **Automap** (`Automap\Automap.cs`) mutates `Project.Current.Elements` directly through the
  `IAutomapCanvas` abstraction implemented by `Canvas.Automap.cs` — it does not go through
  `Domain.Controllers`. It also depends on WinForms directly (`AutomapRoomSameDirectionDialog`,
  `MessageBox`) rather than going through `IAutomapCanvas` for everything.
- **Export** (`Export\CodeExporter` + `Export\Languages\*`) only *reads* `Project.Current.Elements`
  — it never mutates the project.

## The central data structure

Everything revolves around one object graph:

```
Project.Current               (Domain\Application\Project.cs)
└── Elements : BoundList<Element>   (one flat heterogeneous list)
    ├── Room      (Domain\Elements\Room.cs)
    └── Connection (Domain\Elements\Connection.cs, owns 0/1 Door)
```

There are **no separate collections** for rooms, connections, or regions — regions are just a
string property on `Room`, grouped at read-time by consumers (e.g. the exporter's
`findRegions()`). See [`02-domain-model.md`](02-domain-model.md) for full detail.

## Where to start for common tasks

| Task | Start here |
|---|---|
| Fix/extend room or connection behavior, geometry, validation | [`02-domain-model.md`](02-domain-model.md) |
| Fix/extend the `.trizbort` file format, save/load, settings persistence | [`03-storage-and-persistence.md`](03-storage-and-persistence.md) |
| Add a new menu command / understand how a UI action reaches domain state | [`04-commands-and-controllers.md`](04-commands-and-controllers.md) then [`05-ui-and-canvas.md`](05-ui-and-canvas.md) |
| Fix a drawing bug, zoom/pan bug, drag bug, or add a new dialog | [`05-ui-and-canvas.md`](05-ui-and-canvas.md) |
| Fix/extend transcript-based automapping (room/exit detection) | [`06-automap.md`](06-automap.md) |
| Fix/extend an IF-language exporter or image/PDF export | [`07-export-subsystem.md`](07-export-subsystem.md) |
| Add/change a command-line flag or startup behavior | [`08-cli-and-entry-point.md`](08-cli-and-entry-point.md) |
| Change `.csproj`/`app.config`/`app.manifest`, run tests, understand .NET 8 port history | [`09-build-test-and-dotnet8-port.md`](09-build-test-and-dotnet8-port.md) |

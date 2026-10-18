# Architecture Overview

See [`README.md`](README.md) for the doc map. This page is the 30,000-ft view; it intentionally
avoids deep detail — follow the links to the topic docs for specifics.

## Top-level folders

| Folder | Role |
|---|---|
| `src/Trizbort.Core/` | Standalone, platform-neutral domain class library (`net8.0`). Contains core domain models (`Room`, `Connection`, `Door`, `MapLabel`), `Project`, automap engine, exporters, geometry/math primitives (`Vector`, `Rect`, `LineSegment`), settings/themes metadata, commands, and XML persistence utilities. |
| `src/Trizbort.Core.Tests/` | Standalone headless unit test suite (`net8.0`). Validates pure domain logic, automap parsing, exporters, math, and serialization cross-platform. |
| `src/Trizbort/` | Windows desktop application project root (`net8.0-windows`): WinForms forms/dialogs, entry point/configuration, GDI+ rendering pipeline, drawing extension adapters, and the `Canvas` control. |
| `src/Trizbort.Tests/` | WinForms-specific and UI integration test project (`net8.0-windows`), included in the root `Trizbort.sln`. |
| `src/Trizbort.Avalonia/` | Cross-platform Avalonia UI client (`net8.0`) built on `Trizbort.Core` only (no WinForms/GDI+). See the Avalonia section of [`05-ui-and-canvas.md`](05-ui-and-canvas.md). |
| `src/Trizbort.Avalonia.Tests/` | Headless Avalonia tests (`Avalonia.Headless.NUnit`, Skia): gestures, sync, drawing parity, dialogs VMs, minimap transform, Automap, image export. |
| `tests/manual/` | Manual `.trizbort` fixtures and batch/script runners; these are not automated tests. |
| `samples/` | Sample maps/transcripts. |
| `legacy/vendor/` | Preserved legacy DLLs not referenced by the current SDK-style project. |
| `Docs/` | **User-facing** help/website content (unrelated to this `agent-docs/` folder). |
| `agent-docs/` | Layered technical reference for contributors and agents. |

## Layering / call direction

```
UI (MainForm, dialogs, Canvas in src/Trizbort/)
   │  menu/keyboard/mouse events, GDI+ drawing extensions
   ▼
Trizbort.Core:
Domain.Controllers (CommandController, CanvasController, RoomController, CopyController, ElementController)
   │  façade layer
   ▼
Domain (Project, Element/Room/MapLabel/Connection, AppSettings, MapLoader/MapSaver)
   │
   ▼
Util (XmlScribe/XmlElementReader for persistence; misc helpers)
```

The Avalonia client is a second UI peer of the WinForms one: `Views` + `ViewModels` + `Controls\MapCanvasControl`
sit on top of `Services` (DI-registered `IDialogService`, `IStorageService`, `ICommandService`) which call into
`Trizbort.Core` controllers/exporters. No Avalonia types may enter `Trizbort.Core`.

Two things cut across this layering rather than flowing through it:

- **Automap** (`Automap\Automap.cs` in `Trizbort.Core`) mutates `Project.Current.Elements` through the
  `IAutomapCanvas` abstraction implemented headlessly by `AutomapCanvas`, in WinForms by `Canvas.Automap.cs`, and in Avalonia by
  `Services\AvaloniaAutomapCanvas.cs`.
- **Export** (`Export\CodeExporter` + `Export\Languages\*` in `Trizbort.Core`) only *reads* `Project.Current.Elements`
  — it never mutates the project. Image/PDF visual exports are hosted in the UI client (`UI/MainForm.cs`).

## The central data structure

Everything revolves around one object graph:

```
Project.Current               (Domain\Application\Project.cs)
└── Elements : BoundList<Element>   (one flat heterogeneous list)
    ├── Room      (Domain\Elements\Room.cs)
    ├── MapLabel  (Domain\Elements\MapLabel.cs)
    └── Connection (Domain\Elements\Connection.cs, owns 0/1 Door)
```

There are **no separate collections** for rooms or connections. Each room stores its region name
as a string; the map-level `Settings.Regions` list separately stores region definitions and can
include regions unused by any room. See [`02-domain-model.md`](02-domain-model.md) for full detail.

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

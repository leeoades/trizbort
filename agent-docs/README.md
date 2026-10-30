# Trizbort — Technical Reference for AI Coding Agents

> **Layout change (2026-10-30):** the refactored WinForms projects have been removed. The original WinForms app
> (tag `v2.3.1-beta.2`) now lives in `legacy/` with its own `legacy/Trizbort.Legacy.sln` — see `legacy/README.md`.
> WinForms sections of these docs describe the refactored app; for the original, paths are under `legacy/src/Trizbort/`
> and its Domain/Automap/Export code lives inside that project rather than in `Trizbort.Core`.

This folder is a **layered reference** for agents working on this codebase. It exists so an
agent doesn't have to re-derive the architecture from scratch on every task, and so it only
needs to load the one or two sub-documents relevant to the current task instead of the whole
codebase (or the whole doc set) into context.

> User-facing documentation (the in-app help / website) lives in `Docs/` (capital D) at the
> repo root — that's a different thing from this folder. This folder (`agent-docs/`) is for
> engineers/agents working **on** Trizbort, not for people using it.

## What Trizbort is

Trizbort consists of a standalone, cross-platform core library (`Trizbort.Core`, **.NET 10 LTS**) and a
Windows desktop frontend (WinForms, `net10.0-windows`), plus a cross-platform Avalonia frontend
(`Trizbort.Avalonia`, `net10.0`, in active development on branch `port-to-linux`), for drawing
maps of Interactive Fiction (text adventure) games, and exporting those maps either as images/PDF
or as skeleton source code
for IF authoring systems (Inform 6/7, TADS, Hugo, ZIL, Alan, Quest, Adventuron). It can also
auto-generate a map by parsing a game transcript ("Automap").

## How to use this doc set

1. Read this file first — it tells you which sub-document to open next.
2. Open **only** the sub-document(s) relevant to your task. Each one is self-contained and
   gives file/line pointers into the real source rather than duplicating large code blocks.
3. Sub-documents may reference each other for cross-cutting concerns (e.g. the UI doc
   references the domain-model doc for what `Room`/`Connection` mean) — follow those links
   only if you actually need that context.
4. If you change architecture in a way that invalidates one of these docs (new file format,
   new exporter, new settings system, restructured controllers, etc.), **update the relevant
   sub-document in the same change**. Stale docs are worse than no docs.
5. **Immediate Commit Rule**: When making a change, once it is fixed up, building, and tests pass,
   **commit those changes immediately**. Do not build up a working directory of unrelated changes
   across different components or domains.

Source paths for domain logic, exporters, automap, and persistence are relative to `src/Trizbort.Core/`.
UI, Canvas, and WinForms adapter paths are relative to the WinForms project (now the original app at `legacy/src/Trizbort/`). Avalonia paths are
relative to `src/Trizbort.Avalonia/` (documented at the end of `05-ui-and-canvas.md`). Paths explicitly
prefixed with `tests/`, `samples/`, `Docs/`, or `agent-docs/` are relative to the repository root.

## Map of sub-documents

| Doc | Read this when you're working on... |
|---|---|
| [`01-architecture-overview.md`](01-architecture-overview.md) | You need the 30,000-ft view: project layout, layering, where to start for any given kind of change. Read this once per session if you haven't already. |
| [`02-domain-model.md`](02-domain-model.md) | Room/Connection/Door/Element classes, project-wide `Project.Elements`, geometry (`Vector`/`Port`/`Vertex`), validation, IDs. |
| [`03-storage-and-persistence.md`](03-storage-and-persistence.md) | The `.trizbort` XML map file format, save/load pipeline, app-level `appsettings.json`, user-scoped `Properties.Settings`, clipboard JSON. |
| [`04-commands-and-controllers.md`](04-commands-and-controllers.md) | The `Domain\Commands`/`Domain\Controllers` façade layer between UI and domain — what it does and does **not** provide (there is no undo/redo). |
| [`05-ui-and-canvas.md`](05-ui-and-canvas.md) | `MainForm`, menus/toolbar, dialogs, and the `Canvas` drawing/input surface — coordinate transforms, drag handling, dialog catalogue. Also the **Avalonia frontend** (`Trizbort.Avalonia`): DI/scoped dialogs, `MapCanvasControl` state machine, minimap, Automap, image export, Avalonia tests. |
| [`06-automap.md`](06-automap.md) | The transcript-parsing automapper: how rooms/exits are inferred from game text, ambiguity resolution, settings. |
| [`07-export-subsystem.md`](07-export-subsystem.md) | Adding/fixing an IF-language exporter (Inform6/7, TADS, Alan, Hugo, ZIL, Quest, Adventuron) or the image/PDF export. |
| [`08-cli-and-entry-point.md`](08-cli-and-entry-point.md) | Command-line options, `Program.cs`/`MainForm` startup sequence, how CLI flags map to save/load/automap/export actions. |
| [`09-build-test-and-dotnet8-port.md`](09-build-test-and-dotnet8-port.md) | Build/test commands, project file structure, and hard-won gotchas from the .NET Framework 4.8 → .NET 8 port & .NET 10 LTS retargeting (read before touching `.csproj`, `app.config`, or `app.manifest`). |
| [`plans/extract-trizbort-core-and-tests.md`](plans/extract-trizbort-core-and-tests.md) | Architectural roadmap and migration plan for extracting `Trizbort.Core` and `Trizbort.Core.Tests`. |
| [`plans/avalonia-parity-review.md`](plans/avalonia-parity-review.md) | **Active plan**: Avalonia vs original WinForms feature parity review, with task cards for agents. |
| [`plans/windows-classic-parity-checklist.md`](plans/windows-classic-parity-checklist.md) | *(Superseded by the plan above.)* Persistent checklist for comparing the current WinForms application with a pre-refactor Windows baseline using the manual/sample fixture maps. |

## Quick orientation (if you only read one paragraph)

The whole in-memory document is `Project.Current`, a single flat `BoundList<Element>` called
`Project.Elements` holding every `Room`, `MapLabel` and `Connection` (no separate rooms/connections/regions
collections). The UI (`MainForm` + `Canvas`) edits that project through a thin
controller/command façade (`Domain\Controllers`, `Domain\Commands`) that has **no undo/redo**.
Maps are persisted as custom XML (`.trizbort` files) via `Domain\Application\MapLoader`/
`MapSaver`/`LegacyMapFileEngine`. A separate transcript-parsing engine (`Automap\Automap.cs`)
can populate the same `Project.Elements` automatically. Exporters in `Export\Languages\*`
convert `Project.Elements` into IF-language source code via a shared `CodeExporter` base class.

## Repo facts an agent should not have to re-derive

- **C# naming**: PascalCase methods, properties and constants; `_camelCase` private instance
  and static fields; camelCase parameters and local variables. Avoid `m`/`s` Hungarian
  prefixes and all-caps constants. `.editorconfig` records these rules. WinForms control
  `Name` strings, resource keys, native entry points and serialized keys stay unchanged;
  prefer typed access over string-based reflection when referring to members.
  Generated Resources/Settings code is left to its generator. Reflection remains intentional
  for assembly metadata and the naming-convention test's member enumeration; application
  logic and UI tests use typed APIs instead of private-member lookups.

- **UI framework**: `src/Trizbort/` is Windows Forms (Windows-only; porting to .NET Core / .NET 10 did **not**
  make it cross-platform, see `09-build-test-and-dotnet8-port.md`). `src/Trizbort.Avalonia/` is the
  cross-platform Avalonia frontend built on `Trizbort.Core`.
- **Target frameworks & layout**:
  - `src/Trizbort.Core/`: SDK-style `net10.0`, platform-neutral core class library.
  - `src/Trizbort.Core.Tests/`: SDK-style `net10.0`, headless cross-platform test runner.
  - `legacy/src/Trizbort/` + `legacy/src/Trizbort.Tests/`: the original WinForms app (`net8.0-windows`), own solution `legacy/Trizbort.Legacy.sln`.
  - `src/Trizbort.Avalonia/`: SDK-style `net10.0`, Avalonia UI client (Linux/Windows/macOS).
  - `src/Trizbort.Avalonia.Tests/`: SDK-style `net10.0`, Avalonia.Headless NUnit tests
    (`dotnet test src/Trizbort.Avalonia.Tests/Trizbort.Avalonia.Tests.csproj`, ~119 tests).
- **Tests**: Run pure core unit tests cross-platform with `./scripts/run-core-tests.sh` or `dotnet test src/Trizbort.Core.Tests/Trizbort.Core.Tests.csproj`. Run Avalonia headless tests with `dotnet test src/Trizbort.Avalonia.Tests/Trizbort.Avalonia.Tests.csproj`. `dotnet test Trizbort.sln` runs everything for the new app on any OS; legacy tests need Windows (`dotnet test legacy/Trizbort.Legacy.sln`). Manual map fixtures and scripts are in `tests/manual/`.
- **PDF export**: `PDFsharp-GDI` 6.2.4 NuGet package (not the older vendored/legacy
  `PDFsharp-gdi` 1.x).
- **No undo/redo exists anywhere in the codebase.** Don't assume one when reasoning about
  edit operations; see `04-commands-and-controllers.md`.

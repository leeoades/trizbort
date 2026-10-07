# Trizbort — Technical Reference for AI Coding Agents

This folder is a **layered reference** for agents working on this codebase. It exists so an
agent doesn't have to re-derive the architecture from scratch on every task, and so it only
needs to load the one or two sub-documents relevant to the current task instead of the whole
codebase (or the whole doc set) into context.

> User-facing documentation (the in-app help / website) lives in `Docs/` (capital D) at the
> repo root — that's a different thing from this folder. This folder (`agent-docs/`) is for
> engineers/agents working **on** Trizbort, not for people using it.

## What Trizbort is

Trizbort is a Windows desktop app (WinForms, **.NET 8**, Windows-only) for drawing maps of
Interactive Fiction (text adventure) games, and exporting those maps either as images/PDF or
as skeleton source code for IF authoring systems (Inform 6/7, TADS, Hugo, ZIL, Alan, Quest,
Adventuron). It can also auto-generate a map by parsing a game transcript ("Automap").

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

Source paths shown without a repository prefix are relative to `src/Trizbort/`. Paths explicitly
prefixed with `tests/`, `samples/`, `Docs/`, or `agent-docs/` are relative to the repository root.

## Map of sub-documents

| Doc | Read this when you're working on... |
|---|---|
| [`01-architecture-overview.md`](01-architecture-overview.md) | You need the 30,000-ft view: project layout, layering, where to start for any given kind of change. Read this once per session if you haven't already. |
| [`02-domain-model.md`](02-domain-model.md) | Room/Connection/Door/Element classes, project-wide `Project.Elements`, geometry (`Vector`/`Port`/`Vertex`), validation, IDs. |
| [`03-storage-and-persistence.md`](03-storage-and-persistence.md) | The `.trizbort` XML map file format, save/load pipeline, app-level `appsettings.json`, user-scoped `Properties.Settings`, clipboard JSON. |
| [`04-commands-and-controllers.md`](04-commands-and-controllers.md) | The `Domain\Commands`/`Domain\Controllers` façade layer between UI and domain — what it does and does **not** provide (there is no undo/redo). |
| [`05-ui-and-canvas.md`](05-ui-and-canvas.md) | `MainForm`, menus/toolbar, dialogs, and the `Canvas` drawing/input surface — coordinate transforms, drag handling, dialog catalogue. |
| [`06-automap.md`](06-automap.md) | The transcript-parsing automapper: how rooms/exits are inferred from game text, ambiguity resolution, settings. |
| [`07-export-subsystem.md`](07-export-subsystem.md) | Adding/fixing an IF-language exporter (Inform6/7, TADS, Alan, Hugo, ZIL, Quest, Adventuron) or the image/PDF export. |
| [`08-cli-and-entry-point.md`](08-cli-and-entry-point.md) | Command-line options, `Program.cs`/`MainForm` startup sequence, how CLI flags map to save/load/automap/export actions. |
| [`09-build-test-and-dotnet8-port.md`](09-build-test-and-dotnet8-port.md) | Build/test commands, project file structure, and hard-won gotchas from the .NET Framework 4.8 → .NET 8 port (read before touching `.csproj`, `app.config`, or `app.manifest`). |

## Quick orientation (if you only read one paragraph)

The whole in-memory document is `Project.Current`, a single flat `BoundList<Element>` called
`Project.Elements` holding every `Room` and `Connection` (no separate rooms/connections/regions
collections). The UI (`MainForm` + `Canvas`) edits that project through a thin
controller/command façade (`Domain\Controllers`, `Domain\Commands`) that has **no undo/redo**.
Maps are persisted as custom XML (`.trizbort` files) via `Domain\Application\MapLoader`/
`MapSaver`/`LegacyMapFileEngine`. A separate transcript-parsing engine (`Automap\Automap.cs`)
can populate the same `Project.Elements` automatically. Exporters in `Export\Languages\*`
convert `Project.Elements` into IF-language source code via a shared `CodeExporter` base class.

## Repo facts an agent should not have to re-derive

- **UI framework**: Windows Forms (not WPF/UWP). Confirmed Windows-only; porting the runtime
  to .NET 8 did **not** make it cross-platform (see `09-build-test-and-dotnet8-port.md`).
- **Target framework**: `net8.0-windows`, SDK-style `src/Trizbort/Trizbort.csproj`, as of the .NET 8 port
  spike. Previously `.NET Framework 4.8`, legacy `packages.config`-based project.
- **Tests**: `src/Trizbort.Tests` (NUnit3 + Shouldly + FluentAssertions), included in
  `Trizbort.sln`. Run with `dotnet test Trizbort.sln`; manual map fixtures and scripts are in
  `tests/manual/`.
- **PDF export**: `PDFsharp-GDI` 6.2.4 NuGet package (not the older vendored/legacy
  `PDFsharp-gdi` 1.x).
- **No undo/redo exists anywhere in the codebase.** Don't assume one when reasoning about
  edit operations; see `04-commands-and-controllers.md`.

# Agent instructions for this repository

Before working on a non-trivial change, read [`agent-docs/README.md`](agent-docs/README.md) —
it's a layered technical reference (architecture, domain model, persistence, UI/Canvas,
Automap, Export, CLI, build/test/.NET-8-port notes) written specifically so an agent doesn't
have to re-derive the codebase from scratch each session. Open only the sub-document(s)
relevant to your task; don't load the whole set into context unless you're doing a broad
architectural change.

If your change invalidates something documented there (new file format, new exporter, changed
settings system, restructured controllers, etc.), update the relevant sub-document as part of
the same change.

## Quick facts
- Projects:
  - `src/Trizbort.Core/` (`net10.0`): Portable domain logic, automap, geometry, exporters, settings, and persistence without WinForms or GDI+ dependencies. Cross-platform.
  - `src/Trizbort.Core.Tests/` (`net10.0`): Headless cross-platform unit tests. Run with `./scripts/run-core-tests.sh` or `dotnet test src/Trizbort.Core.Tests/Trizbort.Core.Tests.csproj`.
  - `src/Trizbort/` (`net10.0-windows`): Windows Forms UI application, GDI+ rendering pipeline, dialogs, and controls. Windows-only.
  - `src/Trizbort.Tests/` (`net10.0-windows`): WinForms and UI-bound integration tests.
  - `src/Trizbort.Avalonia/` (`net10.0`): Cross-platform Avalonia UI client (Linux/macOS/Windows).
  - `src/Trizbort.Avalonia.Tests/` (`net10.0`): Headless Avalonia unit tests. Run with `dotnet test src/Trizbort.Avalonia.Tests/Trizbort.Avalonia.Tests.csproj`.
- Build:
  - Core only (cross-platform): `dotnet build src/Trizbort.Core/Trizbort.Core.csproj`
  - Full solution (Windows or Linux via `EnableWindowsTargeting=true`): `dotnet build Trizbort.sln -c Debug`
- Test:
  - Core tests (cross-platform): `dotnet test src/Trizbort.Core.Tests/Trizbort.Core.Tests.csproj`
  - Avalonia tests (cross-platform): `dotnet test src/Trizbort.Avalonia.Tests/Trizbort.Avalonia.Tests.csproj`
  - Full solution tests (Windows): `dotnet test Trizbort.sln -c Debug`
- Immediate Commit Rule:
  - When making a change, once it is fixed up, building, and tests pass, **commit those changes immediately**.
  - **Do NOT** accumulate a working directory of unrelated changes across different components or domains.
  - Exclude `.junie/` and user-specific files (`*.DotSettings.user`) from git commits.
- Layout: application and automated-test projects under `src/` (`src/Trizbort.Core/`, `src/Trizbort.Core.Tests/`, `src/Trizbort/`, and `src/Trizbort.Tests/`), manual map fixtures and runners in `tests/manual/`.
- No undo/redo exists anywhere in the codebase — don't assume one.

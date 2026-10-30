# Trizbort 

Trizbort is a tool for creating maps for interactive fiction. It was first developed by [genstein](https://github.com/genstein) and later continued by [Jason Lautzenheiser](https://github.com/JasonLautzenheiser). This repository is my continuation of the project, forked from [Jason's Trizbort repository](https://github.com/JasonLautzenheiser/trizbort), with thanks to Jason and all of his collaborators for their work.

## Links
- [Documentation](Docs/index.md)
- [Issues](https://github.com/leeoades/trizbort/issues)
- [Releases](https://github.com/leeoades/trizbort/releases)
- [Release Notes](changelog.md)
- [Developer/architecture reference](agent-docs/README.md) — technical docs for contributors and AI coding agents working on the codebase

## Acknowledgments
Thanks to [DeadFleshRetro](https://github.com/DeadFleshRetro) for testing and feature requests.

## Contribute
Contributions and bug reports are welcome. [Open an issue](https://github.com/leeoades/trizbort/issues/new) or submit a pull request.

Trizbort follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md).

## Repository layout
- `Trizbort.sln` — the cross-platform app (Core + Avalonia + tests), **.NET 10 LTS**.
- `src/Trizbort.Core/` — portable domain model, automap engine, exporters, geometry, and persistence (`net10.0`).
- `src/Trizbort.Core.Tests/` — cross-platform headless unit tests (`net10.0`).
- `src/Trizbort.Avalonia/` — cross-platform Avalonia UI client (`net10.0`).
- `src/Trizbort.Avalonia.Tests/` — headless Avalonia tests (`net10.0`).
- `legacy/` — the original WinForms app (`v2.3.1-beta.2`) with its own `Trizbort.Legacy.sln`, kept for hotfixes; see [legacy/README.md](legacy/README.md).
- `tests/manual/` — manual map fixtures and test scripts (not run by `dotnet test`).
- `samples/` — sample maps and transcripts.
- `Docs/` — user documentation and related assets.
- `agent-docs/` — technical reference for contributors and coding agents.

## Building and Testing

Prerequisites: **.NET 10 SDK** (10.0.x or later).

### New app (Windows, Linux, macOS)
```bash
dotnet build Trizbort.sln -c Debug
dotnet test Trizbort.sln -c Debug
dotnet run --project src/Trizbort.Avalonia
```

### Legacy WinForms app (Windows only, .NET 8)
```bash
dotnet test legacy/Trizbort.Legacy.sln -c Debug
dotnet run --project legacy/src/Trizbort
```

## Download & releases
Download the latest `Trizbort-<version>-win-x64.zip` from the [GitHub Releases page](https://github.com/leeoades/trizbort/releases), extract it into a writable folder (not Program Files), and run `Trizbort.exe`. These are Windows x64, self-contained builds, so no separate .NET install is required. Keep the accompanying configuration and licence files. The downloads are unsigned, so Windows may display a SmartScreen warning.

## License
[MIT](LICENSE.txt)

## Special notes
This software uses PdfSharp, copyright (c) 2005-2007 empira Software GmbH, Cologne (Germany). See PdfSharp.License.txt.

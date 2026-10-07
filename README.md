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
- `Trizbort.sln` — solution at the repository root.
- `src/Trizbort/` — WinForms application source and project.
- `src/Trizbort.Tests/` — automated NUnit tests.
- `tests/manual/` — manual map fixtures and test scripts (not run by `dotnet test`).
- `samples/` — sample maps and transcripts.
- `Docs/` — user documentation and related assets.
- `agent-docs/` — technical reference for contributors and coding agents.

## Download & releases
Download the latest `Trizbort-<version>-win-x64.zip` from the [GitHub Releases page](https://github.com/leeoades/trizbort/releases), extract it into a writable folder (not Program Files), and run `Trizbort.exe`. These are Windows x64, self-contained builds, so no separate .NET install is required. Keep the accompanying configuration and licence files. The downloads are unsigned, so Windows may display a SmartScreen warning.

## License
[MIT](LICENSE.txt)

## Special notes
This software uses PdfSharp, copyright (c) 2005-2007 empira Software GmbH, Cologne (Germany). See PdfSharp.License.txt.

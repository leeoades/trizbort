# Trizbort: Recast

Trizbort: Recast is a map editor for interactive fiction. Draw rooms and connections as you play, let Automap build the
map from a game transcript, and export it as an image, a PDF, or a starting point for your own game in Inform 7,
Inform 6, TADS, Hugo, ZIL, Quest, Alan or Adventuron.

Recast is a cross-platform rebuild of **Trizbort**. It runs on Windows, Linux and macOS, and opens and saves the same
`.trizbort` maps.

> **Status: beta.** Recast does everything the Windows app does, and more, but it is new. Keep backups of your
> maps and please [report anything that goes wrong](https://github.com/leeoades/trizbort/issues).

## Where it comes from
Trizbort was first written by [Genstein](https://github.com/genstein) and continued by
[Jason Lautzenheiser](https://github.com/JasonLautzenheiser) and many collaborators, whose work this builds on with
thanks. This repository is forked from [Jason's Trizbort repository](https://github.com/JasonLautzenheiser/trizbort).

The fork first released Trizbort 2.x, a Windows-only update of the original app with map labels, curved connections,
themes and more. Recast carries all of that over to a new, cross-platform app and adds undo and redo, trackpad and
touch support, vector PDF export and much more. The [changelog](changelog.md) lists what is new compared with
Trizbort 1.7.4.1.

## Download
Get the latest release from the [GitHub Releases page](https://github.com/leeoades/trizbort/releases). Downloads are
self-contained, so no separate .NET install is needed.

- **Windows x64:** run `TrizbortRecast-win-x64-Setup.exe`, which installs Recast for you alone and keeps it up to
  date. Or extract `TrizbortRecast-<version>-win-x64.zip` into a writable folder and run `TrizbortRecast.exe`.
- **Linux x64:** run the `.AppImage` (after `chmod +x`), which keeps itself up to date. Or extract
  `TrizbortRecast-<version>-linux-x64.tar.gz` and run `trizbort-recast`. To open `.trizbort` files from your file
  manager, see [packaging/README.md](packaging/README.md).
- **macOS:** `TrizbortRecast-osx-arm64-Portable.zip` for Apple Silicon (M1 and later) or `…-osx-x64-Portable.zip` for
  Intel Macs keeps itself up to date; the plain `…-osx-arm64.zip` / `…-osx-x64.zip` don't. Unzip it and drag
  `Trizbort Recast.app` to Applications. The macOS build is new and hasn't been tested on a Mac yet; reports are very
  welcome.

See [Getting started](https://leeoades.github.io/trizbort/getting-started/) for more.

Releases of the Windows-only Trizbort 2.x are still there too (`legacy-v*` tags and the older `v2.*` tags).

## Signing the app, and how you can help
The downloads aren't signed yet, so Windows SmartScreen and macOS warn about them the first time, and some antivirus
tools have flagged them. Trizbort contains no malware: it's built from the public source in this repository by GitHub
Actions. [Why it isn't signed yet, and how you can help](https://leeoades.github.io/trizbort/signing/).

## Links
- [User guide](https://leeoades.github.io/trizbort/) (its pages are in [Docs](Docs/index.md)) and
  [what's new](Docs/whats-new.md), with pictures. The guide to the original app and
  Trizbort 2 is in [legacy/Docs](legacy/Docs/index.md)
- [Issues](https://github.com/leeoades/trizbort/issues)
- [Releases](https://github.com/leeoades/trizbort/releases)
- [Recast changelog](changelog.md) and [Trizbort 1.x and 2.x changelog](legacy/changelog.md)
- [Developer and architecture reference](agent-docs/README.md): technical docs for contributors and AI coding agents

## Acknowledgments
Thanks to [DeadFleshRetro](https://github.com/DeadFleshRetro) for testing and feature requests.

## Contribute
Contributions and bug reports are welcome. [Open an issue](https://github.com/leeoades/trizbort/issues/new) or submit a
pull request.

This project follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md).

## Repository layout
- `Trizbort.sln`: Trizbort: Recast (Core + Avalonia + tests), **.NET 10 LTS**.
- `src/Trizbort.Core/`: the map model, editing, Automap, exporters, geometry and file handling, with no UI (`net10.0`).
- `src/Trizbort.Core.Tests/`: headless unit tests (`net10.0`).
- `src/Trizbort.Avalonia/`: the Avalonia app (`net10.0`).
- `src/Trizbort.Avalonia.Tests/`: headless Avalonia tests (`net10.0`).
- `legacy/`: the Windows Forms Trizbort 2.x (`v2.3.1-beta.2`) with its own `Trizbort.Legacy.sln`, kept for hotfixes
  and as the reference for Recast's behaviour; see [legacy/README.md](legacy/README.md).
- `packaging/`: app icon, macOS app bundle, and file association for Linux and Windows.
- `scripts/`: developer scripts, including the export comparison between the two apps.
- `tests/manual/`: manual map fixtures and test scripts (not run by `dotnet test`).
- `samples/`: sample maps and transcripts.
- `Docs/`: the Recast user guide, and its pictures in `Docs/images` (drawn by `scripts/update-docs-images`).
- `agent-docs/`: technical reference for contributors and coding agents.

## Building and testing
Prerequisites: **.NET 10 SDK** (10.0.x or later).

### Trizbort: Recast (Windows, Linux, macOS)
```bash
dotnet build Trizbort.sln -c Debug
dotnet test Trizbort.sln -c Debug
dotnet run --project src/Trizbort.Avalonia
```

### Trizbort 2.x, the Windows Forms app (Windows only, .NET 8)
```bash
dotnet test legacy/Trizbort.Legacy.sln -c Debug
dotnet run --project legacy/src/Trizbort
```

## Releasing
Pushing a tag starts a release build on GitHub Actions. Every Recast release, beta or full, needs a changelog entry
under a heading without the prefix, such as `## [0.15.0-beta]` or `## [1.0.0]`: it becomes the release notes. Recast
betas are `-beta` or `-beta.N`. The legacy app needs one for a beta tag, and accepts `-beta.N`.

Before tagging a Recast release, redraw the guide's pictures with `scripts/update-docs-images.ps1` (Windows) or
`scripts/update-docs-images.sh`, and add the release's highlights to [Docs/whats-new.md](Docs/whats-new.md).

| Tag | Builds | Changelog |
|---|---|---|
| `recast-v0.15.0-beta` | Trizbort: Recast for Windows x64, Linux x64 and macOS (Apple Silicon and Intel) | [changelog.md](changelog.md) |
| `legacy-v2.3.2` | Trizbort 2.x for Windows x64 | [legacy/changelog.md](legacy/changelog.md) |

## License
[MIT](LICENSE.txt)

## Privacy
Trizbort collects no information about you. See the [privacy policy](PRIVACY.md).

## Special notes
This software uses PDFsharp, copyright (c) 2005-2007 empira Software GmbH, Cologne (Germany). See PdfSharp.License.txt.

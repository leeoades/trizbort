# Trizbort 

Trizbort is a simple tool used to create maps for interactive fiction. First developed by genstein back in 2010, he continued to update until March, 2013 when he open-sourced the code and uploaded it here to Github. Fast forward to 2015 when I picked up the development and along with Andrew Schultz (focused on documentation and testing) we continue to improve this great software. Follow our blog for announcements and updates (https://lautzofif.wordpress.com/tag/trizbort/)

![Mark stale issues and pull requests](https://github.com/JasonLautzenheiser/trizbort/workflows/Mark%20stale%20issues%20and%20pull%20requests/badge.svg)

## Links
- [Documentation](http://www.trizbort.com/Docs/index.shtml)
- [Release Notes](https://github.com/JasonLautzenheiser/trizbort/blob/master/changelog.md)
- [Developer/architecture reference](agent-docs/README.md) — technical docs for contributors and AI coding agents working on the codebase

## Maintainers
We thank all our contributors for all the hard work whether pull requests or simply bug reports.  Special thanks to the following for taking the time to contribute features and bug fixes.

- [andrewshcultz](https://github.com/andrewschultz) - much testing, features and bug fixes. Also updating the documentation.
- [Tymian](https://github.com/Tymian) - Bug fixes and work on copy/paste functionality
- [ThePix](https://github.com/ThePix) - work on the export to Quest
- [matthiaswatkins](https://github.com/matthiaswatkins) - for much work making the automapper faster and more reliable
- [genstein](https://github.com/genstein) - obviously thanks for the intial development and all the hard work that went into Trizbort up to the point I took it over.
- [dfabulich](https://github.com/dfabulich) - fixed some issues in documentation
- [taradinoc](https://github.com/taradinoc) - tweaks to the ZIL exporter

The primary developer on this is [Jason Lautzenheiser](https://github.com/JasonLautzenheiser)

## Contribute
Feel free to dive in!  [Open an issue](https://github.com/JasonLautzenheiser/trizbort/issues/new) or submit PRs.

Trizbort follows the [Contributor Covenant Code of Conduct](https://github.com/JasonLautzenheiser/trizbort/blob/master/CODE_OF_CONDUCT.md)

## Repository layout
- `Trizbort.sln` — solution at the repository root, including the app and automated test projects.
- `src/Trizbort/` — WinForms application source and project.
- `src/Trizbort.Tests/` — automated NUnit tests.
- `tests/manual/` — manual map fixtures and test scripts (not run by `dotnet test`).
- `samples/` — sample maps and transcripts.

## Download & releases
Download `Trizbort-<version>-win-x64.zip` from [Releases](https://github.com/leeoades/trizbort/releases/latest), extract it into a writable folder (not Program Files) and run `Trizbort.exe`. These are Windows x64, self-contained builds: no separate .NET install is required. Keep the accompanying configuration and licence files. The downloads are unsigned, so Windows may display a SmartScreen warning.

**Help → Check for Updates** checks the latest GitHub Release, verifies the download's SHA256 checksum and updates the extracted files in place. Existing `appsettings.json` is not replaced. This requires at least one release published by the new workflow; older ClickOnce downloads still point to the old server and cannot automatically migrate.

To publish a release, first push the [Release workflow](.github/workflows/release.yml) and app changes, then push an increasing version tag, e.g.:

```powershell
git tag v1.8.0
git push leeoades v1.8.0
```

Use your remote's name if it is not `leeoades`. The workflow runs on Windows, tests the solution, builds the self-contained app, and publishes the zip plus `trizbortupdate.xml` with generated release notes. Tags must be `vMAJOR.MINOR.PATCH` or `vMAJOR.MINOR.PATCH.REVISION`; prerelease suffixes are not supported. The tag sets the release binary's version without changing the committed `AssemblyInfo.cs`. No separate web server, GitHub Pages site or additional secret is needed; the workflow uses GitHub's built-in token. Check the workflow run completes successfully before announcing the release.

## License
[MIT](https://github.com/JasonLautzenheiser/trizbort/blob/master/LICENSE.txt)

## Special notes
This software uses PdfSharp, copyright (c) 2005-2007 empira Software GmbH, Cologne (Germany). See PdfSharp.License.txt.

## Special Thanks
[![Resharper](http://www.trizbort.com/img/logo_resharper.png)](https://www.jetbrains.com/resharper/)

[<img src="https://oz-code.com/wp-content/uploads/2020/01/oz-code-logo.svg" width="100">](https://www.oz-code.com/)

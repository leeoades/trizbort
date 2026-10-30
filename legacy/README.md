# Legacy WinForms Trizbort

This folder holds the **original Windows Forms Trizbort** exactly as released at tag `v2.3.1-beta.2`,
restored with its full git history (`git log --follow legacy/src/Trizbort/UI/MainForm.cs` reaches
2013). It is kept for two reasons:

1. **Urgent hotfixes and legacy releases** while the cross-platform app (root `Trizbort.sln`) is
   being completed.
2. **Reference behaviour** for the Avalonia port — see `agent-docs/plans/avalonia-parity-review.md`,
   where this folder is called `$REF`.

It is completely independent of the new app: it does not reference `src/Trizbort.Core` and the root
solution does not reference it. The only shared files are the fixtures in `tests/manual/`.

| Path | What |
|---|---|
| `Trizbort.Legacy.sln` | Solution for the two projects below. |
| `src/Trizbort/` | WinForms app (`net8.0-windows`). Domain, Automap, Export and UI all live in this one project. |
| `src/Trizbort.Tests/` | NUnit tests (`net8.0-windows`). |
| `vendor/` | Old vendored DLLs; not referenced by the projects. |

## Build and test (Windows, .NET 8 SDK or later with the .NET 8 runtime)

```powershell
dotnet build legacy\Trizbort.Legacy.sln -c Debug
dotnet test  legacy\Trizbort.Legacy.sln -c Debug
dotnet run --project legacy\src\Trizbort
```

CI: `.github/workflows/legacy-tests.yml` runs on any change under `legacy/` or `tests/manual/`.

## Hotfix rules

- Keep the change minimal and local to `legacy/`. Don't port refactors or new features here.
- Add or update a test in `legacy/src/Trizbort.Tests/` that fails without the fix.
- Add an entry to the root `changelog.md` under a heading `## [2.3.N]` (no `legacy-` prefix).
- If the bug also exists in the new app, open an issue/ticket for it there too.

## Releasing a hotfix

Push a tag `legacy-vMAJOR.MINOR.PATCH` (optionally `-beta.N`), e.g. `legacy-v2.3.2`.
`.github/workflows/release-legacy.yml` stamps the version, runs the tests, publishes a self-contained
`win-x64` build and creates the GitHub release "Trizbort (legacy) legacy-v2.3.2".
Releases of the new app use plain `v*` tags and `.github/workflows/release.yml`.

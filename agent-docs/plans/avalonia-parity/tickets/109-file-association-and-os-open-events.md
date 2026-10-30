# 109 — File association metadata and OS "open file" activation

Matrix rows: A001 (association half)          Priority: P?          Size: S
Depends on: 104                               Suggested tier: mid (macOS part not verifiable without a Mac)

## What
Double-clicking a `.trizbort` file in the OS launches Trizbort with that file (Windows and Linux), and opening a file while the app is already running on macOS (`UrlsOpened`/activation) loads it through the same open path as 104.

## Reference (read only these)
- `agent-docs/08-cli-and-entry-point.md` (reference had no registration in code; installer only)
- `.github/workflows/release.yml` (publish step) — to see where artefacts are produced

## Implement
1. Linux: add `packaging/linux/trizbort.desktop` (`MimeType=application/x-trizbort;`, `Exec=trizbort %f`) and `packaging/linux/trizbort-mime.xml` (glob `*.trizbort`), plus a short `packaging/linux/README.md` with the `xdg-mime`/`update-desktop-database` install commands.
2. Windows: add `packaging/windows/register-file-association.reg` (HKCU, `.trizbort` -> `Trizbort.Map`, `shell\open\command` `"%1"`) and note it in the same README; an installer is out of scope.
3. Avalonia: handle `IActivatableLifetime`/`App` URL-open events (`ActivatedEventArgs` of kind File) by calling `StartupActions.OpenAsync(path)` from 104 (unsaved-changes prompt applies when a map is already open). Wrap in `OperatingSystem.IsMacOS()` where API differs. Mark "not verifiable without a Mac".
4. `StartupActions.OpenAsync` must be safe to call before the window is shown (queue until `Opened`).

## Tests
- Avalonia: `StartupTests.cs` — an activation event with a file path invokes the open path (event type faked behind a small interface).

## Done when
- Build + Avalonia tests pass on Linux; files committed; matrix A001 updated.
- Committed as `parity(109): file association and open events`.

## Out of scope
- Installers, signing, `.app` bundle / `Info.plist` UTI (T3.4).

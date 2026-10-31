# 100 — Store appsettings.json in a per-user config folder

Matrix rows: A012          Priority: P?          Size: S
Depends on: none          Suggested tier: mid

## What
Settings are read from and written to the OS per-user config folder, never the working directory (fixes I-011). Windows `%APPDATA%\Trizbort`, Linux `$XDG_CONFIG_HOME/trizbort` (fallback `~/.config/trizbort`), macOS `~/Library/Application Support/Trizbort`. The legacy one-time import of `%LocalAppData%\Genstein\Trizbort\Settings.xml` keeps working. An existing `./appsettings.json` in the working directory is imported once if the new file is absent (migration for anyone who ran the dev build).

## Reference (read only these)
- `$REF/src/Trizbort/Domain/AppSettings/ApplicationSettingsController.cs` — `LoadSettings`, `LoadLegacyAppSettings`
- `src/Trizbort.Core/Domain/AppSettings/ApplicationSettingsController.cs` — `AppSettingsFileName` (literal `@".\appsettings.json"`), static constructor, `LoadSettings`, `SaveSettings`
- `agent-docs/plans/avalonia-parity/issues.md` — I-011

## Implement
1. Core: new `AppPaths` static class in `src/Trizbort.Core/Domain/AppSettings/AppPaths.cs` with `ConfigFolder` (per OS as above, using `OperatingSystem.IsWindows/IsMacOS` and `Environment.GetFolderPath(SpecialFolder.ApplicationData)`; on Linux honour `XDG_CONFIG_HOME`), `SettingsFilePath`, and an override hook `AppPaths.OverrideConfigFolder` (tests and `--config-dir`, see 104). Create the folder on demand.
2. Replace the `AppSettingsFileName` literal with `AppPaths.SettingsFilePath` (use `Path.Combine`; no backslash literals). `SaveSettings` creates the folder, writes to a temp file and moves it over the target so a crash cannot truncate settings.
3. Migration order in `LoadSettings`: new path, else legacy working-directory `appsettings.json`, else legacy XML, else defaults.
4. Wrap read in try/catch: on corrupt JSON, rename to `appsettings.json.bad`, log, load defaults (do not crash at startup).
5. Avalonia: nothing.

## Tests
- Core: `src/Trizbort.Core.Tests/AppPathsTests.cs` — folder per OS via an injectable `Func<string> getBase` / env values; override works; migration from working-dir file; corrupt file falls back to defaults; save is atomic (no leftover temp file).

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix row A012 updated to Implemented with evidence; I-011 gets `Ticket: 100`.
- Committed as `parity(100): per-user settings folder`.

## Out of scope
- Persisting new fields (101, 117). Portable/next-to-exe mode.
- Per-language export folder memory (118).

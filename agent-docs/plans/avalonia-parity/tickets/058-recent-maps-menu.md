# 058 — Add Recent Maps submenu

Matrix rows: M012          Priority: P2          Size: M
Depends on: 002, 009, 010        Suggested tier: mid

## What
File > Recent Maps lists up to four recent maps (`&1 path`), is disabled when empty, drops entries whose file no longer exists, and opens the clicked one (asking about unsaved changes).

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `SetupMruMenu` (line ~1012), `FileRecentProject_Click` (525)
- `$REF/src/Trizbort/Domain/AppSettings/ApplicationSettingsController.cs` — `RecentProjectsMaxCount`, recent-project add logic (grep -n 'RecentProjects')

## Implement
1. Core: `file.recent` dynamic group (needs dynamic children support from 009/024) producing `file.openRecent:<path>` entries from `AppSettings.RecentProjects`; pruning missing files; add path on successful open/save. Open goes through the same flow as `file.open`.
2. IUserInteraction unsaved-changes prompt reused.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/RecentMapsTests.cs` — max count, pruning, ordering, disabled when empty.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(058): Add Recent Maps submenu`.

## Out of scope
- App start-up reopening the last map.

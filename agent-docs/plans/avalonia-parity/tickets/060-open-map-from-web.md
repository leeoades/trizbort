# 060 — Add Open Map from Web

Matrix rows: M003          Priority: P3          Size: M
Depends on: 002, 009, 010        Suggested tier: mid

## What
`Primary+Shift+O` asks for a URL and opens that map; URL-sourced maps Save As rather than overwrite, and appear in Recent Maps.

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `FileOpenFromWebMenuItemClick` (1349), `OpenUrl` (grep -n), `FileSaveMenuItem_Click` URL branch (538-545)

## Implement
1. Core: `file.openWeb` using `IEditorPrompts.AskText("Load from Web", "URL")` and an injectable `IHttpFetcher`; load via `LegacyMapFileEngine`. Save on a URL-named project redirects to Save As (`Project.FileName.IsUrl()`).
2. Errors shown through the prompt service.

## Tests
- Core: `src/Trizbort.Core.Tests/Editor/OpenFromWebTests.cs` — fake fetcher returns a fixture map, bad URL error, Save redirect.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix rows updated to Implemented with evidence.
- Committed as `parity(060): Add Open Map from Web`.

## Out of scope
- Network settings, proxies.

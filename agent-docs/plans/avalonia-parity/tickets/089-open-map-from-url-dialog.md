# 089 — Open Map from URL dialog

Matrix rows: D012          Priority: P?          Size: S
Depends on: 009, 010, 070, 002           Suggested tier: mid

## What
File > "Open Map from Web..." (Ctrl+Shift+O per the shortcut map) asks for a URL in a small dialog with validation, then loads the map (after the usual save-changes check).

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/NoCounterpart.md` §D012.
- `$REF/src/Trizbort/UI/MainForm.cs:1349-1388` (`OpenUrl`, `CheckLoseProject`).
- `src/Trizbort.Core/Domain/Application/Project.cs:226-232` (`Load(Uri)` internal), `Properties/AssemblyInfo.cs:5-7`.

## Implement
1. Core: make `Project.Load(Uri)` (or a new `ProjectLoader.LoadFromUriAsync`) public; handle `HttpClient` failures with a clear exception message; file name = last path segment.
2. Avalonia: `Views/Dialogs/OpenUrlDialog.axaml` — title "Load from Web", label "_URL:", TextBox initial focus, error text for invalid/non-http(s) URL, OK/Cancel (Enter/Esc), width resizable 480 px, fixed height; implemented as `IEditorPrompts.PromptTextAsync(title, label)` specialisation `PromptUrlAsync`.
3. Core command `file.openFromWeb` in the catalogue: save-changes check via the existing prompt, URL prompt, load, add to recent files; errors shown with a message ("Could not open <url>: <reason>"), never unhandled.
4. Do not port the generic InputDialog.

## Tests
- Core: `src/Trizbort.Core.Tests/OpenFromUrlTests.cs` — invalid URL message; success path with a fake loader; dirty-check cancel aborts.
- Avalonia: dialog validation test in `DialogViewModelTests.cs`.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D012 -> Implemented (as a purpose-built dialog).
- Committed as `parity(089): open map from url`.

## Out of scope
- Authentication, caching downloaded maps.

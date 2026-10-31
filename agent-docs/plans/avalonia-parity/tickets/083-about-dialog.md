# 083 — About dialog: version, credits, repository links, licence, third-party notices

Matrix rows: D015          Priority: P?          Size: S
Depends on: 070           Suggested tier: cheap

## What
About shows the version (informational version of the assembly), credits for Genstein, Jason Lautzenheiser and Lee Oades with clickable GitHub links, a scrollable read-only licence text, and third-party credits for libraries actually shipped. Enter/Esc close it.

## Reference (read only these)
- `agent-docs/plans/avalonia-parity/dialogs/About.md`.
- `$REF/src/Trizbort/UI/AboutDialog.cs` and `AboutDialog.Designer.cs` (grep `Text =`) — credit text, link URLs, licence text.
- `src/Trizbort.Avalonia/Views/AboutDialog.axaml(.cs)`; screenshots `screenshots/avalonia/About__main__default.png`, `screenshots/winforms/AboutDialog__main__default.png`.
- Launcher: use `TopLevel.GetTopLevel(this).Launcher.LaunchUriAsync` (shared with 088; if 088 has not landed, add a tiny `IUriLauncher` service in `Services/` that 088 reuses).

## Implement
1. Version: `Version {Assembly.GetEntryAssembly()...InformationalVersion}` stripped of `+commit` suffix optionally, shown under the title.
2. Credits with headings and `HyperlinkButton`s to the three GitHub URLs from the legacy dialog; keep the Avalonia tagline.
3. Licence: read-only multi-line `TextBox` in a bordered group "Licence" (copy the legacy licence text; embed as resource `Assets/LICENSE.txt` from repo `LICENSE` if present).
4. Third-party: list libraries really in the Avalonia build (Avalonia, SkiaSharp, CommunityToolkit.Mvvm, and PDFsharp only if still referenced — check the `.csproj` files).
5. OK `IsDefault` and `IsCancel`; use `DialogWindow` from 070; size so licence box is visible without scrolling (about 560x520, resizable false is OK).
6. Link failure: show `IEditorPrompts` message "Unable to open the link: <url>".

## Tests
- Avalonia: `src/Trizbort.Avalonia.Tests/DialogViewModelTests.cs` (or `AboutDialogTests.cs`) — version text non-empty; links present; no-clipping.

## Done when
- Build + Core tests + Avalonia tests pass on Linux.
- Matrix D015 -> Implemented.
- Committed as `parity(083): about dialog`.

## Out of scope
- Artwork image (optional).

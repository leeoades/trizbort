# About dialog review (D015)

Reference: `legacy/src/Trizbort/UI/AboutDialog.cs` + `AboutDialog.Designer.cs`. Avalonia: `src/Trizbort.Avalonia/Views/AboutDialog.axaml` (+ `.axaml.cs`).
Screenshots: `screenshots/winforms/AboutDialog__main__default.png`, `screenshots/avalonia/About__main__default.png`.

| Field | Reference | Avalonia | Status | Notes |
|---|---|---|---|---|
| Title | "About" (Designer:207) | "About Trizbort" (axaml:4) | Different | Fine. |
| Product name | "Trizbort" (Designer:55) | "Trizbort" 28pt bold (axaml:11) | Implemented | |
| Tagline | "Interactive Fiction Mapper" (Designer:65) | "Interactive Fiction Map Editor" + "Modern Cross-Platform Desktop Edition" (axaml:12-13) | Different | Wording changed. |
| Version | Label "Version <InformationalVersion>" (AboutDialog.cs:14) | None | Missing | Most useful item for bug reports; should be added (read assembly informational version). |
| Credits | Genstein, Jason Lautzenheiser, Lee Oades, each with heading (Designer:85,145,179) | One line "Originally created by Genstein, maintained by the IF Community" (axaml:16) | Partial | Lautzenheiser and Oades credits dropped. |
| Repository links | Three clickable github.com links, OnLinkClicked opens browser (AboutDialog.cs:22-28) | None | Missing | No clickable links. |
| Licence text | Read-only multiline textbox with licence (Designer:129-135) | None | Missing | MIT-style licence notice not shown. |
| Third-party notice | PDFsharp v1.50.5147 copyright + link (Designer:97) | "Powered by Avalonia UI 11.x & .NET 8" (axaml:15) | Different | Replaces PDFsharp credit; confirm whether PDFsharp is still used in the Avalonia build, and add credits for libraries actually shipped. |
| Artwork | Wizard4 image on left (Designer:105) | None | Missing | Decorative; optional. |
| OK button | None in reference (closed by title bar X only) | OK button (axaml:19-23) | New-in-Avalonia | Improvement; no IsDefault/IsCancel so Enter/Esc do not close (minor). |
| Size | Fixed, 512x364 | Fixed 460x320, CanResize false | Pass | |

## Recommendation

Keep the Avalonia layout but add the lost content: a Version line, the three credit headings with clickable repository links, the licence text in a read-only scroll box, and third-party credits. Status: Partial.

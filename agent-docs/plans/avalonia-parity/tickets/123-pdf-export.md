# 123 — Export PDF (single page, vector, metadata) — includes engine decision

Matrix rows: M007, E027, E029          Priority: P?          Size: L
Depends on: 120, 121, 122, 011          Suggested tier: mid plus strong-tier review of the diff

## What
File > Export > PDF writes a single-page vector PDF sized to the map bounds with Title/Author/Subject metadata from the project, on Windows and Linux (and macOS in code). Room description annotations are ticket 124.

## Decision: PDF engine (Lee to confirm)

Options compared (E029). None was built or measured here; verify items in step 1 before committing to code.

| | A. Skia PDF (`SKDocument.CreatePdf`) | B. PDFsharp "core" build | C. Hybrid: Skia page + PDFsharp annotations |
|---|---|---|---|
| Fidelity vs on-screen map | Identical: reuses the existing Avalonia `RenderMap` through a Skia lease, one drawing path | Needs a second drawing path (`XGraphics`) for every element, line style, text and theme; the legacy `Canvas.Draw` is GDI+ and cannot be reused | Identical (as A) |
| Effort | S–M | XL (re-implement renderer) | M (A plus a post-pass) |
| Vector output | Yes (text stays text with embedded fonts) | Yes | Yes |
| Room-description annotations (E028) | No annotation API in `SKDocument` | Yes (`PdfTextAnnotation`) | Yes (open the Skia PDF with PDFsharp, add annotations by room bounds) |
| Metadata | `SKDocumentPdfMetadata` supports title/author/subject | Yes | Yes (either) |
| New dependency | SkiaSharp (already a transitive dependency of Avalonia on desktop; pin it explicitly) | PDFsharp 6.x `PDFsharp` core package (MIT) | A + PDFsharp (MIT) |
| Risk | Must get the Skia canvas from Avalonia (`ISkiaSharpApiLeaseFeature` inside an `ICustomDrawOperation`); Avalonia 11.3.22 in csproj, confirm it exposes it; alternatively render the visual tree to Skia directly if Avalonia drawing code can target `SKCanvas` | Two renderers drift apart over time | Post-pass re-parses the PDF; page coordinates must be mapped carefully (PDF origin bottom-left) |

**Recommendation: C (hybrid), delivered in two steps.** Ship A first in this ticket (identical look, small), then add annotations via PDFsharp core in 124 only if Lee wants them; if not, A alone is complete. B is rejected: a second renderer is the largest cost and its main benefit (annotations) is available more cheaply through C. **Lee to confirm.** Raster-in-PDF (PNG on a page) is the fallback if the Skia lease is unavailable; it loses vectors and should be treated as a stop-and-ask.

## Reference (read only these)
- `$REF/src/Trizbort/Export/MapPdfExporter.cs` — whole file (small): page size from map bounds, metadata, drawing
- `$REF/src/Trizbort/UI/MainForm.cs` — the Export PDF click handler (lines ~466–487)
- `src/Trizbort.Avalonia/Controls/MapCanvasControl.cs` — `RenderToBitmap` (line ~1858) and the `RenderMap` method it calls
- `src/Trizbort.Avalonia/Trizbort.Avalonia.csproj`

## Implement
1. Spike (stop and ask if it fails): in an Avalonia test, obtain `SKCanvas` via `ISkiaSharpApiLeaseFeature` from an `ICustomDrawOperation`, render the map into an `SKDocument.CreatePdf` page of map-bounds size. If the headless platform has no Skia lease, test on the `Avalonia.Skia` headless backend (`UseSkia()` in test app builder).
2. Core: `IMapPdfWriter` (from 110) `Task WriteAsync(Project, string path, PdfMetadata)`; `PdfMetadata` from project title/author/description (fallbacks "A Trizbort Map" / "A Trizbort User").
3. Avalonia: `SkiaMapPdfWriter` implementing it; page size = map bounds at 100% (clamped to PDF limits 14,400 pt); respects image-export bounds logic (121 `ImageExportPlanner`).
4. Catalogue command `file.export.pdf`, picker with remembered folder/name (reuse 118 pattern, add `LastExportPdfFileName` setting if absent), overwrite prompt, error message, status "Exported PDF".

## Tests
- Avalonia: `PdfExportTests.cs` — file starts with `%PDF-`, has one page (count `/Type /Page` occurrences), metadata strings present, non-empty for the sample map.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- Rows M007, E027, E029 updated; the decision section stays in this ticket; committed as `parity(123): PDF export`.

## Out of scope
- Annotations (124). Multi-page tiling.

# 120 — Image export formats (PNG, JPEG, BMP) and default image type

Matrix rows: E016, E017, E024          Priority: P?          Size: M
Depends on: 100, 118 (for shared path memory pattern)          Suggested tier: mid

## What
Export Image offers PNG, JPEG and BMP (format chosen by file type filter and extension, consistent with the saved bytes), defaults to the `DefaultImageType` setting, and remembers the last image folder. Today `.jpg` files contain PNG data. EMF is Windows-only: not ported; flagged "Lee to confirm Won't-port".

## Reference (read only these)
- `$REF/src/Trizbort/UI/MainForm.cs` — `FileExportImageMenuItem_Click` (line 430), `ExportImage` (line 366), and the default-image-type switch (lines ~546–566)
- `src/Trizbort.Avalonia/Services/StorageService.cs` — `ExportImageAsync` (line 202)
- `src/Trizbort.Avalonia/Controls/MapCanvasControl.cs` — `RenderToBitmap` (line ~1858)

## Implement
1. Core: `ImageFormat` enum (`Png, Jpeg, Bmp`) with extensions and `FromExtension`; `AppSettings.DefaultImageType` mapping 0=png,1=jpg,2=bmp,3=emf (EMF maps to PNG with a one-time note in the status bar).
2. Avalonia: encode from the rendered `RenderTargetBitmap` pixels with SkiaSharp (`SKBitmap.Encode`; JPEG quality 90) or Avalonia's `Save` for PNG only; choose by extension, else by selected filter, else by setting. Avoid new native dependencies: SkiaSharp already ships with Avalonia desktop.
3. Picker filters per format; start location from the remembered folder (store folder, not file path — E024 quirk); default extension from the setting.
4. Add `Default image type` combo (PNG/JPEG/BMP) to App Settings if missing (E017; check `AppSettingsViewModel`).

## Tests
- Avalonia: `ImageExportTests.cs` — render a small map, encode each format, check magic bytes (PNG `89 50 4E 47`, JPEG `FF D8`, BMP `42 4D`) and extension/format mismatch handling.

## Done when
- Build + Core + Avalonia tests pass on Linux.
- Rows updated; committed as `parity(120): image export formats`.

## Out of scope
- Scale and selection options (121), fidelity (122), EMF.

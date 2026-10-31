using System.IO;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Editor;
using Trizbort.Export;

namespace Trizbort.Avalonia.Services;

/// <summary>Implementations behind File &gt; Open Map from Web (tickets 060, 089) and Smart Save (ticket 059).</summary>
public static class WebAndSmartSaveCommands
{
    /// <summary>Replaced by tests so nothing touches the network.</summary>
    public static IMapDownloader Downloader { get; set; } = new HttpMapDownloader();

    /// <summary>The Smart Save image writer; replaced by tests.</summary>
    public static IMapImageWriter ImageWriter { get; set; } = new SkiaMapImageWriter();

    /// <summary>The Smart Save PDF writer; replaced by tests.</summary>
    public static IMapPdfWriter PdfWriter { get; set; } = new SkiaMapPdfWriter();

    /// <summary>The Smart Save outputs: PDF and image, written through <see cref="SmartSaveService" />.</summary>
    public static IReadOnlyList<ISmartSaveExporter> SmartSaveExporters =>
    [
        new SmartSaveWriterExporter(
            SmartSaveKind.Pdf, ImageWriter, PdfWriter, () => ApplicationSettingsController.AppSettings),
        new SmartSaveWriterExporter(
            SmartSaveKind.Image, ImageWriter, PdfWriter, () => ApplicationSettingsController.AppSettings)
    ];

    public static async Task OpenFromWebAsync(EditorSession session, MainWindowViewModel vm)
    {
        var opened = await OpenFromUrl.RunAsync(
            session, Downloader, vm.CanLoseProjectAsync, StorageService.RecordProjectOpened);
        if (!opened) return;

        if (vm.IsAutomapActive) vm.StopAutomap();
        vm.CommandService.ResetZoom();
        vm.StatusText = $"Opened map: {MapUrl.FileName(new Uri(session.Project.FileName))}";
    }

    public static async Task SmartSaveAsync(EditorSession session, MainWindowViewModel vm)
    {
        var done = await SmartSave.RunAsync(
            session,
            ApplicationSettingsController.AppSettings,
            SmartSaveExporters,
            saveAs => saveAs ? vm.SaveMapAsAsync() : vm.SaveMapAsync());
        if (done) vm.StatusText = "Smart Save complete";
    }
}

/// <summary>Writes the map as a raster image through the same renderer as the image export (ticket 110).</summary>
public sealed class SkiaMapImageWriter : IMapImageWriter
{
    public Task WriteAsync(Project project, string path, ImageFormat format)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrEmpty(path);

        using var bitmap = MapCanvasControl.RenderToBitmap(project);
        if (bitmap == null) throw new InvalidOperationException("Cannot export an empty map.");

        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
        {
            ImageEncoder.Encode(bitmap, format, stream);
        }

        ApplicationSettingsController.AppSettings.LastExportImageFileName = path;
        return Task.CompletedTask;
    }
}

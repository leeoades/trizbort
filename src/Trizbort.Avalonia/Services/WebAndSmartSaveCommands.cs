using System.IO;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.AppSettings;
using Trizbort.Editor;
using Trizbort.Export;

namespace Trizbort.Avalonia.Services;

/// <summary>Implementations behind File &gt; Open Map from Web (tickets 060, 089) and Smart Save (ticket 059).</summary>
public static class WebAndSmartSaveCommands
{
    /// <summary>Replaced by tests so nothing touches the network.</summary>
    public static IMapDownloader Downloader { get; set; } = new HttpMapDownloader();

    /// <summary>
    ///     The Smart Save outputs that exist today. Ticket 110 adds the PDF exporter (and more image formats) here.
    /// </summary>
    public static IReadOnlyList<ISmartSaveExporter> SmartSaveExporters { get; } = new ISmartSaveExporter[]
    {
        new ImageSmartSaveExporter()
    };

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

/// <summary>Writes the map image next to the map file, in the default image format (PNG for unsupported ones).</summary>
public sealed class ImageSmartSaveExporter : ISmartSaveExporter
{
    public SmartSaveKind Kind => SmartSaveKind.Image;

    public Task<string> ExportAsync(EditorSession session, string mapFileName, CancellationToken token = default)
    {
        var format = ImageFormats.FromSetting(ApplicationSettingsController.AppSettings.DefaultImageType);
        var path = Path.ChangeExtension(mapFileName, ImageFormats.Extension(format));

        using var bitmap = MapCanvasControl.RenderToBitmap(session.Project);
        if (bitmap == null) throw new InvalidOperationException("Cannot export an empty map.");

        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
        {
            ImageEncoder.Encode(bitmap, format, stream);
        }

        ApplicationSettingsController.AppSettings.LastExportImageFileName = path;
        return Task.FromResult(path);
    }
}

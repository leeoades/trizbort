using Avalonia.Controls;
using Trizbort.Export;

namespace Trizbort.Avalonia.Services;

public interface IStorageService
{
    Task<bool> OpenMapAsync(TopLevel? topLevel);

    /// <summary>Loads the map at <paramref name="path"/> (file path or URL) without showing a picker.</summary>
    Task<bool> OpenMapFromPathAsync(string path, TopLevel? topLevel) => Task.FromResult(false);

    Task<bool> SaveMapAsync(TopLevel? topLevel, bool saveAs = false);
    Task<bool> ExportCodeAsync(TopLevel? topLevel, CodeExporter exporter);
    Task<bool> ExportImageAsync(TopLevel? topLevel);
    Task<string?> OpenThemeFileAsync(TopLevel? topLevel);
    Task<string?> SaveThemeFileAsync(TopLevel? topLevel, string suggestedName);
    Task<string?> OpenTranscriptFileAsync(TopLevel? topLevel, string? initialPath = null);
}

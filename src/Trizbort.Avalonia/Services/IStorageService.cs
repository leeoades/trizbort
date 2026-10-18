using System.Threading.Tasks;
using Avalonia.Controls;
using Trizbort.Export;

namespace Trizbort.Avalonia.Services;

public interface IStorageService
{
    Task<bool> OpenMapAsync(TopLevel? topLevel);
    Task<bool> SaveMapAsync(TopLevel? topLevel, bool saveAs = false);
    Task<bool> ExportCodeAsync(TopLevel? topLevel, CodeExporter exporter);
    Task<string?> OpenThemeFileAsync(TopLevel? topLevel);
    Task<string?> SaveThemeFileAsync(TopLevel? topLevel, string suggestedName);
    Task<string?> OpenTranscriptFileAsync(TopLevel? topLevel, string? initialPath = null);
}

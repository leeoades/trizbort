using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Trizbort.Avalonia.Controls;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Export;

namespace Trizbort.Avalonia.Services;

public class StorageService(IDialogService dialogService) : IStorageService
{
    private readonly IDialogService _dialogService = dialogService;

    private static readonly FilePickerFileType TrizbortFileType = new("Trizbort Map Files (*.trizbort)")
    {
        Patterns = ["*.trizbort"]
    };

    private static readonly FilePickerFileType ThemeFileType = new("Trizbort Theme Files (*.trizbort-theme; *.json)")
    {
        Patterns = ["*.trizbort-theme", "*.json"]
    };

    private static readonly FilePickerFileType TranscriptFileType = new("Text and Log Files (*.txt; *.log)")
    {
        Patterns = ["*.txt", "*.log"]
    };

    private static readonly FilePickerFileType PngFileType = new("PNG Image (*.png)")
    {
        Patterns = ["*.png"]
    };

    private static Window? GetWindow(TopLevel? topLevel)
    {
        if (topLevel is Window window) return window;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    public async Task<bool> OpenMapAsync(TopLevel? topLevel)
    {
        if (topLevel == null) return false;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Trizbort Map",
            AllowMultiple = false,
            FileTypeFilter = [TrizbortFileType, FilePickerFileTypes.All]
        });

        if (files.Count == 0) return false;

        var filePath = files[0].Path.LocalPath;
        try
        {
            var project = new Project();
            var loader = new MapLoader(project);
            if (loader.LoadMap(filePath))
            {
                project.FileName = filePath;
                project.IsDirty = false;
                Project.Current = project;
                return true;
            }

            await _dialogService.ShowMessageAsync(
                GetWindow(topLevel),
                "Open Map Failed",
                $"Unable to load map file '{Path.GetFileName(filePath)}'. The file format may be invalid or corrupt.");
            return false;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync(
                GetWindow(topLevel),
                "Open Map Error",
                $"Error opening map file '{Path.GetFileName(filePath)}':\n{ex.Message}");
            return false;
        }
    }

    public async Task<bool> SaveMapAsync(TopLevel? topLevel, bool saveAs = false)
    {
        if (topLevel == null) return false;

        var currentProject = Project.Current;
        var originalFileName = currentProject.FileName;
        var filePath = originalFileName;
        var pickedPath = filePath;

        if (saveAs || string.IsNullOrEmpty(filePath))
        {
            var suggestedName = string.IsNullOrEmpty(currentProject.FileName)
                ? (string.IsNullOrEmpty(currentProject.Title) ? "Untitled" : currentProject.Title)
                : Path.GetFileNameWithoutExtension(currentProject.FileName);

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Trizbort Map",
                DefaultExtension = "trizbort",
                SuggestedFileName = suggestedName,
                FileTypeChoices = [TrizbortFileType, FilePickerFileTypes.All]
            });

            if (file == null) return false;
            filePath = file.Path.LocalPath;
            pickedPath = filePath;
        }

        if (!string.Equals(Path.GetExtension(filePath), ".trizbort", StringComparison.Ordinal))
        {
            filePath = Path.ChangeExtension(filePath, ".trizbort");
        }

        if (!string.Equals(filePath, pickedPath, StringComparison.Ordinal) && File.Exists(filePath))
        {
            var replace = await _dialogService.ShowConfirmationAsync(
                GetWindow(topLevel),
                "Confirm Overwrite",
                $"'{Path.GetFileName(filePath)}' already exists. Replace it?");
            if (!replace) return false;
        }

        try
        {
            currentProject.FileName = filePath;
            if (currentProject.Save())
            {
                return true;
            }

            currentProject.FileName = originalFileName;
            await _dialogService.ShowMessageAsync(
                GetWindow(topLevel),
                "Save Map Failed",
                $"Failed to save map to '{Path.GetFileName(filePath)}'.");
            return false;
        }
        catch (Exception ex)
        {
            currentProject.FileName = originalFileName;
            await _dialogService.ShowMessageAsync(
                GetWindow(topLevel),
                "Save Map Error",
                $"Error saving map to '{Path.GetFileName(filePath)}':\n{ex.Message}");
            return false;
        }
    }

    public async Task<bool> ExportCodeAsync(TopLevel? topLevel, CodeExporter exporter)
    {
        if (topLevel == null || exporter == null) return false;

        using var scopedExporter = exporter;

        ExportLanguage? language = ExportPathMemory.TryGetLanguage(scopedExporter, out var found) ? found : null;
        var remembered = language.HasValue
            ? ExportPathMemory.Get(language.Value)
            : (Folder: string.Empty, FileName: string.Empty, Extension: string.Empty);

        var filters = new List<FilePickerFileType>();
        FilePickerFileType? preselected = null;
        foreach (var filter in scopedExporter.FileDialogFilters)
        {
            var ext = filter.Value.StartsWith('.') ? $"*{filter.Value}" : $"*.{filter.Value}";
            var fileType = new FilePickerFileType(filter.Key)
            {
                Patterns = [ext]
            };
            filters.Add(fileType);
            if (preselected == null && remembered.Extension.Length > 0 &&
                string.Equals(NormaliseExtension(filter.Value), remembered.Extension, StringComparison.OrdinalIgnoreCase))
            {
                preselected = fileType;
            }
        }
        filters.Add(FilePickerFileTypes.All);

        string defaultExtension;
        string suggestedName;
        if (language.HasValue)
        {
            suggestedName = ExportNaming.SuggestedFileName(Project.Current, language.Value);
            defaultExtension = CodeExportService.DefaultExtension(language.Value);
        }
        else
        {
            suggestedName = string.IsNullOrEmpty(Project.Current.Title)
                ? (string.IsNullOrEmpty(Project.Current.FileName) ? "map" : Path.GetFileNameWithoutExtension(Project.Current.FileName))
                : Project.Current.Title;
            defaultExtension = filters.Count > 1 ? NormaliseExtension(scopedExporter.FileDialogFilters[0].Value) : string.Empty;
        }

        IStorageFolder? startFolder = null;
        if (remembered.Folder.Length > 0 && Directory.Exists(remembered.Folder))
        {
            try
            {
                startFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(remembered.Folder);
            }
            catch
            {
                // ignore an unusable remembered folder
            }
        }

        var options = new FilePickerSaveOptions
        {
            Title = scopedExporter.FileDialogTitle,
            SuggestedFileName = suggestedName,
            DefaultExtension = defaultExtension.TrimStart('.'),
            SuggestedStartLocation = startFolder,
            FileTypeChoices = filters
        };
        if (preselected != null) options.SuggestedFileType = preselected;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);
        if (file == null) return false;

        return await CompleteCodeExportAsync(
            GetWindow(topLevel), scopedExporter, language, file.Path.LocalPath,
            remembered.Extension.Length > 0 ? remembered.Extension : defaultExtension);
    }

    private static string NormaliseExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return string.Empty;
        return extension.StartsWith('.') ? extension : "." + extension;
    }

    /// <summary>
    ///   Second half of a code export once a path has been picked: appends a missing extension, asks before
    ///   overwriting, writes the file and remembers the path. Returns false when declined or failed.
    /// </summary>
    public async Task<bool> CompleteCodeExportAsync(
        Window? owner, CodeExporter exporter, ExportLanguage? language, string pickedPath, string defaultExtension)
    {
        var exportPath = pickedPath;
        if (string.IsNullOrEmpty(Path.GetExtension(exportPath)) && !string.IsNullOrEmpty(defaultExtension))
        {
            exportPath += NormaliseExtension(defaultExtension);
        }

        try
        {
            // The picker may pre-create an empty placeholder file for an Inform 7 ".inform" bundle; the
            // exporter needs a directory there, so remove a 0-byte placeholder.
            if (File.Exists(exportPath) &&
                string.Equals(Path.GetExtension(exportPath), ".inform", StringComparison.OrdinalIgnoreCase) &&
                new FileInfo(exportPath).Length == 0)
            {
                File.Delete(exportPath);
            }
            else if (File.Exists(exportPath) || Directory.Exists(exportPath))
            {
                var replace = await _dialogService.ShowConfirmationAsync(
                    owner,
                    "Confirm Overwrite",
                    $"'{Path.GetFileName(exportPath)}' already exists. Replace it?");
                if (!replace) return false;
            }

            exporter.Export(exportPath);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync(
                owner,
                "Export Error",
                $"Error exporting code to '{Path.GetFileName(exportPath)}':\n{ex.Message}");
            return false;
        }

        if (language.HasValue)
        {
            ExportPathMemory.Remember(language.Value, exportPath);
            try
            {
                ApplicationSettingsController.SaveSettings();
            }
            catch
            {
                // remembering the path is best effort
            }
        }

        return true;
    }

    public async Task<bool> ExportImageAsync(TopLevel? topLevel)
    {
        if (topLevel == null) return false;

        var currentProject = Project.Current;
        if (currentProject == null || currentProject.Elements.Count == 0)
        {
            await _dialogService.ShowMessageAsync(
                GetWindow(topLevel),
                "Export Image",
                "Cannot export an empty map.");
            return false;
        }

        var suggestedName = string.IsNullOrEmpty(currentProject.Title)
            ? (string.IsNullOrEmpty(currentProject.FileName) ? "map" : Path.GetFileNameWithoutExtension(currentProject.FileName))
            : currentProject.Title;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Image",
            DefaultExtension = "png",
            SuggestedFileName = suggestedName,
            FileTypeChoices = [PngFileType, FilePickerFileTypes.All]
        });

        if (file == null) return false;

        var exportPath = file.Path.LocalPath;
        try
        {
            using var bitmap = MapCanvasControl.RenderToBitmap(currentProject);
            if (bitmap == null)
            {
                await _dialogService.ShowMessageAsync(
                    GetWindow(topLevel),
                    "Export Image",
                    "Cannot export an empty map.");
                return false;
            }

            await using (var stream = await file.OpenWriteAsync())
            {
                bitmap.Save(stream);
            }

            ApplicationSettingsController.AppSettings.LastExportImageFileName = exportPath;
            return true;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync(
                GetWindow(topLevel),
                "Export Error",
                $"Error exporting image to '{Path.GetFileName(exportPath)}':\n{ex.Message}");
            return false;
        }
    }

    public async Task<string?> OpenThemeFileAsync(TopLevel? topLevel)
    {
        if (topLevel == null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Trizbort Theme",
            AllowMultiple = false,
            FileTypeFilter = [ThemeFileType, FilePickerFileTypes.All]
        });

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    public async Task<string?> SaveThemeFileAsync(TopLevel? topLevel, string suggestedName)
    {
        if (topLevel == null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Trizbort Theme",
            DefaultExtension = "trizbort-theme",
            SuggestedFileName = suggestedName,
            FileTypeChoices = [ThemeFileType, FilePickerFileTypes.All]
        });

        return file?.Path.LocalPath;
    }

    public async Task<string?> OpenTranscriptFileAsync(TopLevel? topLevel, string? initialPath = null)
    {
        if (topLevel == null) return null;

        IStorageFolder? suggestedFolder = null;
        if (!string.IsNullOrEmpty(initialPath))
        {
            try
            {
                var dir = Path.GetDirectoryName(initialPath);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    suggestedFolder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(dir);
                }
            }
            catch
            {
                // ignore invalid path
            }
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Transcript",
            AllowMultiple = false,
            SuggestedStartLocation = suggestedFolder,
            FileTypeFilter = [TranscriptFileType, FilePickerFileTypes.All]
        });

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }
}

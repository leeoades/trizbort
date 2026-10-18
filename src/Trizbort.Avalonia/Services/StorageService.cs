using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Trizbort.Domain.Application;
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

        var filters = new List<FilePickerFileType>();
        foreach (var filter in scopedExporter.FileDialogFilters)
        {
            var ext = filter.Value.StartsWith('.') ? $"*{filter.Value}" : $"*.{filter.Value}";
            filters.Add(new FilePickerFileType(filter.Key)
            {
                Patterns = [ext]
            });
        }
        filters.Add(FilePickerFileTypes.All);

        var suggestedName = string.IsNullOrEmpty(Project.Current.Title)
            ? (string.IsNullOrEmpty(Project.Current.FileName) ? "map" : Path.GetFileNameWithoutExtension(Project.Current.FileName))
            : Project.Current.Title;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = scopedExporter.FileDialogTitle,
            SuggestedFileName = suggestedName,
            FileTypeChoices = filters
        });

        if (file == null) return false;

        var exportPath = file.Path.LocalPath;
        try
        {
            scopedExporter.Export(exportPath);
            return true;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync(
                GetWindow(topLevel),
                "Export Error",
                $"Error exporting code to '{Path.GetFileName(exportPath)}':\n{ex.Message}");
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

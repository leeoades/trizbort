using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Trizbort.Domain.Application;
using Trizbort.Export;

namespace Trizbort.Avalonia.Services;

public class StorageService : IStorageService
{
    private static readonly FilePickerFileType TrizbortFileType = new("Trizbort Map Files (*.trizbort)")
    {
        Patterns = ["*.trizbort"]
    };

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
        var project = new Project();
        var loader = new MapLoader(project);
        if (loader.LoadMap(filePath))
        {
            project.FileName = filePath;
            project.IsDirty = false;
            Project.Current = project;
            return true;
        }

        return false;
    }

    public async Task<bool> SaveMapAsync(TopLevel? topLevel, bool saveAs = false)
    {
        if (topLevel == null) return false;

        var currentProject = Project.Current;
        var filePath = currentProject.FileName;

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
        }

        var saver = new MapSaver(currentProject);
        if (saver.SaveMap(filePath))
        {
            currentProject.FileName = filePath;
            currentProject.IsDirty = false;
            return true;
        }

        return false;
    }

    public async Task<bool> ExportCodeAsync(TopLevel? topLevel, CodeExporter exporter)
    {
        if (topLevel == null || exporter == null) return false;

        var filters = new List<FilePickerFileType>();
        foreach (var filter in exporter.FileDialogFilters)
        {
            filters.Add(new FilePickerFileType(filter.Key)
            {
                Patterns = [filter.Value]
            });
        }
        if (filters.Count == 0)
        {
            filters.Add(FilePickerFileTypes.TextPlain);
        }

        var suggestedName = string.IsNullOrEmpty(Project.Current.Title)
            ? (string.IsNullOrEmpty(Project.Current.FileName) ? "map" : Path.GetFileNameWithoutExtension(Project.Current.FileName))
            : Project.Current.Title;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = exporter.FileDialogTitle,
            SuggestedFileName = suggestedName,
            FileTypeChoices = filters
        });

        if (file == null) return false;

        var exportPath = file.Path.LocalPath;
        exporter.Export(exportPath);
        return true;
    }
}

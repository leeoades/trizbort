using Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;

namespace Trizbort.Avalonia;

/// <summary>
///   Actions run once after the main window opens, driven by the command line.
///   Later tickets (automap, export, save flags) add further actions here.
/// </summary>
public static class StartupActions
{
    public static async Task RunAsync(
        MainWindowViewModel vm,
        CommandLineOptions options,
        IStorageService storage,
        IDialogService dialogs,
        Window? owner)
    {
        if (options == null) return;
        await OpenNamedFileAsync(vm, options, storage, dialogs, owner);
    }

    private static async Task OpenNamedFileAsync(
        MainWindowViewModel vm,
        CommandLineOptions options,
        IStorageService storage,
        IDialogService dialogs,
        Window? owner)
    {
        var name = options.FileName;
        if (string.IsNullOrWhiteSpace(name)) return;

        var isUrl = Uri.TryCreate(name, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        var path = isUrl ? name : Path.GetFullPath(name);

        if (!isUrl && !File.Exists(path))
        {
            await dialogs.ShowMessageAsync(owner, "Open Map", $"File not found: {path}");
            return;
        }

        if (await storage.OpenMapFromPathAsync(path, owner))
        {
            vm.UpdateState();
            vm.CommandService.ResetZoom();
            vm.StatusText = $"Opened map: {Path.GetFileName(path)}";
        }
    }
}

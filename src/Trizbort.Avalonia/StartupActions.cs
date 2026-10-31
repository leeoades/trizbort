using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Trizbort.Automap;
using Trizbort.Editor;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Export;

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
        Window? owner,
        ApplicationSettings? settings = null)
    {
        if (options == null) return;
        // the debug commands and rendering toggles only work when started with --debug
        var session = vm.CommandService?.Session;
        if (session != null) session.DebugEnabled = options.Debug;
        if (!string.IsNullOrWhiteSpace(options.FileName))
        {
            // a file named on the command line always wins over "load last project"
            await OpenNamedFileAsync(vm, options, storage, dialogs, owner);
        }
        else
        {
            await OpenLastProjectAsync(vm, options, storage, settings ?? ApplicationSettingsController.AppSettings, owner);
        }

        await RunCommandLineActionsAsync(
            vm, options, Console.Out, Console.Error, settings ?? ApplicationSettingsController.AppSettings);
    }

    /// <summary>
    ///     Closes the application for <c>--exit</c>. Replaced by tests; the default closes the main window.
    /// </summary>
    public static Action ExitApplication { get; set; } = CloseMainWindow;

    private static void CloseMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow?.Close();
    }

    /// <summary>
    ///     The command-line steps that follow loading the map (tickets 107, 105, 110), in the legacy-compatible order:
    ///     automap, code export flags, quick save, smart save, exit. Each step is isolated: a failure goes to
    ///     <paramref name="error" />, sets <see cref="App.ExitCode" /> to 1 and does not stop the later steps, and
    ///     <c>--exit</c> always runs. <c>--name</c> is accepted for compatibility and ignored (it does nothing in the
    ///     reference either).
    /// </summary>
    public static async Task RunCommandLineActionsAsync(
        MainWindowViewModel vm,
        CommandLineOptions options,
        TextWriter output,
        TextWriter error,
        ApplicationSettings settings)
    {
        if (options == null) return;

        if (!string.IsNullOrWhiteSpace(options.Transcript))
        {
            await GuardAsync("automap", error, () => RunAutomapAsync(vm, options.Transcript, settings, output, error));
        }

        await GuardAsync("export", error, () =>
        {
            RunExports(options, output, error);
            return Task.CompletedTask;
        });

        if (!string.IsNullOrWhiteSpace(options.QuickSave))
        {
            await GuardAsync("quicksave", error, () =>
            {
                var problem = HeadlessSave.QuickSave(Project.Current, options.QuickSave);
                if (problem == null)
                {
                    vm.CommandService?.Session.MarkSaved();
                    output.WriteLine($"Saved map to {Project.Current.FileName}");
                }
                else
                {
                    Fail(error, $"Could not quick save to {options.QuickSave}: {problem}");
                }

                return Task.CompletedTask;
            });
        }

        if (options.SmartSave)
        {
            await GuardAsync("smartsave", error, () => RunSmartSaveAsync(settings, output, error));
        }

        if (options.Exit)
        {
            await GuardAsync("exit", error, () =>
            {
                // --exit never asks about unsaved changes
                if (Project.Current != null) Project.Current.IsDirty = false;
                vm.CommandService?.Session.MarkSaved();
                ExitApplication();
                return Task.CompletedTask;
            });
        }
    }

    private static async Task RunAutomapAsync(
        MainWindowViewModel vm, string transcript, ApplicationSettings settings, TextWriter output, TextWriter error)
    {
        var result = await HeadlessAutomap.RunAsync(
            Project.Current, transcript, settings.Automap, NonInteractiveEditorPrompts.Instance);
        // the map changed behind the undo stack's back: start a fresh history and redraw
        vm.CommandService?.Session.Undo.Clear();
        vm.CommandService?.Session.MarkSaved();
        vm.UpdateState();
        vm.CommandService?.InvalidateCanvas();
        if (result.Succeeded) output.WriteLine($"Automapped {transcript}");
        else Fail(error, $"Could not automap {transcript}: {result.Error}");
    }

    private static async Task RunSmartSaveAsync(ApplicationSettings settings, TextWriter output, TextWriter error)
    {
        var plan = SmartSaveService.Plan(Project.Current, settings);
        if (!plan.CanRun)
        {
            // nothing enabled is a settings choice, not a failure; an unsaved map cannot be smart saved silently
            if (plan.NeedsSave) Fail(error, "Could not smart save: the map has not been saved. " + plan.Reason);
            else output.WriteLine(plan.Reason);
            return;
        }

        var result = await SmartSaveService.RunAsync(
            Project.Current, plan, WebAndSmartSaveCommands.ImageWriter, WebAndSmartSaveCommands.PdfWriter);
        foreach (var file in result.Files)
        {
            if (file.Succeeded) output.WriteLine($"Smart saved {file.Path}");
            else Fail(error, $"Could not smart save {file.Path}: {file.Error}");
        }
    }

    private static async Task GuardAsync(string step, TextWriter error, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Fail(error, $"Command line step '{step}' failed: {ex.Message}");
        }
    }

    private static void Fail(TextWriter error, string message)
    {
        error.WriteLine(message);
        App.ExitCode = 1;
    }

    /// <summary>
    ///   Opens a map the OS asked us to open (prompts about unsaved changes first). Safe to route through
    ///   <see cref="FileActivationHandler" />, which queues requests until the window is shown.
    /// </summary>
    public static Task OpenAsync(MainWindowViewModel vm, string path)
    {
        return vm.OpenRecentMapAsync(path);
    }

    /// <summary>
    ///   Runs the code export flags against the current map (ticket 105). Writes "Exported ..." lines to
    ///   <paramref name="output" />, errors to <paramref name="error" />, and sets <see cref="App.ExitCode" /> to 1 on any failure.
    /// </summary>
    public static void RunExports(CommandLineOptions options, TextWriter output, TextWriter error)
    {
        if (!HeadlessExport.HasExportFlags(options)) return;
        foreach (var result in HeadlessExport.Run(options, Project.Current))
        {
            if (result.Error == null)
            {
                output.WriteLine($"Exported {result.Flag} to {result.File}");
            }
            else
            {
                error.WriteLine($"Could not export {result.Flag} to {result.File}: {result.Error}");
                App.ExitCode = 1;
            }
        }
    }

    private static async Task OpenLastProjectAsync(
        MainWindowViewModel vm,
        CommandLineOptions options,
        IStorageService storage,
        ApplicationSettings settings,
        Window? owner)
    {
        if (!options.LoadLastProject && !settings.LoadLastProjectOnStart) return;

        var path = settings.LastProjectFileName;
        if (string.IsNullOrWhiteSpace(path)) return;
        if (!RecentFiles.IsUrl(path) && !File.Exists(path)) return;

        try
        {
            if (await storage.OpenMapFromPathAsync(path, owner))
            {
                vm.UpdateState();
                vm.CommandService.ResetZoom();
                vm.StatusText = $"Opened map: {Path.GetFileName(path)}";
            }
            else
            {
                vm.StatusText = $"Could not reopen {path}";
            }
        }
        catch (Exception)
        {
            vm.StatusText = $"Could not reopen {path}";
        }
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

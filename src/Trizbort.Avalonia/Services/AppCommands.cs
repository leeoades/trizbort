using System;
using System.Threading.Tasks;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Enums;
using Trizbort.Editor;
using Trizbort.Editor.Commands;
using Trizbort.Editor.Input;
using Trizbort.Export;

namespace Trizbort.Avalonia.Services;

/// <summary>
///   Registers the application-level commands (files, dialogs, automap) in the command catalogue. They call the
///   existing <see cref="MainWindowViewModel" /> commands so no logic is duplicated.
/// </summary>
public static class AppCommands
{
    /// <summary>The default catalogue plus the app-level commands. <paramref name="vm" /> may be null (commands then do nothing).</summary>
    public static CommandCatalogue CreateCatalogue(MainWindowViewModel? vm)
    {
        var catalogue = CommandCatalogue.CreateDefault();
        Register(catalogue, vm);
        return catalogue;
    }

    /// <summary>
    ///   Adds the app-level commands. <paramref name="vm" /> may be null, for hosts (such as a standalone canvas) that
    ///   only need the command definitions and supply their own execution.
    /// </summary>
    public static void Register(CommandCatalogue c, MainWindowViewModel? vm)
    {
        var primary = ModifierKeys.Primary;

        Func<EditorSession, Task> Run(Func<Task> action) => _ => vm == null ? Task.CompletedTask : action();

        c.Register(new EditorCommand
        {
            Id = "file.new",
            Title = "_New Map",
            MenuPath = "File",
            Group = "1-file",
            Gestures = new[] { new KeyGesture(InputKey.N, primary) },
            ShowInToolbar = true, ToolbarOrder = 10, ToolbarGroup = "1-file", IconKey = "IconNew",
            ExecuteAsyncFunc = Run(() => vm!.NewMapAsync())
        });
        c.Register(new EditorCommand
        {
            Id = "file.open",
            Title = "_Open Map...",
            MenuPath = "File",
            Group = "1-file",
            Gestures = new[] { new KeyGesture(InputKey.O, primary) },
            ShowInToolbar = true, ToolbarOrder = 20, ToolbarGroup = "1-file", IconKey = "IconOpen",
            ExecuteAsyncFunc = Run(() => vm!.OpenMapAsync())
        });
        c.Register(new EditorCommand
        {
            Id = "file.save",
            Title = "_Save Map",
            MenuPath = "File",
            Group = "1-file",
            Gestures = new[] { new KeyGesture(InputKey.S, primary) },
            ShowInToolbar = true, ToolbarOrder = 30, ToolbarGroup = "1-file", IconKey = "IconSave",
            ExecuteAsyncFunc = Run(() => vm!.SaveMapAsync())
        });
        c.Register(new EditorCommand
        {
            Id = "file.saveAs",
            Title = "Save Map _As...",
            MenuPath = "File",
            Group = "1-file",
            Gestures = new[] { new KeyGesture(InputKey.S, primary | ModifierKeys.Shift) },
            ExecuteAsyncFunc = Run(() => vm!.SaveMapAsAsync())
        });

        // Group is "<group in File menu>/<group in Export submenu>".
        RegisterExport(c, "file.export.png", "_Image...", "2-export/1-image", Run(() => vm!.ExportImageAsync()));
        ExportCommands.Register(
            c,
            language => vm == null ? Task.CompletedTask : RunFileExportAsync(vm, language),
            OperatingSystem.IsMacOS(),
            text =>
            {
                if (vm != null) vm.StatusText = text;
            });

        c.Register(new EditorCommand
        {
            Id = "file.exit",
            Title = "E_xit",
            MenuPath = "File",
            Group = "3-exit",
            // Windows: Alt+F4 is handled by the OS (shown only). Linux: Primary+Q. macOS: Cmd+Q (Primary).
            Gestures = OperatingSystem.IsWindows()
                ? new[] { new KeyGesture(InputKey.F4, ModifierKeys.Alt) }
                : new[] { new KeyGesture(InputKey.Q, primary) },
            GestureDisplayOnly = OperatingSystem.IsWindows(),
            ExecuteAsyncFunc = _ =>
            {
                vm?.Exit();
                return Task.CompletedTask;
            }
        });

        c.Register(new EditorCommand
        {
            Id = "edit.mapSettings",
            Title = "_Map Settings...",
            MenuPath = "Edit",
            Group = "4-settings",
            Gestures = new[] { new KeyGesture(InputKey.OemComma, primary | ModifierKeys.Shift) },
            ContextTargets = ContextTargets.Any,
            ContextOrder = 50, ContextGroup = "5-settings",
            ShowInToolbar = true, ToolbarOrder = 120, ToolbarGroup = "4-tools", IconKey = "IconSettings",
            ExecuteAsyncFunc = Run(() => vm!.ShowMapSettingsAsync())
        });
        c.Register(new EditorCommand
        {
            Id = "edit.mapTheme",
            Title = "Map _Theme...",
            MenuPath = "Edit",
            Group = "4-settings",
            ContextTargets = ContextTargets.Any,
            ContextOrder = 51, ContextGroup = "5-settings",
            ExecuteAsyncFunc = Run(() => vm!.ShowMapThemeAsync())
        });
        c.Register(new EditorCommand
        {
            Id = "edit.appSettings",
            Title = "_Application Settings...",
            MenuPath = "Edit",
            Group = "4-settings",
            Gestures = new[] { new KeyGesture(InputKey.OemComma, primary) },
            ContextTargets = ContextTargets.Any,
            ContextOrder = 52, ContextGroup = "5-settings",
            ExecuteAsyncFunc = Run(() => vm!.ShowAppSettingsAsync())
        });

        c.Register(new EditorCommand
        {
            Id = "project.toggleAutomap",
            Title = "Toggle _Automap",
            MenuPath = "Project",
            Group = "2-automap",
            ShowInToolbar = true, ToolbarOrder = 110, ToolbarGroup = "4-tools", IconKey = "IconAutomap",
            ExecuteAsyncFunc = Run(() => vm!.ToggleAutomapAsync())
        });

        c.Register(new EditorCommand
        {
            Id = "help.online",
            Title = "Online _Help",
            MenuPath = "Help",
            Group = "1-help",
            Gestures = new[] { new KeyGesture(InputKey.F1, ModifierKeys.None) },
            ExecuteAsyncFunc = _ => OpenOnlineHelpAsync()
        });
        c.Register(new EditorCommand
        {
            Id = "help.about",
            Title = "_About Trizbort...",
            MenuPath = "Help",
            Group = "2-about",
            ExecuteAsyncFunc = Run(() => vm!.ShowAboutAsync())
        });
    }

    /// <summary>Where Online Help opens.</summary>
    public const string OnlineHelpUrl = "https://github.com/leeoades/trizbort/blob/master/Docs/index.md";

    private static async Task OpenOnlineHelpAsync()
    {
        try
        {
            var lifetime = global::Avalonia.Application.Current?.ApplicationLifetime
                as global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var launcher = lifetime?.MainWindow?.Launcher;
            if (launcher != null)
            {
                await launcher.LaunchUriAsync(new Uri(OnlineHelpUrl));
            }
        }
        catch (Exception)
        {
            // opening a browser must never crash the app
        }
    }

    private static Task RunFileExportAsync(MainWindowViewModel vm, ExportLanguage language)
    {
        return language switch
        {
            ExportLanguage.Alan => vm.ExportAlanAsync(),
            ExportLanguage.Adventuron => vm.ExportAdventuronAsync(),
            ExportLanguage.Hugo => vm.ExportHugoAsync(),
            ExportLanguage.Inform6 => vm.ExportInform6Async(),
            ExportLanguage.Inform7 => vm.ExportInform7Async(),
            ExportLanguage.Tads => vm.ExportTadsAsync(),
            ExportLanguage.Quest => vm.ExportQuestAsync(),
            ExportLanguage.Zil => vm.ExportZilAsync(),
            _ => Task.CompletedTask
        };
    }

    private static void RegisterExport(CommandCatalogue c, string id, string title, string group, Func<EditorSession, Task> execute)
    {
        c.Register(new EditorCommand
        {
            Id = id,
            Title = title,
            MenuPath = "File/Export",
            Group = group,
            ExecuteAsyncFunc = execute
        });
    }
}

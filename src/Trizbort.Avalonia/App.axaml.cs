using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Trizbort.Automap;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views;
using AutomapEngine = Trizbort.Automap.Automap;

namespace Trizbort.Avalonia;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = ConfigureServices();
        Services = services;

        var dialogService = services.GetRequiredService<IDialogService>();
        var prompts = WireAutomapPrompts(() => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindowViewModel = services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };
            var session = mainWindowViewModel.CommandService.Session;
            session.Prompts = prompts;
            session.DebugEnabled = AppOptions.Debug;
            session.Clipboard = new AvaloniaClipboard(() => desktop.MainWindow);
            FileWatcherWiring.Wire(dialogService, () => desktop.MainWindow, mainWindowViewModel.UpdateState,
                mainWindowViewModel.CommandService.Session);
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static Trizbort.Editor.IEditorPrompts WireAutomapPrompts(Func<Window?> getOwner)
    {
        var prompts = new AvaloniaEditorPrompts(getOwner, App.Services?.GetService<IDialogService>());
        AutomapEngine.Instance.Prompts = prompts;
        return prompts;
    }

    public static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Application services
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IStorageService, StorageService>();
        services.AddSingleton<ICommandService, CommandService>();
        services.AddSingleton<IRecentFilesService, RecentFilesService>();
        services.AddSingleton<IAutomapCanvas>(
            sp => new AvaloniaAutomapCanvas(session: sp.GetRequiredService<ICommandService>().Session));

        // Root ViewModels
        services.AddSingleton<MainWindowViewModel>();

        // Menus, toolbar, context menu and key bindings are generated from this catalogue
        services.AddSingleton<Trizbort.Editor.Commands.CommandCatalogue>(
            sp => AppCommands.CreateCatalogue(sp.GetRequiredService<MainWindowViewModel>()));

        // Scoped Dialog ViewModels (resolved in child IServiceScope)
        services.AddTransient<RoomPropertiesViewModel>();
        services.AddTransient<ConnectionPropertiesViewModel>();
        services.AddTransient<MapLabelPropertiesViewModel>();
        services.AddTransient<MapSettingsViewModel>();
        services.AddTransient<MapThemeViewModel>();
        services.AddTransient<AppSettingsViewModel>();
        services.AddTransient<AutomapSettingsViewModel>();

        return services.BuildServiceProvider();
    }
}

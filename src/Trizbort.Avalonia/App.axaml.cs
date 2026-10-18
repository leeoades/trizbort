using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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
        WireAutomapErrorReporter(dialogService, () => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindowViewModel = services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static Action<string, string> WireAutomapErrorReporter(IDialogService dialogService, Func<Window?> getOwner)
    {
        // Wire Automap.ErrorReporter to IDialogService on the UI dispatcher.
        // Same-direction room conflicts retain default KeepRoom1 (known parity gap: interactive chooser not in Avalonia).
        Action<string, string> reporter = (message, title) =>
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                var owner = getOwner();
                _ = dialogService.ShowMessageAsync(owner, title, message);
            }
            else
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    var owner = getOwner();
                    await dialogService.ShowMessageAsync(owner, title, message);
                });
            }
        };
        AutomapEngine.ErrorReporter = reporter;
        return reporter;
    }

    public static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Application services
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IStorageService, StorageService>();
        services.AddSingleton<ICommandService, CommandService>();
        services.AddSingleton<IAutomapCanvas>(_ => new AvaloniaAutomapCanvas());

        // Root ViewModels
        services.AddSingleton<MainWindowViewModel>();

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

using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Trizbort.Automap;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views;
using Trizbort.Domain.Application;

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

        return services.BuildServiceProvider();
    }
}

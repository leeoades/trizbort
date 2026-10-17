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

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Core domain singletons
        services.AddSingleton(p => Project.Current);
        services.AddSingleton<IAutomapCanvas, AvaloniaAutomapCanvas>();

        // Application services
        services.AddSingleton<ICommandService, CommandService>();
        services.AddSingleton<IStorageService, StorageService>();
        services.AddSingleton<IDialogService, DialogService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }
}

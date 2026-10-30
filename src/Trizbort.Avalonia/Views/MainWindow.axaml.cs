using System;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;

namespace Trizbort.Avalonia.Views;

public partial class MainWindow : Window
{
    private bool _isClosingConfirmed;

    public MainWindow()
    {
        InitializeComponent();
        Closing += OnWindowClosing;
        Opened += OnWindowOpened;
    }

    private bool _startupActionsRun;

    private async void OnWindowOpened(object? sender, EventArgs e)
    {
        if (_startupActionsRun) return;
        _startupActionsRun = true;

        var services = App.Services;
        if (services == null || DataContext is not MainWindowViewModel vm) return;

        try
        {
            await StartupActions.RunAsync(
                vm,
                AppOptions.Current,
                services.GetRequiredService<IStorageService>(),
                services.GetRequiredService<IDialogService>(),
                this);
        }
        catch (Exception)
        {
            // startup actions must never crash the app
        }
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isClosingConfirmed) return;

        if (DataContext is MainWindowViewModel vm)
        {
            if (Project.Current.IsDirty)
            {
                e.Cancel = true;
                var canClose = await vm.CanLoseProjectAsync();
                if (canClose)
                {
                    if (vm.IsAutomapActive)
                    {
                        vm.StopAutomap();
                    }
                    SaveSettingsOnExit();
                    _isClosingConfirmed = true;
                    Close();
                }
            }
            else
            {
                if (vm.IsAutomapActive)
                {
                    vm.StopAutomap();
                }
                SaveSettingsOnExit();
            }
        }
    }

    private static void SaveSettingsOnExit()
    {
        try
        {
            ApplicationSettingsController.SaveSettings();
        }
        catch (Exception)
        {
            // never block closing because settings could not be written
        }
    }
}

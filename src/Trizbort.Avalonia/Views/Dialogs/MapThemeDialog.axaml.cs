using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Setup;

namespace Trizbort.Avalonia.Views.Dialogs;

public partial class MapThemeDialog : Window
{
    public MapThemeDialog()
    {
        InitializeComponent();
    }

    private async void OnImportThemeClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MapThemeViewModel vm) return;

        var storageService = App.Services?.GetService<IStorageService>();
        if (storageService == null) return;

        var path = await storageService.OpenThemeFileAsync(this);
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                var theme = MapTheme.Load(path);
                vm.AddCustomTheme(theme);
            }
            catch (Exception ex)
            {
                var dialogService = App.Services?.GetService<IDialogService>();
                if (dialogService != null)
                {
                    await dialogService.ShowMessageAsync(this, "Import Theme Error", $"Failed to import theme:\n{ex.Message}");
                }
            }
        }
    }

    private async void OnExportThemeClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MapThemeViewModel vm || vm.SelectedTheme == null) return;

        var storageService = App.Services?.GetService<IStorageService>();
        if (storageService == null) return;

        var path = await storageService.SaveThemeFileAsync(this, vm.SelectedTheme.Name);
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                vm.SelectedTheme.Save(path);
            }
            catch (Exception ex)
            {
                var dialogService = App.Services?.GetService<IDialogService>();
                if (dialogService != null)
                {
                    await dialogService.ShowMessageAsync(this, "Export Theme Error", $"Failed to export theme:\n{ex.Message}");
                }
            }
        }
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}

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
        DialogFocus.Prepare(this);
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MapThemeViewModel vm)
            {
                WarningText.Text = MapThemeViewModel.ApplyWarning(vm.HasIndividualStyles);
            }
        };
    }

    /// <summary>Applies an inferred room style to the open map as one undo step (set by the dialog service).</summary>
    public Action<RoomStyleInference>? ApplyInference { get; set; }

    private async void OnExportCurrentThemeClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MapThemeViewModel vm) return;

        var storageService = App.Services?.GetService<IStorageService>();
        if (storageService == null) return;

        var path = await storageService.SaveThemeFileAsync(this, "My theme");
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            vm.ExportCurrentTheme(path);
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

    private async void OnInferStyleClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MapThemeViewModel vm) return;

        var dialogService = App.Services?.GetService<IDialogService>();
        if (dialogService == null) return;

        var inference = vm.AnalyzeRoomStyles();
        if (!inference.HasChanges)
        {
            await dialogService.ShowMessageAsync(this, "Infer default room style", MapThemeViewModel.NothingToInferText);
            return;
        }

        var confirmed = await dialogService.ShowConfirmationAsync(
            this, "Infer default room style", MapThemeViewModel.InferenceSummary(inference));
        if (!confirmed) return;

        ApplyInference?.Invoke(inference);
        vm.Initialize();
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

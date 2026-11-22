using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Trizbort.Avalonia.ViewModels;

namespace Trizbort.Avalonia.Views.Dialogs;

public partial class AppSettingsDialog : Window
{
    public AppSettingsDialog()
    {
        InitializeComponent();
        DialogFocus.Prepare(this);
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    /// <summary>
    ///   Choosing Your own before you've loaded a picture asks for one; if you don't pick one, the token you had stays.
    /// </summary>
    private async void OnHereTokenChosen(object? sender, SelectionChangedEventArgs e)
    {
        if (e.RemovedItems.Count == 0 || DataContext is not AppSettingsViewModel viewModel || !viewModel.UsesOwnToken ||
            !string.IsNullOrWhiteSpace(viewModel.HereTokenFile)) return;
        var before = e.RemovedItems.OfType<HereTokenTile>().FirstOrDefault();
        await LoadOwnTokenAsync(viewModel);
        if (string.IsNullOrWhiteSpace(viewModel.HereTokenFile) && before != null) viewModel.SelectedHereToken = before;
    }

    /// <summary>Your own token: a transparent PNG, or an SVG.</summary>
    private async void OnBrowseTokenClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AppSettingsViewModel viewModel) await LoadOwnTokenAsync(viewModel);
    }

    private async Task LoadOwnTokenAsync(AppSettingsViewModel viewModel)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Your Token",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Pictures (PNG, SVG)") { Patterns = new[] { "*.png", "*.svg" } }
            }
        });
        if (files.Count > 0 && files[0].TryGetLocalPath() is { } path) viewModel.UseOwnToken(path);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}

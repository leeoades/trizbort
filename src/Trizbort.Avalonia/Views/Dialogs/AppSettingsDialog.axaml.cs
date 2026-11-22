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

    /// <summary>Your own token: a transparent PNG, or an SVG.</summary>
    private async void OnBrowseTokenClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not AppSettingsViewModel viewModel) return;
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

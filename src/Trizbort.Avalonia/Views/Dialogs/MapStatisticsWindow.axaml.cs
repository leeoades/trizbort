using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using Trizbort.Avalonia.Services;

namespace Trizbort.Avalonia.Views.Dialogs;

/// <summary>Read-only monospaced report window (ticket 087) with Copy, Save as... and Close.</summary>
public partial class MapStatisticsWindow : Window
{
    public MapStatisticsWindow()
    {
        InitializeComponent();
        DialogFocus.Prepare(this);
        CopyAction = CopyToClipboardAsync;
    }

    /// <summary>The report text shown.</summary>
    public string ReportText
    {
        get => ReportBox.Text ?? string.Empty;
        set => ReportBox.Text = value;
    }

    /// <summary>Puts text on the clipboard. Replaceable so tests need no real clipboard.</summary>
    public Func<string, Task> CopyAction { get; set; }

    private async Task CopyToClipboardAsync(string text)
    {
        var clipboard = GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(text);
        }
    }

    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        await CopyAction(ReportText);
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Map Statistics",
            DefaultExtension = "txt",
            SuggestedFileName = "map-statistics.txt",
            ShowOverwritePrompt = true,
            FileTypeChoices = [FilePickerFileTypes.TextPlain, FilePickerFileTypes.All]
        });
        if (file == null)
        {
            return;
        }

        var dialogService = App.Services?.GetService<IDialogService>();
        if (!await OverwriteGuard.ConfirmAsync(dialogService, this, file.Path.LocalPath))
        {
            return;
        }

        await using var stream = await file.OpenWriteAsync();
        stream.SetLength(0);
        var bytes = new UTF8Encoding(false).GetBytes(ReportText);
        await stream.WriteAsync(bytes);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}

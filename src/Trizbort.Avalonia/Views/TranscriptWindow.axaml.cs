using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Editor.Commands;

namespace Trizbort.Avalonia.Views;

/// <summary>
///   The whole transcript automap has read so far, styled like the game window: a floating helper beside the map,
///   not a dialog. It follows the end of the transcript as automap plays on, unless some text is selected. Keys it
///   doesn't need for reading and copying (undo, F10 for the next move...) go to the map, as if it had the focus.
/// </summary>
public partial class TranscriptWindow : Window
{
    public TranscriptWindow()
    {
        InitializeComponent();
        TranscriptBox.PropertyChanged += (_, e) =>
        {
            if (e.Property != TextBox.TextProperty) return;
            if (TranscriptBox.SelectionStart != TranscriptBox.SelectionEnd) return;
            Dispatcher.UIThread.Post(() => TranscriptBox.CaretIndex = TranscriptBox.Text?.Length ?? 0,
                DispatcherPriority.Background);
        };
        Opened += (_, _) => TranscriptBox.CaretIndex = TranscriptBox.Text?.Length ?? 0;
        AddHandler(KeyDownEvent, OnKeyForTheMap, RoutingStrategies.Tunnel);
    }

    /// <summary>Keys the transcript itself uses: moving about, selecting, copying.</summary>
    private static bool IsReadingKey(KeyEventArgs e)
    {
        var primary = (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0;
        switch (e.Key)
        {
            case Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End or Key.PageUp or Key.PageDown:
                return true;
            case Key.C or Key.A or Key.Insert when primary:
                return true;
            default:
                return false;
        }
    }

    private void OnKeyForTheMap(object? sender, KeyEventArgs e)
    {
        if (e.Handled || IsReadingKey(e)) return;
        if (DataContext is not MainWindowViewModel vm) return;
        var session = vm.CommandService?.Session;
        var catalogue = App.Services?.GetService<CommandCatalogue>();
        if (session == null || catalogue == null) return;

        var input = AvaloniaKeyMapper.ToKeyInput(e.Key, e.KeyModifiers, AvaloniaKeyMapper.IsMacOS);
        var command = catalogue.FindByGesture(input, CommandScope.Canvas) ??
                      catalogue.FindByGesture(input, CommandScope.Window);
        if (command == null || !command.CanExecute(session)) return;

        e.Handled = true;
        _ = command.ExecuteAsync(session);
    }
}

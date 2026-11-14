using System;
using System.ComponentModel;
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
///   Paused before a command, a cursor flashes after it, as on the little monitor; it holds still while text is
///   selected, and isn't copied.
/// </summary>
public partial class TranscriptWindow : Window
{
    private readonly DispatcherTimer _blink = new() { Interval = TimeSpan.FromMilliseconds(530) };
    private bool _cursorOn = true;
    private MainWindowViewModel? _vm;

    public TranscriptWindow()
    {
        InitializeComponent();
        _blink.Tick += (_, _) =>
        {
            // the text can't change under a selection, or while reading further up (it would scroll back to the end),
            // so the cursor waits
            if (HasSelection || !AtTheEnd) return;
            _cursorOn = !_cursorOn;
            ShowText();
        };
        TranscriptBox.TemplateApplied += (_, e) => _scroller = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        DataContextChanged += (_, _) => Watch(DataContext as MainWindowViewModel);
        Closed += (_, _) =>
        {
            _blink.Stop();
            Watch(null);
        };
        TranscriptBox.AddHandler(TextBox.CopyingToClipboardEvent, OnCopying, RoutingStrategies.Bubble);
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

    private ScrollViewer? _scroller;

    private bool HasSelection => TranscriptBox.SelectionStart != TranscriptBox.SelectionEnd;

    /// <summary>Whether the end of the transcript is in view.</summary>
    private bool AtTheEnd =>
        _scroller == null || _scroller.Offset.Y + _scroller.Viewport.Height >= _scroller.Extent.Height - 4;

    private bool ShowsCursor => _vm?.IsAutomapAwaitingCommand == true;

    private void Watch(MainWindowViewModel? vm)
    {
        if (_vm != null) _vm.PropertyChanged -= OnViewModelChanged;
        _vm = vm;
        if (_vm != null) _vm.PropertyChanged += OnViewModelChanged;
        _cursorOn = true;
        UpdateBlink();
        ShowText();
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(MainWindowViewModel.AutomapTranscriptText) or
            nameof(MainWindowViewModel.IsAutomapAwaitingCommand))) return;
        // a new line, or a pause, shows the cursor at once
        _cursorOn = true;
        UpdateBlink();
        ShowText();
    }

    private void UpdateBlink()
    {
        if (ShowsCursor) _blink.Start();
        else _blink.Stop();
    }

    /// <summary>The transcript, and the cursor after it when it's showing (a space when it's off, so nothing jumps).</summary>
    private void ShowText()
    {
        var text = _vm?.AutomapTranscriptText ?? string.Empty;
        var shown = ShowsCursor ? text + (_cursorOn ? TranscriptMonitor.CursorBlock : " ") : text;
        if (TranscriptBox.Text != shown) TranscriptBox.Text = shown;
    }

    /// <summary>Copies the selection without the cursor.</summary>
    private void OnCopying(object? sender, RoutedEventArgs e)
    {
        var text = TranscriptBox.Text ?? string.Empty;
        var start = Math.Min(TranscriptBox.SelectionStart, TranscriptBox.SelectionEnd);
        var end = Math.Max(TranscriptBox.SelectionStart, TranscriptBox.SelectionEnd);
        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, start, text.Length);
        var selected = text.Substring(start, end - start);
        var plain = selected.Replace(TranscriptMonitor.CursorBlock, string.Empty);
        if (plain == selected) return;
        e.Handled = true;
        _ = Clipboard?.SetTextAsync(plain);
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

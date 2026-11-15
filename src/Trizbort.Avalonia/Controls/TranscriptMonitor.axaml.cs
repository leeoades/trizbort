using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///   The game as automap has read it so far, on a little monochrome monitor. Clicking it opens the whole transcript
///   in a window of its own, where text can be selected and copied into the map. Paused before a command, a cursor
///   flashes after it: typed, but not yet entered.
/// </summary>
public partial class TranscriptMonitor : UserControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<TranscriptMonitor, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<bool> ShowsCursorProperty =
        AvaloniaProperty.Register<TranscriptMonitor, bool>(nameof(ShowsCursor));

    /// <summary>The cursor: a block, as on an old terminal (the transcript window shows it too).</summary>
    internal const string CursorBlock = "█";

    private static TranscriptWindow? _window;
    private readonly DispatcherTimer _blink = new() { Interval = TimeSpan.FromMilliseconds(530) };
    private bool _cursorOn = true;

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Whether a cursor flashes after the text (automap is paused before the command it ends with).</summary>
    public bool ShowsCursor
    {
        get => GetValue(ShowsCursorProperty);
        set => SetValue(ShowsCursorProperty, value);
    }

    public TranscriptMonitor()
    {
        InitializeComponent();
        _blink.Tick += (_, _) =>
        {
            _cursorOn = !_cursorOn;
            ShowText();
        };
        ShowText();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ShowsCursorProperty)
        {
            _cursorOn = true;
            if (ShowsCursor && IsAttached) _blink.Start();
            else _blink.Stop();
            ShowText();
        }
        else if (change.Property == TextProperty)
        {
            // a new line shows the cursor at once
            _cursorOn = true;
            ShowText();
        }
    }

    private bool IsAttached => VisualRoot != null;

    protected override void OnAttachedToVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (ShowsCursor) _blink.Start();
    }

    protected override void OnDetachedFromVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _blink.Stop();
    }

    /// <summary>The screen: the text, and the cursor after it when it's showing (a space when it's off, so nothing jumps).</summary>
    private void ShowText()
    {
        var text = Text ?? string.Empty;
        ScreenText.Text = ShowsCursor ? text + (_cursorOn ? CursorBlock : " ") : text;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.InitialPressMouseButton != MouseButton.Left) return;
        if (DataContext is not MainWindowViewModel vm) return;
        e.Handled = true;
        ShowTranscript(vm, TopLevel.GetTopLevel(this) as Window);
    }

    /// <summary>Opens the transcript window, or brings it to the front if it is open already.</summary>
    internal static void ShowTranscript(MainWindowViewModel vm, Window? owner)
    {
        if (_window != null)
        {
            _window.Activate();
            return;
        }

        _window = new TranscriptWindow { DataContext = vm };
        vm.SetTranscriptWindowOpen(true);
        _window.Closed += (_, _) =>
        {
            vm.SetTranscriptWindowOpen(false);
            _window = null;
        };
        if (owner != null) _window.Show(owner);
        else _window.Show();
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///   The game as automap has read it so far, on a little monochrome monitor. Clicking it opens the whole transcript
///   in a window of its own, where text can be selected and copied into the map.
/// </summary>
public partial class TranscriptMonitor : UserControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<TranscriptMonitor, string>(nameof(Text), string.Empty);

    private static TranscriptWindow? _window;

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public TranscriptMonitor()
    {
        InitializeComponent();
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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///     Automap's controls, docked beside the map while it runs, in three parts: where you are (the room automap
///     thinks you're in), the transcript (moving through it a move at a time) and mapping (whether automap changes
///     the map, or only tracks where you are). Bound to <see cref="ViewModels.MainWindowViewModel" />. Its left edge
///     drags to make it wider, the game screen growing with it.
/// </summary>
public partial class AutomapPanel : UserControl
{
    public const double UsualWidth = 280;
    public const double WidestWidth = 720;

    /// <summary>The width the panel was dragged to, kept while the app runs.</summary>
    private static double _width = UsualWidth;

    private bool _resizing;
    private double _startX;
    private double _startWidth;

    public AutomapPanel()
    {
        InitializeComponent();
        ApplyWidth(_width);
        ResizeGrip.PointerPressed += OnGripPressed;
        ResizeGrip.PointerMoved += OnGripMoved;
        ResizeGrip.PointerReleased += OnGripReleased;
        ResizeGrip.DoubleTapped += (_, e) =>
        {
            ApplyWidth(UsualWidth);
            e.Handled = true;
        };
    }

    /// <summary>The panel this wide (between the usual width and <see cref="WidestWidth" />), the game screen with it.</summary>
    private void ApplyWidth(double width)
    {
        _width = Math.Clamp(width, UsualWidth, WidestWidth);
        PanelBorder.Width = _width;
        MonitorBox.Width = _width - (UsualWidth - 240);
    }

    private void OnGripPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(ResizeGrip).Properties.IsLeftButtonPressed) return;
        _resizing = true;
        _startX = e.GetPosition(TopLevel.GetTopLevel(this)).X;
        _startWidth = _width;
        e.Pointer.Capture(ResizeGrip);
        e.Handled = true;
    }

    private void OnGripMoved(object? sender, PointerEventArgs e)
    {
        if (!_resizing) return;
        // the panel's on the right: dragging its left edge left makes it wider
        var x = e.GetPosition(TopLevel.GetTopLevel(this)).X;
        ApplyWidth(_startWidth - (x - _startX));
        e.Handled = true;
    }

    private void OnGripReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_resizing) return;
        _resizing = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }
}

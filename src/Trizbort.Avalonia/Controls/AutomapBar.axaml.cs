using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace Trizbort.Avalonia.Controls;

public partial class AutomapBar : UserControl
{
    public static readonly StyledProperty<string> StatusProperty =
        AvaloniaProperty.Register<AutomapBar, string>(nameof(Status), "Automap is not running.");

    public static readonly StyledProperty<ICommand?> StopCommandProperty =
        AvaloniaProperty.Register<AutomapBar, ICommand?>(nameof(StopCommand));

    public static readonly StyledProperty<bool> IsMoveByMoveProperty =
        AvaloniaProperty.Register<AutomapBar, bool>(nameof(IsMoveByMove));

    public static readonly StyledProperty<ICommand?> NextMoveCommandProperty =
        AvaloniaProperty.Register<AutomapBar, ICommand?>(nameof(NextMoveCommand));

    public static readonly StyledProperty<ICommand?> PlayToEndCommandProperty =
        AvaloniaProperty.Register<AutomapBar, ICommand?>(nameof(PlayToEndCommand));

    public static readonly StyledProperty<ICommand?> PauseCommandProperty =
        AvaloniaProperty.Register<AutomapBar, ICommand?>(nameof(PauseCommand));

    public static readonly StyledProperty<bool> IsMonitorShownProperty =
        AvaloniaProperty.Register<AutomapBar, bool>(nameof(IsMonitorShown), true);

    public static readonly StyledProperty<ICommand?> ToggleMonitorCommandProperty =
        AvaloniaProperty.Register<AutomapBar, ICommand?>(nameof(ToggleMonitorCommand));

    /// <summary>True while the game monitor is shown: the bar offers to hide it, otherwise to show it.</summary>
    public bool IsMonitorShown
    {
        get => GetValue(IsMonitorShownProperty);
        set => SetValue(IsMonitorShownProperty, value);
    }

    public ICommand? ToggleMonitorCommand
    {
        get => GetValue(ToggleMonitorCommandProperty);
        set => SetValue(ToggleMonitorCommandProperty, value);
    }

    /// <summary>True while automap plays a move at a time: shows Next Move and Play to End instead of Pause.</summary>
    public bool IsMoveByMove
    {
        get => GetValue(IsMoveByMoveProperty);
        set => SetValue(IsMoveByMoveProperty, value);
    }

    public ICommand? NextMoveCommand
    {
        get => GetValue(NextMoveCommandProperty);
        set => SetValue(NextMoveCommandProperty, value);
    }

    public ICommand? PlayToEndCommand
    {
        get => GetValue(PlayToEndCommandProperty);
        set => SetValue(PlayToEndCommandProperty, value);
    }

    public ICommand? PauseCommand
    {
        get => GetValue(PauseCommandProperty);
        set => SetValue(PauseCommandProperty, value);
    }

    public string Status
    {
        get => GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public ICommand? StopCommand
    {
        get => GetValue(StopCommandProperty);
        set => SetValue(StopCommandProperty, value);
    }

    public AutomapBar()
    {
        InitializeComponent();
    }
}

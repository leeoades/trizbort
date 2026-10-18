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

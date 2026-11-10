using Avalonia.Controls;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///     Automap's controls, docked beside the map while it runs, in three parts: where you are (the room automap
///     thinks you're in), the transcript (moving through it a move at a time) and mapping (whether automap changes
///     the map, or only tracks where you are). Bound to <see cref="ViewModels.MainWindowViewModel" />.
/// </summary>
public partial class AutomapPanel : UserControl
{
    public AutomapPanel()
    {
        InitializeComponent();
    }
}

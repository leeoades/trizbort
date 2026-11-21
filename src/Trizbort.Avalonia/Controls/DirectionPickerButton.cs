using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///   A small compass button beside a join's Start or End text. It opens a pop-up with the eight compass directions
///   laid out as a compass, plus Up and Down, each pressed to pick or unpick it, and a Custom box for anything else
///   (a spell, "climb beanstalk"). The text is rewritten as they change: the directions in compass order, then the
///   custom text, joined with " / ", written short or in full as the map settings say (as written: the way the label
///   already writes them, otherwise short).
/// </summary>
public class DirectionPickerButton : UserControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<DirectionPickerButton, string?>(nameof(Text), defaultBindingMode: BindingMode.TwoWay);

    private readonly Button _button;
    private readonly Dictionary<MappableDirection, ToggleButton> _toggles = new();
    private readonly TextBox _custom;
    private bool _full;
    private bool _syncing;

    public DirectionPickerButton()
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,12,Auto"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            ColumnSpacing = 2,
            RowSpacing = 2
        };
        foreach (var direction in DirectionWords.Pickable)
        {
            var toggle = new ToggleButton
            {
                Content = Caption(direction),
                Width = direction is MappableDirection.Up or MappableDirection.Down ? 56 : 36,
                Height = 28,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Name = direction + "Toggle"
            };
            AutomationProperties.SetName(toggle, DirectionWords.Word(direction));
            ToolTip.SetTip(toggle, DirectionWords.Word(direction));
            var (row, column) = Cell(direction);
            Grid.SetRow(toggle, row);
            Grid.SetColumn(toggle, column);
            toggle.IsCheckedChanged += (_, _) => Write();
            _toggles[direction] = toggle;
            grid.Children.Add(toggle);
        }

        // the room in the middle of the compass
        var middle = new Border
        {
            Width = 36, Height = 28, CornerRadius = new CornerRadius(3),
            BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1)
        };
        Grid.SetRow(middle, 1);
        Grid.SetColumn(middle, 1);
        grid.Children.Add(middle);

        _custom = new TextBox { Name = "CustomBox", Watermark = "a spell, \"climb tree\"...", MinWidth = 200 };
        AutomationProperties.SetName(_custom, "Custom");
        // as the text changes, typed or set (TextChanged waits for the box to be shown)
        _custom.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty) Write();
        };

        var panel = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                grid,
                new TextBlock { Text = "Custom:" },
                _custom,
                new TextBlock
                {
                    Text = "Several are written \"n / up\".", Opacity = 0.7, FontSize = 11
                }
            }
        };

        var flyout = new Flyout { Content = panel, Placement = PlacementMode.BottomEdgeAlignedRight };
        flyout.Opening += (_, _) => Read();
        _button = new Button
        {
            Name = "CompassButton",
            Padding = new Thickness(4, 2),
            VerticalAlignment = VerticalAlignment.Stretch,
            Content = new PathIcon { Data = Compass, Width = 16, Height = 16 },
            Flyout = flyout
        };
        ToolTip.SetTip(_button, "Pick the directions, or type a custom command");
        Content = _button;
    }

    /// <summary>The label: what the Start or End box holds.</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>The pop-up's direction buttons, for tests.</summary>
    internal IReadOnlyDictionary<MappableDirection, ToggleButton> Toggles => _toggles;

    /// <summary>The pop-up's Custom box, for tests.</summary>
    internal TextBox CustomBox => _custom;

    /// <summary>Fills the pop-up from the label, as it opens.</summary>
    internal void Read()
    {
        var (directions, custom, full) = DirectionWords.Pick(Text ?? string.Empty);
        _full = Settings.LabelDirectionWords switch
        {
            DirectionWordStyle.Full => true,
            DirectionWordStyle.Short => false,
            _ => full
        };
        _syncing = true;
        try
        {
            foreach (var (direction, toggle) in _toggles) toggle.IsChecked = directions.Contains(direction);
            _custom.Text = custom;
        }
        finally
        {
            _syncing = false;
        }
    }

    private void Write()
    {
        if (_syncing) return;
        var picked = _toggles.Where(pair => pair.Value.IsChecked == true).Select(pair => pair.Key);
        var text = DirectionWords.Compose(picked, _custom.Text ?? string.Empty, _full);
        if (text != (Text ?? string.Empty)) Text = text;
    }

    private static string Caption(MappableDirection direction) => direction switch
    {
        MappableDirection.Up => "Up",
        MappableDirection.Down => "Down",
        _ => DirectionWords.Short(direction).ToUpperInvariant()
    };

    private static (int Row, int Column) Cell(MappableDirection direction) => direction switch
    {
        MappableDirection.NorthWest => (0, 0),
        MappableDirection.North => (0, 1),
        MappableDirection.NorthEast => (0, 2),
        MappableDirection.West => (1, 0),
        MappableDirection.East => (1, 2),
        MappableDirection.SouthWest => (2, 0),
        MappableDirection.South => (2, 1),
        MappableDirection.SouthEast => (2, 2),
        MappableDirection.Up => (0, 4),
        _ => (2, 4)
    };

    /// <summary>A compass rose: a ring with a needle pointing north.</summary>
    private static readonly Geometry Compass = Geometry.Parse(
        "M12,2 A10,10 0 1 0 12,22 A10,10 0 1 0 12,2 Z M12,4 A8,8 0 1 1 12,20 A8,8 0 1 1 12,4 Z " +
        "M12,5 L14.5,12 L12,19 L9.5,12 Z");
}

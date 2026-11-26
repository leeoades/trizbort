using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Trizbort.Domain.Enums;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///   A row of three toggle buttons with the usual text-alignment icons (Lee, 26 Nov 2026): left / centre / right, or
///   top / middle / bottom. One is lit for the chosen place; none for Usual (as labels have always been). Clicking the
///   lit one again goes back to Usual.
/// </summary>
public abstract class AlignButtonRow : StackPanel
{
    private readonly List<(int Value, ToggleButton Button)> _buttons = new();

    protected AlignButtonRow()
    {
        Orientation = Orientation.Horizontal;
        Spacing = 2;
        // a Label's access key lands here: pass the focus to a button
        Focusable = true;
        IsTabStop = false;
    }

    protected void Build(params (int Value, string Name, string Icon)[] choices)
    {
        foreach (var (value, name, icon) in choices)
        {
            var path = new global::Avalonia.Controls.Shapes.Path
            {
                Data = StreamGeometry.Parse(icon),
                Width = 16,
                Height = 16,
                Stretch = Stretch.None
            };
            path.Bind(Shape.FillProperty, new Binding("Foreground") { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor) { AncestorType = typeof(ToggleButton) } });
            var button = new ToggleButton
            {
                Content = path,
                Padding = new Thickness(6, 4),
                Name = GetType().Name + name
            };
            ToolTip.SetTip(button, name);
            AutomationProperties.SetName(button, name);
            button.Click += (_, _) =>
            {
                Choose(Current == value ? 0 : value);
                Refresh();
            };
            _buttons.Add((value, button));
            Children.Add(button);
        }

        Refresh();
    }

    /// <summary>The chosen place, as its number (0 is Usual).</summary>
    protected abstract int Current { get; }

    protected abstract void Choose(int value);

    protected void Refresh()
    {
        foreach (var (value, button) in _buttons) button.IsChecked = value == Current;
    }

    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        base.OnGotFocus(e);
        if (ReferenceEquals(e.Source, this))
            (_buttons.FirstOrDefault(b => b.Button.IsChecked == true).Button ?? _buttons.FirstOrDefault().Button)?.Focus();
    }

    // 16 x 16 icons: lines of text, or blocks against a line
    protected const string LeftIcon = "M2,2h12v1.5H2z M2,6h8v1.5H2z M2,10h12v1.5H2z M2,14h8v1.5H2z";
    protected const string CentreIcon = "M2,2h12v1.5H2z M4,6h8v1.5H4z M2,10h12v1.5H2z M4,14h8v1.5H4z";
    protected const string RightIcon = "M2,2h12v1.5H2z M6,6h8v1.5H6z M2,10h12v1.5H2z M6,14h8v1.5H6z";
    protected const string TopIcon = "M1,1.5h14v1.5H1z M4,4.5h3v9H4z M9,4.5h3v5H9z";
    protected const string MiddleIcon = "M1,7.25h14v1.5H1z M4,2.5h3v11H4z M9,4.5h3v7H9z";
    protected const string BottomIcon = "M1,13h14v1.5H1z M4,2.5h3v9H4z M9,6.5h3v5H9z";
}

/// <summary>Left, centre or right (or Usual, with none lit).</summary>
public sealed class LabelAlignButtons : AlignButtonRow
{
    public static readonly StyledProperty<LabelAlign> ValueProperty =
        AvaloniaProperty.Register<LabelAlignButtons, LabelAlign>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public LabelAlignButtons() =>
        Build(((int)LabelAlign.Left, "Left", LeftIcon), ((int)LabelAlign.Centre, "Centre", CentreIcon),
            ((int)LabelAlign.Right, "Right", RightIcon));

    public LabelAlign Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    protected override int Current => (int)Value;

    protected override void Choose(int value) => Value = (LabelAlign)value;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty) Refresh();
    }
}

/// <summary>Top, middle or bottom (or Usual, with none lit).</summary>
public sealed class LabelVerticalAlignButtons : AlignButtonRow
{
    public static readonly StyledProperty<LabelVerticalAlign> ValueProperty =
        AvaloniaProperty.Register<LabelVerticalAlignButtons, LabelVerticalAlign>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public LabelVerticalAlignButtons() =>
        Build(((int)LabelVerticalAlign.Top, "Top", TopIcon), ((int)LabelVerticalAlign.Middle, "Middle", MiddleIcon),
            ((int)LabelVerticalAlign.Bottom, "Bottom", BottomIcon));

    public LabelVerticalAlign Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    protected override int Current => (int)Value;

    protected override void Choose(int value) => Value = (LabelVerticalAlign)value;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty) Refresh();
    }
}

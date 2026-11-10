using System;
using Avalonia;
using Avalonia.Controls;

namespace Trizbort.Avalonia.Controls;

/// <summary>
///   Shows the bottom of its child: the child is as tall as it likes, and sits with its bottom on this control's
///   bottom, so when it is taller its top is cut off. The game monitor uses it so the latest line is always in view.
/// </summary>
public class BottomAnchor : Decorator
{
    public BottomAnchor()
    {
        ClipToBounds = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child == null) return default;
        Child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        var desired = Child.DesiredSize;
        return new Size(desired.Width, double.IsInfinity(availableSize.Height) ? desired.Height : Math.Min(desired.Height, availableSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child == null) return finalSize;
        var height = Child.DesiredSize.Height;
        Child.Arrange(new Rect(0, finalSize.Height - height, finalSize.Width, height));
        return finalSize;
    }
}

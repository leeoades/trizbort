using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views.Dialogs;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///   Lee, 19 Nov 2026: "Don't automap from here" was cut off at "Dor" at the right of the room dialog. The boxes under
///   the reference now wrap onto a second line rather than run off the edge.
/// </summary>
[TestFixture]
[Category("Unit")]
public class RoomDialogHeaderTests
{
    [AvaloniaTest]
    [TestCase(560)]
    [TestCase(476)]
    public void EveryBoxUnderTheReference_IsWhollyInTheWindow(double width)
    {
        var dialog = new RoomPropertiesDialog { DataContext = new RoomPropertiesViewModel(), Width = width };
        dialog.Show();
        try
        {
            dialog.UpdateLayout();
            var boxes = dialog.GetVisualDescendants().OfType<CheckBox>()
                .Where(c => c.IsEffectivelyVisible && c.Content is string).ToList();
            boxes.Select(b => (string)b.Content!).ShouldContain("Don't _automap from here");
            foreach (var box in boxes)
            {
                var right = box.TranslatePoint(new Point(box.Bounds.Width, 0), dialog)!.Value.X;
                right.ShouldBeLessThanOrEqualTo(dialog.Bounds.Width, (string)box.Content!);
            }
        }
        finally
        {
            dialog.Close();
        }
    }
}

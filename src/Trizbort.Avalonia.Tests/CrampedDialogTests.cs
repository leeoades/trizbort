using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views.Dialogs;

namespace Trizbort.Avalonia.Tests;

/// <summary>Dialogs squeezed by a tiling window manager scroll their contents and keep their buttons in view.</summary>
[TestFixture]
public class CrampedDialogTests
{
    [AvaloniaTest]
    public void AutomapSettings_InAShortWindow_KeepsStartAndCancelInView()
    {
        var dialog = new AutomapSettingsDialog { DataContext = new AutomapSettingsViewModel(null!) };
        dialog.Height = 320;
        dialog.Show();
        try
        {
            using var frame = dialog.CaptureRenderedFrame();
            var shots = System.Environment.GetEnvironmentVariable("TRIZBORT_SCREENSHOTS");
            if (!string.IsNullOrEmpty(shots)) frame!.Save(System.IO.Path.Combine(shots, "automap-dialog-cramped.png"));

            dialog.Bounds.Height.ShouldBe(320);
            foreach (var button in dialog.GetVisualDescendants().OfType<Button>())
            {
                if (button.Content is not string text || !(text.Contains("Start") || text == "Cancel")) continue;
                var bottom = button.TranslatePoint(new Point(0, button.Bounds.Height), dialog)!.Value.Y;
                bottom.ShouldBeLessThanOrEqualTo(320, text);
            }

            dialog.GetVisualDescendants().OfType<ScrollViewer>().ShouldNotBeEmpty();
        }
        finally
        {
            dialog.Close();
        }
    }
}

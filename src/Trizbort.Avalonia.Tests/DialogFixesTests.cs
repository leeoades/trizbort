using System.IO;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views.Dialogs;

namespace Trizbort.Avalonia.Tests;

/// <summary>Ticket 134: fixes from the manual checklist (I-033..I-039).</summary>
[TestFixture]
[Category("Unit")]
public class DialogFixesTests
{
    [AvaloniaTest]
    public void RoomDialog_FocusesFirstInput()
    {
        var dialog = new RoomPropertiesDialog { DataContext = new RoomPropertiesViewModel() };
        dialog.Show();
        try
        {
            DialogFocus.FocusFirstInput(dialog);
            dialog.FocusManager?.GetFocusedElement().ShouldBe(dialog.NameBox);
        }
        finally
        {
            dialog.Close();
        }
    }

    [AvaloniaTest]
    public void RoomDialog_ObjectsBoxDoesNotAcceptTab()
    {
        var dialog = new RoomPropertiesDialog { DataContext = new RoomPropertiesViewModel() };
        dialog.Show();
        try
        {
            dialog.ObjectsBox.AcceptsTab.ShouldBeFalse();
        }
        finally
        {
            dialog.Close();
        }
    }

    [AvaloniaTest]
    public void AppSettings_HasNoNotActiveYetHints()
    {
        var dialog = new AppSettingsDialog { DataContext = new AppSettingsViewModel() };
        dialog.Show();
        try
        {
            var texts = dialog.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? string.Empty);
            texts.Any(t => t.Contains("not active yet")).ShouldBeFalse();
        }
        finally
        {
            dialog.Close();
        }
    }

    [AvaloniaTest]
    public void ImageExport_CurrentZoomLabelSaysWholeMap()
    {
        var dialog = new ImageExportOptionsDialog();
        dialog.Configure(0.5, false);
        dialog.Show();
        try
        {
            dialog.CurrentZoomRadio.Content!.ToString()!.ShouldContain("Whole map");
        }
        finally
        {
            dialog.Close();
        }
    }

    [AvaloniaTest]
    public void ColorChooser_HasOkAndCancelAndIsWideEnough()
    {
        var picker = new ColorPickerButton();
        picker.OkButton.Content.ShouldBe("OK");
        picker.CancelButton.Content.ShouldBe("Cancel");
        picker.FlyoutPanel.Width.ShouldBeGreaterThanOrEqualTo(300);
        picker.FullColorView.Width.ShouldBeGreaterThanOrEqualTo(300);
    }

    [Test]
    public void OverwriteGuard_AllowsMissingFile()
    {
        var path = Path.Combine(Path.GetTempPath(), "trizbort-missing-" + Path.GetRandomFileName());
        OverwriteGuard.ConfirmAsync(null, null, path).GetAwaiter().GetResult().ShouldBeTrue();
    }
}

using System.Drawing;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
public class ColorPickerButtonTests
{
    [AvaloniaTest]
    public void DefaultShowsNoColorText()
    {
        var picker = new ColorPickerButton();
        picker.DisplayText.ShouldBe("No Color Set");
    }

    [AvaloniaTest]
    public void ClearSetsTransparent()
    {
        var picker = new ColorPickerButton { SelectedColor = Color.Red };
        picker.DisplayText.ShouldNotBe("No Color Set");
        picker.ClearColor();
        picker.SelectedColor.ShouldBe(Color.Transparent);
        picker.DisplayText.ShouldBe("No Color Set");
    }

    [AvaloniaTest]
    public void HexInputSetsColor()
    {
        var picker = new ColorPickerButton();
        picker.TryApplyHex("336699").ShouldBeTrue();
        picker.SelectedColor.ToArgb().ShouldBe(Color.FromArgb(255, 0x33, 0x66, 0x99).ToArgb());
        picker.TryApplyHex("not a colour").ShouldBeFalse();
    }

    [AvaloniaTest]
    public void ColorViewChangeUpdatesSelectedColor()
    {
        var picker = new ColorPickerButton();
        picker.ApplyColorViewColor(global::Avalonia.Media.Color.FromRgb(10, 20, 30));
        picker.SelectedColor.ToArgb().ShouldBe(Color.FromArgb(255, 10, 20, 30).ToArgb());
    }
}

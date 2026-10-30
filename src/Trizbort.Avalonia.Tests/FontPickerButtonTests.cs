using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.ViewModels;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
public class FontPickerButtonTests
{
    private static FontPickerButton Create(FontSpec initial)
    {
        var picker = new FontPickerButton
        {
            AvailableFamilies = new[] { "Alpha", "Beta", "Gamma" },
            SelectedFont = initial
        };
        picker.Cancel(); // loads the draft from SelectedFont
        return picker;
    }

    [AvaloniaTest]
    public void CommitAppliesFamilySizeAndStyle()
    {
        var picker = Create(new FontSpec("Alpha", 10, false, false));

        picker.SelectFamily("Beta");
        picker.SizeBox.Value = 14;
        picker.BoldToggle.IsChecked = true;
        picker.ItalicToggle.IsChecked = true;
        picker.Commit();

        picker.SelectedFont.ShouldBe(new FontSpec("Beta", 14, true, true));
    }

    [AvaloniaTest]
    public void TogglingStyleOnlyUpdatesSelectedFontOnCommit()
    {
        var picker = Create(new FontSpec("Alpha", 10, false, false));

        picker.BoldToggle.IsChecked = true;
        picker.SelectedFont.Bold.ShouldBeFalse();
        picker.Commit();

        picker.SelectedFont.Bold.ShouldBeTrue();
        picker.SelectedFont.Italic.ShouldBeFalse();
    }

    [AvaloniaTest]
    public void UnknownFamilyIsPreservedOnCommit()
    {
        var picker = Create(new FontSpec("Missing Font", 12, false, false));

        picker.DraftFamily.ShouldBe("Missing Font");
        picker.BoldToggle.IsChecked = true;
        picker.Commit();

        picker.SelectedFont.Family.ShouldBe("Missing Font");
        picker.SelectedFont.Size.ShouldBe(12);
        picker.SelectedFont.Bold.ShouldBeTrue();
    }

    [AvaloniaTest]
    public void CancelKeepsOldValue()
    {
        var original = new FontSpec("Alpha", 10, false, false);
        var picker = Create(original);

        picker.SelectFamily("Gamma");
        picker.SizeBox.Value = 30;
        picker.BoldToggle.IsChecked = true;
        picker.Cancel();

        picker.SelectedFont.ShouldBe(original);
        picker.DraftFamily.ShouldBe("Alpha");
        picker.DraftBold.ShouldBeFalse();
    }
}

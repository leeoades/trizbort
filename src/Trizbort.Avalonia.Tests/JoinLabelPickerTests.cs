using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Enums;
using Trizbort.Setup;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///   Lee, 20 Nov 2026: a compass button beside a join's Start and End text picks the directions (and custom text) for
///   the label, and Map Settings has a Join labels section with a sample room.
/// </summary>
[TestFixture]
public class JoinLabelPickerTests
{
    private DirectionWordStyle _words;
    private bool _upDown;

    [SetUp]
    public void Remember()
    {
        _words = Settings.LabelDirectionWords;
        _upDown = Settings.LabelUpDownExits;
    }

    [TearDown]
    public void Restore()
    {
        Settings.LabelDirectionWords = _words;
        Settings.LabelUpDownExits = _upDown;
    }

    [AvaloniaTest]
    public void ThePicker_ShowsTheLabelsDirections_AndRewritesTheLabelAsTheyArePicked()
    {
        Settings.LabelDirectionWords = DirectionWordStyle.AsWritten;
        var picker = new DirectionPickerButton { Text = "up / climb beanstalk" };

        picker.Read();
        picker.Toggles[MappableDirection.Up].IsChecked.ShouldBe(true);
        picker.Toggles[MappableDirection.North].IsChecked.ShouldBe(false);
        picker.CustomBox.Text.ShouldBe("climb beanstalk");

        picker.Toggles[MappableDirection.North].IsChecked = true;
        // written in full, as the label was
        picker.Text.ShouldBe("north / up / climb beanstalk");

        picker.Toggles[MappableDirection.Up].IsChecked = false;
        picker.CustomBox.Text = "";
        picker.Text.ShouldBe("north");
    }

    [AvaloniaTest]
    public void ThePicker_WritesTheMapsWordStyle()
    {
        Settings.LabelDirectionWords = DirectionWordStyle.Short;
        var picker = new DirectionPickerButton { Text = "north" };

        picker.Read();
        picker.Toggles[MappableDirection.Down].IsChecked = true;

        picker.Text.ShouldBe("n / d");
    }

    [AvaloniaTest]
    public void MapSettings_JoinLabelOptions_RoundTripThroughSettings()
    {
        var vm = new MapSettingsViewModel();
        vm.Initialize();
        vm.LabelUpDownExits = false;
        vm.DirectionWordsIndex = 2;
        vm.LabelDirectionWords.ShouldBe(DirectionWordStyle.Full);
        vm.ApplyToSettings();

        Settings.LabelUpDownExits.ShouldBeFalse();
        Settings.LabelDirectionWords.ShouldBe(DirectionWordStyle.Full);

        var reloaded = new MapSettingsViewModel();
        reloaded.Initialize();
        reloaded.LabelUpDownExits.ShouldBeFalse();
        reloaded.DirectionWordsIndex.ShouldBe(2);
    }

    [AvaloniaTest]
    public void TheSampleRoom_IsLabelledAsTheSettingsSay()
    {
        var vm = new MapSettingsViewModel();
        vm.Initialize();
        vm.LabelCompassExits = true;
        vm.LabelDiagonalExits = false;
        vm.LabelUpDownExits = true;
        vm.LabelDirectionWords = DirectionWordStyle.Full;

        JoinLabelsPreview.LabelFor(MappableDirection.North, "", vm).ShouldBe("north");
        JoinLabelsPreview.LabelFor(MappableDirection.NorthEast, "", vm).ShouldBeEmpty();
        JoinLabelsPreview.LabelFor(MappableDirection.SouthWest, "d", vm).ShouldBe("down");

        vm.LabelUpDownExits = false;
        vm.LabelDirectionWords = DirectionWordStyle.Short;
        JoinLabelsPreview.LabelFor(MappableDirection.West, "west / up", vm).ShouldBe("w");
        JoinLabelsPreview.LabelFor(MappableDirection.SouthWest, "d", vm).ShouldBeEmpty();
    }
}

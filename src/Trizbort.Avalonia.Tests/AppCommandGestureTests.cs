using System;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Enums;
using Trizbort.Editor.Commands;
using Trizbort.Editor.Input;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[Category("Unit")]
public class AppCommandGestureTests
{
    private CommandCatalogue _catalogue = null!;

    [SetUp]
    public void SetUp()
    {
        _catalogue = AppCommands.CreateCatalogue(null);
    }

    private EditorCommand Command(string id)
    {
        var command = _catalogue.Find(id);
        command.ShouldNotBeNull(id);
        return command!;
    }

    [Test]
    public void SaveAs_IsPrimaryShiftS()
    {
        Command("file.saveAs").Gestures.ShouldBe(new[] { new KeyGesture(InputKey.S, ModifierKeys.Primary | ModifierKeys.Shift) });
    }

    [Test]
    public void AppSettings_IsPrimaryComma_MapSettings_IsPrimaryShiftComma()
    {
        Command("edit.appSettings").Gestures.ShouldBe(new[] { new KeyGesture(InputKey.OemComma, ModifierKeys.Primary) });
        Command("edit.mapSettings").Gestures.ShouldBe(new[] { new KeyGesture(InputKey.OemComma, ModifierKeys.Primary | ModifierKeys.Shift) });
    }

    [Test]
    public void OnlineHelp_IsF1_AndAboutHasNoGesture()
    {
        Command("help.online").Gestures.ShouldBe(new[] { new KeyGesture(InputKey.F1, ModifierKeys.None) });
        Command("help.about").Gestures.ShouldBeEmpty();
    }

    [Test]
    public void Exit_UsesPlatformGesture()
    {
        var exit = Command("file.exit");
        if (OperatingSystem.IsWindows())
        {
            exit.Gestures.ShouldBe(new[] { new KeyGesture(InputKey.F4, ModifierKeys.Alt) });
            exit.GestureDisplayOnly.ShouldBeTrue();
        }
        else
        {
            exit.Gestures.ShouldBe(new[] { new KeyGesture(InputKey.Q, ModifierKeys.Primary) });
            exit.GestureDisplayOnly.ShouldBeFalse();
        }
    }

    [Test]
    public void FullCatalogue_HasNoGestureClashes()
    {
        CatalogueValidator.Validate(_catalogue).ShouldBeEmpty();
    }
}

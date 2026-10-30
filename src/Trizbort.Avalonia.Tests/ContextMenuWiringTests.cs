using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Setup;
using Trizbort.Domain.Application;
using Trizbort.Domain.Misc;
using Trizbort.Editor.Commands;
using Vector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

/// <summary>Ticket 066 / I-018: the generated context menu per target and the dynamic Set Region submenu.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class ContextMenuWiringTests
{
    private Project? _previousProject;
    private List<Region>? _savedRegions;
    private CommandService? _service;
    private CommandCatalogue? _catalogue;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
        _savedRegions = Settings.Regions;
        Settings.Regions = new List<Region>
        {
            new Region { RegionName = Region.DefaultRegion },
            new Region { RegionName = "Alpha" },
            new Region { RegionName = "Zebra" }
        };
        _service = new CommandService();
        _catalogue = AppCommands.CreateCatalogue(null);
    }

    [TearDown]
    public void TearDown()
    {
        Settings.Regions = _savedRegions!;
        var created = Project.Current;
        Project.Current = _previousProject!;
        created.Dispose();
    }

    private static string Plain(object? header) => (header?.ToString() ?? string.Empty).Replace("_", string.Empty);

    [AvaloniaTest]
    public void Room_Menu_HasSetRegionSubmenuWithCurrentTicked()
    {
        var room = _service!.AddRoom(new Vector(0, 0));
        room.Region = "Alpha";
        _service.Select(room);

        var menu = CatalogueMenuBuilder.BuildContextMenu(_catalogue!, _service.Session, ContextTargets.Room);
        var setRegion = menu.Items.OfType<MenuItem>().First(m => Plain(m.Header) == "Set Region");
        setRegion.IsEnabled.ShouldBeTrue();
        setRegion.Items.OfType<MenuItem>().Select(m => Plain(m.Header))
            .ShouldBe(new[] { Region.DefaultRegion, "Alpha", "Zebra" });
        setRegion.Items.OfType<MenuItem>().Where(m => m.IsChecked).Select(m => Plain(m.Header))
            .ShouldBe(new[] { "Alpha" });
    }

    [AvaloniaTest]
    public void SetRegionSubmenu_Click_SetsRegion()
    {
        var room = _service!.AddRoom(new Vector(0, 0));
        _service.Select(room);

        var menu = CatalogueMenuBuilder.BuildContextMenu(_catalogue!, _service.Session, ContextTargets.Room);
        var setRegion = menu.Items.OfType<MenuItem>().First(m => Plain(m.Header) == "Set Region");
        var zebra = setRegion.Items.OfType<MenuItem>().First(m => Plain(m.Header) == "Zebra");
        zebra.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));

        room.Region.ShouldBe("Zebra");
    }

    [AvaloniaTest]
    public void Connection_And_Empty_Menus_HaveNoSetRegion()
    {
        foreach (var target in new[] { ContextTargets.Connection, ContextTargets.Label, ContextTargets.Empty })
        {
            var menu = CatalogueMenuBuilder.BuildContextMenu(_catalogue!, _service!.Session, target);
            menu.Items.OfType<MenuItem>().Select(m => Plain(m.Header)).ShouldNotContain("Set Region");
        }
    }

    [AvaloniaTest]
    public void Paste_IsDisabledWhenClipboardEmpty()
    {
        var menu = CatalogueMenuBuilder.BuildContextMenu(_catalogue!, _service!.Session, ContextTargets.Empty);
        var paste = menu.Items.OfType<MenuItem>().First(m => Plain(m.Header) == "Paste");
        paste.IsEnabled.ShouldBe(_service.Session.CanPaste);
    }
}

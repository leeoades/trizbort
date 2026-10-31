using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Setup;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Editor.Commands;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 066: which commands each context menu target shows, and the dynamic Set Region submenu (I-018).</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class ContextMenuModelTests
{
  private Project _previousProject;
  private Project _project;
  private EditorSession _session;
  private CommandCatalogue _catalogue;
  private List<Region> _savedRegions;

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _session = new EditorSession();
    _catalogue = CommandCatalogue.CreateDefault();
    _savedRegions = Settings.Regions;
    Settings.Regions = new List<Region>
    {
      new Region { RegionName = Region.DefaultRegion },
      new Region { RegionName = "Zebra" },
      new Region { RegionName = "Alpha" }
    };
  }

  [TearDown]
  public void TearDown()
  {
    _session.Dispose();
    Settings.Regions = _savedRegions;
    Project.Current = _previousProject;
    _project.Dispose();
  }

  private List<string> Ids(ContextTargets target)
  {
    return _catalogue.ForContext(target).Select(c => c.Id).ToList();
  }

  [Test]
  public void Room_ShowsRoomItems()
  {
    var ids = Ids(ContextTargets.Room);
    ids.ShouldContain("edit.properties");
    ids.ShouldContain("room.rename");
    ids.ShouldContain("room.setRegion");
    ids.ShouldContain("room.setStart");
    ids.ShouldContain("element.sendToBack");
    ids.ShouldContain("element.bringToFront");
    ids.ShouldContain("project.addRoom");
    ids.ShouldNotContain("connection.reverse");
  }

  [Test]
  public void Connection_ShowsLineItemsButNoRoomItems()
  {
    var ids = Ids(ContextTargets.Connection);
    ids.ShouldContain("edit.properties");
    ids.ShouldContain("connection.reverse");
    ids.ShouldContain("connection.toggleDotted");
    ids.ShouldNotContain("room.rename");
    ids.ShouldNotContain("room.setRegion");
    ids.ShouldNotContain("element.sendToBack");
  }

  [Test]
  public void Label_ShowsArrangeButNoRoomOrLineItems()
  {
    var ids = Ids(ContextTargets.Label);
    ids.ShouldContain("edit.properties");
    ids.ShouldContain("element.sendToBack");
    ids.ShouldNotContain("room.rename");
    ids.ShouldNotContain("connection.reverse");
  }

  [Test]
  public void Empty_HasNoPropertiesAndNoElementItems()
  {
    var ids = Ids(ContextTargets.Empty);
    ids.ShouldContain("project.addRoom");
    ids.ShouldContain("edit.paste");
    ids.ShouldNotContain("edit.properties");
    ids.ShouldNotContain("edit.delete");
    ids.ShouldNotContain("room.setRegion");
    ids.ShouldNotContain("connection.reverse");
  }

  [Test]
  public void SetRegion_ListsRegionsDefaultFirstWithCurrentTicked()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    a.Region = "Alpha";
    _session.Selection.Set(new Element[] { a });

    var command = _catalogue.Find("room.setRegion");
    command.DynamicChildren.ShouldNotBeNull();
    command.CanExecute(_session).ShouldBeTrue();

    var items = command.DynamicChildren(_session);
    items.Select(i => i.Title).ShouldBe(new[] { Region.DefaultRegion, "Alpha", "Zebra" });
    items.Where(i => i.IsChecked).Select(i => i.Title).ShouldBe(new[] { "Alpha" });
  }

  [Test]
  public void SetRegion_ChoosingItemSetsRegionAsOneUndoStep()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(300, 0));
    _session.Selection.Set(new Element[] { a, b });

    var zebra = _catalogue.Find("room.setRegion").DynamicChildren(_session).First(i => i.Title == "Zebra");
    zebra.Execute(_session);

    a.Region.ShouldBe("Zebra");
    b.Region.ShouldBe("Zebra");

    _session.PerformUndo();
    Project.Current.Elements.OfType<Room>().ShouldAllBe(r => r.Region != "Zebra");
  }
}

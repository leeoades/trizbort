using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Controllers;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Export.Languages;

namespace Trizbort.Core.Tests;

[TestFixture]
[NonParallelizable]
public class MapLabelTests
{
  [Test]
  public void NewLabel_IsNotARoom_AndCanBeSelectedWithoutOutlineOrBackground()
  {
    var project = new Project();
    var label = new MapLabel(project) { Position = new Vector(20, 30) };
    project.Elements.Add(label);

    project.Elements.OfType<Room>().ShouldBeEmpty();
    label.BorderStyle.ShouldBe(BorderDashStyle.None);
    label.HasBackground.ShouldBeFalse();
    label.Distance(new Vector(25, 35), false).ShouldBe(0);
    label.Intersects(new Rect(20, 30, 10, 10)).ShouldBeTrue();
    label.UnionBoundsWith(Rect.Empty, false).ShouldBe(label.InnerBounds);
    label.PortList.Count.ShouldBe(16);
    label.PortList.All(port => port.Owner == label).ShouldBeTrue();
  }

  [Test]
  public void StyleAndGeometryChanges_RaiseChanged()
  {
    var label = new MapLabel(new Project());
    var changes = 0;
    label.Changed += (_, __) => changes++;
    label.Text = "Notes";
    label.Position = new Vector(10, 20);
    label.Size = new Vector(120, 70);
    label.Shape = RoomShape.Ellipse;
    label.BorderStyle = BorderDashStyle.Dash;
    label.HasBackground = true;
    label.TextColor = Color.Blue;
    label.BorderColor = Color.Red;
    label.BackgroundColor = Color.Yellow;
    changes.ShouldBe(9);
    label.Text = "Notes";
    changes.ShouldBe(9);
  }

  [Test]
  public void Connection_RemainsDockedWhenLabelMovesOrResizes_AndIsNotARoomExit()
  {
    var project = new Project();
    var room = new Room(project);
    project.Elements.Add(room);
    var label = new MapLabel(project);
    project.Elements.Add(label);
    var connection = new Connection(
      project,
      new Vertex(room.PortAt(CompassPoint.East)),
      new Vertex(label.PortAt(CompassPoint.West)));
    project.Elements.Add(connection);

    connection.IsDangling.ShouldBeFalse();
    connection.GetSourceRoom().ShouldBeSameAs(room);
    connection.GetTargetRoom().ShouldBeNull();
    label.Position = new Vector(300, 40);
    label.Size = new Vector(120, 80);
    connection.VertexList[1].Position.ShouldBe(new Vector(300, 80));
    connection.ConnectedRoomToRotate(false).ShouldBe(0);
    project.Elements.Remove(label);
    project.Elements.ShouldNotContain(connection);
  }

  [Test]
  public void SaveAndLoad_PreservesLabelsAndDocking_WhileLegacyTagsContainOnlyRoomsAndOrdinaryLines()
  {
    var project = new Project();
    var room = new Room(project) { Name = "Room" };
    project.Elements.Add(room);
    var label = new MapLabel(project) {
      Text = "Map notes\n<another map>", Position = new Vector(210.5f, 45.25f), Size = new Vector(150, 90),
      Shape = RoomShape.Octagonal, BorderStyle = BorderDashStyle.DashDot, HasBackground = true,
      TextColor = Color.Blue, BorderColor = Color.Red, BackgroundColor = Color.Yellow, ZOrder = 4
    };
    project.Elements.Add(label);
    var labelLine = new Connection(
      project,
      new Vertex(room.PortAt(CompassPoint.East)),
      new Vertex(label.PortAt(CompassPoint.West)));
    labelLine.SetCurveWaypoint(CurveWaypoint.Middle, new Vector(180, 90));
    project.Elements.Add(labelLine);
    var ordinaryLine = new Connection(
      project,
      new Vertex(room.PortAt(CompassPoint.North)),
      new Vertex(new Vector(0, -100)));
    project.Elements.Add(ordinaryLine);
    var path = Path.GetTempFileName();
    var loaded = new Project();
    try
    {
      new LegacyMapFileEngine(project).Save(path).ShouldBeTrue();
      var document = new XmlDocument();
      document.Load(path);
      document.SelectNodes("/trizbort/map/room").Count.ShouldBe(1);
      document.SelectNodes("/trizbort/map/line").Count.ShouldBe(1);
      document.SelectNodes("/trizbort/map/label").Count.ShouldBe(1);
      document.SelectNodes("/trizbort/map/labelLine").Count.ShouldBe(1);
      document.SelectSingleNode("/trizbort/map/line/dock").Attributes["id"].Value.ShouldBe(room.Id.ToString());

      new LegacyMapFileEngine(loaded).Load(path).ShouldBeTrue();
      var loadedLabel = loaded.Elements.OfType<MapLabel>().Single();
      AssertLabelEqual(loadedLabel, label);
      loadedLabel.Position.ShouldBe(label.Position);
      loadedLabel.Id.ShouldBe(label.Id);
      var loadedLine = loaded.Elements.OfType<Connection>().Single(line => line.Id == labelLine.Id);
      loadedLine.VertexList[1].Port.Owner.ShouldBeSameAs(loadedLabel);
      loadedLine.GetCurveWaypoint(CurveWaypoint.Middle).ShouldBe(new Vector(180, 90));
      loadedLine.IsDangling.ShouldBeFalse();
    }
    finally
    {
      Project.FileWatcher.StopWatcher();
      loaded.Dispose();
      File.Delete(path);
    }
  }

  [Test]
  public void ClipboardDto_RoundTripsLabelStyleAndDockPositions_AndAcceptsOldClipboardData()
  {
    var project = new Project();
    var label = new MapLabel(project) {
      Text = "Note", Position = new Vector(300, 40), Size = new Vector(120, 80), Shape = RoomShape.Ellipse,
      BorderStyle = BorderDashStyle.Dot, HasBackground = true, TextColor = Color.Green,
      BorderColor = Color.Purple, BackgroundColor = Color.Yellow, ZOrder = 2
    };
    project.Elements.Add(label);
    var connection = new Connection(
      project,
      new Vertex(label.PortAt(CompassPoint.West)),
      new Vertex(new Vector(10, 20)));
    var controller = new CopyController();
    var copy = controller.CreateCopyObject(new Element[] { label, connection });
    var loaded = JsonConvert.DeserializeObject<CopyController.CopyObject>(JsonConvert.SerializeObject(copy));
    var pasted = new MapLabel(project);
    controller.SetLabel(pasted, loaded.Labels.Single());

    AssertLabelEqual(pasted, label);
    loaded.Connections.Single().VertextList[0].OwnerId.ShouldBe(label.Id);
    loaded.Connections.Single().VertextList[0].Position.ShouldBe(connection.VertexList[0].Position);
    JsonConvert.DeserializeObject<CopyController.CopyObject>("{\"Rooms\":[],\"Connections\":[]}").Labels
               .ShouldBeEmpty();
  }

  [Test]
  public void LanguageExport_DoesNotTurnLabelsOrTheirConnectionsIntoRoomsOrExits()
  {
    var previous = Project.Current;
    var project = new Project();
    Project.Current = project;
    try
    {
      var room = new Room(project) { Name = "Observatory" };
      project.Elements.Add(room);
      string expected;
      using (var exporter = new Inform7Exporter())
      {
        expected = exporter.Export();
      }

      var label = new MapLabel(project) { Text = "AnnotationOnly" };
      project.Elements.Add(label);
      project.Elements.Add(
        new Connection(
          project,
          new Vertex(room.PortAt(CompassPoint.East)),
          new Vertex(label.PortAt(CompassPoint.West))));
      using (var exporter = new Inform7Exporter())
      {
        exporter.Export().ShouldBe(expected);
      }
    }
    finally
    {
      Project.Current = previous;
    }
  }

  private static void AssertLabelEqual(MapLabel actual, MapLabel expected)
  {
    actual.Text.ShouldBe(expected.Text);
    actual.Size.ShouldBe(expected.Size);
    actual.Shape.ShouldBe(expected.Shape);
    actual.BorderStyle.ShouldBe(expected.BorderStyle);
    actual.HasBackground.ShouldBe(expected.HasBackground);
    actual.TextColor.ToArgb().ShouldBe(expected.TextColor.ToArgb());
    actual.BorderColor.ToArgb().ShouldBe(expected.BorderColor.ToArgb());
    actual.BackgroundColor.ToArgb().ShouldBe(expected.BackgroundColor.ToArgb());
    actual.ZOrder.ShouldBe(expected.ZOrder);
  }
}
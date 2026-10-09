using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Newtonsoft.Json;
using NUnit.Framework;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Controllers;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Export;
using Trizbort.UI;
using Trizbort.UI.Controls;

namespace Trizbort.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public class MapLabelTests {
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
    try {
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
    finally {
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

  [TestCase(RoomShape.SquareCorners)]
  [TestCase(RoomShape.RoundedCorners)]
  [TestCase(RoomShape.Ellipse)]
  [TestCase(RoomShape.Octagonal)]
  public void Rendering_BackgroundAndOutlineAreIndependent_AndSelectionShowsInvisibleLabel(RoomShape shape)
  {
    var label = new MapLabel(new Project()) {
      Text = "", Position = new Vector(20, 20), Size = new Vector(100, 60), Shape = shape,
      BackgroundColor = Color.Yellow, BorderColor = Color.Red
    };
    using (var bitmap = Render(label, false)) {
      CountNonWhitePixels(bitmap).ShouldBe(0);
    }

    label.HasBackground = true;
    using (var bitmap = Render(label, false)) {
      bitmap.GetPixel(70, 50).ToArgb().ShouldBe(Color.Yellow.ToArgb());
    }

    label.HasBackground = false;
    label.BorderStyle = BorderDashStyle.Solid;
    using (var bitmap = Render(label, false)) {
      CountNonWhitePixels(bitmap).ShouldBeGreaterThan(0);
      bitmap.GetPixel(70, 50).ToArgb().ShouldBe(Color.White.ToArgb());
    }

    label.BorderStyle = BorderDashStyle.None;
    using (var bitmap = Render(label, true)) {
      CountNonWhitePixels(bitmap).ShouldBeGreaterThan(0);
    }
  }

  [Test]
  public void TextOnlyLabel_RendersText_AndCanBeExportedToPdf()
  {
    var label = new MapLabel(new Project())
      { Text = "Notes", Position = new Vector(20, 20), Size = new Vector(100, 60) };
    using (var bitmap = Render(label, false)) {
      CountNonWhitePixels(bitmap).ShouldBeGreaterThan(0);
      bitmap.GetPixel(20, 20).ToArgb().ShouldBe(Color.White.ToArgb());
    }

    using (var document = new PdfDocument()) {
      using (var stream = new MemoryStream()) {
        var page = document.AddPage();
        using (var graphics = XGraphics.FromPdfPage(page)) {
          using (var palette = new Palette()) {
            label.Draw(graphics, palette, new DrawingContext(1));
          }
        }

        document.Save(stream);
        stream.Length.ShouldBeGreaterThan(500);
      }
    }
  }

  [Test]
  public void LanguageExport_DoesNotTurnLabelsOrTheirConnectionsIntoRoomsOrExits()
  {
    var previous = Project.Current;
    var project = new Project();
    Project.Current = project;
    try {
      var room = new Room(project) { Name = "Observatory" };
      project.Elements.Add(room);
      var type = typeof(MapLabel).Assembly.GetType("Trizbort.Export.Languages.Inform7Exporter", true);
      string expected;
      using (var exporter = (CodeExporter)Activator.CreateInstance(type)) {
        expected = exporter.Export();
      }

      var label = new MapLabel(project) { Text = "AnnotationOnly" };
      project.Elements.Add(label);
      project.Elements.Add(
        new Connection(
          project,
          new Vertex(room.PortAt(CompassPoint.East)),
          new Vertex(label.PortAt(CompassPoint.West))));
      using (var exporter = (CodeExporter)Activator.CreateInstance(type)) {
        exporter.Export().ShouldBe(expected);
      }
    }
    finally {
      Project.Current = previous;
    }
  }

  [Test]
  public void Canvas_AddSelectResizeAndPaste_SupportLabelsAndTheirConnections()
  {
    var previous = Project.Current;
    var project = new Project();
    Project.Current = project;
    try {
      using var canvas = new Canvas { Size = new Size(600, 400) };
      var label = canvas.AddLabel(false, false);
      canvas.SelectedElement.ShouldBeSameAs(label);
      var handles = (ICollection)typeof(Canvas).GetField("_handles", BindingFlags.Instance | BindingFlags.NonPublic)
                                               .GetValue(canvas);
      handles.Count.ShouldBe(8);
      var width = label.Width;
      typeof(Canvas).GetMethod("ResizeRoom", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(canvas, new object[] { Keys.Right });
      label.Width.ShouldBeGreaterThan(width);
      project.IsDirty = false;
      label.Text = "Changed";
      project.IsDirty.ShouldBeTrue();
      canvas.SelectedElement = null;
      canvas.HoverElement = label;
      var ports = (ICollection)typeof(Canvas).GetField("_ports", BindingFlags.Instance | BindingFlags.NonPublic)
                                             .GetValue(canvas);
      ports.Count.ShouldBe(16);

      var room = canvas.AddRoom(false, false, false);
      var connection = new Connection(
        project,
        new Vertex(room.PortAt(CompassPoint.East)),
        new Vertex(label.PortAt(CompassPoint.West)));
      project.Elements.Add(connection);
      var controller = new CopyController();
      var copy = controller.CreateCopyObject(new Element[] { label, room, connection });
      typeof(Canvas).GetMethod("PasteRooms", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(canvas, new object[] { false, copy, controller });
      canvas.SelectedElements.Count.ShouldBe(3);
      handles.Count.ShouldBe(0);
      var pasted = canvas.SelectedElements.OfType<MapLabel>().Single();
      pasted.Text.ShouldBe("Changed");
      var pastedLine = canvas.SelectedElements.OfType<Connection>().Single();
      pastedLine.VertexList[1].Port.Owner.ShouldBeSameAs(pasted);
      pastedLine.VertexList[0].Port.Owner.ShouldBeSameAs(canvas.SelectedElements.OfType<Room>().Single());

      canvas.SelectedElement = connection;
      var inserted = canvas.AddRoom(false, true, false);
      project.Elements.ShouldNotContain(connection);
      var splitLines = project.Elements.OfType<Connection>()
                              .Where(line => line.VertexList.Any(vertex => vertex.Port?.Owner == inserted)).ToList();
      splitLines.Count.ShouldBe(2);
      splitLines.Any(line => line.VertexList.Any(vertex => vertex.Port?.Owner == label)).ShouldBeTrue();
      splitLines.All(line => !line.IsDangling).ShouldBeTrue();
    }
    finally {
      Project.Current = previous;
    }
  }

  [TestCase(false)]
  [TestCase(true)]
  public void Dialog_OnlyAppliesChangesOnOK(bool accept)
  {
    var label = new MapLabel(new Project()) { Text = "Original" };
    using (var dialog = new LabelPropertiesDialog(label)) {
      dialog.Shown += (_, __) => dialog.BeginInvoke(() => {
        ((TextBox)dialog.Controls.Find("labelText", true)[0]).Text = "Updated";
        ((CheckBox)dialog.Controls.Find("outlineEnabled", true)[0]).Checked = true;
        ((ComboBox)dialog.Controls.Find("outlineShape", true)[0]).SelectedItem = RoomShape.Ellipse;
        ((ComboBox)dialog.Controls.Find("outlineStyle", true)[0]).SelectedItem = BorderDashStyle.Dash;
        ((CheckBox)dialog.Controls.Find("backgroundEnabled", true)[0]).Checked = true;
        ((Button)(accept ? dialog.AcceptButton : dialog.CancelButton)).PerformClick();
      });
      TestDialog.Show(dialog);
    }

    label.Text.ShouldBe(accept ? "Updated" : "Original");
    label.Shape.ShouldBe(accept ? RoomShape.Ellipse : RoomShape.SquareCorners);
    label.BorderStyle.ShouldBe(accept ? BorderDashStyle.Dash : BorderDashStyle.None);
    label.HasBackground.ShouldBe(accept);
  }

  [Test]
  public void Dialog_GroupsVisibilityWithRelatedControls_AndRetainsStyleWhenToggled()
  {
    var label = new MapLabel(new Project()) { BorderStyle = BorderDashStyle.Dash };
    using var dialog = new LabelPropertiesDialog(label);
    var outlineEnabled = (CheckBox)dialog.Controls.Find("outlineEnabled", true)[0];
    var backgroundEnabled = (CheckBox)dialog.Controls.Find("backgroundEnabled", true)[0];
    var shape = (ComboBox)dialog.Controls.Find("outlineShape", true)[0];
    var style = (ComboBox)dialog.Controls.Find("outlineStyle", true)[0];
    var outlineColor = dialog.Controls.Find("outlineColor", true)[0];
    var backgroundColor = dialog.Controls.Find("backgroundColor", true)[0];
    outlineEnabled.Parent.ShouldBeSameAs(shape.Parent);
    outlineEnabled.Parent.ShouldBeSameAs(style.Parent);
    outlineEnabled.Parent.ShouldBeSameAs(outlineColor.Parent);
    backgroundEnabled.Parent.ShouldBeSameAs(backgroundColor.Parent);
    outlineEnabled.Parent.Parent.ShouldBeOfType<GroupBox>();
    backgroundEnabled.Parent.Parent.ShouldBeOfType<GroupBox>();
    style.Items.Contains(BorderDashStyle.None).ShouldBeFalse();
    style.Enabled.ShouldBeTrue();
    outlineColor.Enabled.ShouldBeTrue();
    backgroundColor.Enabled.ShouldBeFalse();

    outlineEnabled.Checked = false;
    style.Enabled.ShouldBeFalse();
    outlineColor.Enabled.ShouldBeFalse();
    shape.Enabled.ShouldBeFalse();
    backgroundEnabled.Checked = true;
    backgroundColor.Enabled.ShouldBeTrue();
    shape.Enabled.ShouldBeTrue();
    outlineEnabled.Checked = true;
    style.Enabled.ShouldBeTrue();
    outlineColor.Enabled.ShouldBeTrue();
    style.SelectedItem.ShouldBe(BorderDashStyle.Dash);
    backgroundEnabled.Checked = false;
    backgroundColor.Enabled.ShouldBeFalse();
    shape.Enabled.ShouldBeTrue();
  }

  [Test]
  public void Dialog_DisablingOutlineSavesNoneWithoutChangingBackgroundOrShape()
  {
    var label = new MapLabel(new Project()) {
      BorderStyle = BorderDashStyle.Dot, Shape = RoomShape.Ellipse, HasBackground = true
    };
    using (var dialog = new LabelPropertiesDialog(label)) {
      dialog.Shown += (_, __) => dialog.BeginInvoke(() => {
        ((CheckBox)dialog.Controls.Find("outlineEnabled", true)[0]).Checked = false;
        ((Button)dialog.AcceptButton).PerformClick();
      });
      TestDialog.Show(dialog);
    }

    label.BorderStyle.ShouldBe(BorderDashStyle.None);
    label.HasBackground.ShouldBeTrue();
    label.Shape.ShouldBe(RoomShape.Ellipse);
  }

  private static Bitmap Render(MapLabel label, bool selected)
  {
    var bitmap = new Bitmap(160, 110);
    using var native = Graphics.FromImage(bitmap);
    using var graphics = XGraphics.FromGraphics(native, new XSize(bitmap.Width, bitmap.Height));
    using var palette = new Palette();
    native.Clear(Color.White);
    label.Draw(graphics, palette, new DrawingContext(1) { Selected = selected });
    return bitmap;
  }

  private static int CountNonWhitePixels(Bitmap bitmap)
  {
    var count = 0;
    for (var x = 0; x < bitmap.Width; x++)
      for (var y = 0; y < bitmap.Height; y++)
        if (bitmap.GetPixel(x, y).ToArgb() != Color.White.ToArgb())
          count++;
    return count;
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
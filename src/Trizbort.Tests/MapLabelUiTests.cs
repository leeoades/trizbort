using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Controllers;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.UI;
using Trizbort.UI.Controls;

namespace Trizbort.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public class MapLabelUiTests
{
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
    using (var bitmap = Render(label, false))
    {
      CountNonWhitePixels(bitmap).ShouldBe(0);
    }

    label.HasBackground = true;
    using (var bitmap = Render(label, false))
    {
      bitmap.GetPixel(70, 50).ToArgb().ShouldBe(Color.Yellow.ToArgb());
    }

    label.HasBackground = false;
    label.BorderStyle = BorderDashStyle.Solid;
    using (var bitmap = Render(label, false))
    {
      CountNonWhitePixels(bitmap).ShouldBeGreaterThan(0);
      bitmap.GetPixel(70, 50).ToArgb().ShouldBe(Color.White.ToArgb());
    }

    label.BorderStyle = BorderDashStyle.None;
    using (var bitmap = Render(label, true))
    {
      CountNonWhitePixels(bitmap).ShouldBeGreaterThan(0);
    }
  }

  [Test]
  public void TextOnlyLabel_RendersText_AndCanBeExportedToPdf()
  {
    var label = new MapLabel(new Project())
      { Text = "Notes", Position = new Vector(20, 20), Size = new Vector(100, 60) };
    using (var bitmap = Render(label, false))
    {
      CountNonWhitePixels(bitmap).ShouldBeGreaterThan(0);
      bitmap.GetPixel(20, 20).ToArgb().ShouldBe(Color.White.ToArgb());
    }

    using (var document = new PdfDocument())
    {
      using (var stream = new MemoryStream())
      {
        var page = document.AddPage();
        using (var graphics = XGraphics.FromPdfPage(page))
        {
          using (var palette = new Palette())
          {
            label.Draw(graphics, palette, new DrawingContext(1));
          }
        }

        document.Save(stream);
        stream.Length.ShouldBeGreaterThan(500);
      }
    }
  }

  [Test]
  public void Canvas_AddSelectResizeAndPaste_SupportLabelsAndTheirConnections()
  {
    var previous = Project.Current;
    var project = new Project();
    Project.Current = project;
    try
    {
      using var canvas = new Canvas { Size = new Size(600, 400) };
      var label = canvas.AddLabel(false, false);
      canvas.SelectedElement.ShouldBeSameAs(label);
      canvas.ResizeHandleCount.ShouldBe(8);
      var width = label.Width;
      canvas.ResizeRoom(Keys.Right);
      label.Width.ShouldBeGreaterThan(width);
      project.IsDirty = false;
      label.Text = "Changed";
      project.IsDirty.ShouldBeTrue();
      canvas.SelectedElement = null;
      canvas.HoverElement = label;
      canvas.PortCount.ShouldBe(16);

      var room = canvas.AddRoom(false, false, false);
      var connection = new Connection(
        project,
        new Vertex(room.PortAt(CompassPoint.East)),
        new Vertex(label.PortAt(CompassPoint.West)));
      project.Elements.Add(connection);
      var controller = new CopyController();
      var copy = controller.CreateCopyObject(new Element[] { label, room, connection });
      canvas.PasteRooms(false, copy, controller);
      canvas.SelectedElements.Count.ShouldBe(3);
      canvas.ResizeHandleCount.ShouldBe(0);
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
    finally
    {
      Project.Current = previous;
    }
  }

  [TestCase(false)]
  [TestCase(true)]
  public void Dialog_OnlyAppliesChangesOnOK(bool accept)
  {
    var label = new MapLabel(new Project()) { Text = "Original" };
    using (var dialog = new LabelPropertiesDialog(label))
    {
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
    using (var dialog = new LabelPropertiesDialog(label))
    {
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
}

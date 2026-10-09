using System.Drawing;
using System.Linq;
using NUnit.Framework;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Export;
using Trizbort.Setup;
using Trizbort.UI.Controls;

namespace Trizbort.Tests {
  [TestFixture, Category("Rendering")]
  public class RenderingIntegrationTests : IsolatedProjectTests {
    [TestCase(RoomShape.SquareCorners)]
    [TestCase(RoomShape.RoundedCorners)]
    [TestCase(RoomShape.Ellipse)]
    [TestCase(RoomShape.Octagonal)]
    public void CanvasRendering_ProducesRoomFillAndSelectedHandles_WithoutChangingViewport(RoomShape shape) {
      Settings.IsGridVisible = false;
      Settings.ShowOrigin = false;
      using var canvas = new Canvas {Size = new Size(300, 200)};
      var room = canvas.AddRoom(false, false, false);
      room.Name = "";
      room.Shape = shape;
      room.Position = new Vector(-50, -30);
      room.Size = new Vector(100, 60);
      room.RoomFillColor = Color.Red;
      canvas.Origin = Vector.Zero;
      canvas.ZoomFactor = 1;
      using (var bitmap = Render(canvas, false)) {
        bitmap.GetPixel(150, 100).ToArgb().ShouldBe(Color.Red.ToArgb());
        bitmap.GetPixel(20, 20).ToArgb().ShouldBe(Color.White.ToArgb());
        var handle = new ResizeHandle(CompassPoint.SouthEast, room);
        var point = Point.Round(canvas.CanvasToClient(handle.Position + new Vector(Settings.HandleSize / 2)));
        bitmap.GetPixel(point.X, point.Y).ToArgb().ShouldNotBe(Color.Red.ToArgb());
      }
      var origin = canvas.Viewport.Center;
      var zoom = canvas.ZoomFactor;
      using (var bitmap = Render(canvas, true))
        bitmap.GetPixel(150, 100).ToArgb().ShouldBe(Color.Red.ToArgb());
      canvas.Viewport.Center.ShouldBe(origin);
      canvas.ZoomFactor.ShouldBe(zoom);
    }

    [Test]
    public void PdfExport_ReopensWithCorrectBoundsMetadataAndRoomAnnotations() {
      using var canvas = new Canvas {Size = new Size(600, 400)};
      Project.Current.Title = "Regression map";
      Project.Current.Author = "Test author";
      var room = canvas.AddRoom(false, false, false);
      room.Name = "Observatory";
      room.AddDescription("A bright dome.");
      room.Position = new Vector(-200, -100);
      room.Size = new Vector(120, 80);
      var second = canvas.AddRoom(false, false, false);
      second.Position = new Vector(100, 40);
      second.Shape = RoomShape.Ellipse;
      second.AddDescription("A marble hall.");
      ProjectRegressionTests.Connect(room, second);
      Project.Current.Elements.Add(new MapLabel(Project.Current) {Text = "Map note", Position = new Vector(300, 100)});
      var bounds = canvas.ComputeCanvasBounds(true);
      var origin = canvas.Viewport.Center;
      var zoom = canvas.ZoomFactor;
      MapPdfExporter.Save(canvas, Files.File("map.pdf"));
      canvas.Viewport.Center.ShouldBe(origin);
      canvas.ZoomFactor.ShouldBe(zoom);
      using var pdf = PdfReader.Open(Files.File("map.pdf"), PdfDocumentOpenMode.Import);
      pdf.PageCount.ShouldBe(1);
      pdf.Info.Title.ShouldBe("Regression map");
      pdf.Info.Author.ShouldBe("Test author");
      var page = pdf.Pages[0];
      page.Width.Point.ShouldBe(bounds.Width, .01);
      page.Height.Point.ShouldBe(bounds.Height, .01);
      page.Annotations.Count.ShouldBe(2);
      Enumerable.Range(0, page.Annotations.Count).Select(index => page.Annotations[index].Elements.GetString("/Contents"))
                .OrderBy(value => value).ShouldBe(new[] {"A bright dome.", "A marble hall."});
      foreach (var annotation in Enumerable.Range(0, page.Annotations.Count).Select(index => page.Annotations[index])) {
        annotation.Rectangle.X1.ShouldBeGreaterThanOrEqualTo(0);
        annotation.Rectangle.Y1.ShouldBeGreaterThanOrEqualTo(0);
        annotation.Rectangle.X2.ShouldBeLessThanOrEqualTo(bounds.Width);
        annotation.Rectangle.Y2.ShouldBeLessThanOrEqualTo(bounds.Height);
      }
    }

    [TestCase("17-tall.trizbort")]
    [TestCase("17-wide.trizbort")]
    [TestCase("horizontal-connector-only.trizbort")]
    [TestCase("vertical-connector-only.trizbort")]
    public void LegacyExtentMaps_FitWithFiniteZoomAndCanRenderToImage(string fixture) {
      PersistenceRegressionTests.Load(Project.Current, System.IO.Path.Combine(PersistenceRegressionTests.FixtureDirectory, fixture));
      using var canvas = new Canvas {Size = new Size(600, 400)};
      canvas.ZoomToFit();
      float.IsFinite(canvas.ZoomFactor).ShouldBeTrue();
      canvas.ZoomFactor.ShouldBeGreaterThan(0);
      canvas.ComputeCanvasBounds(true).Width.ShouldBeGreaterThanOrEqualTo(0);
      canvas.ComputeCanvasBounds(true).Height.ShouldBeGreaterThanOrEqualTo(0);
      using (var bitmap = Render(canvas, true)) {
        bitmap.Save(Files.File("map.png"), System.Drawing.Imaging.ImageFormat.Png);
        using (var reopened = Image.FromFile(Files.File("map.png"))) {
          reopened.Width.ShouldBe(600);
          reopened.Height.ShouldBe(400);
        }
      }
      MapPdfExporter.Save(canvas, Files.File("map.pdf"));
      using (var pdf = PdfReader.Open(Files.File("map.pdf"), PdfDocumentOpenMode.Import)) {
        pdf.Pages[0].Width.Point.ShouldBeGreaterThan(0);
        pdf.Pages[0].Height.Point.ShouldBeGreaterThan(0);
      }
    }

    private static Bitmap Render(Canvas canvas, bool final) {
      var bitmap = new Bitmap(canvas.Width, canvas.Height);
      using var native = Graphics.FromImage(bitmap);
      using var graphics = XGraphics.FromGraphics(native, new XSize(bitmap.Width, bitmap.Height));
      native.Clear(Color.White);
      canvas.Draw(graphics, final, bitmap.Width, bitmap.Height);
      return bitmap;
    }
  }
}

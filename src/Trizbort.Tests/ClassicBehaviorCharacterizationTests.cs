using System;
using System.Drawing;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PdfSharp.Drawing;
using Shouldly;
using Trizbort.Core.Tests;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.UI.Controls;

namespace Trizbort.Tests;

[TestFixture]
[Category("Characterization")]
public class ClassicBehaviorCharacterizationTests : IsolatedProjectTests
{
  [Test]
  public void ManualConnectionsFixture_PreservesClassicConnectionSemantics()
  {
    LoadFixture("connections.trizbort");

    var connections = Project.Current.Elements.OfType<Connection>().OrderBy(connection => connection.Id).ToArray();
    connections.Length.ShouldBe(5);
    connections.Count(connection => connection.Flow == ConnectionFlow.OneWay).ShouldBe(2);
    connections.Count(connection => connection.Style == ConnectionStyle.Dashed).ShouldBe(2);

    connections.Single(connection => connection.Id == 4).ConnectionColor.ToArgb().ShouldBe(Color.Lime.ToArgb());
    connections.Single(connection => connection.Id == 4).StartText.ShouldBe("in");
    connections.Single(connection => connection.Id == 4).EndText.ShouldBe("out");

    connections.Single(connection => connection.Id == 5).StartText.ShouldBe("custom start");
    connections.Single(connection => connection.Id == 5).MidText.ShouldBe("custom middle");
    connections.Single(connection => connection.Id == 5).EndText.ShouldBe("custom end");

    connections.Single(connection => connection.Id == 6).ConnectionColor.ToArgb().ShouldBe(Color.Red.ToArgb());
    connections.Single(connection => connection.Id == 7).Style.ShouldBe(ConnectionStyle.Dashed);
    connections.Single(connection => connection.Id == 7).Flow.ShouldBe(ConnectionFlow.OneWay);
  }

  [Test]
  public void ManualFillsFixture_RendersClassicRegionFillAndSecondFillColors()
  {
    LoadFixture("fills-orange-region.trizbort");

    using var bitmap = RenderFinal();

    CountPixelsNear(bitmap, Color.FromArgb(255, 128, 0), 24).ShouldBeGreaterThan(6_000);
    CountPixelsNear(bitmap, Color.Red, 24).ShouldBeGreaterThan(1_000);
  }

  [Test]
  public void ManualLineColorsFixture_RendersClassicConnectionColors()
  {
    LoadFixture("line-colors.trizbort");

    using var bitmap = RenderFinal();

    CountPixelsNear(bitmap, Color.Red, 40).ShouldBeGreaterThan(10);
    CountPixelsNear(bitmap, Color.FromArgb(255, 128, 0), 40).ShouldBeGreaterThan(10);
    CountPixelsNear(bitmap, Color.Blue, 40).ShouldBeGreaterThan(10);
    CountPixelsNear(bitmap, Color.FromArgb(64, 0, 128), 40).ShouldBeGreaterThan(10);
    CountPixelsNear(bitmap, Color.Green, 40).ShouldBeGreaterThan(10);
    CountPixelsNear(bitmap, Color.Gray, 40).ShouldBeGreaterThan(10);
    CountPixelsNear(bitmap, Color.Yellow, 40).ShouldBeGreaterThan(10);
  }

  private static void LoadFixture(string fixture)
  {
    PersistenceRegressionTests.Load(
      Project.Current,
      Path.Combine(PersistenceRegressionTests.FixtureDirectory, fixture));
  }

  private static Bitmap RenderFinal()
  {
    using var canvas = new Canvas { Size = new Size(640, 480) };
    var bitmap = new Bitmap(canvas.Width, canvas.Height);
    using var native = Graphics.FromImage(bitmap);
    using var graphics = XGraphics.FromGraphics(native, new XSize(bitmap.Width, bitmap.Height));
    canvas.Draw(graphics, true, bitmap.Width, bitmap.Height);
    return bitmap;
  }

  private static int CountPixelsNear(Bitmap bitmap, Color expected, int tolerance)
  {
    var count = 0;
    for (var y = 0; y < bitmap.Height; y++)
      for (var x = 0; x < bitmap.Width; x++)
      {
        var actual = bitmap.GetPixel(x, y);
        if (Math.Abs(actual.R - expected.R) <= tolerance &&
            Math.Abs(actual.G - expected.G) <= tolerance &&
            Math.Abs(actual.B - expected.B) <= tolerance)
          count++;
      }

    return count;
  }
}

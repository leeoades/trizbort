using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using PdfSharp.Drawing;
using Trizbort.Core.Tests;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.UI.Controls;

namespace Trizbort.Tests;

[TestFixture]
[Category("Capture")]
public class ClassicVisualCaptureTests : IsolatedProjectTests
{
  private const int CaptureWidth = 1200;
  private const int CaptureHeight = 900;

  [Test]
  public void CaptureClassicManualFixtureScreenshots()
  {
    var root = Environment.GetEnvironmentVariable("TRIZBORT_CAPTURE_OUTPUT");
    if (string.IsNullOrWhiteSpace(root))
      Assert.Ignore("Set TRIZBORT_CAPTURE_OUTPUT to generate classic WinForms visual captures.");

    var output = Path.Combine(root, "classic-winforms");
    Directory.CreateDirectory(output);
    var manifest = new List<string>
    {
      "frontend,state,fixture,width,height,bytes,sha256,path"
    };

    foreach (var fixture in ManualMaps())
    {
      Project.Current = new Project();
      PersistenceRegressionTests.Load(
        Project.Current,
        Path.Combine(PersistenceRegressionTests.FixtureDirectory, fixture));

      using var canvas = new Canvas { Size = new Size(CaptureWidth, CaptureHeight) };
      CaptureState(output, manifest, fixture, "export", canvas, finalRender: true);

      canvas.SelectedElement = null;
      canvas.ZoomToFit();
      CaptureState(output, manifest, fixture, "edit-none", canvas, finalRender: false);

      canvas.SelectedElement = RepresentativeElement();
      canvas.ZoomToFit();
      CaptureState(output, manifest, fixture, "edit-selected", canvas, finalRender: false);

      Project.Current.Dispose();
    }

    File.WriteAllLines(Path.Combine(output, "manifest.csv"), manifest, Encoding.UTF8);
  }

  private static IEnumerable<string> ManualMaps()
  {
    return Directory.GetFiles(PersistenceRegressionTests.FixtureDirectory, "*.trizbort")
                    .Select(Path.GetFileName)
                    .OrderBy(name => name);
  }

  private static Element RepresentativeElement()
  {
    return Project.Current.Elements.OfType<Room>().OrderBy(room => room.Id).Cast<Element>().FirstOrDefault()
           ?? Project.Current.Elements.OrderBy(element => element.Id).FirstOrDefault();
  }

  private static void CaptureState(
    string output,
    ICollection<string> manifest,
    string fixture,
    string state,
    Canvas canvas,
    bool finalRender)
  {
    using var bitmap = Render(canvas, finalRender);
    var fileName = $"{Path.GetFileNameWithoutExtension(fixture)}--{state}.png";
    var path = Path.Combine(output, fileName);
    bitmap.Save(path, ImageFormat.Png);
    var info = new FileInfo(path);
    manifest.Add(
      $"classic-winforms,{state},{Escape(fixture)},{bitmap.Width},{bitmap.Height},{info.Length},{Sha256(path)},{Escape(path)}");
  }

  private static Bitmap Render(Canvas canvas, bool finalRender)
  {
    var bitmap = new Bitmap(canvas.Width, canvas.Height);
    using var native = Graphics.FromImage(bitmap);
    native.Clear(Color.White);
    using var graphics = XGraphics.FromGraphics(native, new XSize(bitmap.Width, bitmap.Height));
    canvas.Draw(graphics, finalRender, bitmap.Width, bitmap.Height);
    return bitmap;
  }

  private static string Sha256(string path)
  {
    using var stream = File.OpenRead(path);
    return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
  }

  private static string Escape(string value)
  {
    return "\"" + value.Replace("\"", "\"\"") + "\"";
  }
}

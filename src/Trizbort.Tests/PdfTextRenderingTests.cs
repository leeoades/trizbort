using System.Drawing;
using System.Text;
using System.Threading;
using NUnit.Framework;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

namespace Trizbort.Tests {
  [TestFixture]
  [Apartment(ApartmentState.STA)]
  public class PdfTextRenderingTests {
    [TestCase("\r\n")]
    [TestCase("\r")]
    [TestCase("\n")]
    public void MultilineText_UsesTheSamePdfLayoutForAllLineEndings(string lineEnding) {
      var content = RenderRoom(lineEnding);

      content.ShouldBe(RenderRoom("\n"));
      content.ShouldNotContain(@"\r");
      content.ShouldContain("(Keys) Tj");
      content.ShouldContain("(Lamp) Tj");
      content.ShouldContain("(Food) Tj");
      content.ShouldContain("(Bottle of water) Tj");
    }

    [Test]
    public void ObjectList_UsesItsConfiguredColourForEveryLine() {
      var content = RenderRoom("\r\n");

      var start = content.IndexOf("(Keys) Tj");
      var end = content.IndexOf("(Bottle of water) Tj", start);
      var textBlockStart = content.LastIndexOf("BT\n", start);
      content.Substring(textBlockStart, start - textBlockStart).ShouldStartWith("BT\n0.502 0.502 0.251 rg\n");
      content.Substring(start, end - start).ShouldNotContain(" rg");
    }

    private static string RenderRoom(string lineEnding) {
      using var project = new Project();
      using var document = new PdfDocument();
      using var palette = new Palette();
      var objects = string.Join(lineEnding, new[] {"Keys", "", "Lamp", "Food", "Bottle of water", ""});
      var room = new Room(project) {
        Name = "Inside" + lineEnding + "building",
        SubTitle = "A" + lineEnding + "B",
        Objects = objects,
        RoomNameColor = Color.Gray,
        RoomObjectTextColor = Color.FromArgb(128, 128, 64),
        Position = new Vector(50, 50)
      };
      var page = document.AddPage();
      using (var graphics = XGraphics.FromPdfPage(page))
        room.Draw(graphics, palette, new DrawingContext(1));
      room.Objects.ShouldBe(objects);
      room.Name.ShouldBe("Inside" + lineEnding + "building");
      return Encoding.ASCII.GetString(page.Contents.Elements.GetDictionary(0).Stream.Value);
    }
  }
}

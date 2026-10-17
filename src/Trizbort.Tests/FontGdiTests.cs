using System;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Setup;

namespace Trizbort.Tests;

[TestFixture]
public class FontGdiTests : IsolatedProjectTests
{
  [Test]
  public void AppliedThemeFonts_UseWorldUnits()
  {
    MapTheme.BuiltInThemes()[1].Apply(false);
    Settings.RoomNameFont.Unit.ShouldBe(GraphicsUnit.World);
    Settings.ObjectFont.Unit.ShouldBe(GraphicsUnit.World);
    Settings.SubtitleFont.Unit.ShouldBe(GraphicsUnit.World);
    Settings.LineFont.Unit.ShouldBe(GraphicsUnit.World);
  }

  [Test]
  public void UnavailableFont_DoesNotPartiallyApplyTheme()
  {
    var theme = MapTheme.BuiltInThemes()[2];
    theme.SubtitleFont.Name = "Trizbort missing font " + Guid.NewGuid();
    var before = JsonConvert.SerializeObject(MapTheme.Capture("Before"));
    Project.Current.IsDirty = false;
    Should.Throw<InvalidDataException>(() => theme.Apply(false));
    JsonConvert.SerializeObject(MapTheme.Capture("Before")).ShouldBe(before);
    Project.Current.IsDirty.ShouldBeFalse();
  }

  [TestCase("yes", "no", FontStyle.Bold)]
  [TestCase("no", "yes", FontStyle.Italic)]
  [TestCase("yes", "yes", FontStyle.Bold | FontStyle.Italic)]
  public void FontScriptCases_GdiFallsBackForUnavailableFamily(string bold, string italic, FontStyle style)
  {
    File.WriteAllText(
      Files.File("font.trizbort"),
      $"<trizbort version=\"1.0\"><settings><fonts><room size=\"18\" bold=\"{bold}\" italic=\"{italic}\">Trizbort-Test-Font-Does-Not-Exist</room></fonts></settings></trizbort>");
    Exception failure = null;
    new LegacyMapFileEngine(
        Project.Current,
        error => failure = error,
        _ => { },
        (message, title) => TestContext.Out.WriteLine(title + ": " + message)).Load(Files.File("font.trizbort"))
                                                                              .ShouldBeTrue(failure?.ToString());
    Settings.RoomNameFont.Style.ShouldBe(style);
    Settings.RoomNameFont.Size.ShouldBe(18);
    Settings.RoomNameFont.Name.ShouldNotBe("Trizbort-Test-Font-Does-Not-Exist");
    Settings.RoomFontMetadata.Name.ShouldBe("Trizbort-Test-Font-Does-Not-Exist");
  }
}

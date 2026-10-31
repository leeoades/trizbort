using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Editor;
using Trizbort.Export;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 107: automap and quick save without a UI.</summary>
[TestFixture]
[Category("Integration")]
public class HeadlessAutomapTests : IsolatedProjectTests
{
  private const string TwoRooms =
    "Example Game\n\nFirst Room\nA small room.\n\n>e\n\nSecond Room\nAnother room.\n";

  private static AutomapSettings Settings()
  {
    var settings = AutomapSettings.Default;
    settings.VerboseTranscript = true;
    settings.AssumeTwoWayConnections = true;
    settings.GuessExits = false;
    return settings;
  }

  private string WriteTranscript(string text)
  {
    var path = Files.File("transcript.txt");
    File.WriteAllText(path, text);
    return path;
  }

  [Test]
  public async Task RunAsync_MapsTranscriptAndLeavesProjectClean()
  {
    Project.Current.IsDirty = true;

    var result = await HeadlessAutomap.RunAsync(
      Project.Current, WriteTranscript(TwoRooms), Settings(), NonInteractiveEditorPrompts.Instance);

    result.Succeeded.ShouldBeTrue();
    Project.Current.Elements.OfType<Room>().Count().ShouldBe(2);
    Project.Current.IsDirty.ShouldBeFalse();
  }

  [Test]
  public async Task RunAsync_NullPrompts_UsesNonInteractiveDefaults()
  {
    var result = await HeadlessAutomap.RunAsync(Project.Current, WriteTranscript(TwoRooms), Settings(), null);

    result.Succeeded.ShouldBeTrue();
    Project.Current.Elements.OfType<Room>().Count().ShouldBe(2);
  }

  [Test]
  public async Task RunAsync_MissingTranscript_ReturnsErrorWithoutThrowing()
  {
    Project.Current.IsDirty = true;

    var result = await HeadlessAutomap.RunAsync(
      Project.Current, Files.File("missing.txt"), Settings(), NonInteractiveEditorPrompts.Instance);

    result.Succeeded.ShouldBeFalse();
    result.Error.ShouldContain("not found");
    Project.Current.Elements.OfType<Room>().ShouldBeEmpty();
  }

  [Test]
  public async Task QuickSave_AfterAutomap_WritesMapAndNamesProject()
  {
    await HeadlessAutomap.RunAsync(Project.Current, WriteTranscript(TwoRooms), Settings(), null);
    var target = Files.File("out.trizbort");

    var problem = HeadlessSave.QuickSave(Project.Current, target);

    problem.ShouldBeNull();
    File.Exists(target).ShouldBeTrue();
    Project.Current.FileName.ShouldBe(target);
    Project.Current.IsDirty.ShouldBeFalse();
  }

  [Test]
  public void QuickSave_MissingFolder_ReportsErrorAndKeepsFileName()
  {
    var problem = HeadlessSave.QuickSave(Project.Current, Files.File("nope/out.trizbort"));

    problem.ShouldContain("Folder not found");
    Project.Current.FileName.ShouldBeEmpty();
  }

  [Test]
  public void QuickSave_OtherExtension_IsSavedAsTrizbort()
  {
    var problem = HeadlessSave.QuickSave(Project.Current, Files.File("out.txt"));

    problem.ShouldBeNull();
    File.Exists(Files.File("out.trizbort")).ShouldBeTrue();
  }
}

using NUnit.Framework;
using Shouldly;
using Trizbort.Editor;

namespace Trizbort.Core.Tests;

[TestFixture]
[Category("Unit")]
public class TitleFormatterTests
{
  private const string AppName = "Trizbort";
  private const string Version = "2.4.0";
  private const string FilePath = @"C:\maps\basics.trizbort";
  private const string UrlFile = "https://example.com/maps/basics.trizbort";

  [TestCase(false, false, "Untitled - Trizbort - 2.4.0")]
  [TestCase(true, false, "Untitled* - Trizbort - 2.4.0")]
  [TestCase(false, true, "Untitled - Trizbort - 2.4.0")]
  [TestCase(true, true, "Untitled* - Trizbort - 2.4.0")]
  public void Unsaved_UsesUntitledWithOptionalDirtyMarker(bool isDirty, bool showFullPath, string expected)
  {
    var title = TitleFormatter.Format("Untitled", string.Empty, isDirty, showFullPath, AppName, Version);

    title.ShouldBe(expected);
  }

  [TestCase(false, "basics - Trizbort - 2.4.0")]
  [TestCase(true, "basics* - Trizbort - 2.4.0")]
  public void Saved_ShowsProjectNameWhenFullPathIsOff(bool isDirty, string expected)
  {
    var title = TitleFormatter.Format("basics", FilePath, isDirty, false, AppName, Version);

    title.ShouldBe(expected);
  }

  [TestCase(false, @"C:\maps\basics.trizbort - Trizbort - 2.4.0")]
  [TestCase(true, @"C:\maps\basics.trizbort* - Trizbort - 2.4.0")]
  public void Saved_ShowsFullPathWhenEnabled(bool isDirty, string expected)
  {
    var title = TitleFormatter.Format("basics", FilePath, isDirty, true, AppName, Version);

    title.ShouldBe(expected);
  }

  [TestCase(false, "https://example.com/maps/basics.trizbort - Trizbort - 2.4.0")]
  [TestCase(true, "https://example.com/maps/basics.trizbort* - Trizbort - 2.4.0")]
  public void UrlFileName_IsShownWhole_WhenFullPathIsEnabled(bool isDirty, string expected)
  {
    var title = TitleFormatter.Format("basics", UrlFile, isDirty, true, AppName, Version);

    title.ShouldBe(expected);
  }

  [Test]
  public void UrlFileName_UsesProjectName_WhenFullPathIsOff()
  {
    var title = TitleFormatter.Format("basics", UrlFile, false, false, AppName, Version);

    title.ShouldBe("basics - Trizbort - 2.4.0");
  }

  [Test]
  public void FullPathEnabled_WithoutFile_StillShowsUntitled()
  {
    var title = TitleFormatter.Format("Untitled", null, true, true, AppName, Version);

    title.ShouldBe("Untitled* - Trizbort - 2.4.0");
  }

  [Test]
  public void EmptyProjectName_WithFile_FallsBackToUntitled()
  {
    var title = TitleFormatter.Format(string.Empty, FilePath, false, false, AppName, Version);

    title.ShouldBe("Untitled - Trizbort - 2.4.0");
  }

  [Test]
  public void BuildMetadata_IsStrippedFromVersion()
  {
    var title = TitleFormatter.Format("basics", FilePath, false, false, AppName, "2.4.0+abc1234");

    title.ShouldBe("basics - Trizbort - 2.4.0");
  }

  [Test]
  public void EmptyVersion_OmitsVersionSegment()
  {
    var title = TitleFormatter.Format("basics", FilePath, true, false, AppName, string.Empty);

    title.ShouldBe("basics* - Trizbort");
  }
}

using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;

namespace Trizbort.Core.Tests;

[TestFixture]
[Category("Unit")]
public class CommandLineParsingTests
{
  [Test]
  public void FileOnly()
  {
    var o = CommandLineParsing.Parse(new[] { "map.trizbort" });
    o.FileName.ShouldBe("map.trizbort");
    o.Debug.ShouldBeFalse();
  }

  [Test]
  public void FlagsOnly()
  {
    var o = CommandLineParsing.Parse(new[] { "--debug", "--config-dir", "/tmp/cfg" });
    o.FileName.ShouldBeNull();
    o.Debug.ShouldBeTrue();
    o.ConfigDir.ShouldBe("/tmp/cfg");
  }

  [Test]
  public void UnknownFlagsAreIgnored()
  {
    var o = CommandLineParsing.Parse(new[] { "--bogus", "map.trizbort", "--debug" });
    o.Debug.ShouldBeTrue();
    o.ShouldNotBeNull();
  }

  [Test]
  public void PathWithSpacesIsKeptWhole()
  {
    var o = CommandLineParsing.Parse(new[] { "C:\\My Maps\\a map.trizbort" });
    o.FileName.ShouldBe("C:\\My Maps\\a map.trizbort");
  }

  [Test]
  public void EmptyArgs()
  {
    CommandLineParsing.Parse(new string[0]).FileName.ShouldBeNull();
    CommandLineParsing.Parse(null).ShouldNotBeNull();
  }

  [Test]
  public void CombinedOptionsStillParse()
  {
    var o = CommandLineParsing.Parse(new[] { "map.trizbort", "--automap", "t.txt", "--exit" });
    o.FileName.ShouldBe("map.trizbort");
    o.Transcript.ShouldBe("t.txt");
    o.Exit.ShouldBeTrue();
  }
}

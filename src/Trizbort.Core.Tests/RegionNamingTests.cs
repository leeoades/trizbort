using NUnit.Framework;
using Shouldly;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

[TestFixture]
[Category("Unit")]
public class RegionNamingTests
{
  [Test]
  public void Sanitize_TrimsAndReplacesDoubleQuotes()
  {
    RegionNaming.Sanitize("  The \"Keep\"  ").ShouldBe("The 'Keep'");
    RegionNaming.Sanitize(null).ShouldBe(string.Empty);
  }

  [TestCase("")]
  [TestCase("   ")]
  public void Validate_RejectsEmpty(string name)
  {
    RegionNaming.Validate(name, new string[0], null).ShouldBe(RegionNaming.EmptyMessage);
  }

  [TestCase("a_b")]
  [TestCase("a:b")]
  public void Validate_RejectsUnderscoreAndColon(string name)
  {
    RegionNaming.Validate(name, new string[0], null).ShouldBe(RegionNaming.InvalidCharacterMessage);
  }

  [Test]
  public void Validate_RejectsDuplicateIgnoringCase()
  {
    RegionNaming.Validate("forest", new[] { "Forest" }, null).ShouldBe(RegionNaming.DuplicateMessage("forest"));
  }

  [Test]
  public void Validate_AllowsOwnNameAndNewNames()
  {
    RegionNaming.Validate("FOREST", new[] { "Forest" }, "Forest").ShouldBe(string.Empty);
    RegionNaming.Validate("Castle", new[] { "Forest" }, "Forest").ShouldBe(string.Empty);
  }
}

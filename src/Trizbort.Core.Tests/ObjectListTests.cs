using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Misc;

namespace Trizbort.Tests;

[TestFixture]
public class ObjectListTests
{
  [Test]
  public void Parse_BulletsDenoteContainment()
  {
    var items = ObjectList.Parse("Chest [c]\r\n- Pouch\r\n-- Gem\r\n- Coin\r\nLamp");

    items.Count.ShouldBe(5);
    items[0].Name.ShouldBe("Chest");
    items[0].PropString.ShouldBe("c");
    items[0].ParentIndex.ShouldBe(-1);
    items[1].Name.ShouldBe("Pouch");
    items[1].ParentIndex.ShouldBe(0);
    items[1].Depth.ShouldBe(1);
    items[2].Name.ShouldBe("Gem");
    items[2].ParentIndex.ShouldBe(1);
    items[2].Depth.ShouldBe(2);
    items[3].ParentIndex.ShouldBe(0);
    items[4].ParentIndex.ShouldBe(-1);
  }

  [TestCase("Table\n  Cup")]
  [TestCase("Table\n\tCup")]
  [TestCase("Table\n  - Cup")]
  [TestCase("Table\n* Cup")]
  [TestCase("Table\n\u2022 Cup")]
  public void Parse_IndentationStylesAllDenoteContainment(string text)
  {
    var items = ObjectList.Parse(text);
    items.Count.ShouldBe(2);
    items[1].Name.ShouldBe("Cup");
    items[1].ParentIndex.ShouldBe(0);
  }

  [TestCase("-shaped key", "-shaped key")]
  [TestCase("*star*", "*star*")]
  [TestCase("[s] rock", "rock")]
  public void Parse_LeavesNamesAlone(string line, string expected)
  {
    var items = ObjectList.Parse(line);
    items.Count.ShouldBe(1);
    items[0].Name.ShouldBe(expected);
    items[0].Depth.ShouldBe(0);
  }

  [Test]
  public void FormatForDisplay_WithoutNesting_OnlyStripsProperties()
  {
    ObjectList.FormatForDisplay("Lamp [s]\r\nRock").ShouldBe("Lamp \r\nRock");
  }

  [Test]
  public void FormatForDisplay_IndentsContainedObjectsWithBullets()
  {
    ObjectList.FormatForDisplay("Chest [c]\r\n- Pouch\r\n-- Gem")
              .ShouldBe("Chest\r\n  \u2022 Pouch\r\n      \u2022 Gem");
  }
}
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Misc;
using Trizbort.UI;

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

  [Test]
  public void ChangeIndent_TabAddsBulletLevel()
  {
    var result = ObjectListEditor.ChangeIndent("Chest\r\nPouch", 7, 0, false);
    result.Text.ShouldBe("Chest\r\n- Pouch");
    result.SelectionStart.ShouldBe(9);

    result = ObjectListEditor.ChangeIndent(result.Text, result.SelectionStart, 0, false);
    result.Text.ShouldBe("Chest\r\n-- Pouch");
  }

  [Test]
  public void ChangeIndent_ShiftTabRemovesBulletLevel()
  {
    ObjectListEditor.ChangeIndent("Chest\r\n-- Pouch", 10, 0, true).Text.ShouldBe("Chest\r\n- Pouch");
    ObjectListEditor.ChangeIndent("Chest\r\n- Pouch", 10, 0, true).Text.ShouldBe("Chest\r\nPouch");
    ObjectListEditor.ChangeIndent("Chest\r\n\tPouch", 10, 0, true).Text.ShouldBe("Chest\r\nPouch");
    ObjectListEditor.ChangeIndent("Chest", 2, 0, true).Text.ShouldBe("Chest");
  }

  [Test]
  public void ChangeIndent_AppliesToEverySelectedLine()
  {
    var result = ObjectListEditor.ChangeIndent("Chest\r\nPouch\r\nGem", 7, 10, false);
    result.Text.ShouldBe("Chest\r\n- Pouch\r\n- Gem");
    result.SelectionStart.ShouldBe(7);
    result.SelectionLength.ShouldBe(14);
  }

  [Test]
  public void NewLine_ContinuesIndentation()
  {
    var result = ObjectListEditor.NewLine("Chest\r\n- Pouch", 14, 0);
    result.ShouldNotBeNull();
    result.Value.Text.ShouldBe("Chest\r\n- Pouch\r\n- ");
    result.Value.SelectionStart.ShouldBe(18);
  }

  [Test]
  public void NewLine_OnEmptyBulletOutdents()
  {
    var result = ObjectListEditor.NewLine("Chest\r\n- ", 9, 0);
    result.ShouldNotBeNull();
    result.Value.Text.ShouldBe("Chest\r\n");
  }

  [Test]
  public void NewLine_WithoutIndentationUsesDefault()
  {
    ObjectListEditor.NewLine("Chest", 5, 0).ShouldBeNull();
  }
}
using NUnit.Framework;
using Shouldly;
using Trizbort.UI;

namespace Trizbort.Tests;

[TestFixture]
public class ObjectListEditorTests
{
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

using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

namespace Trizbort.Core.Tests;

/// <summary>
///   Lee, 22 Nov 2026: in the label editor, select some text and press B for bold or I for italic; Insert Table puts
///   in a sample table; Add Link lists the rooms and objects, alphabetically, narrowed as you type.
/// </summary>
[TestFixture]
[Category("Unit")]
public class MarkdownEditingTests : IsolatedProjectTests
{
  [Test]
  public void BoldRoundTheSelection_AndOffAgain()
  {
    var bold = MarkdownEditing.Wrap("the gold nugget", 4, 8, "**");
    bold.Text.ShouldBe("the **gold** nugget");
    bold.SelectionStart.ShouldBe(6);
    bold.SelectionEnd.ShouldBe(10);

    var plain = MarkdownEditing.Wrap(bold.Text, bold.SelectionStart, bold.SelectionEnd, "**");
    plain.Text.ShouldBe("the gold nugget");
    (plain.SelectionStart, plain.SelectionEnd).ShouldBe((4, 8));
  }

  [Test]
  public void ItalicOfAWordPickedWithItsSpace_LeavesTheSpaceOutside()
  {
    MarkdownEditing.Wrap("the gold nugget", 4, 9, "*").Text.ShouldBe("the *gold* nugget");
  }

  [Test]
  public void ItalicOfABoldWord_IsBoldAndItalic_NotBoldTakenOff()
  {
    MarkdownEditing.Wrap("the **gold** nugget", 6, 10, "*").Text.ShouldBe("the ***gold*** nugget");
  }

  [Test]
  public void BoldWithNothingSelected_LeavesTheCaretBetweenTheMarks()
  {
    var edit = MarkdownEditing.Wrap("Treasure: ", 10, 10, "**");
    edit.Text.ShouldBe("Treasure: ****");
    edit.SelectionStart.ShouldBe(12);
    edit.SelectionEnd.ShouldBe(12);
  }

  [Test]
  public void ListsAndHeadings_StartEachSelectedLine_AndComeOffAgain()
  {
    var text = "Lamp\nRod\nBird";
    var bullets = MarkdownEditing.Lines(text, 0, text.Length, MarkdownEditing.LineKind.Bullet);
    bullets.Text.ShouldBe("- Lamp\n- Rod\n- Bird");

    var numbered = MarkdownEditing.Lines(bullets.Text, 0, bullets.Text.Length, MarkdownEditing.LineKind.Numbered);
    numbered.Text.ShouldBe("1. Lamp\n2. Rod\n3. Bird");

    MarkdownEditing.Lines(numbered.Text, 0, numbered.Text.Length, MarkdownEditing.LineKind.Numbered).Text
      .ShouldBe(text);

    // the caret on the middle line: just that line
    MarkdownEditing.Lines(text, 6, 6, MarkdownEditing.LineKind.Heading).Text.ShouldBe("Lamp\n## Rod\nBird");
  }

  [Test]
  public void InsertTable_GoesOnLinesOfItsOwn_Selected()
  {
    var edit = MarkdownEditing.Block("Treasures", 9, 9, MarkdownEditing.SampleTable, true);
    edit.Text.ShouldBe("Treasures\n" + MarkdownEditing.SampleTable);
    edit.Text.Substring(edit.SelectionStart, edit.SelectionEnd - edit.SelectionStart).ShouldBe(MarkdownEditing.SampleTable);

    var table = LabelMarkdown.Parse(edit.Text).Single(block => block.Kind == MarkdownBlockKind.Table);
    table.HasHeader.ShouldBeTrue();
    table.Rows.Count.ShouldBe(4);
  }

  [Test]
  public void LinkChoices_RoomsAndObjects_Alphabetical_NarrowedAsYouType()
  {
    var project = Project.Current;
    var hall = new Room(project) { Name = "Hall of Mists" };
    var building = new Room(project) { Name = "Inside Building", Objects = "set of keys\nbrass lantern\nfood" };
    var mazeA = new Room(project) { Name = "Maze" };
    var mazeB = new Room(project) { Name = "Maze", Objects = "black rod [d]" };
    foreach (var room in new[] { hall, building, mazeA, mazeB }) project.Elements.Add(room);

    var rooms = MarkdownEditing.Rooms(project);
    rooms.Select(choice => choice.Name).ShouldBe(new[] { "Hall of Mists", "Inside Building", "Maze", "Maze" });
    rooms[0].Markdown.ShouldBe("[[Hall of Mists]]");
    // two rooms called Maze: told apart, and linked by id
    rooms[2].Where.ShouldBe("#" + mazeA.Id);
    rooms[3].Markdown.ShouldBe($"[[Maze#{mazeB.Id}]]");

    var objects = MarkdownEditing.Objects(project);
    objects.Select(choice => choice.Name).ShouldBe(new[] { "black rod", "brass lantern", "food", "set of keys" });
    objects[0].Where.ShouldBe("in Maze");
    objects[0].Markdown.ShouldBe($"[[Maze#{mazeB.Id}|black rod]]");
    objects[1].Markdown.ShouldBe("[[Inside Building|brass lantern]]");

    MarkdownEditing.Matching(rooms, "mi").Select(choice => choice.Name).ShouldBe(new[] { "Hall of Mists" });
    MarkdownEditing.Matching(objects, "building").Count.ShouldBe(3);
    MarkdownEditing.Matching(objects, "bra lan").Single().Name.ShouldBe("brass lantern");
  }
}

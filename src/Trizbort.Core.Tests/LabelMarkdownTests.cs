using System.IO;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

namespace Trizbort.Core.Tests;

/// <summary>
///   Lee, 21 Nov 2026: formatted text on the map, with tables and links to rooms ("a table of the treasures and the
///   rooms they're found in, the room names links to the rooms").
/// </summary>
[TestFixture]
[Category("Unit")]
public class LabelMarkdownTests : IsolatedProjectTests
{
  [Test]
  public void ATableOfTreasures_WithRoomLinks()
  {
    var blocks = LabelMarkdown.Parse(
      "# Treasures\n\n| Treasure | Room |\n|---|---|\n| Golden eggs | [[Giant Room]] |\n| Pearl | [[In Shell Room|the shell]] |");

    blocks.Select(block => block.Kind).ShouldBe(new[]
      { MarkdownBlockKind.Heading, MarkdownBlockKind.Blank, MarkdownBlockKind.Table });
    blocks[0].Level.ShouldBe(1);
    blocks[0].Spans.Single().Text.ShouldBe("Treasures");

    var table = blocks[2];
    table.HasHeader.ShouldBeTrue();
    table.Rows.Count.ShouldBe(3);
    table.Rows[0].Select(cell => cell.Single().Text).ShouldBe(new[] { "Treasure", "Room" });
    var eggs = table.Rows[1][1].Single();
    eggs.Text.ShouldBe("Giant Room");
    eggs.Link.ShouldBe("room:Giant Room");
    // the | inside a room link doesn't end the cell
    var pearl = table.Rows[2][1].Single();
    pearl.Text.ShouldBe("the shell");
    pearl.Link.ShouldBe("room:In Shell Room");
  }

  [Test]
  public void BoldItalicCodeAndWebLinks()
  {
    var spans = LabelMarkdown.Inline("Drop **all** the *treasure* in the `well house`, see [the wiki](https://ifwiki.org)");

    spans.Single(span => span.Text == "all").Bold.ShouldBeTrue();
    spans.Single(span => span.Text == "treasure").Italic.ShouldBeTrue();
    spans.Single(span => span.Text == "well house").Code.ShouldBeTrue();
    var wiki = spans.Single(span => span.Text == "the wiki");
    wiki.Link.ShouldBe("https://ifwiki.org");
    LabelMarkdown.IsWebLink(wiki.Link).ShouldBeTrue();
    string.Concat(spans.Select(span => span.Text))
      .ShouldBe("Drop all the treasure in the well house, see the wiki");
  }

  [Test]
  public void PlainTextIsLeftAlone()
  {
    // snake_case, a sum, and a lone star aren't formatting
    string.Concat(LabelMarkdown.Inline("tb_see 5 * 3 a * b").Select(span => span.Text)).ShouldBe("tb_see 5 * 3 a * b");
  }

  [Test]
  public void ListsAndRules()
  {
    var blocks = LabelMarkdown.Parse("- lamp\n  - batteries\n1. first\n---");

    blocks[0].Kind.ShouldBe(MarkdownBlockKind.Bullet);
    blocks[1].Kind.ShouldBe(MarkdownBlockKind.Bullet);
    blocks[1].Level.ShouldBe(1);
    blocks[2].Kind.ShouldBe(MarkdownBlockKind.Numbered);
    blocks[2].Number.ShouldBe(1);
    blocks[3].Kind.ShouldBe(MarkdownBlockKind.Rule);
  }

  [Test]
  public void ARoomLink_FindsTheRoom_ByIdWhenRoomsShareAName()
  {
    var first = new Room(Project.Current) { Name = "Maze" };
    var second = new Room(Project.Current) { Name = "Maze" };
    Project.Current.Elements.Add(first);
    Project.Current.Elements.Add(second);

    LabelMarkdown.LinkedRoom(Project.Current, "room:maze").ShouldBe(first);
    LabelMarkdown.LinkedRoom(Project.Current, $"room:Maze#{second.Id}").ShouldBe(second);
    LabelMarkdown.LinkedRoom(Project.Current, "room:Nowhere").ShouldBeNull();
    LabelMarkdown.Inline($"[[Maze#{second.Id}]]").Single().Text.ShouldBe("Maze");
    // Copy Room Link: by id, as the name is shared
    LabelMarkdown.RoomLink(Project.Current, second).ShouldBe($"[[Maze#{second.Id}]]");
    var hall = new Room(Project.Current) { Name = "Hall" };
    Project.Current.Elements.Add(hall);
    LabelMarkdown.RoomLink(Project.Current, hall).ShouldBe("[[Hall]]");
  }

  [Test]
  public void AMarkdownLabel_IsSavedAsSuch_AndAPlainOneAsBefore()
  {
    var plain = new MapLabel(Project.Current) { Text = "Plain" };
    var formatted = new MapLabel(Project.Current) { Text = "# Treasures", Markdown = true };
    Project.Current.Elements.Add(plain);
    Project.Current.Elements.Add(formatted);
    var path = Files.File("labels.trizbort");

    new LegacyMapFileEngine(Project.Current).Save(path).ShouldBeTrue();
    File.ReadAllText(path).Split("markdown=").Length.ShouldBe(2);

    using var reopened = new Project();
    PersistenceRegressionTests.Load(reopened, path);
    var labels = reopened.Elements.OfType<MapLabel>().ToList();
    labels.Single(label => label.Text == "Plain").Markdown.ShouldBeFalse();
    labels.Single(label => label.Text == "# Treasures").Markdown.ShouldBeTrue();
  }
}

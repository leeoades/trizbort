using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Editor.Commands;
using Trizbort.Editor.Input;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

/// <summary>
///   Lee, 24 Nov 2026: coloured backgrounds behind parts of the map. Picked up by the edge, or Alt+click inside;
///   inside, the map is as empty as anywhere, for clicking and boxing the rooms on it. It moves on its own.
/// </summary>
public class BackgroundTests : CanvasGestureTestBase
{
  private Background AddBackground(float x, float y, float w, float h, RoomShape shape = RoomShape.SquareCorners)
  {
    var background = new Background(CurrentProject)
    {
      Position = new Vector(x, y), Size = new Vector(w, h), Shape = shape
    };
    CurrentProject.Elements.Add(background);
    return background;
  }

  [Test]
  public void ItIsDrawnBelowTheJoinsAndRooms()
  {
    var background = AddBackground(0, 0, 400, 300);
    var room = AddRoom(100, 100);
    background.Depth.ShouldBe(Depth.Low);
    background.CompareTo(room).ShouldBeLessThan(0);
  }

  [Test]
  public void OnlyItsEdge_PicksItUp_InsideIsEmptyMap()
  {
    var background = AddBackground(0, 0, 400, 300);
    var hit = new HitTester(Session);

    hit.HitElement(new Vector(200, 150)).ShouldBeNull();
    hit.HitElement(new Vector(1, 150)).ShouldBe(background);
    hit.HitElement(new Vector(200, 299)).ShouldBe(background);
    Session.BackgroundAt(new Vector(200, 150)).ShouldBe(background);
    Session.BackgroundAt(new Vector(500, 150)).ShouldBeNull();
  }

  [Test]
  public void AnEllipse_IsPickedUpByItsCurvedEdge_NotItsBox()
  {
    var background = AddBackground(0, 0, 400, 200, RoomShape.Ellipse);
    var hit = new HitTester(Session);

    hit.HitElement(new Vector(400, 100)).ShouldBe(background);
    // the box's corner is well off the ellipse
    hit.HitElement(new Vector(2, 2)).ShouldBeNull();
    Session.BackgroundAt(new Vector(2, 2)).ShouldBeNull();
    Session.BackgroundAt(new Vector(200, 100)).ShouldBe(background);
  }

  [Test]
  public void ARoomOnItsEdge_IsPickedBeforeIt()
  {
    var background = AddBackground(0, 0, 400, 300);
    var room = AddRoom(-32, 100);
    new HitTester(Session).HitElement(new Vector(0, 120)).ShouldBe(room);
  }

  [Test]
  public void PressingInside_BoxesTheRoomsOnIt()
  {
    var background = AddBackground(0, 0, 600, 400);
    var room = AddRoom(100, 100);

    Press(new Vector(50, 50));
    Move(new Vector(250, 250));
    Release(new Vector(250, 250));

    Session.Selection.Items.ShouldBe(new Element[] { room });
    background.Position.ShouldBe(new Vector(0, 0));
  }

  [Test]
  public void ABox_TakesItOnlyWhenItTakesInAllOfIt()
  {
    var background = AddBackground(0, 0, 200, 200);

    Press(new Vector(-50, -50));
    Move(new Vector(100, 100));
    Release(new Vector(100, 100));
    Session.Selection.Contains(background).ShouldBeFalse();

    Press(new Vector(-50, -50));
    Move(new Vector(300, 300));
    Release(new Vector(300, 300));
    Session.Selection.Contains(background).ShouldBeTrue();
  }

  [Test]
  public void AltPressInside_PicksItUp_AndDraggingMovesItAlone()
  {
    var background = AddBackground(0, 0, 600, 400);
    var room = AddRoom(100, 100);

    Press(new Vector(300, 300), ModifierKeys.Alt);
    Session.Selection.Items.ShouldBe(new Element[] { background });
    Move(new Vector(350, 330), ModifierKeys.Alt);
    Release(new Vector(350, 330), ModifierKeys.Alt);

    var moved = Session.Project.Elements.OfType<Background>().Single();
    moved.Position.ShouldBe(new Vector(50, 30));
    Session.Project.Elements.OfType<Room>().Single().Position.ShouldBe(new Vector(100, 100));
  }

  // Lee, 24 Nov 2026: just added (so selected), a drag inside it moved it. It shouldn't: only Alt or the edge
  [Test]
  public void EvenWhenSelected_DraggingInside_BoxesRooms_AndLeavesItWhereItIs()
  {
    var background = AddBackground(0, 0, 600, 400);
    var room = AddRoom(100, 100);
    Select(background);

    Press(new Vector(50, 50));
    Move(new Vector(250, 250));
    Release(new Vector(250, 250));

    Session.Project.Elements.OfType<Background>().Single().Position.ShouldBe(new Vector(0, 0));
    Session.Selection.Items.ShouldBe(new Element[] { room });
  }

  [Test]
  public void ItsEdge_DragsIt()
  {
    var background = AddBackground(0, 0, 600, 400);

    Press(new Vector(2, 200));
    Move(new Vector(34, 200));
    Release(new Vector(34, 200));

    Session.Project.Elements.OfType<Background>().Single().Position.ShouldBe(new Vector(32, 0));
  }

  [Test]
  public async Task AddBackground_RoundTheSelectedRooms_WithAGridSquareToSpare()
  {
    var a = AddRoom(0, 0);
    var b = AddRoom(200, 100);
    Select(a, b);

    var background = await Session.AddBackgroundAsync(null, false);

    background.Position.ShouldBe(new Vector(-32, -32));
    background.Size.ShouldBe(new Vector(264 + 64, 164 + 64));
    Session.Selection.Items.ShouldBe(new Element[] { background });

    // one undo step takes it away
    Session.PerformUndo();
    Session.Project.Elements.OfType<Background>().ShouldBeEmpty();
  }

  [Test]
  public async Task AddBackground_IsOnTheCommands_AndTheMenus()
  {
    var catalogue = CommandCatalogue.CreateDefault();
    var add = catalogue.Find("project.addBackground");
    add.Gestures.ShouldContain(g => g.Key == InputKey.B && g.Modifiers == ModifierKeys.None);
    (add.ContextTargets & ContextTargets.Empty).ShouldBe(ContextTargets.Empty);
    (catalogue.Find("edit.properties").ContextTargets & ContextTargets.Background).ShouldBe(ContextTargets.Background);
    (catalogue.Find("edit.delete").ContextTargets & ContextTargets.Background).ShouldBe(ContextTargets.Background);

    Session.Prompts = NonInteractiveEditorPrompts.Instance;
    await add.ExecuteAsync(Session);
    Session.Project.Elements.OfType<Background>().Count().ShouldBe(1);
    EditorSession.HasPropertiesDialog(Session.Project.Elements.OfType<Background>().Single()).ShouldBeTrue();
  }

  [Test]
  public void SavedAndLoaded_WithItsLook()
  {
    var background = AddBackground(10, 20, 300, 200, RoomShape.Octagonal);
    background.FillColor = Color.FromArgb(0xFF, 0x12, 0x34, 0x56);
    background.BorderStyle = BorderDashStyle.Dash;
    background.BorderColor = Color.FromArgb(0xFF, 0x65, 0x43, 0x21);
    background.HandDrawnStyle = HandDrawnStyle.HandDrawn;
    var id = background.Id;

    var file = Path.GetTempFileName();
    try
    {
      new LegacyMapFileEngine(CurrentProject).Save(file).ShouldBeTrue();
      File.ReadAllText(file).ShouldContain("<background ");
      var loaded = new Project();
      new LegacyMapFileEngine(loaded).Load(file).ShouldBeTrue();
      var back = loaded.Elements.OfType<Background>().Single();
      back.Id.ShouldBe(id);
      back.Position.ShouldBe(new Vector(10, 20));
      back.Size.ShouldBe(new Vector(300, 200));
      back.Shape.ShouldBe(RoomShape.Octagonal);
      back.FillColor.ToArgb().ShouldBe(background.FillColor.ToArgb());
      back.BorderStyle.ShouldBe(BorderDashStyle.Dash);
      back.BorderColor.ToArgb().ShouldBe(background.BorderColor.ToArgb());
      back.HandDrawnStyle.ShouldBe(HandDrawnStyle.HandDrawn);
      loaded.Dispose();
    }
    finally
    {
      File.Delete(file);
    }
  }

  [Test]
  public void CopiedAndPasted_WithItsLook()
  {
    var background = AddBackground(0, 0, 300, 200, RoomShape.Ellipse);
    background.FillColor = Color.FromArgb(0xFF, 0x20, 0x40, 0x60);
    Select(background);

    var json = Session.CopySelected();
    Session.Paste(json, new Vector(1000, 1000));

    var pasted = Session.Project.Elements.OfType<Background>().Single(b => b != background);
    pasted.Shape.ShouldBe(RoomShape.Ellipse);
    pasted.Size.ShouldBe(new Vector(300, 200));
    pasted.FillColor.ToArgb().ShouldBe(background.FillColor.ToArgb());
    pasted.Position.ShouldBe(new Vector(1000, 1000));
  }
}

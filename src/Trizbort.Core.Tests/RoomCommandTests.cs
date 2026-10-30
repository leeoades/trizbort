using System.Drawing;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Elements;
using Trizbort.Editor;
using Trizbort.Editor.Commands;
using Trizbort.Editor.Input;
using Trizbort.Domain.Enums;

namespace Trizbort.Core.Tests;

/// <summary>Tickets 025, 026, 027 and 029: lighting, start/end, shape and swap commands.</summary>
[TestFixture]
[Category("Unit")]
public class RoomCommandTests : RoomTestBase
{
  private EditorCommand Find(InputKey key, ModifierKeys modifiers, CommandScope scope)
  {
    return Catalogue.FindByGesture(new KeyInput(key, modifiers), scope);
  }

  [Test]
  public void DefaultCatalogue_StaysValid()
  {
    CatalogueValidator.Validate(Catalogue).ShouldBeEmpty();
  }

  // ---- 025 lighting ----

  [Test]
  public void ToggleDarkness_FlipsEachRoomOfAMixedSelection()
  {
    var a = NewRoom("A", 0, 0);
    var b = NewRoom("B", 200, 0);
    a.IsDark = true;
    Select(a, b);

    Run("room.toggleDark");

    a.IsDark.ShouldBeFalse();
    b.IsDark.ShouldBeTrue();
  }

  [Test]
  public void ForceDarkAndForceLit_SetEveryRoom()
  {
    var a = NewRoom("A", 0, 0);
    var b = NewRoom("B", 200, 0);
    b.IsDark = true;
    Select(a, b);

    Run("room.forceDark");
    a.IsDark.ShouldBeTrue();
    b.IsDark.ShouldBeTrue();

    Run("room.forceLit");
    a.IsDark.ShouldBeFalse();
    b.IsDark.ShouldBeFalse();
  }

  [Test]
  public void Lighting_IsOneUndoStep()
  {
    var a = NewRoom("A", 0, 0);
    var b = NewRoom("B", 200, 0);
    Select(a, b);

    Run("room.forceDark");
    Session.Undo.UndoTitle.ShouldBe("Force darkness");
    Session.PerformUndo();

    Current(a).IsDark.ShouldBeFalse();
    Current(b).IsDark.ShouldBeFalse();
  }

  [Test]
  public void Lighting_CanExecuteNeedsARoom()
  {
    CanRun("room.toggleDark").ShouldBeFalse();
    var a = NewRoom("A", 0, 0);
    Select(a);
    CanRun("room.toggleDark").ShouldBeTrue();
  }

  [Test]
  public void ToggleDarkness_IsCheckedForPrimaryRoom()
  {
    var a = NewRoom("A", 0, 0);
    var b = NewRoom("B", 200, 0);
    b.IsDark = true;
    Select(a, b);

    Catalogue.Find("room.toggleDark").IsChecked(Session).ShouldBe(true);
    Select(b, a);
    Catalogue.Find("room.toggleDark").IsChecked(Session).ShouldBe(false);
  }

  [Test]
  public void LightingGestures_AreExact()
  {
    Find(InputKey.K, ModifierKeys.None, CommandScope.Canvas).Id.ShouldBe("room.toggleDark");
    Find(InputKey.K, ModifierKeys.Primary, CommandScope.Canvas).ShouldBeNull();
    Find(InputKey.K, ModifierKeys.Primary, CommandScope.Window).Id.ShouldBe("room.forceDark");
    Find(InputKey.K, ModifierKeys.Primary | ModifierKeys.Shift, CommandScope.Window).Id.ShouldBe("room.forceLit");
    Find(InputKey.K, ModifierKeys.Alt, CommandScope.Canvas).ShouldBeNull();
  }

  // ---- 026 start / end ----

  [Test]
  public void SetStartRoom_LeavesOnlyOneStartRoom()
  {
    var a = NewRoom("A", 0, 0);
    var b = NewRoom("B", 200, 0);
    a.IsStartRoom = true;
    Select(b);

    Run("room.setStart");

    a.IsStartRoom.ShouldBeFalse();
    b.IsStartRoom.ShouldBeTrue();
    Catalogue.Find("room.setStart").IsChecked(Session).ShouldBe(true);
  }

  [Test]
  public void SetStartRoom_OnTheStartRoomClearsIt()
  {
    var a = NewRoom("A", 0, 0);
    a.IsStartRoom = true;
    Select(a);

    Run("room.setStart");

    a.IsStartRoom.ShouldBeFalse();
  }

  [Test]
  public void SetStartRoom_NeedsExactlyOneRoom()
  {
    var a = NewRoom("A", 0, 0);
    var b = NewRoom("B", 200, 0);
    CanRun("room.setStart").ShouldBeFalse();
    Select(a, b);
    CanRun("room.setStart").ShouldBeFalse();
    Select(a);
    CanRun("room.setStart").ShouldBeTrue();
  }

  [Test]
  public void SetEndRoom_TogglesEverySelectedRoom()
  {
    var a = NewRoom("A", 0, 0);
    var b = NewRoom("B", 200, 0);
    a.IsEndRoom = true;
    Select(a, b);

    Run("room.setEnd");

    a.IsEndRoom.ShouldBeFalse();
    b.IsEndRoom.ShouldBeTrue();
  }

  [Test]
  public void SetStartRoom_Undo_RestoresPreviousStartRoom()
  {
    var a = NewRoom("A", 0, 0);
    var b = NewRoom("B", 200, 0);
    a.IsStartRoom = true;
    Select(b);

    Run("room.setStart");
    Session.PerformUndo();

    Current(a).IsStartRoom.ShouldBeTrue();
    Current(b).IsStartRoom.ShouldBeFalse();
  }

  [Test]
  public void StartEndGestures_AreExact()
  {
    Find(InputKey.F5, ModifierKeys.Primary, CommandScope.Window).Id.ShouldBe("room.setStart");
    Find(InputKey.F5, ModifierKeys.Shift, CommandScope.Canvas).Id.ShouldBe("room.setEnd");
    Find(InputKey.F5, ModifierKeys.None, CommandScope.Canvas).ShouldBeNull();
  }

  // ---- 027 shapes ----

  [TestCase("room.shape.square", RoomShape.SquareCorners)]
  [TestCase("room.shape.ellipse", RoomShape.Ellipse)]
  [TestCase("room.shape.rounded", RoomShape.RoundedCorners)]
  [TestCase("room.shape.octagonal", RoomShape.Octagonal)]
  public void ShapeCommand_AppliesToAllSelectedRoomsAndUndoes(string id, RoomShape shape)
  {
    var a = NewRoom("A", 0, 0);
    var b = NewRoom("B", 200, 0);
    var original = shape == RoomShape.SquareCorners ? RoomShape.Ellipse : RoomShape.SquareCorners;
    a.Shape = original;
    b.Shape = original;
    Select(a, b);

    Run(id);

    a.Shape.ShouldBe(shape);
    b.Shape.ShouldBe(shape);
    Catalogue.Find(id).IsChecked(Session).ShouldBe(true);

    Session.PerformUndo();
    Current(a).Shape.ShouldBe(original);
    Current(b).Shape.ShouldBe(original);
  }

  [Test]
  public void ControlE_SetsEllipse_NotSquareCorners()
  {
    var a = NewRoom("A", 0, 0);
    Select(a);

    var command = Find(InputKey.E, ModifierKeys.Primary, CommandScope.Window);
    command.ShouldNotBeNull();
    command.ExecuteAsync(Session).GetAwaiter().GetResult();

    a.Shape.ShouldBe(RoomShape.Ellipse);
  }

  [Test]
  public void ShapeGestures_AreExact()
  {
    Find(InputKey.H, ModifierKeys.Primary, CommandScope.Window).Id.ShouldBe("room.shape.square");
    Find(InputKey.E, ModifierKeys.Primary, CommandScope.Window).Id.ShouldBe("room.shape.ellipse");
    Find(InputKey.R, ModifierKeys.Primary, CommandScope.Window).Id.ShouldBe("room.shape.rounded");
    Find(InputKey.D8, ModifierKeys.Primary, CommandScope.Window).Id.ShouldBe("room.shape.octagonal");
    // Bare R is Add Room (canvas), never a shape.
    Find(InputKey.R, ModifierKeys.None, CommandScope.Canvas).Id.ShouldBe("project.addRoom");
    Find(InputKey.R, ModifierKeys.Primary, CommandScope.Canvas).ShouldBeNull();
  }

  // ---- 029 swaps ----

  [Test]
  public void SwapObjects_SwapsAndIsItsOwnInverse()
  {
    var a = NewRoom("A", 0, 0);
    var b = NewRoom("B", 200, 0);
    a.Objects = "lamp";
    b.Objects = "key";
    Select(a, b);

    Run("room.swap.objects");
    a.Objects.ShouldBe("key");
    b.Objects.ShouldBe("lamp");

    Run("room.swap.objects");
    a.Objects.ShouldBe("lamp");
    b.Objects.ShouldBe("key");
  }

  [Test]
  public void SwapNames_SwapsNames()
  {
    var a = NewRoom("A", 0, 0);
    var b = NewRoom("B", 200, 0);
    Select(a, b);

    Run("room.swap.names");

    a.Name.ShouldBe("B");
    b.Name.ShouldBe("A");
  }

  [Test]
  public void SwapFormats_SwapsColoursBorderAndShape()
  {
    var a = NewRoom("A", 0, 0);
    var b = NewRoom("B", 200, 0);
    a.RoomFillColor = Color.Red;
    a.SecondFillColor = Color.Blue;
    a.SecondFillLocation = "Top";
    a.RoomBorderColor = Color.Green;
    a.BorderStyle = BorderDashStyle.Dash;
    a.Shape = RoomShape.Ellipse;
    b.RoomFillColor = Color.Yellow;
    b.RoomBorderColor = Color.Black;
    Select(a, b);

    Run("room.swap.formats");

    b.RoomFillColor.ToArgb().ShouldBe(Color.Red.ToArgb());
    b.SecondFillColor.ToArgb().ShouldBe(Color.Blue.ToArgb());
    b.SecondFillLocation.ShouldBe("Top");
    b.RoomBorderColor.ToArgb().ShouldBe(Color.Green.ToArgb());
    b.BorderStyle.ShouldBe(BorderDashStyle.Dash);
    b.Shape.ShouldBe(RoomShape.Ellipse);
    a.RoomFillColor.ToArgb().ShouldBe(Color.Yellow.ToArgb());
    a.RoomBorderColor.ToArgb().ShouldBe(Color.Black.ToArgb());
    a.Shape.ShouldNotBe(RoomShape.Ellipse);
  }

  [Test]
  public void SwapRegions_SwapsRegionsAndUndoes()
  {
    var a = NewRoom("A", 0, 0);
    var b = NewRoom("B", 200, 0);
    a.Region = "Cave";
    b.Region = "Forest";
    Select(a, b);

    Run("room.swap.regions");
    a.Region.ShouldBe("Forest");
    b.Region.ShouldBe("Cave");

    Session.PerformUndo();
    Current(a).Region.ShouldBe("Cave");
    Current(b).Region.ShouldBe("Forest");
  }

  [Test]
  public void Swap_NeedsExactlyTwoRooms()
  {
    var a = NewRoom("A", 0, 0);
    var b = NewRoom("B", 200, 0);
    var c = NewRoom("C", 400, 0);
    foreach (var id in new[] { "room.swap.objects", "room.swap.names", "room.swap.formats", "room.swap.regions" })
    {
      Select(a);
      CanRun(id).ShouldBeFalse(id);
      Select(a, b);
      CanRun(id).ShouldBeTrue(id);
      Select(a, b, c);
      CanRun(id).ShouldBeFalse(id);
    }
  }

  [Test]
  public void SwapGestures_AreDistinguishedByExactModifiers()
  {
    Find(InputKey.W, ModifierKeys.None, CommandScope.Canvas).Id.ShouldBe("room.swap.objects");
    Find(InputKey.W, ModifierKeys.Primary, CommandScope.Window).Id.ShouldBe("room.swap.names");
    Find(InputKey.W, ModifierKeys.Shift, CommandScope.Canvas).Id.ShouldBe("room.swap.formats");
    Find(InputKey.W, ModifierKeys.Alt, CommandScope.Canvas).Id.ShouldBe("room.swap.regions");
    Find(InputKey.W, ModifierKeys.Primary, CommandScope.Canvas).ShouldBeNull();
  }
}

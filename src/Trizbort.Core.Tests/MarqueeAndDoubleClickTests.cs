using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor.Input;

namespace Trizbort.Core.Tests;

// Ticket 063: legacy marquee modifiers and the double-click empty-space rule.
[TestFixture]
public class MarqueeAndDoubleClickTests : CanvasGestureTestBase
{
  private bool _previousDoubleClick;

  [SetUp]
  public void SetUp()
  {
    _previousDoubleClick = ApplicationSettingsController.AppSettings.DoubleClickToAddRoom;
    ApplicationSettingsController.AppSettings.DoubleClickToAddRoom = true;
  }

  [TearDown]
  public void TearDown()
  {
    ApplicationSettingsController.AppSettings.DoubleClickToAddRoom = _previousDoubleClick;
  }

  private void Marquee(ModifierKeys modifiers)
  {
    var from = new Vector(-50, -50);
    var to = new Vector(100, 100);
    Move(from, modifiers);
    Press(from, modifiers);
    Move(to, modifiers);
    Release(to, modifiers);
  }

  [Test]
  public void Marquee_NoModifier_ReplacesSelection()
  {
    var a = AddRoom(0, 0);
    var c = AddRoom(500, 500);
    Select(c);

    Marquee(ModifierKeys.None);

    Session.Selection.Items.ShouldBe(new Element[] { a });
  }

  [Test]
  public void Marquee_Ctrl_AddsAndNeverRemoves()
  {
    var a = AddRoom(0, 0);
    var c = AddRoom(500, 500);
    Select(a, c);

    Marquee(ModifierKeys.Control);

    Session.Selection.Contains(a).ShouldBeTrue();
    Session.Selection.Contains(c).ShouldBeTrue();
  }

  [Test]
  public void Marquee_Shift_TogglesElements()
  {
    var a = AddRoom(0, 0);
    var b = AddRoom(60, 60);
    var c = AddRoom(500, 500);
    Select(a, c);

    Marquee(ModifierKeys.Shift);

    Session.Selection.Contains(a).ShouldBeFalse();
    Session.Selection.Contains(b).ShouldBeTrue();
    Session.Selection.Contains(c).ShouldBeTrue();
  }

  [Test]
  public void DoubleClick_TrulyEmptySpace_AddsRoom()
  {
    Move(new Vector(600, 600));
    Press(new Vector(600, 600), ModifierKeys.None, 2);

    Session.Project.Elements.OfType<Room>().Count().ShouldBe(1);
  }

  [Test]
  public void DoubleClick_OverResizeHandle_DoesNotAddRoom()
  {
    var room = AddRoom(0, 0);
    Select(room);
    var corner = room.InnerBounds.GetCorner(Trizbort.Domain.Misc.CompassPoint.SouthEast);

    Move(corner);
    Press(corner, ModifierKeys.None, 2);
    Release(corner);

    Session.Project.Elements.OfType<Room>().Count().ShouldBe(1);
  }

  [Test]
  public void DoubleClick_OverPort_DoesNotAddRoom()
  {
    var room = AddRoom(0, 0);
    var port = PortOf(room, Trizbort.Domain.Misc.CompassPoint.East).Position;

    Move(port);
    Press(port, ModifierKeys.None, 2);
    Release(port);

    Session.Project.Elements.OfType<Room>().Count().ShouldBe(1);
  }

  [Test]
  public void DoubleClick_Setting_Off_DoesNotAddRoom()
  {
    ApplicationSettingsController.AppSettings.DoubleClickToAddRoom = false;

    Move(new Vector(600, 600));
    Press(new Vector(600, 600), ModifierKeys.None, 2);

    Session.Project.Elements.OfType<Room>().ShouldBeEmpty();
  }
}

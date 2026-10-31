using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 004: every editing path of <see cref="EditorSession" /> is one undo step.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class EditorSessionUndoTests
{
  private Project _previousProject;
  private Project _project;
  private EditorSession _session;

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _session = new EditorSession();
  }

  [TearDown]
  public void TearDown()
  {
    _session.Dispose();
    var current = Project.Current;
    Project.Current = _previousProject;
    if (current != null && current != _previousProject) current.Dispose();
    if (_project != current) _project.Dispose();
  }

  private static int RoomCount()
  {
    return Project.Current.Elements.OfType<Room>().Count();
  }

  private static Connection Connect(Room from, Room to)
  {
    var connection = new Connection(Project.Current,
      new Vertex(from.PortAt(CompassPoint.East)),
      new Vertex(to.PortAt(CompassPoint.West)));
    Project.Current.Elements.Add(connection);
    return connection;
  }

  [Test]
  public void AddRoom_IsUndoable()
  {
    _session.AddRoom(new Vector(0, 0));

    _session.Undo.UndoTitle.ShouldBe("Add room");
    RoomCount().ShouldBe(1);

    _session.PerformUndo();
    RoomCount().ShouldBe(0);

    _session.PerformRedo();
    RoomCount().ShouldBe(1);
  }

  [Test]
  public void Delete_IsUndoable_WithConnections()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(300, 0));
    _session.Execute("Connect", () => Connect(a, b));
    var undoDepthMarker = _session.Undo.UndoTitle;
    undoDepthMarker.ShouldBe("Connect");

    _session.Select(a, false, false);
    _session.DeleteSelected();
    _session.Undo.UndoTitle.ShouldBe("Delete");
    RoomCount().ShouldBe(1);
    Project.Current.Elements.OfType<Connection>().ShouldBeEmpty();

    _session.PerformUndo();

    RoomCount().ShouldBe(2);
    Project.Current.Elements.OfType<Connection>().Count().ShouldBe(1);
  }

  [Test]
  public void Paste_IsOneStep_AndUndoRemovesAllPastedElements()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(300, 0));
    _session.Execute("Connect", () => Connect(a, b));
    _session.SelectAll();
    var json = _session.CopySelected();
    var countBefore = Project.Current.Elements.Count;

    _session.Paste(json, new Vector(1000, 1000));

    Project.Current.Elements.Count.ShouldBeGreaterThan(countBefore);
    _session.Undo.UndoTitle.ShouldBe("Paste");

    _session.PerformUndo();

    Project.Current.Elements.Count.ShouldBe(countBefore);
    _session.Undo.UndoTitle.ShouldBe("Connect");
  }

  [Test]
  public void Cut_IsOneStep()
  {
    var room = _session.AddRoom(new Vector(0, 0));
    _session.Select(room, false, false);

    _session.CutSelected();

    _session.Undo.UndoTitle.ShouldBe("Cut");
    RoomCount().ShouldBe(0);
    _session.PerformUndo();
    RoomCount().ShouldBe(1);
  }

  [Test]
  public void Nudge_IsUndoable()
  {
    var room = _session.AddRoom(new Vector(0, 0));
    var start = room.Position;
    _session.Select(room, false, false);

    _session.NudgeSelected(new Vector(10, 20));

    _session.Undo.UndoTitle.ShouldBe("Move");
    room.Position.ShouldBe(start + new Vector(10, 20));

    _session.PerformUndo();

    Project.Current.Elements.OfType<Room>().Single().Position.ShouldBe(start);
  }

  [Test]
  public void ResizeSelected_RespectsGridMinimum_AndIsUndoable()
  {
    var grid = Settings.GridSize;
    var room = _session.AddRoom(new Vector(0, 0));
    room.Size = new Vector(grid * 4, grid * 4);
    _session.Select(room, false, false);
    _session.Undo.Clear();

    _session.ResizeSelected(new Vector(grid, 0));
    _session.Undo.UndoTitle.ShouldBe("Resize");
    room.Width.ShouldBe(grid * 5, 0.01f);

    // Shrinking below one grid cell leaves the size alone and records nothing.
    _session.ResizeSelected(new Vector(-grid * 10, 0));
    room.Width.ShouldBe(grid * 5, 0.01f);

    _session.PerformUndo();
    Project.Current.Elements.OfType<Room>().Single().Width.ShouldBe(grid * 4, 0.01f);
    _session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void Apply_Dialog_IsOneStep()
  {
    var room = _session.AddRoom(new Vector(0, 0));

    _session.Apply("Edit room", room, () =>
    {
      room.Name = "Kitchen";
      room.Position = new Vector(64, 64);
    });

    _session.Undo.UndoTitle.ShouldBe("Edit room");
    _session.PerformUndo();
    Project.Current.Elements.OfType<Room>().Single().Name.ShouldNotBe("Kitchen");
    _session.Undo.UndoTitle.ShouldBe("Add room");
  }

  [Test]
  public void Apply_NoChange_RecordsNoStep()
  {
    var room = _session.AddRoom(new Vector(0, 0));
    _session.Undo.Clear();

    _session.Apply("Edit room", room, () => room.Name = room.Name);

    _session.Undo.CanUndo.ShouldBeFalse();
    _session.Undo.IsDirty.ShouldBeFalse();
  }

  [Test]
  public void AutomapBatch_OneStep()
  {
    var canvas = new AutomapCanvas();
    using (_session.BeginTransaction("Automap"))
    {
      var first = canvas.CreateRoom(null, "First");
      var second = canvas.CreateRoom(first, "Second");
      var third = canvas.CreateRoom(second, "Third");
      canvas.Connect(first, MappableDirection.East, second, true);
      canvas.Connect(second, MappableDirection.East, third, true);
    }

    RoomCount().ShouldBe(3);
    _session.PerformUndo();
    RoomCount().ShouldBe(0);
    _session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void Dirty_ClearAfterUndoToSaved()
  {
    _session.MarkSaved();
    Project.Current.IsDirty.ShouldBeFalse();

    _session.AddRoom(new Vector(0, 0));
    Project.Current.IsDirty.ShouldBeTrue();

    _session.PerformUndo();
    Project.Current.IsDirty.ShouldBeFalse();
  }

  [Test]
  public void NewProject_ClearsHistory()
  {
    _session.AddRoom(new Vector(0, 0));

    _session.NewProject();

    _session.Undo.CanUndo.ShouldBeFalse();
    Project.Current.IsDirty.ShouldBeFalse();
  }
}

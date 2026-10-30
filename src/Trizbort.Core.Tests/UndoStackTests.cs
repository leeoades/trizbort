using System;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class UndoStackTests
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

  private static int ConnectionCount()
  {
    return Project.Current.Elements.OfType<Connection>().Count();
  }

  private Room AddRoomStep(string title = "Add room", float x = 0)
  {
    Room room = null;
    _session.Execute(title, () => room = _session.AddRoom(new Vector(x, 0)));
    return room;
  }

  private static Connection Connect(Room from, Room to)
  {
    var connection = new Connection(Project.Current,
      new Vertex(from.PortAt(CompassPoint.East)),
      new Vertex(to.PortAt(CompassPoint.West)));
    Project.Current.Elements.Add(connection);
    return connection;
  }

  private void AddThenThrow()
  {
    _session.AddRoom(new Vector(500, 0));
    throw new InvalidOperationException("boom");
  }

  [Test]
  public void Undo_RestoresPreviousMap()
  {
    AddRoomStep();
    RoomCount().ShouldBe(1);

    _session.PerformUndo();

    RoomCount().ShouldBe(0);
    _session.Undo.CanUndo.ShouldBeFalse();
    _session.Undo.CanRedo.ShouldBeTrue();
  }

  [Test]
  public void Redo_ReappliesEdit()
  {
    AddRoomStep();
    _session.PerformUndo();

    _session.PerformRedo();

    RoomCount().ShouldBe(1);
    _session.Undo.CanRedo.ShouldBeFalse();
    _session.Undo.CanUndo.ShouldBeTrue();
  }

  [Test]
  public void Undo_ReplacesProjectObject_AndKeepsFileName()
  {
    Project.Current.FileName = "map.trizbort";
    var before = Project.Current;
    AddRoomStep();

    _session.PerformUndo();

    Project.Current.ShouldNotBeSameAs(before);
    Project.Current.FileName.ShouldBe("map.trizbort");
    _session.Project.ShouldBeSameAs(Project.Current);
  }

  [Test]
  public void Undo_RaisesModelSelectionAndAdorners()
  {
    AddRoomStep();
    var seen = EditorChange.None;
    _session.Changed += change => seen |= change;

    _session.PerformUndo();

    seen.HasFlag(EditorChange.Model).ShouldBeTrue();
    seen.HasFlag(EditorChange.Selection).ShouldBeTrue();
    seen.HasFlag(EditorChange.Adorners).ShouldBeTrue();
    seen.HasFlag(EditorChange.Dirty).ShouldBeTrue();
  }

  [Test]
  public void Undo_DeleteRoom_RestoresCascadedConnections()
  {
    var a = AddRoomStep("Add a", 0);
    var b = AddRoomStep("Add b", 300);
    var c = AddRoomStep("Add c", 600);
    _session.Execute("Connect", () =>
    {
      Connect(a, b);
      Connect(b, c);
    });
    ConnectionCount().ShouldBe(2);

    _session.Selection.Set(new Element[] { b });
    _session.Execute("Delete", () => _session.DeleteSelected());
    RoomCount().ShouldBe(2);
    ConnectionCount().ShouldBe(0);

    _session.PerformUndo();

    RoomCount().ShouldBe(3);
    ConnectionCount().ShouldBe(2);
    foreach (var connection in Project.Current.Elements.OfType<Connection>())
    {
      connection.VertexList.Count.ShouldBe(2);
      foreach (var vertex in connection.VertexList)
      {
        var owner = vertex.Port.Owner;
        owner.ShouldBeOfType<Room>();
        // Docked to a room of the restored project, not to a stale object.
        Project.Current.Elements.ShouldContain(owner);
      }
    }
    var restoredB = Project.Current.Elements.OfType<Room>().Single(room => room.Id == b.Id);
    Project.Current.Elements.OfType<Connection>()
      .Count(connection => connection.VertexList.Any(vertex => vertex.Port.Owner == restoredB))
      .ShouldBe(2);
  }

  [Test]
  public void Undo_RestoresSelectionById()
  {
    var a = AddRoomStep("Add a", 0);
    var b = AddRoomStep("Add b", 300);
    _session.Selection.Set(new Element[] { a, b });

    _session.Execute("Delete", () => _session.DeleteSelected());
    _session.Selection.Count.ShouldBe(0);

    _session.PerformUndo();

    _session.Selection.Count.ShouldBe(2);
    _session.Selection.Items.Select(element => element.Id).ShouldBe(new[] { a.Id, b.Id });
    _session.Selection.Primary.Id.ShouldBe(b.Id);
    foreach (var element in _session.Selection.Items)
      Project.Current.Elements.ShouldContain(element);
    Project.Current.ActiveSelectedElement.ShouldBe(_session.Selection.Primary);
  }

  [Test]
  public void NewEditAfterUndo_ClearsRedo()
  {
    AddRoomStep("First", 0);
    AddRoomStep("Second", 300);
    _session.PerformUndo();
    _session.Undo.CanRedo.ShouldBeTrue();

    AddRoomStep("Third", 600);

    _session.Undo.CanRedo.ShouldBeFalse();
    _session.Undo.RedoTitle.ShouldBeNull();
    _session.Undo.UndoTitle.ShouldBe("Third");
  }

  [Test]
  public void Capacity_DropsOldestStep()
  {
    _session.Dispose();
    _session = new EditorSession(new UndoStack(3));
    for (var i = 0; i < 5; i++) AddRoomStep("Add " + i, i * 300);
    RoomCount().ShouldBe(5);

    var undos = 0;
    while (_session.Undo.CanUndo)
    {
      _session.PerformUndo();
      undos++;
    }

    undos.ShouldBe(3);
    RoomCount().ShouldBe(2);
  }

  [Test]
  public void Transaction_Nested_RecordsOneStep()
  {
    using (_session.BeginTransaction("Outer"))
    {
      _session.Execute("Inner one", () => _session.AddRoom(new Vector(0, 0)));
      using (_session.BeginTransaction("Inner two"))
      {
        _session.AddRoom(new Vector(300, 0));
      }
    }

    RoomCount().ShouldBe(2);
    _session.Undo.UndoTitle.ShouldBe("Outer");

    _session.PerformUndo();

    RoomCount().ShouldBe(0);
    _session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void Transaction_NoChange_RecordsNothing()
  {
    AddRoomStep();
    var title = _session.Undo.UndoTitle;

    _session.Execute("Nothing", () => { });
    using (_session.BeginTransaction("Also nothing"))
    {
    }

    _session.Undo.UndoTitle.ShouldBe(title);
    _session.PerformUndo();
    _session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void Transaction_Cancel_RestoresAndRecordsNothing()
  {
    AddRoomStep();
    var transaction = _session.BeginTransaction("Drag");
    _session.AddRoom(new Vector(300, 0));
    RoomCount().ShouldBe(2);

    transaction.Cancel();
    transaction.Dispose();

    RoomCount().ShouldBe(1);
    _session.Undo.UndoTitle.ShouldBe("Add room");
    _session.Undo.CanRedo.ShouldBeFalse();
    _session.PerformUndo();
    _session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void Execute_Throws_RollsBackAndRethrows()
  {
    AddRoomStep();

    var ex = Should.Throw<InvalidOperationException>(() => _session.Execute("Failing", AddThenThrow));

    ex.Message.ShouldBe("boom");
    RoomCount().ShouldBe(1);
    _session.Undo.UndoTitle.ShouldBe("Add room");
    _session.Undo.CanRedo.ShouldBeFalse();
  }

  [Test]
  public void Undo_InsideTransaction_Throws()
  {
    AddRoomStep();
    using (_session.BeginTransaction("Open"))
    {
      Should.Throw<InvalidOperationException>(() => _session.PerformUndo());
    }
  }

  [Test]
  public void Dirty_FalseAfterMarkSaved()
  {
    AddRoomStep();
    _session.Undo.IsDirty.ShouldBeTrue();

    _session.MarkSaved();

    _session.Undo.IsDirty.ShouldBeFalse();
    Project.Current.IsDirty.ShouldBeFalse();
  }

  [Test]
  public void Dirty_TrueAfterEdit()
  {
    _session.MarkSaved();
    _session.Undo.IsDirty.ShouldBeFalse();

    AddRoomStep();

    _session.Undo.IsDirty.ShouldBeTrue();
    Project.Current.IsDirty.ShouldBeTrue();
  }

  [Test]
  public void Dirty_FalseAgainAfterUndoToSavedPoint()
  {
    AddRoomStep("One", 0);
    _session.MarkSaved();
    AddRoomStep("Two", 300);
    _session.Undo.IsDirty.ShouldBeTrue();

    _session.PerformUndo();
    _session.Undo.IsDirty.ShouldBeFalse();
    Project.Current.IsDirty.ShouldBeFalse();

    _session.PerformRedo();
    _session.Undo.IsDirty.ShouldBeTrue();
    Project.Current.IsDirty.ShouldBeTrue();
  }

  [Test]
  public void Dirty_StaysTrueWhenSavedStepWasTrimmed()
  {
    _session.Dispose();
    _session = new EditorSession(new UndoStack(3));
    _session.MarkSaved();
    for (var i = 0; i < 4; i++) AddRoomStep("Add " + i, i * 300);

    while (_session.Undo.CanUndo) _session.PerformUndo();

    // Back at the oldest reachable state, which is not the saved one.
    _session.Undo.IsDirty.ShouldBeTrue();
    Project.Current.IsDirty.ShouldBeTrue();

    _session.MarkSaved();
    _session.Undo.IsDirty.ShouldBeFalse();
  }

  [Test]
  public void Dirty_StaysTrueWhenSavedStepWasRedoTruncated()
  {
    AddRoomStep("One", 0);
    _session.MarkSaved();
    _session.PerformUndo();

    // Recording a new step discards the redo entry that led to the saved state.
    AddRoomStep("Other", 300);
    _session.Undo.IsDirty.ShouldBeTrue();

    _session.PerformUndo();
    _session.Undo.IsDirty.ShouldBeTrue();
    _session.PerformRedo();
    _session.Undo.IsDirty.ShouldBeTrue();

    _session.MarkSaved();
    _session.Undo.IsDirty.ShouldBeFalse();
  }

  [Test]
  public void Clear_ForgetsHistoryAndMarksClean()
  {
    AddRoomStep();
    _session.PerformUndo();

    _session.Undo.Clear();

    _session.Undo.CanUndo.ShouldBeFalse();
    _session.Undo.CanRedo.ShouldBeFalse();
    _session.Undo.IsDirty.ShouldBeFalse();
  }

  [Test]
  public void Touched_Hint_IsAccepted_AndIgnored()
  {
    var room = AddRoomStep();

    _session.Execute("Move", () => room.Position = new Vector(400, 400), new Element[] { room });
    _session.Execute("Null hint", () => Project.Current.Elements.OfType<Room>().First().Position = new Vector(10, 10), null);
    using (_session.BeginTransaction("Hint with null item", new Element[] { null }))
    {
      Project.Current.Elements.OfType<Room>().First().Position = new Vector(20, 20);
    }

    _session.Undo.UndoTitle.ShouldBe("Hint with null item");
    _session.PerformUndo();
    Project.Current.Elements.OfType<Room>().First().Position.ShouldBe(new Vector(10, 10));
    _session.PerformUndo();
    Project.Current.Elements.OfType<Room>().First().Position.ShouldBe(new Vector(400, 400));
  }

  [Test]
  public void UndoTitle_ReflectsLastStep()
  {
    _session.Undo.UndoTitle.ShouldBeNull();
    AddRoomStep("Step A", 0);
    AddRoomStep("Step B", 300);
    _session.Undo.UndoTitle.ShouldBe("Step B");

    _session.PerformUndo();

    _session.Undo.UndoTitle.ShouldBe("Step A");
    _session.Undo.RedoTitle.ShouldBe("Step B");

    _session.PerformRedo();

    _session.Undo.UndoTitle.ShouldBe("Step B");
    _session.Undo.RedoTitle.ShouldBeNull();
  }

  [Test]
  public void Changed_IsRaisedWhenStackChanges()
  {
    var count = 0;
    _session.Undo.Changed += (_, _) => count++;

    AddRoomStep();
    _session.PerformUndo();
    _session.PerformRedo();
    _session.MarkSaved();

    count.ShouldBe(4);
  }
}

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

[TestFixture]
[NonParallelizable]
public class EditorSessionTests
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

  private Connection Connect(Room from, Room to)
  {
    var connection = new Connection(Project.Current,
      new Vertex(from.PortAt(CompassPoint.East)),
      new Vertex(to.PortAt(CompassPoint.West)));
    Project.Current.Elements.Add(connection);
    return connection;
  }

  [Test]
  public void AddRoom_AtPosition_AddsAndSelects()
  {
    var room = _session.AddRoom(new Vector(100, 200));

    Project.Current.Elements.ShouldContain(room);
    _session.Selection.Primary.ShouldBe(room);
    _session.Selection.Count.ShouldBe(1);
    room.Position.ShouldBe(Settings.Snap(new Vector(100, 200)));
  }

  [Test]
  public void DeleteSelected_RemovesRoomAndItsConnections()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(300, 0));
    var connection = Connect(a, b);

    _session.Selection.Set(new Element[] { a });
    var count = _session.DeleteSelected();

    count.ShouldBe(1);
    Project.Current.Elements.ShouldNotContain(a);
    Project.Current.Elements.ShouldContain(b);
    Project.Current.Elements.ShouldNotContain(connection);
    _session.Selection.Count.ShouldBe(0);
  }

  [Test]
  public void NudgeSelected_MovesAllSelected()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(300, 0));
    var aStart = a.Position;
    var bStart = b.Position;
    _session.Selection.Set(new Element[] { a, b });

    _session.NudgeSelected(new Vector(10, 20));

    a.Position.ShouldBe(aStart + new Vector(10, 20));
    b.Position.ShouldBe(bStart + new Vector(10, 20));
  }

  [Test]
  public void CopyPaste_RoundTrip_CreatesRemappedCopies()
  {
    var a = _session.AddRoom(new Vector(0, 0));
    var b = _session.AddRoom(new Vector(300, 0));
    Connect(a, b);
    _session.SelectAll();

    var json = _session.CopySelected();
    json.ShouldNotBeNull();
    var originals = Project.Current.Elements.ToList();

    _session.Paste(json, new Vector(1000, 1000));

    var pastedRooms = Project.Current.Elements.OfType<Room>().Where(r => r != a && r != b).ToList();
    var pastedConnections = Project.Current.Elements.OfType<Connection>().Where(c => !originals.Contains(c)).ToList();
    pastedRooms.Count.ShouldBe(2);
    pastedConnections.Count.ShouldBe(1);
    pastedRooms.Select(r => r.Id).ShouldNotContain(a.Id);
    pastedRooms.Select(r => r.Id).ShouldNotContain(b.Id);

    var owners = pastedConnections[0].VertexList.Where(v => v.Port != null).Select(v => v.Port.Owner).ToList();
    owners.Count.ShouldBe(2);
    owners.ShouldAllBe(o => pastedRooms.Contains(o as Room));
    _session.Selection.Count.ShouldBe(3);
  }

  [Test]
  public void Selection_Change_RaisesChangedWithSelectionFlag()
  {
    var room = new Room(Project.Current);
    Project.Current.Elements.Add(room);
    var changes = new List<EditorChange>();
    _session.Changed += changes.Add;

    _session.Selection.Set(new Element[] { room });

    changes.ShouldBe(new[] { EditorChange.Selection });
  }

  [Test]
  public void Viewport_Change_RaisesChangedWithViewportFlag()
  {
    var changes = new List<EditorChange>();
    _session.Changed += changes.Add;

    _session.Viewport.SetView(new Vector(10, 10), 2.0f);

    changes.ShouldBe(new[] { EditorChange.Viewport });
  }

  [Test]
  public void ProjectActiveSelectedElement_TracksPrimary()
  {
    var a = new Room(Project.Current);
    var b = new Room(Project.Current);
    Project.Current.Elements.Add(a);
    Project.Current.Elements.Add(b);

    _session.Selection.Set(new Element[] { a, b });
    Project.Current.ActiveSelectedElement.ShouldBe(b);
    Project.Current.GetSelectedElements().ShouldBe(new Element[] { a, b });

    _session.Selection.Clear();
    Project.Current.ActiveSelectedElement.ShouldBeNull();
  }

  [Test]
  public void NewProject_ReplacesProjectCurrent_AndRaisesModel()
  {
    var old = Project.Current;
    var room = _session.AddRoom(new Vector(0, 0));
    var changes = new List<EditorChange>();
    _session.Changed += changes.Add;

    _session.NewProject();

    Project.Current.ShouldNotBe(old);
    Project.Current.Elements.Count.ShouldBe(0);
    _session.Selection.Count.ShouldBe(0);
    room.ShouldNotBeNull();
    changes.ShouldContain(c => (c & EditorChange.Model) != 0);
  }
}

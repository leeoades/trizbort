using System.Linq;
using NUnit.Framework;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Editor.Commands;

namespace Trizbort.Core.Tests;

/// <summary>Shared setup for the room command tests: a fresh project, session and default catalogue.</summary>
[NonParallelizable]
public abstract class RoomTestBase
{
  private Project _previousProject;
  private Project _project;

  protected EditorSession Session { get; private set; }

  protected CommandCatalogue Catalogue { get; private set; }

  [SetUp]
  public void BaseSetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    Session = new EditorSession();
    Catalogue = CommandCatalogue.CreateDefault();
  }

  [TearDown]
  public void BaseTearDown()
  {
    Session.Dispose();
    var current = Project.Current;
    Project.Current = _previousProject;
    if (current != null && current != _previousProject) current.Dispose();
    if (_project != current) _project.Dispose();
  }

  protected Room NewRoom(string name, float x, float y)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y) };
    Project.Current.Elements.Add(room);
    return room;
  }

  protected void Select(params Element[] elements)
  {
    Session.Selection.Set(elements);
  }

  protected void Run(string id)
  {
    var command = Catalogue.Find(id);
    Assert.That(command, Is.Not.Null, id);
    Assert.That(command.CanExecute(Session), Is.True, id + " CanExecute");
    command.ExecuteAsync(Session).GetAwaiter().GetResult();
  }

  protected bool CanRun(string id)
  {
    return Catalogue.Find(id).CanExecute(Session);
  }

  /// <summary>The room with the same id in the current project (undo replaces the project object).</summary>
  protected static Room Current(Room room)
  {
    return Project.Current.Elements.OfType<Room>().First(r => r.Id == room.Id);
  }
}

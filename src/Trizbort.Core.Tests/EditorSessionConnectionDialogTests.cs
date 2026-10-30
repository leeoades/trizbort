using System.Drawing;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 075: applying the connection dialog is one undo step.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class EditorSessionConnectionDialogTests
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

  private static Connection CurrentConnection()
  {
    return Project.Current.Elements.OfType<Connection>().Single();
  }

  [Test]
  public void ConnectionOk_IsOneUndoStep()
  {
    var connection = new Connection(Project.Current, new Vertex(new Vector(0, 0)), new Vertex(new Vector(100, 0)));
    Project.Current.Elements.Add(connection);
    _session.Undo.Clear();

    _session.Apply("Edit connection properties", connection, () =>
    {
      connection.Flow = ConnectionFlow.OneWay;
      connection.Style = ConnectionStyle.Dashed;
      connection.StartText = "up";
      connection.EndText = "down";
      connection.ConnectionColor = Color.Teal;
      connection.Door = new Door { Openable = true, Lockable = true };
    });

    CurrentConnection().Flow.ShouldBe(ConnectionFlow.OneWay);
    CurrentConnection().Door.ShouldNotBeNull();

    _session.PerformUndo();

    var restored = CurrentConnection();
    restored.Flow.ShouldBe(ConnectionFlow.TwoWay);
    restored.Style.ShouldBe(ConnectionStyle.Solid);
    restored.StartText.ShouldBeNullOrEmpty();
    restored.EndText.ShouldBeNullOrEmpty();
    restored.Door.ShouldBeNull();
    _session.Undo.CanUndo.ShouldBeFalse();

    _session.PerformRedo();

    var redone = CurrentConnection();
    redone.Flow.ShouldBe(ConnectionFlow.OneWay);
    redone.Style.ShouldBe(ConnectionStyle.Dashed);
    redone.StartText.ShouldBe("up");
    redone.ConnectionColor.ToArgb().ShouldBe((Color.Teal).ToArgb());
    redone.Door.ShouldNotBeNull();
  }
}

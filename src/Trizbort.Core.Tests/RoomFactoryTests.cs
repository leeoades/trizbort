using System.Drawing;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 115: new rooms honour "Apply style to new rooms".</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class RoomFactoryTests
{
  private Project _previousProject;
  private Project _project;
  private bool _previousApplyStyle;

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _previousApplyStyle = ApplicationSettingsController.AppSettings.ApplyStyleToNewRooms;
  }

  [TearDown]
  public void TearDown()
  {
    ApplicationSettingsController.AppSettings.ApplyStyleToNewRooms = _previousApplyStyle;
    var current = Project.Current;
    Project.Current = _previousProject;
    if (current != null && current != _previousProject) current.Dispose();
    if (_project != current) _project.Dispose();
  }

  private Room StyledSource()
  {
    var source = new Room(_project)
    {
      Size = new Vector(128, 64),
      IsDark = true,
      RoomFillColor = Color.Red,
      RoomBorderColor = Color.Green,
      Region = "Cave",
      Shape = RoomShape.Ellipse
    };
    _project.Elements.Add(source);
    return source;
  }

  [Test]
  public void ApplyStyleOn_CopiesStyleFromTheSource()
  {
    var source = StyledSource();

    var room = RoomFactory.CreateNewRoom(_project, new Vector(500, 500), source, true);

    room.RoomFillColor.ToArgb().ShouldBe(Color.Red.ToArgb());
    room.RoomBorderColor.ToArgb().ShouldBe(Color.Green.ToArgb());
    room.Region.ShouldBe("Cave");
    room.Shape.ShouldBe(RoomShape.Ellipse);
    room.Size.ShouldBe(new Vector(128, 64));
    room.IsDark.ShouldBeTrue();
  }

  [Test]
  public void ApplyStyleOff_CopiesOnlySizeAndDarkness()
  {
    var source = StyledSource();

    var room = RoomFactory.CreateNewRoom(_project, new Vector(500, 500), source, false);

    room.Size.ShouldBe(new Vector(128, 64));
    room.IsDark.ShouldBeTrue();
    room.RoomFillColor.ToArgb().ShouldNotBe(Color.Red.ToArgb());
    room.Region.ShouldNotBe("Cave");
    room.Shape.ShouldNotBe(RoomShape.Ellipse);
  }

  [Test]
  public void NoSource_UsesDefaults_EvenWhenApplyStyleIsOn()
  {
    var room = RoomFactory.CreateNewRoom(_project, new Vector(500, 500), null, true);

    room.Size.ShouldBe(new Vector(Settings.GridSize * 3, Settings.GridSize * 2));
    room.IsDark.ShouldBeFalse();
    room.Position.ShouldBe(Settings.Snap(new Vector(500, 500) - room.Size / 2));
  }

  [Test]
  public void SessionAddRoom_UsesTheSettingAndTheSelectedRoom()
  {
    using var session = new EditorSession();
    var source = StyledSource();
    session.Selection.Set(new Element[] { source });

    ApplicationSettingsController.AppSettings.ApplyStyleToNewRooms = true;
    var styled = session.AddRoom(new Vector(900, 900));
    styled.RoomFillColor.ToArgb().ShouldBe(Color.Red.ToArgb());

    session.Selection.Set(new Element[] { source });
    ApplicationSettingsController.AppSettings.ApplyStyleToNewRooms = false;
    var plain = session.AddRoom(new Vector(1300, 900));
    plain.RoomFillColor.ToArgb().ShouldNotBe(Color.Red.ToArgb());
  }

  [Test]
  public void SelectedNonRoom_GivesDefaults()
  {
    using var session = new EditorSession();
    var a = new Room(_project) { Position = new Vector(0, 0) };
    var b = new Room(_project) { Position = new Vector(300, 0) };
    _project.Elements.Add(a);
    _project.Elements.Add(b);
    var connection = new Connection(_project, new Vertex(a.PortAt(CompassPoint.East)), new Vertex(b.PortAt(CompassPoint.West)));
    _project.Elements.Add(connection);
    ApplicationSettingsController.AppSettings.ApplyStyleToNewRooms = true;
    a.RoomFillColor = Color.Red;
    session.Selection.Set(new Element[] { connection });

    var room = session.AddRoom(new Vector(900, 900));

    room.RoomFillColor.ToArgb().ShouldNotBe(Color.Red.ToArgb());
  }
}

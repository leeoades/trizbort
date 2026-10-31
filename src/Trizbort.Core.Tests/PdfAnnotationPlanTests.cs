using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Export;

namespace Trizbort.Core.Tests;

[TestFixture]
[NonParallelizable]
public class PdfAnnotationPlanTests
{
  private Project _previousProject;
  private Project _project;

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
  }

  [TearDown]
  public void TearDown()
  {
    Project.Current = _previousProject;
    _project.Dispose();
  }

  private Room AddRoom(float x, float y, float width, float height, RoomShape shape, string description)
  {
    var room = new Room(_project) { Position = new Vector(x, y), Size = new Vector(width, height) };
    room.Shape = shape;
    // Setting the shape can adjust the size, so set it again.
    room.Size = new Vector(width, height);
    if (description != null) room.AddDescription(description);
    _project.Elements.Add(room);
    return room;
  }

  [Test]
  public void SquareCorners_NoteSitsAtLeftEdgeAndYIsFlipped()
  {
    AddRoom(100, 50, 80, 40, RoomShape.SquareCorners, "A cave");
    var bounds = new Rect(20, 10, 300, 200);

    var plan = PdfAnnotationPlan.Build(_project, bounds, 1.0, 200);

    plan.Count.ShouldBe(1);
    plan[0].Text.ShouldBe("A cave");
    plan[0].Width.ShouldBe(10);
    plan[0].Height.ShouldBe(10);
    plan[0].X.ShouldBe(80);
    // 200 - ((50 - 10) + 10)
    plan[0].Y.ShouldBe(150);
  }

  [TestCase(RoomShape.RoundedCorners)]
  [TestCase(RoomShape.Ellipse)]
  public void OtherShapes_NoteIsCentredHorizontally(RoomShape shape)
  {
    AddRoom(100, 50, 80, 40, shape, "Round");
    var bounds = new Rect(20, 10, 300, 200);

    var plan = PdfAnnotationPlan.Build(_project, bounds, 1.0, 200);

    plan.Count.ShouldBe(1);
    // 100 - 20 + 40 - 5
    plan[0].X.ShouldBe(115);
    plan[0].Y.ShouldBe(150);
  }

  [Test]
  public void RoomsWithoutDescriptions_AreSkipped()
  {
    AddRoom(0, 0, 80, 40, RoomShape.SquareCorners, null);
    AddRoom(200, 0, 80, 40, RoomShape.SquareCorners, "Has one");

    var plan = PdfAnnotationPlan.Build(_project, new Rect(0, 0, 400, 100), 1.0, 100);

    plan.Count.ShouldBe(1);
    plan[0].Text.ShouldBe("Has one");
  }

  [Test]
  public void Scale_ScalesPositionAndSizeAndFlipsAgainstPageHeight()
  {
    AddRoom(100, 50, 80, 40, RoomShape.SquareCorners, "Scaled");
    var bounds = new Rect(0, 0, 400, 200);

    var plan = PdfAnnotationPlan.Build(_project, bounds, 0.5, 100);

    plan[0].X.ShouldBe(50);
    plan[0].Width.ShouldBe(5);
    plan[0].Height.ShouldBe(5);
    // 100 - (50 + 10) * 0.5
    plan[0].Y.ShouldBe(70);
  }

  [Test]
  public void NullProject_ReturnsEmpty()
  {
    PdfAnnotationPlan.Build(null, new Rect(0, 0, 10, 10), 1.0, 10).Count.ShouldBe(0);
  }
}

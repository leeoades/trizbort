using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Core.Tests;

/// <summary>The preview of a new room (Disambiguate Rooms) is where automap would put it, and adds nothing.</summary>
[TestFixture]
[Category("Unit")]
public class NewRoomBoundsTests : IsolatedProjectTests
{
  [TestCase(MappableDirection.North)]
  [TestCase(MappableDirection.East)]
  [TestCase(MappableDirection.Up)]
  [TestCase(null)]
  public void NewRoomBounds_IsWhereCreateRoomPutsIt_AndAddsNothing(MappableDirection? direction)
  {
    var hall = ProjectRegressionTests.AddRoom("Hall");
    hall.Position = new Vector(0, 0);
    var canvas = new AutomapCanvas();
    var count = Project.Current.Elements.Count;

    var preview = canvas.NewRoomBounds(hall, direction);

    preview.ShouldNotBeNull();
    Project.Current.Elements.Count.ShouldBe(count);
    var made = direction is { } way ? canvas.CreateRoom(hall, way, "Attic", "Attic") : canvas.CreateRoom(hall, "Attic");
    preview.Value.X.ShouldBe(made.Position.X);
    preview.Value.Y.ShouldBe(made.Position.Y);
  }
}

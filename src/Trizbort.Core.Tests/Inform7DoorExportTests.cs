using System.Text.RegularExpressions;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Export.Languages;

namespace Trizbort.Core.Tests;

/// <summary>
///   Inform 7 declares a door once however many directions lead through it (a connection labelled "north / down");
///   the Inform 6 equivalent is in <see cref="ExporterRegressionTests" />.
/// </summary>
[TestFixture]
[Category("Unit")]
public class Inform7DoorExportTests : IsolatedProjectTests
{
  [Test]
  public void ADoorReachedSeveralWays_IsDeclaredOnce()
  {
    var cliff = ProjectRegressionTests.AddRoom("Cliff");
    var beach = ProjectRegressionTests.AddRoom("Beach");
    var door = new Connection(
      Project.Current,
      new Vertex(cliff.PortAt(CompassPoint.North)),
      new Vertex(beach.PortAt(CompassPoint.South)));
    Project.Current.Elements.Add(door);
    door.Flow = ConnectionFlow.TwoWay;
    door.StartText = "north / down";
    door.EndText = "south / up";
    door.Name = "Gate";
    door.Door = new Door();

    using var exporter = new Inform7Exporter();
    var code = exporter.Export();

    Regex.Matches(code, @"Gate is a door\.").Count.ShouldBe(1);
  }
}

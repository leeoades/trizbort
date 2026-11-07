using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

/// <summary>The map options that write a join's direction at ends with no label of their own.</summary>
[TestFixture]
[Category("Unit")]
public class ConnectionEndLabelsTests : IsolatedProjectTests
{
  private bool _diagonal;
  private bool _compass;

  [SetUp]
  public void RememberSettings()
  {
    _diagonal = Settings.LabelDiagonalExits;
    _compass = Settings.LabelCompassExits;
  }

  [TearDown]
  public void RestoreSettings()
  {
    Settings.LabelDiagonalExits = _diagonal;
    Settings.LabelCompassExits = _compass;
  }

  private static Connection Join(CompassPoint fromSide, CompassPoint toSide)
  {
    var from = ProjectRegressionTests.AddRoom("Hall");
    var to = ProjectRegressionTests.AddRoom("Garden");
    to.Position = new Vector(300, 300);
    var connection = new Connection(Project.Current, new Vertex(from.PortAt(fromSide)), new Vertex(to.PortAt(toSide)));
    Project.Current.Elements.Add(connection);
    return connection;
  }

  [Test]
  public void ANewMap_LabelsDiagonalExitsButNotStraightOnes()
  {
    Settings.Reset();

    Settings.LabelDiagonalExits.ShouldBeTrue();
    Settings.LabelCompassExits.ShouldBeFalse();
  }

  [Test]
  public void DiagonalEnds_ShowTheirDirection()
  {
    Settings.LabelDiagonalExits = true;
    Settings.LabelCompassExits = false;
    var join = Join(CompassPoint.SouthEast, CompassPoint.NorthWest);

    ConnectionEndLabels.Text(join, true).ShouldBe("se");
    ConnectionEndLabels.Text(join, false).ShouldBe("nw");

    Settings.LabelDiagonalExits = false;
    ConnectionEndLabels.Text(join, true).ShouldBeEmpty();
  }

  [Test]
  public void StraightEnds_ShowTheirDirection_OnlyWhenAskedFor()
  {
    Settings.LabelDiagonalExits = true;
    Settings.LabelCompassExits = false;
    var join = Join(CompassPoint.East, CompassPoint.West);
    ConnectionEndLabels.Text(join, true).ShouldBeEmpty();

    Settings.LabelCompassExits = true;
    ConnectionEndLabels.Text(join, true).ShouldBe("e");
    ConnectionEndLabels.Text(join, false).ShouldBe("w");
  }

  [Test]
  public void AnEndsOwnLabel_Wins_AndTheFarEndOfAOneWayJoinIsNotAnExit()
  {
    Settings.LabelDiagonalExits = true;
    var join = Join(CompassPoint.NorthEast, CompassPoint.SouthWest);
    join.StartText = "up";
    join.Flow = ConnectionFlow.OneWay;

    ConnectionEndLabels.Text(join, true).ShouldBe("up");
    ConnectionEndLabels.Text(join, false).ShouldBeEmpty();
  }
}

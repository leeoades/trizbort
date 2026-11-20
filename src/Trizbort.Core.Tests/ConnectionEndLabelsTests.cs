using System.IO;
using System.Xml;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using Trizbort.Util;

namespace Trizbort.Core.Tests;

/// <summary>The map options that write a join's direction at ends with no label of their own.</summary>
[TestFixture]
[Category("Unit")]
public class ConnectionEndLabelsTests : IsolatedProjectTests
{
  private bool _diagonal;
  private bool _compass;
  private bool _upDown;
  private DirectionWordStyle _words;

  [SetUp]
  public void RememberSettings()
  {
    _diagonal = Settings.LabelDiagonalExits;
    _compass = Settings.LabelCompassExits;
    _upDown = Settings.LabelUpDownExits;
    _words = Settings.LabelDirectionWords;
  }

  [TearDown]
  public void RestoreSettings()
  {
    Settings.LabelDiagonalExits = _diagonal;
    Settings.LabelCompassExits = _compass;
    Settings.LabelUpDownExits = _upDown;
    Settings.LabelDirectionWords = _words;
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

  [Test]
  public void DirectionWords_CanBeDrawnShortOrInFull_WhicheverWayTheyWereWritten()
  {
    var join = Join(CompassPoint.East, CompassPoint.West);
    join.StartText = "north / u / climb beanstalk";
    join.EndText = "down";

    Settings.LabelDirectionWords = DirectionWordStyle.AsWritten;
    ConnectionEndLabels.Text(join, true).ShouldBe("north / u / climb beanstalk");

    Settings.LabelDirectionWords = DirectionWordStyle.Short;
    ConnectionEndLabels.Text(join, true).ShouldBe("n / u / climb beanstalk");
    ConnectionEndLabels.Text(join, false).ShouldBe("d");

    Settings.LabelDirectionWords = DirectionWordStyle.Full;
    ConnectionEndLabels.Text(join, true).ShouldBe("north / up / climb beanstalk");

    // only the drawing changes
    join.StartText.ShouldBe("north / u / climb beanstalk");
  }

  [Test]
  public void TheSideOfARoom_IsLabelledInTheMapsWords()
  {
    Settings.LabelDiagonalExits = true;
    Settings.LabelDirectionWords = DirectionWordStyle.Full;
    var join = Join(CompassPoint.SouthEast, CompassPoint.NorthWest);

    ConnectionEndLabels.Text(join, true).ShouldBe("southeast");
  }

  [Test]
  public void UpAndDown_CanBeLeftOffTheMap()
  {
    var join = Join(CompassPoint.East, CompassPoint.West);
    join.StartText = "east / up";
    join.EndText = "d";

    Settings.LabelUpDownExits = true;
    ConnectionEndLabels.Text(join, true).ShouldBe("east / up");

    Settings.LabelUpDownExits = false;
    ConnectionEndLabels.Text(join, true).ShouldBe("east");
    ConnectionEndLabels.Text(join, false).ShouldBeEmpty();
  }

  [Test]
  public void JoinLabelSettings_AreSavedWithTheMap_AndOlderMapsKeepTheDefaults()
  {
    Settings.Reset();
    Settings.LabelUpDownExits.ShouldBeTrue();
    Settings.LabelDirectionWords.ShouldBe(DirectionWordStyle.AsWritten);

    Settings.LabelUpDownExits = false;
    Settings.LabelDirectionWords = DirectionWordStyle.Full;
    var saved = SavedSettings("changed.xml");
    saved.ShouldContain("<labelUpDownExits>no</labelUpDownExits>");
    saved.ShouldContain("<directionWords>full</directionWords>");

    Settings.Reset();
    var document = new XmlDocument();
    document.LoadXml(saved);
    Settings.Load(new XmlElementReader(document.DocumentElement));
    Settings.LabelUpDownExits.ShouldBeFalse();
    Settings.LabelDirectionWords.ShouldBe(DirectionWordStyle.Full);

    Settings.Reset();
    var defaults = SavedSettings("defaults.xml");
    defaults.ShouldNotContain("directionWords");
    defaults.ShouldNotContain("labelUpDownExits");
  }

  private string SavedSettings(string name)
  {
    using (var scribe = XmlScribe.Create(Files.File(name)))
    {
      scribe.StartElement("settings");
      Settings.Save(scribe);
      scribe.EndElement();
    }
    return File.ReadAllText(Files.File(name));
  }

  [Test]
  public void APickedLabel_IsTakenApart_AndPutBackInCompassOrder()
  {
    var (directions, custom, full) = DirectionWords.Pick("up / north / xyzzy / in");
    directions.ShouldBe(new[] { MappableDirection.Up, MappableDirection.North });
    custom.ShouldBe("xyzzy / in");
    full.ShouldBeTrue();
    DirectionWords.Pick("u / n").Full.ShouldBeFalse();

    DirectionWords.Compose(new[] { MappableDirection.Down, MappableDirection.West, MappableDirection.North }, "xyzzy", false)
      .ShouldBe("n / w / d / xyzzy");
    DirectionWords.Compose(new[] { MappableDirection.Up }, "", true).ShouldBe("up");
    DirectionWords.Compose(new MappableDirection[0], "", true).ShouldBeEmpty();
  }
}

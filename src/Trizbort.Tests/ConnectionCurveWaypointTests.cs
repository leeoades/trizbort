using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Util;

namespace Trizbort.Tests
{
  [TestFixture]
  public class ConnectionCurveWaypointTests
  {
    private static Connection createConnection(Project project = null) {
      return new Connection(project ?? new Project(), new Vertex(new Vector(0, 0)), new Vertex(new Vector(100, 0)));
    }

    [Test]
    public void Evaluate_PassesThroughSpanEndpoints()
    {
      var p0 = new Vector(-50, 10);
      var p1 = new Vector(0, 0);
      var p2 = new Vector(40, 30);
      var p3 = new Vector(90, -20);

      CurveGeometry.Evaluate(p0, p1, p2, p3, 0).Distance(p1).ShouldBeLessThan(0.01f);
      CurveGeometry.Evaluate(p0, p1, p2, p3, 1).Distance(p2).ShouldBeLessThan(0.01f);
    }

    [Test]
    public void StraightConnection_OffersOnlyMiddleHandle_AtMidpoint()
    {
      var connection = createConnection();

      connection.HasCurveWaypoints.ShouldBeFalse();
      connection.CanAddCurveWaypoint(CurveWaypoint.Middle).ShouldBeTrue();
      connection.CanAddCurveWaypoint(CurveWaypoint.Quarter).ShouldBeFalse();
      connection.CanAddCurveWaypoint(CurveWaypoint.ThreeQuarter).ShouldBeFalse();
      connection.GetCurveWaypointHandlePosition(CurveWaypoint.Middle).ShouldBe(new Vector(50, 0));
    }

    [Test]
    public void SettingMiddle_BendsLineThroughIt_AndOffersQuarterHandlesOnTheCurve()
    {
      var connection = createConnection();
      var middle = new Vector(50, 60);

      connection.SetCurveWaypoint(CurveWaypoint.Middle, middle);

      connection.HasCurveWaypoints.ShouldBeTrue();
      connection.Distance(middle, false).ShouldBeLessThan(0.01f);
      connection.Distance(new Vector(50, 0), false).ShouldBeGreaterThan(10);
      connection.CanAddCurveWaypoint(CurveWaypoint.Middle).ShouldBeFalse();
      connection.CanAddCurveWaypoint(CurveWaypoint.Quarter).ShouldBeTrue();
      connection.CanAddCurveWaypoint(CurveWaypoint.ThreeQuarter).ShouldBeTrue();

      var quarter = connection.GetCurveWaypointHandlePosition(CurveWaypoint.Quarter);
      var threeQuarter = connection.GetCurveWaypointHandlePosition(CurveWaypoint.ThreeQuarter);
      connection.Distance(quarter, false).ShouldBeLessThan(1f);
      connection.Distance(threeQuarter, false).ShouldBeLessThan(1f);
      quarter.X.ShouldBeLessThan(middle.X);
      threeQuarter.X.ShouldBeGreaterThan(middle.X);
    }

    [Test]
    public void AllThreeWaypoints_LineTravelsThroughEach()
    {
      var connection = createConnection();
      var points = new[] {new Vector(20, 40), new Vector(50, -30), new Vector(80, 40)};

      connection.SetCurveWaypoint(CurveWaypoint.Quarter, points[0]);
      connection.SetCurveWaypoint(CurveWaypoint.Middle, points[1]);
      connection.SetCurveWaypoint(CurveWaypoint.ThreeQuarter, points[2]);

      foreach (var point in points) connection.Distance(point, false).ShouldBeLessThan(0.01f);
      new[] {CurveWaypoint.Quarter, CurveWaypoint.Middle, CurveWaypoint.ThreeQuarter}.Any(connection.CanAddCurveWaypoint).ShouldBeFalse();
    }

    [Test]
    public void RemoveCurveWaypoint_RemovesOnlyThatWaypoint()
    {
      var connection = createConnection();
      connection.SetCurveWaypoint(CurveWaypoint.Middle, new Vector(50, 60));
      connection.SetCurveWaypoint(CurveWaypoint.Quarter, new Vector(25, 40));

      connection.RemoveCurveWaypoint(CurveWaypoint.Middle).ShouldBeTrue();
      connection.RemoveCurveWaypoint(CurveWaypoint.Middle).ShouldBeFalse();

      connection.GetCurveWaypoint(CurveWaypoint.Middle).ShouldBeNull();
      connection.GetCurveWaypoint(CurveWaypoint.Quarter).ShouldBe(new Vector(25, 40));
      connection.CanAddCurveWaypoint(CurveWaypoint.Middle).ShouldBeTrue();
    }

    [Test]
    public void Reverse_SwapsQuarterWaypoints()
    {
      var connection = createConnection();
      connection.SetCurveWaypoint(CurveWaypoint.Quarter, new Vector(25, 40));
      connection.SetCurveWaypoint(CurveWaypoint.ThreeQuarter, new Vector(75, 40));

      connection.Reverse();

      connection.GetCurveWaypoint(CurveWaypoint.Quarter).ShouldBe(new Vector(75, 40));
      connection.GetCurveWaypoint(CurveWaypoint.ThreeQuarter).ShouldBe(new Vector(25, 40));
    }

    [Test]
    public void MoveCurveWaypointsBy_TranslatesWaypoints()
    {
      var connection = createConnection();
      connection.SetCurveWaypoint(CurveWaypoint.Middle, new Vector(50, 60));

      connection.MoveCurveWaypointsBy(new Vector(10, -5));

      connection.GetCurveWaypoint(CurveWaypoint.Middle).ShouldBe(new Vector(60, 55));
    }

    [Test]
    public void SaveAndLoad_RoundTripsWaypoints()
    {
      var project = new Project();
      var connection = createConnection(project);
      connection.SetCurveWaypoint(CurveWaypoint.Middle, new Vector(50.5f, 60.25f));
      connection.SetCurveWaypoint(CurveWaypoint.ThreeQuarter, new Vector(-75, 40));

      var path = Path.GetTempFileName();
      try {
        using (var scribe = XmlScribe.Create(path)) {
          scribe.StartElement("line");
          connection.Save(scribe);
          scribe.EndElement();
        }

        var document = new XmlDocument();
        document.Load(path);
        var loaded = new Connection(project);
        var state = loaded.BeginLoad(new XmlElementReader(document.DocumentElement));
        loaded.EndLoad(state);

        loaded.GetCurveWaypoint(CurveWaypoint.Quarter).ShouldBeNull();
        loaded.GetCurveWaypoint(CurveWaypoint.Middle).ShouldBe(new Vector(50.5f, 60.25f));
        loaded.GetCurveWaypoint(CurveWaypoint.ThreeQuarter).ShouldBe(new Vector(-75, 40));
      } finally {
        File.Delete(path);
      }
    }
  }
}

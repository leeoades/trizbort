using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Tests {
  [TestFixture, Category("Unit")]
  public class GeometryRegressionTests : IsolatedProjectTests {
    [TestCase(0, 0, 0, 0)]
    [TestCase(3, 4, .6f, .8f)]
    [TestCase(-3, -4, -.6f, -.8f)]
    public void Normalize_HandlesZeroAndSignedComponents(float x, float y, float nx, float ny) {
      var normalized = Vector.Normalize(new Vector(x, y));
      normalized.X.ShouldBe(nx, .0001f);
      normalized.Y.ShouldBe(ny, .0001f);
    }

    [TestCase(-20, -10, true)]
    [TestCase(20, 10, true)]
    [TestCase(0, 0, true)]
    [TestCase(-21, 0, false)]
    [TestCase(0, 11, false)]
    public void Rectangle_ContainsIncludesEdges_AndClampProjectsOutsidePoints(float x, float y, bool inside) {
      var rect = new Rect(-20, -10, 40, 20);
      var point = new Vector(x, y);
      rect.Contains(point).ShouldBe(inside);
      rect.Contains(rect.Clamp(point)).ShouldBeTrue();
      if (inside) rect.Clamp(point).ShouldBe(point);
    }

    [Test]
    public void Rectangle_UnionInflateAndIntersectionPreserveBounds() {
      var a = new Rect(-10, -20, 20, 40);
      a.Union(new Rect(15, 25, 10, 5)).ShouldBe(new Rect(-10, -20, 35, 50));
      a.Union(Rect.Empty).ShouldBe(a);
      Rect.Empty.Union(a).ShouldBe(a);
      a.IntersectsWith(new Rect(9, 19, 5, 5)).ShouldBeTrue();
      a.IntersectsWith(new Rect(11, 21, 5, 5)).ShouldBeFalse();
      a.Inflate(2, 3);
      a.ShouldBe(new Rect(-12, -23, 24, 46));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    [TestCase(10)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(13)]
    [TestCase(14)]
    [TestCase(15)]
    public void CompassNamesAndOpposites_RoundTripAllPorts(int index) {
      var direction = (CompassPoint) index;
      CompassPointHelper.ToName(direction, out var name).ShouldBeTrue();
      CompassPointHelper.FromName(name.ToUpperInvariant(), out var parsed).ShouldBeTrue();
      parsed.ShouldBe(direction);
      CompassPointHelper.GetOpposite(CompassPointHelper.GetOpposite(direction)).ShouldBe(direction);
      CompassPointHelper.GetAutomapOpposite(CompassPointHelper.GetAutomapOpposite(direction)).ShouldBe(direction);
      new Rect(-20, -10, 80, 40).Contains(new Rect(-20, -10, 80, 40).GetCorner(direction)).ShouldBeTrue();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("sideways")]
    public void CompassName_RejectsUnknownDirections(string name) {
      CompassPointHelper.FromName(name, out _).ShouldBeFalse();
    }

    [TestCase(-17, -20)]
    [TestCase(-14, -10)]
    [TestCase(0, 0)]
    [TestCase(14, 10)]
    [TestCase(17, 20)]
    public void Snap_UsesGridOnlyWhenEnabled(float input, float snapped) {
      Settings.GridSize = 10;
      Settings.SnapToGrid = true;
      Settings.Snap(input).ShouldBe(snapped);
      Settings.SnapToGrid = false;
      Settings.Snap(input).ShouldBe(input);
    }

    [TestCase(0)]
    [TestCase(4)]
    [TestCase(10)]
    [TestCase(12)]
    public void SegmentShortening_NeverPassesStart(float amount) {
      var segment = new LineSegment(new Vector(-5, 0), new Vector(5, 0));
      segment.Shorten(amount).ShouldBe(amount < 10);
      segment.Length.ShouldBe(System.Math.Max(0, 10 - amount));
      segment.Start.ShouldBe(new Vector(-5, 0));
    }

    [Test]
    public void SegmentIntersection_FindsCrossingAndRejectsSeparatedParallelLines() {
      var a = new LineSegment(new Vector(-10, 0), new Vector(10, 0));
      a.Intersect(new LineSegment(new Vector(0, -10), new Vector(0, 10)), false, out var points).ShouldBeTrue();
      points.Single().Position.ShouldBe(Vector.Zero);
      a.Intersect(new LineSegment(new Vector(-10, 3), new Vector(10, 3)), false, out _).ShouldBeFalse();
      a.IntersectsWith(new Rect(-1, -1, 2, 2)).ShouldBeTrue();
      a.IntersectsWith(new Rect(-1, 2, 2, 2)).ShouldBeFalse();
    }

    [Test]
    public void SegmentIntersection_IgnoredEndpointIsNotAnIntersection() {
      var a = new LineSegment(Vector.Zero, new Vector(10, 0));
      a.Intersect(new LineSegment(new Vector(10, 0), new Vector(10, 10)), true, out var points).ShouldBeFalse();
      points.ShouldBeEmpty();
    }

    [Test]
    public void ApproximateCompassDirections_GroupCardinalNeighboursButNotDiagonals() {
      var groups = new[] {0, 0, 1, 2, 2, 2, 3, 4, 4, 4, 5, 6, 6, 6, 7, 0};
      for (var first = 0; first < groups.Length; first++)
      for (var second = 0; second < groups.Length; second++)
        CompassPointHelper.IsSameApproximateDirection((CompassPoint) first, (CompassPoint) second)
          .ShouldBe(groups[first] == groups[second], first + " vs " + second);
      CompassPointHelper.IsSameApproximateDirection((CompassPoint) (-1), CompassPoint.North).ShouldBeFalse();
      CompassPointHelper.IsSameApproximateDirection((CompassPoint) 16, CompassPoint.North).ShouldBeFalse();
    }

    [Test]
    public void BoundList_AddRemoveAndReversePublishNotificationsInOrder() {
      var list = new BoundList<int>();
      var events = new List<string>();
      list.Added += (_, e) => events.Add("+" + e.Item);
      list.Removed += (_, e) => events.Add("-" + e.Item);
      list.AddRange(new[] {1, 2, 3});
      list.Remove(2);
      list.Reverse();
      list.ShouldBe(new[] {3, 1});
      events.ShouldBe(new[] {"+1", "+2", "+3", "-2", "+3", "+1"});
    }
  }
}

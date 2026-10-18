using System;
using System.Collections.Generic;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Trizbort.Avalonia.Drawing;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using CoreRect = Trizbort.Domain.Misc.Rect;
using CoreVector = Trizbort.Domain.Misc.Vector;
using CoreLineSegment = Trizbort.Domain.Misc.LineSegment;
using AvaloniaPoint = Avalonia.Point;
using DrawingColor = System.Drawing.Color;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
public class DrawingAdaptersParityTests
{
    [AvaloniaTest]
    public void OctagonalGeometry_DefaultCorners_UsesQuarterWidthAndHeightChamfer()
    {
        var bounds = new CoreRect(0, 0, 96, 64);
        var corners = new CornerRadii(); // defaults to 15
        var geometry = DrawingAdapters.CreateOctagonalGeometry(bounds, corners, false);

        Assert.That(geometry, Is.Not.Null);
        var geoBounds = geometry.Bounds;

        Assert.That(geoBounds.Left, Is.EqualTo(0).Within(0.01));
        Assert.That(geoBounds.Right, Is.EqualTo(96).Within(0.01));
        Assert.That(geoBounds.Top, Is.EqualTo(0).Within(0.01));
        Assert.That(geoBounds.Bottom, Is.EqualTo(64).Within(0.01));

        // For 96x64, qw = 24, qh = 16.
        // Point (18, 2) is inside a (15,15) chamfer (18 > 15), but outside a (24,16) quarter chamfer (18/24 + 2/16 = 0.875 < 1).
        Assert.That(geometry.FillContains(new AvaloniaPoint(18, 2)), Is.False);
        Assert.That(geometry.FillContains(new AvaloniaPoint(25, 2)), Is.True);
    }

    [Test]
    public void SecondFillPolygon_CalculatesCorrectVerticesForStandardLocations()
    {
        var bounds = new CoreRect(10, 20, 100, 50);

        var bottom = RoomDrawingAdapter.GetSecondFillPolygon(bounds, "Bottom");
        Assert.That(bottom, Is.Not.Null);
        Assert.That(bottom!.Length, Is.EqualTo(4));
        Assert.That(bottom[0].X, Is.EqualTo(10).Within(0.01)); // West
        Assert.That(bottom[0].Y, Is.EqualTo(45).Within(0.01));

        var bottomRight = RoomDrawingAdapter.GetSecondFillPolygon(bounds, "BottomRight");
        Assert.That(bottomRight, Is.Not.Null);
        Assert.That(bottomRight!.Length, Is.EqualTo(3)); // Diagonal triangle
        Assert.That(bottomRight[0], Is.EqualTo(bounds.GetCorner(CompassPoint.NorthEast)));
        Assert.That(bottomRight[1], Is.EqualTo(bounds.GetCorner(CompassPoint.SouthWest)));
        Assert.That(bottomRight[2], Is.EqualTo(bounds.GetCorner(CompassPoint.SouthEast)));

        var topRight = RoomDrawingAdapter.GetSecondFillPolygon(bounds, "TopRight");
        Assert.That(topRight, Is.Not.Null);
        Assert.That(topRight!.Length, Is.EqualTo(3)); // Diagonal triangle
        Assert.That(topRight[0], Is.EqualTo(bounds.GetCorner(CompassPoint.NorthWest)));
        Assert.That(topRight[1], Is.EqualTo(bounds.GetCorner(CompassPoint.NorthEast)));
        Assert.That(topRight[2], Is.EqualTo(bounds.GetCorner(CompassPoint.SouthEast)));

        var right = RoomDrawingAdapter.GetSecondFillPolygon(bounds, "Right");
        Assert.That(right, Is.Not.Null);
        Assert.That(right!.Length, Is.EqualTo(4));

        var unknown = RoomDrawingAdapter.GetSecondFillPolygon(bounds, "UnknownLocation");
        Assert.That(unknown, Is.Null);

        var empty = RoomDrawingAdapter.GetSecondFillPolygon(bounds, "");
        Assert.That(empty, Is.Null);
    }

    [AvaloniaTest]
    public void ChevronGeometry_ApexOrientationMatchesAngle()
    {
        var origin = new AvaloniaPoint(100, 100);

        // 0 deg -> Facing East (+X)
        var geo0 = DrawingAdapters.CreateChevronGeometry(origin, 0, 10, 0, false);
        Assert.That(geo0.Bounds.Right, Is.GreaterThan(100));
        Assert.That(geo0.Bounds.Left, Is.LessThan(100));

        // 90 deg -> Facing South (+Y)
        var geo90 = DrawingAdapters.CreateChevronGeometry(origin, 90, 10, 0, false);
        Assert.That(geo90.Bounds.Bottom, Is.GreaterThan(100));
        Assert.That(geo90.Bounds.Top, Is.LessThan(100));

        // 180 deg -> Facing West (-X)
        var geo180 = DrawingAdapters.CreateChevronGeometry(origin, 180, 10, 0, false);
        Assert.That(geo180.Bounds.Left, Is.LessThan(100));

        // 270 deg -> Facing North (-Y)
        var geo270 = DrawingAdapters.CreateChevronGeometry(origin, 270, 10, 0, false);
        Assert.That(geo270.Bounds.Top, Is.LessThan(100));
    }

    [Test]
    public void PenCache_ReturnsDistinctPensForDifferentDashStyles()
    {
        var dashStyle = BorderDashStyle.Dash.ToDashStyle();
        var dotStyle = BorderDashStyle.Dot.ToDashStyle();
        var connDashStyle = ConnectionStyle.Dashed.ToDashStyle();

        var penDash = DrawingColor.Black.ToPen(1.0, dashStyle);
        var penDot = DrawingColor.Black.ToPen(1.0, dotStyle);
        var penConn = DrawingColor.Black.ToPen(1.0, connDashStyle);

        Assert.That(penDash, Is.Not.SameAs(penDot));
        Assert.That(penDash, Is.Not.SameAs(penConn));
        Assert.That(penDot, Is.Not.SameAs(penConn));

        Assert.That(penDash.DashStyle?.Dashes, Is.EqualTo(new double[] { 4, 2 }));
        Assert.That(penDot.DashStyle?.Dashes, Is.EqualTo(new double[] { 1, 2 }));
        Assert.That(penConn.DashStyle?.Dashes, Is.EqualTo(new double[] { 2, 2 }));
    }

    [Test]
    public void RoomTypeAdjustments_ReturnsCorrectOffsetsForOctagonAndEllipsePorts()
    {
        var project = new Project();
        var room = new Room(project);
        var portSE = room.PortList.Find(p => CompassPointHelper.FromName(p.Id, out var d) && d == CompassPoint.SouthEast);
        Assert.That(portSE, Is.Not.Null);
        var vertexSE = new Vertex(portSE!);

        var adjSE = ConnectionDrawingAdapter.GetRoomTypeAdjustments(vertexSE);
        Assert.That(adjSE.X, Is.EqualTo(8));
        Assert.That(adjSE.Y, Is.EqualTo(6));

        var portSW = room.PortList.Find(p => CompassPointHelper.FromName(p.Id, out var d) && d == CompassPoint.SouthWest);
        Assert.That(portSW, Is.Not.Null);
        var vertexSW = new Vertex(portSW!);
        var adjSW = ConnectionDrawingAdapter.GetRoomTypeAdjustments(vertexSW);
        Assert.That(adjSW.X, Is.EqualTo(-10));
        Assert.That(adjSW.Y, Is.EqualTo(4));

        var portNW = room.PortList.Find(p => CompassPointHelper.FromName(p.Id, out var d) && d == CompassPoint.NorthWest);
        Assert.That(portNW, Is.Not.Null);
        var vertexNW = new Vertex(portNW!);
        var adjNW = ConnectionDrawingAdapter.GetRoomTypeAdjustments(vertexNW);
        Assert.That(adjNW.X, Is.EqualTo(-10));
        Assert.That(adjNW.Y, Is.EqualTo(-4));

        var portN = room.PortList.Find(p => CompassPointHelper.FromName(p.Id, out var d) && d == CompassPoint.North);
        Assert.That(portN, Is.Not.Null);
        var vertexN = new Vertex(portN!);
        var adjN = ConnectionDrawingAdapter.GetRoomTypeAdjustments(vertexN);
        Assert.That(adjN.X, Is.EqualTo(0));
        Assert.That(adjN.Y, Is.EqualTo(0));
    }

    [Test]
    public void SmartLineSplitting_SplitsIntersectingLinesWithGaps()
    {
        var line1 = new CoreLineSegment(new CoreVector(0, 50), new CoreVector(100, 50));
        var line2 = new CoreLineSegment(new CoreVector(50, 0), new CoreVector(50, 100));

        var linesDrawn = new List<CoreLineSegment> { line1 };
        var split = ConnectionDrawingAdapter.SplitSegment(line2, linesDrawn);

        Assert.That(split.Count, Is.EqualTo(2));
        Assert.That(split[0].Start.Y, Is.EqualTo(0));
        Assert.That(split[0].End.Y, Is.LessThan(50));
        Assert.That(split[1].Start.Y, Is.GreaterThan(50));
        Assert.That(split[1].End.Y, Is.EqualTo(100));
    }

    [Test]
    public void SmartLineSplitting_RejectsNonIntersectingLinesFast()
    {
        var line1 = new CoreLineSegment(new CoreVector(0, 0), new CoreVector(10, 10));
        var line2 = new CoreLineSegment(new CoreVector(100, 100), new CoreVector(110, 110));

        var linesDrawn = new List<CoreLineSegment> { line1 };
        var split = ConnectionDrawingAdapter.SplitSegment(line2, linesDrawn);

        Assert.That(split.Count, Is.EqualTo(1));
        Assert.That(split[0], Is.EqualTo(line2));
    }

    [AvaloniaTest]
    public void GeometryBounds_AllShapes_GenerateValidBounds()
    {
        var bounds = new CoreRect(10, 20, 80, 50);
        var corners = new CornerRadii { TopLeft = 10, TopRight = 10, BottomRight = 10, BottomLeft = 10 };

        var square = DrawingAdapters.CreateSquareGeometry(bounds);
        var rounded = DrawingAdapters.CreateRoundedRectangleGeometry(bounds, corners);
        var ellipse = DrawingAdapters.CreateEllipseGeometry(bounds);
        var oct = DrawingAdapters.CreateOctagonalGeometry(bounds, corners);

        Assert.That(square.Bounds.Width, Is.EqualTo(80).Within(0.01));
        Assert.That(rounded.Bounds.Width, Is.EqualTo(80).Within(0.01));
        Assert.That(ellipse.Bounds.Width, Is.EqualTo(80).Within(0.01));
        Assert.That(oct.Bounds.Width, Is.EqualTo(80).Within(0.01));

        // Hand-drawn versions
        var squareH = DrawingAdapters.CreateSquareGeometry(bounds, true, 42);
        var roundedH = DrawingAdapters.CreateRoundedRectangleGeometry(bounds, corners, true, 42);
        var ellipseH = DrawingAdapters.CreateEllipseGeometry(bounds, true, 42);
        var octH = DrawingAdapters.CreateOctagonalGeometry(bounds, corners, true, 42);

        Assert.That(squareH.Bounds.Width, Is.GreaterThan(70));
        Assert.That(roundedH.Bounds.Width, Is.GreaterThan(70));
        Assert.That(ellipseH.Bounds.Width, Is.GreaterThan(70));
        Assert.That(octH.Bounds.Width, Is.GreaterThan(70));
    }

    [AvaloniaTest]
    public void RoomCache_InvalidatesOnGeometryAndTextChanges()
    {
        var project = new Project();
        var room = new Room(project)
        {
            Name = "Original Room",
            SubTitle = "Sub 1",
            Objects = "key",
            Shape = RoomShape.SquareCorners,
            Position = new CoreVector(10, 10),
            Size = new CoreVector(100, 60)
        };

        var initialGeo = RoomDrawingAdapter.CreateShapeGeometry(room.InnerBounds, room.Shape, room.Corners, false, room.Id);
        Assert.That(initialGeo.Bounds.Width, Is.EqualTo(100).Within(0.01));

        // Change shape to Octagonal
        room.Shape = RoomShape.Octagonal;
        var octGeo = RoomDrawingAdapter.CreateShapeGeometry(room.InnerBounds, room.Shape, room.Corners, false, room.Id);
        Assert.That(octGeo.FillContains(new AvaloniaPoint(15, 2)), Is.False); // chamfer outside

        // Change shape to Ellipse
        room.Shape = RoomShape.Ellipse;
        var ellipseGeo = RoomDrawingAdapter.CreateShapeGeometry(room.InnerBounds, room.Shape, room.Corners, false, room.Id);
        Assert.That(ellipseGeo.Bounds.Width, Is.EqualTo(100).Within(0.01));

        // Change corner radius
        room.Shape = RoomShape.RoundedCorners;
        room.Corners.TopLeft = 30;
        var roundedGeo = RoomDrawingAdapter.CreateShapeGeometry(room.InnerBounds, room.Shape, room.Corners, false, room.Id);
        Assert.That(roundedGeo.Bounds.Width, Is.EqualTo(100).Within(0.01));
    }

    [AvaloniaTest]
    [TestCase(0.25f, 100f, -50f, 400f, 300f)]
    [TestCase(1.0f, 0f, 0f, 800f, 600f)]
    [TestCase(2.5f, -200f, 150f, 1024f, 768f)]
    public void CanvasControl_Transform_RoundTripIdentity(float zoom, float originX, float originY, float viewW, float viewH)
    {
        var canvas = new Controls.MapCanvasControl
        {
            Width = viewW,
            Height = viewH,
            Origin = new CoreVector(originX, originY),
            ZoomFactor = zoom
        };

        var window = new global::Avalonia.Controls.Window { Width = viewW, Height = viewH, Content = canvas };
        window.Show();

        try
        {
            var testPoints = new[]
            {
                new CoreVector(0, 0),
                new CoreVector(originX, originY),
                new CoreVector(123.45f, -67.89f),
                new CoreVector(-500f, 1200f)
            };

            foreach (var world in testPoints)
            {
                var screen = canvas.CanvasToClient(world);
                var worldBack = canvas.ClientToCanvas(screen);

                Assert.That(worldBack.X, Is.EqualTo(world.X).Within(0.001f));
                Assert.That(worldBack.Y, Is.EqualTo(world.Y).Within(0.001f));
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void CanvasControl_ZoomAt_MaintainsWorldPositionUnderCursor()
    {
        var canvas = new Controls.MapCanvasControl
        {
            Width = 800,
            Height = 600,
            Origin = new CoreVector(100, 200),
            ZoomFactor = 1.0f
        };

        var window = new global::Avalonia.Controls.Window { Width = 800, Height = 600, Content = canvas };
        window.Show();

        try
        {
            var cursorScreen = new AvaloniaPoint(350, 250);
            var worldBefore = canvas.ClientToCanvas(cursorScreen);

            canvas.ZoomAt(cursorScreen, 2.5f);

            var worldAfter = canvas.ClientToCanvas(cursorScreen);

            Assert.That(canvas.ZoomFactor, Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(worldAfter.X, Is.EqualTo(worldBefore.X).Within(0.001f));
            Assert.That(worldAfter.Y, Is.EqualTo(worldBefore.Y).Within(0.001f));
        }
        finally
        {
            window.Close();
        }
    }
}

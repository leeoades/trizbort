using Avalonia;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Trizbort.Avalonia.Drawing;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
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
        Assert.That(bottomRight[0], Is.EqualTo(bounds.GetCorner(CompassPoint.SouthWest)));
        Assert.That(bottomRight[1], Is.EqualTo(bounds.GetCorner(CompassPoint.NorthEast)));
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

    [TestCase("Bottom")]
    [TestCase("BottomRight")]
    [TestCase("BottomLeft")]
    [TestCase("Left")]
    [TestCase("Right")]
    [TestCase("TopRight")]
    [TestCase("TopLeft")]
    [TestCase("Top")]
    public void SecondFillPolygon_AllEightLocations_NonEmptyAndInsideBounds(string location)
    {
        var bounds = new CoreRect(10, 20, 100, 50);
        var poly = RoomDrawingAdapter.GetSecondFillPolygon(bounds, location);
        Assert.That(poly, Is.Not.Null);
        Assert.That(poly!.Length, Is.GreaterThanOrEqualTo(3));
        foreach (var p in poly)
        {
            Assert.That(p.X, Is.InRange(10f, 110f));
            Assert.That(p.Y, Is.InRange(20f, 70f));
        }
    }

    [Test]
    public void SecondFillPolygon_AllEightLocations_MatchLegacyGeometry()
    {
        var b = new CoreRect(10, 20, 100, 50);
        var tl = b.GetCorner(CompassPoint.NorthWest);
        var tr = b.GetCorner(CompassPoint.NorthEast);
        var bl = b.GetCorner(CompassPoint.SouthWest);
        var br = b.GetCorner(CompassPoint.SouthEast);
        var tc = b.GetCorner(CompassPoint.North);
        var bc = b.GetCorner(CompassPoint.South);
        var lc = b.GetCorner(CompassPoint.West);
        var rc = b.GetCorner(CompassPoint.East);

        Assert.That(RoomDrawingAdapter.GetSecondFillPolygon(b, "Bottom"), Is.EqualTo(new[] { lc, rc, br, bl }));
        Assert.That(RoomDrawingAdapter.GetSecondFillPolygon(b, "BottomRight"), Is.EqualTo(new[] { bl, tr, br }));
        Assert.That(RoomDrawingAdapter.GetSecondFillPolygon(b, "BottomLeft"), Is.EqualTo(new[] { br, tl, bl }));
        Assert.That(RoomDrawingAdapter.GetSecondFillPolygon(b, "Left"), Is.EqualTo(new[] { tc, tl, bl, bc }));
        Assert.That(RoomDrawingAdapter.GetSecondFillPolygon(b, "Right"), Is.EqualTo(new[] { tc, tr, br, bc }));
        Assert.That(RoomDrawingAdapter.GetSecondFillPolygon(b, "TopRight"), Is.EqualTo(new[] { tl, tr, br }));
        Assert.That(RoomDrawingAdapter.GetSecondFillPolygon(b, "TopLeft"), Is.EqualTo(new[] { tl, tr, bl }));
        Assert.That(RoomDrawingAdapter.GetSecondFillPolygon(b, "Top"), Is.EqualTo(new[] { lc, rc, tr, tl }));
    }

    [AvaloniaTest]
    public void ChevronGeometry_ApexOrientationMatchesAngle()
    {
        var origin = new AvaloniaPoint(100, 100);

        // 0 deg -> Facing East (+X), apex at (105, 100)
        var geo0 = DrawingAdapters.CreateChevronGeometry(origin, 0, 10, 0, false);
        Assert.That(geo0.Bounds.Right, Is.EqualTo(105).Within(0.01));
        Assert.That((geo0.Bounds.Top + geo0.Bounds.Bottom) / 2.0, Is.EqualTo(100).Within(0.01));

        // 90 deg -> Facing South (+Y), apex at (100, 105)
        var geo90 = DrawingAdapters.CreateChevronGeometry(origin, 90, 10, 0, false);
        Assert.That(geo90.Bounds.Bottom, Is.EqualTo(105).Within(0.01));
        Assert.That((geo90.Bounds.Left + geo90.Bounds.Right) / 2.0, Is.EqualTo(100).Within(0.01));

        // 180 deg -> Facing West (-X), apex at (95, 100)
        var geo180 = DrawingAdapters.CreateChevronGeometry(origin, 180, 10, 0, false);
        Assert.That(geo180.Bounds.Left, Is.EqualTo(95).Within(0.01));
        Assert.That((geo180.Bounds.Top + geo180.Bounds.Bottom) / 2.0, Is.EqualTo(100).Within(0.01));

        // 270 deg -> Facing North (-Y), apex at (100, 95)
        var geo270 = DrawingAdapters.CreateChevronGeometry(origin, 270, 10, 0, false);
        Assert.That(geo270.Bounds.Top, Is.EqualTo(95).Within(0.01));
        Assert.That((geo270.Bounds.Left + geo270.Bounds.Right) / 2.0, Is.EqualTo(100).Within(0.01));
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
    public void SmartLineSplitting_ProductionSpatialIndex_SplitsIntersectingLines()
    {
        var spatialIndex = new SpatialLineIndex();
        var line1 = new CoreLineSegment(new CoreVector(0, 50), new CoreVector(100, 50));
        spatialIndex.Add(line1);

        var line2 = new CoreLineSegment(new CoreVector(50, 0), new CoreVector(50, 100));
        var split = ConnectionDrawingAdapter.SplitSegment(line2, spatialIndex);

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
        Assert.That(split[0].Start, Is.EqualTo(line2.Start));
        Assert.That(split[0].End, Is.EqualTo(line2.End));
    }

    [Test]
    public void CrossingConnections_SecondConnectionIsSplitInSpatialRebuild()
    {
        var project = new Project();
        var conn1 = new Connection(project, new Vertex(new CoreVector(0, 50)), new Vertex(new CoreVector(200, 50)));
        var conn2 = new Connection(project, new Vertex(new CoreVector(100, 0)), new Vertex(new CoreVector(100, 100)));
        project.Elements.Add(conn1);
        project.Elements.Add(conn2);

        var spatialIndex = new SpatialLineIndex();
        ConnectionDrawingAdapter.RebuildSpatialIndex(project.Elements, spatialIndex, 1);

        // Conn1 was added first -> 1 segment
        var cache1 = ConnectionDrawingAdapter.GetCachedSmartSegments(conn1, 1);
        Assert.That(cache1.Count, Is.EqualTo(1));

        // Conn2 crosses Conn1 -> Conn2 must have smart segments split into 2 parts around the intersection
        var cache2 = ConnectionDrawingAdapter.GetCachedSmartSegments(conn2, 1);
        Assert.That(cache2.Count, Is.EqualTo(2));
        Assert.That(cache2[0].Start.Y, Is.EqualTo(0));
        Assert.That(cache2[0].End.Y, Is.LessThan(50));
        Assert.That(cache2[1].Start.Y, Is.GreaterThan(50));
        Assert.That(cache2[1].End.Y, Is.EqualTo(100));
    }

    [Test]
    public void SmartLineSplitting_SpatialRebuild_DoesNotSplitAgainstRoomBorders()
    {
        var project = new Project();
        var room = new Room(project)
        {
            Position = new CoreVector(40, 25),
            Size = new CoreVector(40, 50)
        };
        var conn = new Connection(project, new Vertex(new CoreVector(0, 50)), new Vertex(new CoreVector(120, 50)));
        project.Elements.Add(room);
        project.Elements.Add(conn);

        var spatialIndex = new SpatialLineIndex();
        ConnectionDrawingAdapter.RebuildSpatialIndex(project.Elements, spatialIndex, 1);

        var cache = ConnectionDrawingAdapter.GetCachedSmartSegments(conn, 1);
        Assert.That(cache.Count, Is.EqualTo(1));
        Assert.That(cache[0].Start, Is.EqualTo(new CoreVector(0, 50)));
        Assert.That(cache[0].End, Is.EqualTo(new CoreVector(120, 50)));
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

    private static (byte B, byte G, byte R, byte A) ReadPixel(global::Avalonia.Media.Imaging.RenderTargetBitmap bmp, int x, int y)
    {
        var buf = new byte[4];
        var handle = System.Runtime.InteropServices.GCHandle.Alloc(buf, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            bmp.CopyPixels(new PixelRect(x, y, 1, 1), handle.AddrOfPinnedObject(), buf.Length, 4);
        }
        finally
        {
            handle.Free();
        }
        return (buf[0], buf[1], buf[2], buf[3]);
    }

    private static void AssertPixelIsConnectionRed(byte b, byte g, byte r, string because)
    {
        Assert.That(r, Is.GreaterThan(180), because);
        Assert.That(g, Is.LessThan(100), because);
        Assert.That(b, Is.LessThan(100), because);
    }

    private static void AssertPixelIsCanvasBackground(byte b, byte g, byte r, string because)
    {
        Assert.That(r, Is.GreaterThan(200), because);
        Assert.That(g, Is.GreaterThan(200), because);
        Assert.That(b, Is.GreaterThan(200), because);
    }

    private static CoreVector ArrowDirectionAlongStroke(CoreVector strokeDirection, CoreVector flowDirection)
    {
        var sign = strokeDirection.X * flowDirection.X + strokeDirection.Y * flowDirection.Y >= 0 ? 1 : -1;
        return new CoreVector(strokeDirection.X * sign, strokeDirection.Y * sign);
    }

    private static void AssertHandDrawnChevronMatchesStroke(
        global::Avalonia.Media.Geometry rendered,
        CoreVector snapped,
        CoreVector arrowDirection,
        int seed)
    {
        var pos = snapped.ToAvaloniaPoint();
        var angleDeg = Math.Atan2(arrowDirection.Y, arrowDirection.X) * 180.0 / Math.PI;
        var size = (double)Setup.Settings.ConnectionArrowSize;
        var expected = DrawingAdapters.CreateChevronGeometry(pos, angleDeg, size, seed, true);
        var reversed = DrawingAdapters.CreateChevronGeometry(pos, angleDeg + 180.0, size, seed, true);

        Assert.That(rendered.Bounds.Left, Is.EqualTo(expected.Bounds.Left).Within(0.01));
        Assert.That(rendered.Bounds.Right, Is.EqualTo(expected.Bounds.Right).Within(0.01));
        Assert.That(rendered.Bounds.Top, Is.EqualTo(expected.Bounds.Top).Within(0.01));
        Assert.That(rendered.Bounds.Bottom, Is.EqualTo(expected.Bounds.Bottom).Within(0.01));

        // AABB of a 180°-rotated chevron is nearly identical; occupancy against the reversed
        // geometry is what proves the apex follows the snapped stroke tangent.
        var discriminating = 0;
        for (var dx = -8; dx <= 8; dx++)
        {
            for (var dy = -8; dy <= 8; dy++)
            {
                var probe = new AvaloniaPoint(pos.X + dx * 0.5, pos.Y + dy * 0.5);
                var inExpected = expected.FillContains(probe);
                var inReversed = reversed.FillContains(probe);
                if (inExpected == inReversed)
                    continue;
                discriminating++;
                Assert.That(rendered.FillContains(probe), Is.EqualTo(inExpected),
                    $"Rendered chevron orientation does not match stroke tangent at offset ({dx * 0.5}, {dy * 0.5})");
            }
        }

        Assert.That(discriminating, Is.GreaterThan(0),
            "Expected and 180°-reversed chevrons must differ so orientation is actually tested");
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

        using var bmp = new global::Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(200, 200));
        using (var ctx = bmp.CreateDrawingContext())
        {
            // Initial render
            room.DrawRoom(ctx, false, false);
        }

        var geo1 = RoomDrawingAdapter.GetCachedGeometry(room);
        var name1 = RoomDrawingAdapter.GetCachedName(room);
        var nameText1 = RoomDrawingAdapter.GetCachedNameText(room);
        Assert.That(geo1, Is.Not.Null);
        Assert.That(name1, Is.Not.Null);
        Assert.That(nameText1, Is.EqualTo("Original Room"));

        // Change shape to Octagonal & name to "Updated"
        room.Shape = RoomShape.Octagonal;
        room.Name = "Updated Room";
        room.Objects = "gold";

        using (var ctx2 = bmp.CreateDrawingContext())
        {
            // Re-render must invalidate cache and rebuild new shape and text
            room.DrawRoom(ctx2, true, false);
        }

        var geo2 = RoomDrawingAdapter.GetCachedGeometry(room);
        var name2 = RoomDrawingAdapter.GetCachedName(room);
        var nameText2 = RoomDrawingAdapter.GetCachedNameText(room);
        Assert.That(geo2, Is.Not.Null);
        Assert.That(name2, Is.Not.Null);
        Assert.That(nameText2, Is.EqualTo("Updated Room"));
        Assert.That(ReferenceEquals(geo1, geo2), Is.False); // Cached geometry was replaced
        Assert.That(ReferenceEquals(name1, name2), Is.False); // Cached name FormattedText was replaced

        // Verify Octagonal chamfer on new geometry
        Assert.That(geo2!.FillContains(new AvaloniaPoint(15, 2)), Is.False);
    }

    [AvaloniaTest]
    public void CanvasControl_Transform_PixelProbe_MatchesCanvasToClient()
    {
        var project = new Project();
        var prevProj = Project.Current;
        Project.Current = project;

        try
        {
            var room = new Room(project)
            {
                Name = string.Empty,
                Position = new CoreVector(100, 100),
                Size = new CoreVector(60, 60),
                RoomFillColor = DrawingColor.Red,
                RoomBorderColor = DrawingColor.Red
            };
            project.Elements.Add(room);

            var canvas = new Controls.MapCanvasControl
            {
                Width = 400,
                Height = 300,
                Origin = new CoreVector(130, 130), // center of room is at (130, 130)
                ZoomFactor = 2.0f
            };

            var window = new global::Avalonia.Controls.Window { Width = 400, Height = 300, Content = canvas };
            window.Show();

            try
            {
                // Force layout measurement and arrange
                window.Measure(new Size(400, 300));
                window.Arrange(new global::Avalonia.Rect(0, 0, 400, 300));
                canvas.Measure(new Size(400, 300));
                canvas.Arrange(new global::Avalonia.Rect(0, 0, 400, 300));

                using var bmp = new global::Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(400, 300));
                bmp.Render(canvas);

                // World (130, 130) transforms to client center (200, 150)
                var centerScreen = canvas.CanvasToClient(new CoreVector(130, 130));
                Assert.That(centerScreen.X, Is.EqualTo(200).Within(0.01));
                Assert.That(centerScreen.Y, Is.EqualTo(150).Within(0.01));

                var (cb, cg, cr, ca) = ReadPixel(bmp, 200, 150);
                Assert.That(cr, Is.GreaterThan(200));
                Assert.That(cg, Is.LessThan(50));
                Assert.That(cb, Is.LessThan(50));
                Assert.That(ca, Is.GreaterThan(200));

                // Off-center world probe inside room: world (110, 110)
                var offCenterScreen = canvas.CanvasToClient(new CoreVector(110, 110));
                Assert.That(offCenterScreen.X, Is.EqualTo(160).Within(0.01));
                Assert.That(offCenterScreen.Y, Is.EqualTo(110).Within(0.01));

                var (ob, og, or, oa) = ReadPixel(bmp, (int)offCenterScreen.X, (int)offCenterScreen.Y);
                Assert.That(or, Is.GreaterThan(200));
                Assert.That(og, Is.LessThan(50));
                Assert.That(ob, Is.LessThan(50));

                // Background probe outside room: world (10, 10)
                var (bb, bg, br, ba) = ReadPixel(bmp, 10, 10);
                // Canvas background is white
                Assert.That(br, Is.GreaterThan(200));
                Assert.That(bg, Is.GreaterThan(200));
                Assert.That(bb, Is.GreaterThan(200));

                // Test with second origin/zoom configuration: Origin = (0, 0), Zoom = 1.0f
                canvas.Origin = new CoreVector(0, 0);
                canvas.ZoomFactor = 1.0f;
                bmp.Render(canvas);

                // Room at [100, 100, 60, 60], center is (130, 130).
                // With Origin = 0, Zoom = 1: screen is (130 - 0) * 1 + 200 = 330, (130 - 0) * 1 + 150 = 280.
                var roomCenter2 = canvas.CanvasToClient(new CoreVector(130, 130));
                Assert.That(roomCenter2.X, Is.EqualTo(330).Within(0.01));
                Assert.That(roomCenter2.Y, Is.EqualTo(280).Within(0.01));

                var (cb2, cg2, cr2, ca2) = ReadPixel(bmp, 330, 280);
                Assert.That(cr2, Is.GreaterThan(200));
                Assert.That(cg2, Is.LessThan(50));
                Assert.That(cb2, Is.LessThan(50));

                // Client center (200, 150) is now empty canvas background
                var (centerB2, centerG2, centerR2, _) = ReadPixel(bmp, 200, 150);
                Assert.That(centerR2, Is.GreaterThan(200));
                Assert.That(centerG2, Is.GreaterThan(200));
                Assert.That(centerB2, Is.GreaterThan(200));
            }
            finally
            {
                window.Close();
            }
        }
        finally
        {
            Project.Current = prevProj;
        }
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

    [AvaloniaTest]
    [TestCase(0, 0, 100, 0, 0)]       // East
    [TestCase(100, 0, 0, 0, 180)]     // West
    [TestCase(0, 0, 0, 100, 90)]      // South
    [TestCase(0, 100, 0, 0, -90)]     // North
    [TestCase(0, 0, 100, 100, 45)]    // South-East
    [TestCase(0, 100, 100, 0, -45)]   // North-East
    public void ConnectionDrawing_OneWayChevron_OrientedAlongLineDirection(float x1, float y1, float x2, float y2, double expectedAngleDeg)
    {
        var project = new Project();
        var conn = new Connection(project, new Vertex(new CoreVector(x1, y1)), new Vertex(new CoreVector(x2, y2)))
        {
            Flow = ConnectionFlow.OneWay
        };

        var group = new global::Avalonia.Media.DrawingGroup();
        using (var ctx = group.Open())
        {
            conn.DrawConnection(ctx, isSelected: false, isHover: false, linesDrawn: null);
        }

        var geometryDrawings = group.Children.OfType<global::Avalonia.Media.GeometryDrawing>().ToList();
        Assert.That(geometryDrawings.Count, Is.GreaterThanOrEqualTo(2));

        var chevronDrawing = geometryDrawings.Last();
        Assert.That(chevronDrawing.Brush, Is.Not.Null);
        Assert.That(chevronDrawing.Geometry, Is.Not.Null);

        var midPoint = new AvaloniaPoint((x1 + x2) / 2.0, (y1 + y2) / 2.0);
        var expectedChevron = DrawingAdapters.CreateChevronGeometry(midPoint, expectedAngleDeg, Setup.Settings.ConnectionArrowSize, conn.Id, false);

        Assert.That(chevronDrawing.Geometry!.Bounds.Left, Is.EqualTo(expectedChevron.Bounds.Left).Within(0.01));
        Assert.That(chevronDrawing.Geometry!.Bounds.Right, Is.EqualTo(expectedChevron.Bounds.Right).Within(0.01));
        Assert.That(chevronDrawing.Geometry!.Bounds.Top, Is.EqualTo(expectedChevron.Bounds.Top).Within(0.01));
        Assert.That(chevronDrawing.Geometry!.Bounds.Bottom, Is.EqualTo(expectedChevron.Bounds.Bottom).Within(0.01));
    }

    [AvaloniaTest]
    public void ConnectionDrawing_OneWayChevron_RespectsCustomArrowSize()
    {
        var prevArrowSize = Setup.Settings.ConnectionArrowSize;
        Setup.Settings.ConnectionArrowSize = 24.0f;
        try
        {
            var project = new Project();
            var conn = new Connection(project, new Vertex(new CoreVector(0, 0)), new Vertex(new CoreVector(100, 0)))
            {
                Flow = ConnectionFlow.OneWay
            };

            var group = new global::Avalonia.Media.DrawingGroup();
            using (var ctx = group.Open())
            {
                conn.DrawConnection(ctx, isSelected: false, isHover: false, linesDrawn: null);
            }

            var geometryDrawings = group.Children.OfType<global::Avalonia.Media.GeometryDrawing>().ToList();
            var chevronDrawing = geometryDrawings.Last();
            var midPoint = new AvaloniaPoint(50, 0);
            var expectedChevron = DrawingAdapters.CreateChevronGeometry(midPoint, 0, 24.0, conn.Id, false);

            Assert.That(chevronDrawing.Geometry!.Bounds.Width, Is.EqualTo(expectedChevron.Bounds.Width).Within(0.01));
            Assert.That(chevronDrawing.Geometry!.Bounds.Height, Is.EqualTo(expectedChevron.Bounds.Height).Within(0.01));
        }
        finally
        {
            Setup.Settings.ConnectionArrowSize = prevArrowSize;
        }
    }

    [AvaloniaTest]
    public void ConnectionDrawing_OneWayChevron_ShortSegment_NoChevron()
    {
        var project = new Project();
        // Segment length 8 is smaller than default ConnectionArrowSize 12
        var conn = new Connection(project, new Vertex(new CoreVector(0, 0)), new Vertex(new CoreVector(8, 0)))
        {
            Flow = ConnectionFlow.OneWay
        };

        var group = new global::Avalonia.Media.DrawingGroup();
        using (var ctx = group.Open())
        {
            conn.DrawConnection(ctx, isSelected: false, isHover: false, linesDrawn: null);
        }

        // Only line should be drawn, no chevron drawing with brush
        var chevronDrawings = group.Children.OfType<global::Avalonia.Media.GeometryDrawing>().Where(d => d.Brush != null).ToList();
        Assert.That(chevronDrawings.Count, Is.EqualTo(0));
    }

    [AvaloniaTest]
    public void ConnectionDrawing_OneWayChevron_Curved_OneChevronPerSpan()
    {
        var project = new Project();
        var conn = new Connection(project, new Vertex(new CoreVector(0, 0)), new Vertex(new CoreVector(200, 0)))
        {
            Flow = ConnectionFlow.OneWay,
            Style = ConnectionStyle.Solid
        };
        conn.SetCurveWaypoint(0, new CoreVector(100, 50)); // Middle waypoint bends the curve

        var group = new global::Avalonia.Media.DrawingGroup();
        using (var ctx = group.Open())
        {
            conn.DrawConnection(ctx, isSelected: false, isHover: false, linesDrawn: null);
        }

        var chevronDrawings = group.Children.OfType<global::Avalonia.Media.GeometryDrawing>().Where(d => d.Brush != null).ToList();
        conn.GetCurvedSegments(out var spans);
        Assert.That(spans.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(chevronDrawings.Count, Is.EqualTo(spans.Count));

        for (var i = 0; i < spans.Count; i++)
        {
            var mid = CurveGeometry.PolylineMidpoint(spans[i], out var dir);
            var expectedChevron = DrawingAdapters.CreateChevronGeometry(mid.ToAvaloniaPoint(), Math.Atan2(dir.Y, dir.X) * 180.0 / Math.PI, Setup.Settings.ConnectionArrowSize, conn.Id, false);

            Assert.That(chevronDrawings[i].Geometry!.Bounds.Left, Is.EqualTo(expectedChevron.Bounds.Left).Within(0.1));
            Assert.That(chevronDrawings[i].Geometry!.Bounds.Top, Is.EqualTo(expectedChevron.Bounds.Top).Within(0.1));
        }
    }

    [AvaloniaTest]
    public void ConnectionDrawing_OneWayChevron_HandDrawn_SnapsToStrokeAndFollowsTangent()
    {
        var prevHandDrawn = Setup.Settings.HandDrawn;
        Setup.Settings.HandDrawn = true;
        try
        {
            var project = new Project();
            // Straight connection
            var connStraight = new Connection(project, new Vertex(new CoreVector(10, 20)), new Vertex(new CoreVector(110, 20)))
            {
                Flow = ConnectionFlow.OneWay,
                ConnectionColor = DrawingColor.DarkRed
            };

            var groupStraight = new global::Avalonia.Media.DrawingGroup();
            using (var ctx = groupStraight.Open())
            {
                connStraight.DrawConnection(ctx, isSelected: false, isHover: false, linesDrawn: null);
            }

            var straightChevrons = groupStraight.Children.OfType<global::Avalonia.Media.GeometryDrawing>().Where(d => d.Brush != null).ToList();
            Assert.That(straightChevrons.Count, Is.EqualTo(1));

            var straightChevron = straightChevrons.Single();
            var solidBrush = straightChevron.Brush as global::Avalonia.Media.ISolidColorBrush;
            Assert.That(solidBrush, Is.Not.Null);
            Assert.That(solidBrush!.Color.R, Is.EqualTo(DrawingColor.DarkRed.R));
            Assert.That(solidBrush!.Color.G, Is.EqualTo(DrawingColor.DarkRed.G));
            Assert.That(solidBrush!.Color.B, Is.EqualTo(DrawingColor.DarkRed.B));

            // Verify snapping onto the sketched stroke
            var stroke = Sketch.Line(new CoreVector(10, 20), new CoreVector(110, 20), Sketch.Seeded(connStraight.Id));
            var snappedPoint = Sketch.Nearest(stroke, new CoreVector(60, 20), out var strokeDir);

            var bounds = straightChevron.Geometry!.Bounds;
            var chevronCenter = bounds.Center;
            Assert.That(chevronCenter.X, Is.EqualTo(snappedPoint.X).Within(2.0));
            Assert.That(chevronCenter.Y, Is.EqualTo(snappedPoint.Y).Within(2.0));

            var straightFlow = new CoreVector(100, 0);
            var straightArrowDir = ArrowDirectionAlongStroke(strokeDir, straightFlow);
            AssertHandDrawnChevronMatchesStroke(
                straightChevron.Geometry!,
                snappedPoint,
                straightArrowDir,
                connStraight.Id + 5000);

            // Curved hand-drawn connection
            var connCurved = new Connection(project, new Vertex(new CoreVector(0, 0)), new Vertex(new CoreVector(200, 0)))
            {
                Flow = ConnectionFlow.OneWay
            };
            connCurved.SetCurveWaypoint(0, new CoreVector(100, 50));

            var groupCurved = new global::Avalonia.Media.DrawingGroup();
            using (var ctx = groupCurved.Open())
            {
                connCurved.DrawConnection(ctx, isSelected: false, isHover: false, linesDrawn: null);
            }

            var curvedChevrons = groupCurved.Children.OfType<global::Avalonia.Media.GeometryDrawing>().Where(d => d.Brush != null).ToList();
            connCurved.GetCurvedSegments(out var spans);
            Assert.That(curvedChevrons.Count, Is.EqualTo(spans.Count));

            var curveSegments = connCurved.GetSegments();
            Assert.That(curveSegments, Is.Not.Null);
            Assert.That(curveSegments!.Count, Is.GreaterThan(0));
            var curvePoints = new List<CoreVector>(curveSegments.Count + 1);
            foreach (var seg in curveSegments)
                curvePoints.Add(seg.Start);
            curvePoints.Add(curveSegments[^1].End);
            var sketchedPolyline = Sketch.Polyline(curvePoints, Sketch.Seeded(connCurved.Id));

            for (var i = 0; i < spans.Count; i++)
            {
                var mid = CurveGeometry.PolylineMidpoint(spans[i], out var spanDir);
                var snappedCurve = Sketch.Nearest(sketchedPolyline, mid, out var curveStrokeDir);
                Assert.That(curvedChevrons[i].Geometry, Is.Not.Null);

                var curveArrowDir = ArrowDirectionAlongStroke(curveStrokeDir, spanDir);
                AssertHandDrawnChevronMatchesStroke(
                    curvedChevrons[i].Geometry!,
                    snappedCurve,
                    curveArrowDir,
                    connCurved.Id + 5000);
            }
        }
        finally
        {
            Setup.Settings.HandDrawn = prevHandDrawn;
        }
    }

    [AvaloniaTest]
    public void ConnectionDrawing_DockedRooms_MultiSegments_PositionsDirectionsAndReversal()
    {
        var project = new Project();

        // Room A at (0, 0, 96, 64) and Room B at (240, 0, 96, 64)
        var roomA = new Room(project)
        {
            Position = new CoreVector(0, 0),
            Size = new CoreVector(96, 64),
            Name = "Room A"
        };
        project.Elements.Add(roomA);

        var roomB = new Room(project)
        {
            Position = new CoreVector(240, 0),
            Size = new CoreVector(96, 64),
            Name = "Room B"
        };
        project.Elements.Add(roomB);

        var portAEast = roomA.PortAt(CompassPoint.East);
        var portBWest = roomB.PortAt(CompassPoint.West);

        // 1. Forward direction: A(East) -> B(West)
        var connForward = new Connection(project, new Vertex(portAEast), new Vertex(portBWest))
        {
            Flow = ConnectionFlow.OneWay
        };

        var groupForward = new global::Avalonia.Media.DrawingGroup();
        using (var ctx = groupForward.Open())
        {
            connForward.DrawConnection(ctx, isSelected: false, isHover: false, linesDrawn: null);
        }

        var chevronsForward = groupForward.Children.OfType<global::Avalonia.Media.GeometryDrawing>().Where(d => d.Brush != null).ToList();
        // 4 segments of lengths 16, 56, 56, 16 > arrowSize(12) -> 4 chevrons
        Assert.That(chevronsForward.Count, Is.EqualTo(4));

        var expectedForwardX = new[] { 104.0, 140.0, 196.0, 232.0 };
        for (var i = 0; i < 4; i++)
        {
            var bounds = chevronsForward[i].Geometry!.Bounds;
            Assert.That(bounds.Center.X, Is.EqualTo(expectedForwardX[i]).Within(0.5));
            Assert.That(bounds.Center.Y, Is.EqualTo(32.0).Within(0.5));
            // Points East -> right bound is further from center than left bound
            var apexX = bounds.Right;
            var baseX = bounds.Left;
            Assert.That(apexX - bounds.Center.X, Is.GreaterThan(0));
            Assert.That(apexX, Is.EqualTo(expectedForwardX[i] + 6.0).Within(0.1));
            Assert.That(baseX, Is.EqualTo(expectedForwardX[i] - 6.0).Within(0.1));
        }

        // 2. Reversed direction: B(West) -> A(East)
        var connReverse = new Connection(project, new Vertex(portBWest), new Vertex(portAEast))
        {
            Flow = ConnectionFlow.OneWay
        };

        var groupReverse = new global::Avalonia.Media.DrawingGroup();
        using (var ctx = groupReverse.Open())
        {
            connReverse.DrawConnection(ctx, isSelected: false, isHover: false, linesDrawn: null);
        }

        var chevronsReverse = groupReverse.Children.OfType<global::Avalonia.Media.GeometryDrawing>().Where(d => d.Brush != null).ToList();
        Assert.That(chevronsReverse.Count, Is.EqualTo(4));

        var expectedReverseX = new[] { 232.0, 196.0, 140.0, 104.0 };
        for (var i = 0; i < 4; i++)
        {
            var bounds = chevronsReverse[i].Geometry!.Bounds;
            Assert.That(bounds.Center.X, Is.EqualTo(expectedReverseX[i]).Within(0.5));
            Assert.That(bounds.Center.Y, Is.EqualTo(32.0).Within(0.5));
            // Points West -> apex is at Left (base is at Right)
            var apexX = bounds.Left;
            var baseX = bounds.Right;
            Assert.That(bounds.Center.X - apexX, Is.GreaterThan(0));
            Assert.That(apexX, Is.EqualTo(expectedReverseX[i] - 6.0).Within(0.1));
            Assert.That(baseX, Is.EqualTo(expectedReverseX[i] + 6.0).Within(0.1));
        }

        // 3. With spatial line index (crossing lines)
        var crossingLine = new CoreLineSegment(new CoreVector(140, 0), new CoreVector(140, 100));
        var linesDrawn = new List<CoreLineSegment> { crossingLine };

        var groupSpatial = new global::Avalonia.Media.DrawingGroup();
        using (var ctx = groupSpatial.Open())
        {
            connForward.DrawConnection(ctx, isSelected: false, isHover: false, linesDrawn: linesDrawn);
        }

        var chevronsSpatial = groupSpatial.Children.OfType<global::Avalonia.Media.GeometryDrawing>().Where(d => d.Brush != null).ToList();
        Assert.That(chevronsSpatial.Count, Is.GreaterThanOrEqualTo(3));
        foreach (var chev in chevronsSpatial)
        {
            Assert.That(chev.Geometry!.Bounds.Center.Y, Is.EqualTo(32.0).Within(0.5));
            // All remain pointing East
            Assert.That(chev.Geometry!.Bounds.Right - chev.Geometry!.Bounds.Center.X, Is.GreaterThan(0));
        }
    }

    [AvaloniaTest]
    public void ConnectionDrawing_DockedRooms_PixelProbe_VisibleAndClearOfRoomBorder()
    {
        var prevProj = Project.Current;
        var prevHandDrawn = Setup.Settings.HandDrawn;
        Setup.Settings.HandDrawn = false;
        try
        {
            var project = new Project();
            Project.Current = project;

            // Room A at (0, 0, 96, 64)
            var roomA = new Room(project)
            {
                Position = new CoreVector(0, 0),
                Size = new CoreVector(96, 64),
                Name = "Room A"
            };
            project.Elements.Add(roomA);

            // Room B at (240, 0, 96, 64)
            var roomB = new Room(project)
            {
                Position = new CoreVector(240, 0),
                Size = new CoreVector(96, 64),
                Name = "Room B"
            };
            project.Elements.Add(roomB);

            // Connection A(East) -> B(West)
            var portA = roomA.PortAt(CompassPoint.East);
            var portB = roomB.PortAt(CompassPoint.West);

            var conn = new Connection(project, new Vertex(portA), new Vertex(portB))
            {
                Flow = ConnectionFlow.OneWay,
                ConnectionColor = DrawingColor.Red
            };
            project.Elements.Add(conn);

            // Test rendering through MapCanvasControl at Zoom 1.0
            var canvas = new Controls.MapCanvasControl
            {
                Width = 400,
                Height = 300,
                Origin = new CoreVector(168, 32), // midpoint of line is at (168, 32)
                ZoomFactor = 1.0f
            };

            var window = new global::Avalonia.Controls.Window { Width = 400, Height = 300, Content = canvas };
            window.Show();

            try
            {
                window.Measure(new Size(400, 300));
                window.Arrange(new global::Avalonia.Rect(0, 0, 400, 300));
                canvas.Measure(new Size(400, 300));
                canvas.Arrange(new global::Avalonia.Rect(0, 0, 400, 300));

                using var bmp = new global::Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(400, 300));
                bmp.Render(canvas);

                // World midpoint (168, 32) maps to client center (200, 150)
                var centerScreen = canvas.CanvasToClient(new CoreVector(168, 32));
                Assert.That(centerScreen.X, Is.EqualTo(200).Within(0.01));
                Assert.That(centerScreen.Y, Is.EqualTo(150).Within(0.01));

                // 1. OneWay at Zoom 1.0:
                // Chevron at (140, 32) spans world X [134..146], Y [26..38].
                // Centerline is at Y=32 (line width 2px -> Y [31..33]).
                // Off-center points at world (136, 29) and (136, 35) are exclusively inside the chevron wing.
                var wingTopScreen1 = canvas.CanvasToClient(new CoreVector(136, 29));
                var (bTop1, gTop1, rTop1, aTop1) = ReadPixel(bmp, (int)wingTopScreen1.X, (int)wingTopScreen1.Y);
                Assert.That(rTop1, Is.GreaterThan(180), "Upper chevron wing at world (136, 29) should be red in OneWay");
                Assert.That(gTop1, Is.LessThan(100));
                Assert.That(bTop1, Is.LessThan(100));

                var wingBotScreen1 = canvas.CanvasToClient(new CoreVector(136, 35));
                var (bBot1, gBot1, rBot1, aBot1) = ReadPixel(bmp, (int)wingBotScreen1.X, (int)wingBotScreen1.Y);
                Assert.That(rBot1, Is.GreaterThan(180), "Lower chevron wing at world (136, 35) should be red in OneWay");
                Assert.That(gBot1, Is.LessThan(100));
                Assert.That(bBot1, Is.LessThan(100));

                // Point just past the apex off the line at world (147, 29) should be background (white)
                var pastApexScreen1 = canvas.CanvasToClient(new CoreVector(147, 29));
                var (bPast1, gPast1, rPast1, _) = ReadPixel(bmp, (int)pastApexScreen1.X, (int)pastApexScreen1.Y);
                AssertPixelIsCanvasBackground(bPast1, gPast1, rPast1, "Point past apex off line should be canvas background");

                // Last chevron is at (232, 32); apex at x=238, Room B border at x=240.
                // Off-line wing of the last chevron must be red, and the gap between the chevron
                // and the room border, off the line, must stay canvas background.
                var lastWingScreen = canvas.CanvasToClient(new CoreVector(228, 29));
                var (bLastWing, gLastWing, rLastWing, _) = ReadPixel(bmp, (int)lastWingScreen.X, (int)lastWingScreen.Y);
                AssertPixelIsConnectionRed(bLastWing, gLastWing, rLastWing, "Last chevron off-line wing at world (228, 29) should be red in OneWay");

                // Room B's 2px border is centred on x=240, so x=239 is inside the stroke.
                // (236, 29) sits past the last chevron (which only reaches x≈232 at y=29) and short of the border.
                var clearanceScreen = canvas.CanvasToClient(new CoreVector(236, 29));
                var (bClear, gClear, rClear, _) = ReadPixel(bmp, (int)clearanceScreen.X, (int)clearanceScreen.Y);
                AssertPixelIsCanvasBackground(bClear, gClear, rClear, "Off-line pixel between last chevron and Room B border should be canvas background");

                // 2. OneWay at Zoom 0.5:
                canvas.ZoomFactor = 0.5f;
                bmp.Render(canvas);
                var arrowCenterZoomed = canvas.CanvasToClient(new CoreVector(140, 32));
                // Pixel (x-2, y-2) has its centre 1.5 px above the line; (x-2, y+1) mirrors that below.
                var (bTopZ, gTopZ, rTopZ, _) = ReadPixel(bmp, (int)arrowCenterZoomed.X - 2, (int)arrowCenterZoomed.Y - 2);
                AssertPixelIsConnectionRed(bTopZ, gTopZ, rTopZ, "Upper chevron wing at zoom 0.5 should be red in OneWay");

                var (bBotZ, gBotZ, rBotZ, _) = ReadPixel(bmp, (int)arrowCenterZoomed.X - 2, (int)arrowCenterZoomed.Y + 1);
                AssertPixelIsConnectionRed(bBotZ, gBotZ, rBotZ, "Lower chevron wing at zoom 0.5 should be red in OneWay");

                // 3. TwoWay Control:
                // When flow is TwoWay, no chevrons should be drawn, so off-center wing points MUST be background.
                conn.Flow = ConnectionFlow.TwoWay;
                canvas.ZoomFactor = 1.0f;
                bmp.Render(canvas);

                var (bTop2Way, gTop2Way, rTop2Way, _) = ReadPixel(bmp, (int)wingTopScreen1.X, (int)wingTopScreen1.Y);
                AssertPixelIsCanvasBackground(bTop2Way, gTop2Way, rTop2Way, "TwoWay connection should NOT render chevron: upper wing pixel must be canvas background");

                var (bBot2Way, gBot2Way, rBot2Way, _) = ReadPixel(bmp, (int)wingBotScreen1.X, (int)wingBotScreen1.Y);
                AssertPixelIsCanvasBackground(bBot2Way, gBot2Way, rBot2Way, "TwoWay connection should NOT render chevron: lower wing pixel must be canvas background");

                // However, centerline itself is still red on TwoWay
                var (bLine2Way, gLine2Way, rLine2Way, _) = ReadPixel(bmp, (int)centerScreen.X, (int)centerScreen.Y);
                AssertPixelIsConnectionRed(bLine2Way, gLine2Way, rLine2Way, "Line centerline remains red on TwoWay");

                // TwoWay at Zoom 0.5:
                canvas.ZoomFactor = 0.5f;
                bmp.Render(canvas);
                var (bTopZ2Way, gTopZ2Way, rTopZ2Way, _) = ReadPixel(bmp, (int)arrowCenterZoomed.X - 2, (int)arrowCenterZoomed.Y - 2);
                AssertPixelIsCanvasBackground(bTopZ2Way, gTopZ2Way, rTopZ2Way, "TwoWay at zoom 0.5 must have canvas background at upper chevron wing position");

                var (bBotZ2Way, gBotZ2Way, rBotZ2Way, _) = ReadPixel(bmp, (int)arrowCenterZoomed.X - 2, (int)arrowCenterZoomed.Y + 1);
                AssertPixelIsCanvasBackground(bBotZ2Way, gBotZ2Way, rBotZ2Way, "TwoWay at zoom 0.5 must have canvas background at lower chevron wing position");
            }
            finally
            {
                window.Close();
            }
        }
        finally
        {
            Setup.Settings.HandDrawn = prevHandDrawn;
            Project.Current = prevProj;
        }
    }
}

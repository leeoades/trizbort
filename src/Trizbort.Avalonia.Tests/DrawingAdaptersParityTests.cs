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
}

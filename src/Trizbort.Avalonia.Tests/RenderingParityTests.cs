using Avalonia.Headless.NUnit;
using Avalonia.Media;
using NUnit.Framework;
using Trizbort.Avalonia.Drawing;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using CoreRect = Trizbort.Domain.Misc.Rect;
using CoreVector = Trizbort.Domain.Misc.Vector;
using DrawingColor = System.Drawing.Color;

namespace Trizbort.Avalonia.Tests;

/// <summary>Focused tests for the rendering differences found by the legacy-vs-new export comparison (I-025 R1..R8).</summary>
[TestFixture]
[NonParallelizable]
public class RenderingParityTests
{
    private Project? _previousProject;
    private Project? _project;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        _project = new Project();
        Project.Current = _project;
        Settings.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        Settings.Reset();
        Project.Current = _previousProject!;
        _project?.Dispose();
    }

    private static List<global::Avalonia.Media.Drawing> Flatten(DrawingGroup group)
    {
        var result = new List<global::Avalonia.Media.Drawing>();
        foreach (var child in group.Children)
        {
            if (child is DrawingGroup nested) result.AddRange(Flatten(nested));
            else result.Add(child);
        }

        return result;
    }

    private Room NewRoom(string name, float width = 100, float height = 60)
    {
        return new Room(_project!)
        {
            Name = name,
            Position = new CoreVector(10, 10),
            Size = new CoreVector(width, height)
        };
    }

    // R1: legacy Room.cs:912-916 uses the region's text colour unless the room has its own name colour.
    [Test]
    public void R1_RoomNameColor_UsesRegionTextColorUnlessRoomHasOwn()
    {
        var edge = new Region { RegionName = "Edge", RColor = DrawingColor.Blue, TextColor = DrawingColor.Yellow };
        Settings.Regions.Add(edge);
        try
        {
            var room = NewRoom("Edge room");
            room.Region = "Edge";

            Assert.That(RoomDrawingAdapter.ResolveRoomNameColor(room).ToArgb(), Is.EqualTo(DrawingColor.Yellow.ToArgb()));

            room.RoomNameColor = DrawingColor.Red;
            Assert.That(RoomDrawingAdapter.ResolveRoomNameColor(room).ToArgb(), Is.EqualTo(DrawingColor.Red.ToArgb()));

            var plain = NewRoom("Plain");
            var defaultRegion = Settings.Regions.Find(r => r.RegionName == Region.DefaultRegion)!;
            Assert.That(RoomDrawingAdapter.ResolveRoomNameColor(plain).ToArgb(), Is.EqualTo(defaultRegion.TextColor.ToArgb()));
        }
        finally
        {
            Settings.Regions.Remove(edge);
        }
    }

    // R3: legacy Palette.DashedLinePen uses DashStyle.Dot (1 on, 1 off, flat dashes).
    [Test]
    public void R3_DashedConnectionStyle_IsFineDots()
    {
        var pen = DrawingColor.Black.ToPen(2.0, ConnectionStyle.Dashed.ToDashStyle());

        Assert.That(pen.DashStyle?.Dashes, Is.EqualTo(new double[] { 1, 1 }));
        Assert.That(pen.LineCap, Is.EqualTo(PenLineCap.Flat));
    }

    // R4: legacy Room.cs:803 skips fill and border when BorderStyle is None (spiral "Fall to Carriage").
    [AvaloniaTest]
    public void R4_RoomWithNoBorderStyle_DrawsNoFillOrBorder()
    {
        var room = NewRoom("Fall to Carriage");
        room.BorderStyle = BorderDashStyle.None;
        var group = new DrawingGroup();
        using (var ctx = group.Open())
        {
            room.DrawRoom(ctx, false, false);
        }

        Assert.That(Flatten(group).OfType<GeometryDrawing>().Count(), Is.EqualTo(0));

        var solid = NewRoom("Solid");
        solid.BorderStyle = BorderDashStyle.Solid;
        var solidGroup = new DrawingGroup();
        using (var ctx = solidGroup.Open())
        {
            solid.DrawRoom(ctx, false, false);
        }

        Assert.That(Flatten(solidGroup).OfType<GeometryDrawing>().Count(), Is.GreaterThan(0));
    }

    // R5: legacy Room.cs:889-917, triangle in the top-right corner sized by the stripe size / shape.
    [Test]
    public void R5_DarknessTriangle_TopRightCornerAndSized()
    {
        var bounds = new CoreRect(10, 20, 100, 60);

        var square = RoomDrawingAdapter.GetDarknessTriangle(bounds, RoomShape.SquareCorners, null, 24);
        Assert.That(square[0], Is.EqualTo(new CoreVector(110, 20)));
        Assert.That(square[1], Is.EqualTo(new CoreVector(86, 20)));
        Assert.That(square[2], Is.EqualTo(new CoreVector(110, 44)));

        var big = RoomDrawingAdapter.GetDarknessTriangle(bounds, RoomShape.SquareCorners, null, 48);
        Assert.That(big[1], Is.EqualTo(new CoreVector(62, 20)));
        Assert.That(big[2], Is.EqualTo(new CoreVector(110, 68)));

        var ellipse = RoomDrawingAdapter.GetDarknessTriangle(bounds, RoomShape.Ellipse, null, 24);
        Assert.That(ellipse[1], Is.EqualTo(new CoreVector(110 - 40, 20)));
        Assert.That(ellipse[2], Is.EqualTo(new CoreVector(110, 20 + 24)));

        var octagon = RoomDrawingAdapter.GetDarknessTriangle(bounds, RoomShape.Octagonal, null, 24);
        Assert.That(octagon[1], Is.EqualTo(new CoreVector(110 - 35, 20)));
        Assert.That(octagon[2], Is.EqualTo(new CoreVector(110, 20 + 21)));

        var rounded = RoomDrawingAdapter.GetDarknessTriangle(bounds, RoomShape.RoundedCorners, new CornerRadii { TopRight = 30 }, 12);
        Assert.That(rounded[1], Is.EqualTo(new CoreVector(110 - 15, 20)));
        Assert.That(rounded[2], Is.EqualTo(new CoreVector(110, 20 + 15)));
    }

    // R6: legacy TextBlock.RebuildCachedLayout wraps only between words; a long word stays on one line.
    [Test]
    public void R6_WrapLikeLegacy_BreaksOnlyBetweenWords()
    {
        static double Measure(string s) => s.Length * 10.0;

        Assert.That(RoomDrawingAdapter.WrapLikeLegacy("Cave", 20, Measure, true), Is.EqualTo("Cave"));
        Assert.That(RoomDrawingAdapter.WrapLikeLegacy("Cave", 100, Measure, true), Is.EqualTo("Cave"));

        var wrapped = RoomDrawingAdapter.WrapLikeLegacy("Narrow cave", 60, Measure, true)
            .Split('\n').Select(l => l.TrimEnd(' ')).ToArray();
        Assert.That(wrapped, Is.EqualTo(new[] { "Narrow", "cave" }));

        Assert.That(RoomDrawingAdapter.WrapLikeLegacy("one\ntwo", 10, Measure, true), Is.EqualTo("one\ntwo"));
    }

    [AvaloniaTest]
    public void R6_NarrowRoom_NameStaysOnOneLine()
    {
        var room = NewRoom("Cave", 30, 64);
        using var bmp = new global::Avalonia.Media.Imaging.RenderTargetBitmap(new global::Avalonia.PixelSize(100, 100));
        using (var ctx = bmp.CreateDrawingContext())
        {
            room.DrawRoom(ctx, false, false);
        }

        var name = RoomDrawingAdapter.GetCachedName(room);
        Assert.That(name, Is.Not.Null);
        // One line of the default room font is ~15 px; a wrapped "Cav / e" would be about twice that.
        Assert.That(name!.Height, Is.LessThan(25));
    }
}

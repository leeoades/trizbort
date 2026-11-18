using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Drawing;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using CoreRect = Trizbort.Domain.Misc.Rect;
using CoreVector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///   Lee, 18 Nov 2026: "enter closet / in", on the join out of the Foyer's south-east corner, was drawn across the
///   join out of its south side. With no room on either side of its own line, it goes in a corner beside where the
///   join meets the room, clear of its line.
/// </summary>
[TestFixture]
[NonParallelizable]
public class EndLabelCornerTests
{
    [Test]
    public void ALabelWithNoRoomBesideItsLine_GoesInACornerByTheRoom()
    {
        var previous = Project.Current;
        var previousClashes = ConnectionDrawingAdapter.LabelClashes;
        var project = new Project();
        Project.Current = project;
        try
        {
            var foyer = new CoreRect(0, 0, 96, 64);
            var south = new LineSegment(new CoreVector(48, 64), new CoreVector(48, 96));
            var stalk = new LineSegment(new CoreVector(96, 64), new CoreVector(119, 87));
            var join = new Connection(project);
            ConnectionDrawingAdapter.LabelClashes = (_, bounds) =>
                (foyer.IntersectsWith(bounds) ? 1 : 0) + (south.IntersectsWith(bounds) ? 1 : 0);
            const double width = 85;
            const double height = 12;

            // where it went: below the corner, to the left, across the south join
            var (x, y) = ConnectionDrawingAdapter.OutOfTheWay(join, stalk, true, 96 - 4 - width, 64 + 4, width, height);

            var placed = new CoreRect((float)x, (float)y, (float)width, (float)height);
            foyer.IntersectsWith(placed).ShouldBeFalse();
            south.IntersectsWith(placed).ShouldBeFalse();
            stalk.IntersectsWith(placed).ShouldBeFalse();
            // beside the corner, not off somewhere else
            placed.Left.ShouldBeLessThan(110);
            placed.Bottom.ShouldBeLessThanOrEqualTo(64);
        }
        finally
        {
            ConnectionDrawingAdapter.LabelClashes = previousClashes;
            Project.Current = previous;
            project.Dispose();
        }
    }
}

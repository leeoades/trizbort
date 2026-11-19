using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Drawing;
using Trizbort.Domain.Misc;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///   Lee, 19 Nov 2026: ticking Door on a join and saving closed the app ("Automap direction vector should not be
///   zero"): the door icons were placed by the middle piece of the line, which had no length.
/// </summary>
[TestFixture]
[Category("Unit")]
public class DoorIconTests
{
    [Test]
    public void APieceOfLineWithNoLength_HasTheIconsAbove_WithoutAsking()
    {
        ConnectionDrawingAdapter.DoorDirection(new LineSegment(new Vector(10, 10), new Vector(10, 10)))
            .ShouldBe(CompassPoint.East);
        ConnectionDrawingAdapter.DoorDirection(new LineSegment(new Vector(0, 0), new Vector(0, 50)))
            .ShouldBe(CompassPoint.South);
    }
}

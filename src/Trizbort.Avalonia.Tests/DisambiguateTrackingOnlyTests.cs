using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///   Lee, 19 Nov 2026: tracking only, "which room does it mean?" offered to add a new Dead End, and showed where it
///   would go, but nothing was added (tracking only adds nothing). The line says what it means then: somewhere not on
///   the map.
/// </summary>
[TestFixture]
[Category("Unit")]
public class DisambiguateTrackingOnlyTests
{
    private Project? _previous;

    [SetUp]
    public void SetUp()
    {
        _previous = Project.Current;
        Project.Current = new Project();
    }

    [TearDown]
    public void TearDown()
    {
        var created = Project.Current;
        Project.Current = _previous!;
        created.Dispose();
    }

    [Test]
    public void TrackingOnly_TheNewRoomLine_IsSomewhereNotOnTheMap()
    {
        var rooms = new[] { new Room(Project.Current) { Name = "Dead End" }, new Room(Project.Current) { Name = "Dead End" } };

        var mapping = new DisambiguateRoomsViewModel("Dead End", "You have reached a dead end.", rooms);
        var tracking = new DisambiguateRoomsViewModel("Dead End", "You have reached a dead end.", rooms, trackingOnly: true);

        mapping.Choices[^1].Name.ShouldBe(DisambiguateRoomsViewModel.NewRoomName);
        tracking.Choices[^1].Name.ShouldBe(DisambiguateRoomsViewModel.NotOnTheMapName);
        tracking.Choices[^1].IsNewRoom.ShouldBeTrue();
        tracking.IntroText.ShouldContain("switch to Mapping");
        // several rooms: none picked until the user picks one
        tracking.Selected.ShouldBeNull();
    }
}

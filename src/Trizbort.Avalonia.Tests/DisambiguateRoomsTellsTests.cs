using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;

namespace Trizbort.Avalonia.Tests;

/// <summary>"Which room does it mean?" in a maze of rooms all alike: each shows what tells it apart; one picked on the map is picked in the list.</summary>
[TestFixture]
[NonParallelizable]
public class DisambiguateRoomsTellsTests
{
    private const string Alike = "You are in a maze of twisty little passages, all alike.";

    [Test]
    public void RoomsAllAlike_ShowWhatTellsThemApart_AndOnePickedOnTheMapIsPickedInTheList()
    {
        var previous = Project.Current;
        var project = new Project();
        Project.Current = project;
        try
        {
            Room Add(string objects)
            {
                var room = new Room(project) { Name = "Maze", Objects = objects };
                room.AddDescription(Alike);
                project.Elements.Add(room);
                return room;
            }

            var rod = Add("black rod");
            var bird = Add("bird");
            var other = new Room(project) { Name = "Hall" };
            var vm = new DisambiguateRoomsViewModel("Maze", Alike, new[] { rod, bird });

            vm.Choices.Select(choice => choice.Detail).ShouldBe(new[] { "objects: black rod", "objects: bird", "" });
            vm.Choices[0].HasDetail.ShouldBeTrue();
            vm.Choices[2].HasDetail.ShouldBeFalse();

            vm.PickFromMap(bird).ShouldBeTrue();
            vm.Selected!.Room.ShouldBe(bird);
            // the panel on the right: the description, then the objects in the room
            vm.SelectedDescription.ShouldBe(Alike + "\n\nObjects:\n  bird");
            vm.PickFromMap(other).ShouldBeFalse();
            vm.Selected!.Room.ShouldBe(bird);
        }
        finally
        {
            Project.Current = previous;
            project.Dispose();
        }
    }
}

using System.Drawing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class DialogViewModelTests
{
    private Project? _previousProject;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
    }

    [TearDown]
    public void TearDown()
    {
        var created = Project.Current;
        Project.Current = _previousProject!;
        created.Dispose();
    }

    [Test]
    public void ServiceScope_ChildScopeDisposal_LeavesRootProviderUsable()
    {
        var rootProvider = App.ConfigureServices();

        using (var scope = rootProvider.CreateScope())
        {
            var roomVm = scope.ServiceProvider.GetRequiredService<RoomPropertiesViewModel>();
            var connVm = scope.ServiceProvider.GetRequiredService<ConnectionPropertiesViewModel>();
            var labelVm = scope.ServiceProvider.GetRequiredService<MapLabelPropertiesViewModel>();
            var mapSettingsVm = scope.ServiceProvider.GetRequiredService<MapSettingsViewModel>();
            var mapThemeVm = scope.ServiceProvider.GetRequiredService<MapThemeViewModel>();
            var appSettingsVm = scope.ServiceProvider.GetRequiredService<AppSettingsViewModel>();
            var automapSettingsVm = scope.ServiceProvider.GetRequiredService<AutomapSettingsViewModel>();

            roomVm.ShouldNotBeNull();
            connVm.ShouldNotBeNull();
            labelVm.ShouldNotBeNull();
            mapSettingsVm.ShouldNotBeNull();
            mapThemeVm.ShouldNotBeNull();
            appSettingsVm.ShouldNotBeNull();
            automapSettingsVm.ShouldNotBeNull();
        }

        // Root provider should still resolve singletons and spawn new scopes without error
        var dialogService = rootProvider.GetRequiredService<IDialogService>();
        var commandService = rootProvider.GetRequiredService<ICommandService>();
        var storageService = rootProvider.GetRequiredService<IStorageService>();
        var mainWindowVm = rootProvider.GetRequiredService<MainWindowViewModel>();

        dialogService.ShouldNotBeNull();
        commandService.ShouldNotBeNull();
        storageService.ShouldNotBeNull();
        mainWindowVm.ShouldNotBeNull();

        using (var secondScope = rootProvider.CreateScope())
        {
            var roomVm2 = secondScope.ServiceProvider.GetRequiredService<RoomPropertiesViewModel>();
            roomVm2.ShouldNotBeNull();
        }
    }

    [Test]
    public void RoomPropertiesViewModel_ReferenceRoomById_HandlesDuplicateNames()
    {
        var room1 = new Room(Project.Current) { Name = "Cave", Id = 1 };
        var room2 = new Room(Project.Current) { Name = "Cave", Id = 2 };
        var target = new Room(Project.Current) { Name = "Target", Id = 3 };
        Project.Current.Elements.Add(room1);
        Project.Current.Elements.Add(room2);
        Project.Current.Elements.Add(target);

        // Target references the SECOND duplicate; a name-based lookup would resolve room1.
        target.ReferenceRoomId = room2.Id;

        var vm = new RoomPropertiesViewModel();
        vm.Initialize(target);
        vm.SelectedReferenceRoom.ShouldNotBeNull();
        vm.SelectedReferenceRoom!.Id.ShouldBe(room2.Id);

        vm.ApplyTo(target);
        target.ReferenceRoomId.ShouldBe(room2.Id);

        // Changing the selection to the other duplicate writes that Id.
        vm.SelectedReferenceRoom = vm.AvailableReferenceRooms.Single(x => x.Id == room1.Id);
        vm.ApplyTo(target);
        target.ReferenceRoomId.ShouldBe(room1.Id);
    }

    [Test]
    public void RoomPropertiesViewModel_SelfNameCollision_KeepsReferenceAndExcludesSelf()
    {
        // The edited room is first in Elements and shares the referenced room's name.
        var editing = new Room(Project.Current) { Name = "Cave", Id = 1 };
        var other = new Room(Project.Current) { Name = "Cave", Id = 2 };
        Project.Current.Elements.Add(editing);
        Project.Current.Elements.Add(other);
        editing.ReferenceRoomId = other.Id;

        var vm = new RoomPropertiesViewModel();
        vm.Initialize(editing);

        vm.AvailableReferenceRooms.Any(x => x.Id == editing.Id).ShouldBeFalse();
        vm.SelectedReferenceRoom!.Id.ShouldBe(other.Id);

        vm.ApplyTo(editing);
        editing.ReferenceRoomId.ShouldBe(other.Id);
    }

    [Test]
    public void RoomPropertiesViewModel_InitializeAndApplyTo_RoundTripsValuesCorrectly()
    {
        var room = new Room(Project.Current)
        {
            Name = "Great Cavern",
            SubTitle = "Echoing Depths",
            Objects = "rusty sword\ngold coin",
            ObjectsPosition = CompassPoint.South,
            IsDark = true,
            IsStartRoom = true,
            IsEndRoom = false,
            Shape = RoomShape.RoundedCorners,
            BorderStyle = BorderDashStyle.Dash,
            HandDrawnStyle = HandDrawnStyle.HandDrawn,
            RoomFillColor = Color.DarkSlateBlue,
            RoomBorderColor = Color.Cyan,
            RoomNameColor = Color.White,
            RoomSubtitleColor = Color.LightGray,
            RoomObjectTextColor = Color.Yellow,
            SecondFillColor = Color.Indigo,
            SecondFillLocation = "Bottom",
            AllCornersEqual = true,
            Corners = new CornerRadii { TopLeft = 15, TopRight = 15, BottomRight = 15, BottomLeft = 15 }
        };
        room.AddDescription("A massive subterranean hall.");
        Project.Current.Elements.Add(room);

        var vm = new RoomPropertiesViewModel();
        vm.Initialize(room);

        vm.Name.ShouldBe("Great Cavern");
        vm.SubTitle.ShouldBe("Echoing Depths");
        vm.Description.ShouldBe("A massive subterranean hall.");
        vm.Objects.ShouldBe("rusty sword\ngold coin");
        vm.ObjectsPosition.ShouldBe(CompassPoint.South);
        vm.IsDark.ShouldBeTrue();
        vm.IsStartRoom.ShouldBeTrue();
        vm.IsEndRoom.ShouldBeFalse();
        vm.Shape.ShouldBe(RoomShape.RoundedCorners);
        vm.BorderStyle.ShouldBe(BorderDashStyle.Dash);
        vm.HandDrawnStyle.ShouldBe(HandDrawnStyle.HandDrawn);
        vm.RoomFillColor.ShouldBe(Color.DarkSlateBlue);
        vm.RoomBorderColor.ShouldBe(Color.Cyan);
        vm.RoomNameColor.ShouldBe(Color.White);
        vm.RoomSubtitleColor.ShouldBe(Color.LightGray);
        vm.RoomObjectTextColor.ShouldBe(Color.Yellow);
        vm.SecondFillColor.ShouldBe(Color.Indigo);
        vm.SecondFillLocation.ShouldBe("Bottom");
        vm.AllCornersEqual.ShouldBeTrue();
        vm.CornerTopLeft.ShouldBe(15);

        // Edit via ViewModel
        vm.Name = "Underground Vault";
        vm.SubTitle = "Locked and Secure";
        vm.Description = "Solid titanium walls surround you.";
        vm.Objects = "diamond";
        vm.ObjectsPosition = CompassPoint.NorthEast;
        vm.IsDark = false;
        vm.IsStartRoom = false;
        vm.IsEndRoom = true;
        vm.Shape = RoomShape.SquareCorners;
        vm.BorderStyle = BorderDashStyle.Solid;
        vm.HandDrawnStyle = HandDrawnStyle.Straight;
        vm.RoomFillColor = Color.Silver;
        vm.CornerTopLeft = 0;

        vm.ApplyTo(room);

        room.Name.ShouldBe("Underground Vault");
        room.SubTitle.ShouldBe("Locked and Secure");
        room.PrimaryDescription.ShouldBe("Solid titanium walls surround you.");
        room.Objects.ShouldBe("diamond");
        room.ObjectsPosition.ShouldBe(CompassPoint.NorthEast);
        room.IsDark.ShouldBeFalse();
        room.IsStartRoom.ShouldBeFalse();
        room.IsEndRoom.ShouldBeTrue();
        room.Shape.ShouldBe(RoomShape.SquareCorners);
        room.BorderStyle.ShouldBe(BorderDashStyle.Solid);
        room.HandDrawnStyle.ShouldBe(HandDrawnStyle.Straight);
        room.RoomFillColor.ShouldBe(Color.Silver);
    }

    [Test]
    public void ConnectionPropertiesViewModel_InitializeAndApplyTo_RoundTripsValuesCorrectly()
    {
        var v1 = new Vertex(new Vector(0, 0));
        var v2 = new Vertex(new Vector(100, 100));
        var connection = new Connection(Project.Current, v1, v2)
        {
            Name = "Stone Archway",
            Description = "An ornate passageway",
            StartText = "north",
            MidText = "through arch",
            EndText = "south",
            Flow = ConnectionFlow.OneWay,
            Style = ConnectionStyle.Dashed,
            ConnectionColor = Color.MediumPurple,
            Door = new Door
            {
                Lockable = true,
                Locked = true,
                Open = false,
                Openable = true
            }
        };
        Project.Current.Elements.Add(connection);

        var vm = new ConnectionPropertiesViewModel();
        vm.Initialize(connection);

        vm.ConnectionName.ShouldBe("Stone Archway");
        vm.ConnectionDescription.ShouldBe("An ornate passageway");
        vm.StartText.ShouldBe("north");
        vm.MidText.ShouldBe("through arch");
        vm.EndText.ShouldBe("south");
        vm.IsDirectional.ShouldBeTrue();
        vm.IsDotted.ShouldBeTrue();
        vm.ConnectionColor.ShouldBe(Color.MediumPurple);
        vm.HasDoor.ShouldBeTrue();
        vm.Lockable.ShouldBeTrue();
        vm.Locked.ShouldBeTrue();
        vm.Open.ShouldBeFalse();
        vm.Openable.ShouldBeTrue();

        // Edit via ViewModel
        vm.ConnectionName = "Open Corridor";
        vm.ConnectionDescription = "Wide and bright";
        vm.StartText = "east";
        vm.MidText = "";
        vm.EndText = "west";
        vm.IsDirectional = false;
        vm.IsDotted = false;
        vm.ConnectionColor = Color.Teal;
        vm.HasDoor = false;

        vm.ApplyTo(connection);

        connection.Name.ShouldBe("Open Corridor");
        connection.Description.ShouldBe("Wide and bright");
        connection.StartText.ShouldBe("east");
        connection.MidText.ShouldBe("");
        connection.EndText.ShouldBe("west");
        connection.Flow.ShouldBe(ConnectionFlow.TwoWay);
        connection.Style.ShouldBe(ConnectionStyle.Solid);
        connection.ConnectionColor.ShouldBe(Color.Teal);
        connection.Door.ShouldBeNull();
    }

    [Test]
    public void MapLabelPropertiesViewModel_InitializeAndApplyTo_RoundTripsValuesCorrectly()
    {
        var label = new MapLabel(Project.Current)
        {
            Text = "Danger Zone",
            TextColor = Color.Red,
            Shape = RoomShape.SquareCorners,
            BorderStyle = BorderDashStyle.Solid,
            BorderColor = Color.DarkRed,
            HasBackground = true,
            BackgroundColor = Color.LightYellow
        };
        Project.Current.Elements.Add(label);

        var vm = new MapLabelPropertiesViewModel();
        vm.Initialize(label);

        vm.Text.ShouldBe("Danger Zone");
        vm.TextColor.ShouldBe(Color.Red);
        vm.Shape.ShouldBe(RoomShape.SquareCorners);
        vm.HasOutline.ShouldBeTrue();
        vm.BorderStyle.ShouldBe(BorderDashStyle.Solid);
        vm.BorderColor.ShouldBe(Color.DarkRed);
        vm.HasBackground.ShouldBeTrue();
        vm.BackgroundColor.ShouldBe(Color.LightYellow);

        // Edit via ViewModel
        vm.Text = "Safe Haven";
        vm.TextColor = Color.Green;
        vm.Shape = RoomShape.RoundedCorners;
        vm.HasOutline = false;
        vm.HasBackground = false;

        vm.ApplyTo(label);

        label.Text.ShouldBe("Safe Haven");
        label.TextColor.ShouldBe(Color.Green);
        label.Shape.ShouldBe(RoomShape.RoundedCorners);
        label.BorderStyle.ShouldBe(BorderDashStyle.None);
        label.HasBackground.ShouldBeFalse();
    }
}

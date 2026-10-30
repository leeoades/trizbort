using System.Drawing;
using Avalonia;
using Avalonia.Headless.NUnit;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views.Dialogs;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Setup;
using Region = Trizbort.Domain.Misc.Region;
using Vector = Trizbort.Domain.Misc.Vector;

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
    public void RoomPropertiesViewModel_CustomPositionAndBorderNone_RoundTrip()
    {
        var room = new Room(Project.Current)
        {
            Name = "Hall",
            ObjectsCustomPosition = true,
            ObjectsCustomPositionDown = 12,
            ObjectsCustomPositionRight = -7,
            BorderStyle = BorderDashStyle.None
        };
        Project.Current.Elements.Add(room);

        var vm = new RoomPropertiesViewModel();
        vm.Initialize(room);
        vm.UseCustomPosition.ShouldBeTrue();
        vm.CustomPositionDown.ShouldBe(12);
        vm.CustomPositionRight.ShouldBe(-7);
        vm.AvailableBorderStyles.ShouldContain(BorderDashStyle.None);
        vm.BorderStyle.ShouldBe(BorderDashStyle.None);

        vm.CustomPositionDown = 3;
        vm.UseCustomPosition = false;
        vm.ApplyTo(room);
        room.ObjectsCustomPosition.ShouldBeFalse();
        room.ObjectsCustomPositionDown.ShouldBe(3);
        room.ObjectsCustomPositionRight.ShouldBe(-7);
        room.BorderStyle.ShouldBe(BorderDashStyle.None);
    }

    [TestCase("Bottom")]
    [TestCase("BottomRight")]
    [TestCase("BottomLeft")]
    [TestCase("Left")]
    [TestCase("Right")]
    [TestCase("TopRight")]
    [TestCase("TopLeft")]
    [TestCase("Top")]
    public void RoomPropertiesViewModel_SecondFillLocation_RoundTrips(string location)
    {
        var room = new Room(Project.Current) { Name = "Fill", SecondFillLocation = location };
        Project.Current.Elements.Add(room);

        var vm = new RoomPropertiesViewModel();
        vm.AvailableSecondFillLocations.ShouldContain(location);
        vm.AvailableSecondFillLocations.Count.ShouldBe(8);
        vm.Initialize(room);
        vm.SecondFillLocation.ShouldBe(location);

        room.SecondFillLocation = "Bottom";
        vm.ApplyTo(room);
        room.SecondFillLocation.ShouldBe(location);
    }

    [Test]
    public void EnumDisplay_ProducesFriendlyNames()
    {
        EnumDisplay.For(RoomShape.SquareCorners).ShouldBe("Straight Edges");
        EnumDisplay.For(RoomShape.RoundedCorners).ShouldBe("Rounded Corners");
        EnumDisplay.For(RoomShape.Ellipse).ShouldBe("Ellipse");
        EnumDisplay.For(RoomShape.Octagonal).ShouldBe("Octagonal");
        EnumDisplay.For(BorderDashStyle.DashDot).ShouldBe("Dash-dot");
        EnumDisplay.For(BorderDashStyle.DashDotDot).ShouldBe("Dash-dot-dot");
        EnumDisplay.For(BorderDashStyle.None).ShouldBe("None");
        EnumDisplay.For(HandDrawnStyle.HandDrawn).ShouldBe("Hand-drawn");
        EnumDisplay.For(HandDrawnStyle.Straight).ShouldBe("Straight");
        EnumDisplay.For(HandDrawnStyle.MapDefault).ShouldStartWith("Map setting (");
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

    [Test]
    public void MapLabelPropertiesViewModel_ShapeEnabled_WhenOutlineOrBackgroundIsOn()
    {
        var label = new MapLabel(Project.Current) { BorderStyle = BorderDashStyle.None, HasBackground = false };
        Project.Current.Elements.Add(label);

        var vm = new MapLabelPropertiesViewModel();
        vm.Initialize(label);

        vm.ShapeEnabled.ShouldBeFalse();

        vm.HasBackground = true;
        vm.ShapeEnabled.ShouldBeTrue();

        vm.HasBackground = false;
        vm.HasOutline = true;
        vm.ShapeEnabled.ShouldBeTrue();

        vm.HasOutline = false;
        vm.ShapeEnabled.ShouldBeFalse();
    }

    [Test]
    public void MapLabelPropertiesViewModel_ShapeEnabled_RaisesChangeNotification()
    {
        var vm = new MapLabelPropertiesViewModel();
        var raised = new List<string>();
        vm.PropertyChanged += (_, args) => raised.Add(args.PropertyName ?? string.Empty);

        vm.HasBackground = true;

        raised.ShouldContain(nameof(MapLabelPropertiesViewModel.ShapeEnabled));
    }

    [Test]
    public void MapLabelPropertiesViewModel_EditEverySetting_RoundTripsThroughLabel()
    {
        var label = new MapLabel(Project.Current) { Text = "Old", BorderStyle = BorderDashStyle.None, HasBackground = false };
        Project.Current.Elements.Add(label);

        var vm = new MapLabelPropertiesViewModel();
        vm.Initialize(label);
        vm.Text = "Cellar";
        vm.TextColor = Color.Green;
        vm.HasOutline = true;
        vm.Shape = RoomShape.Ellipse;
        vm.BorderStyle = BorderDashStyle.DashDot;
        vm.BorderColor = Color.Maroon;
        vm.HasBackground = true;
        vm.BackgroundColor = Color.Khaki;
        vm.ApplyTo(label);

        var reloaded = new MapLabelPropertiesViewModel();
        reloaded.Initialize(label);

        reloaded.Text.ShouldBe("Cellar");
        reloaded.TextColor.ShouldBe(Color.Green);
        reloaded.HasOutline.ShouldBeTrue();
        reloaded.Shape.ShouldBe(RoomShape.Ellipse);
        reloaded.BorderStyle.ShouldBe(BorderDashStyle.DashDot);
        reloaded.BorderColor.ShouldBe(Color.Maroon);
        reloaded.HasBackground.ShouldBeTrue();
        reloaded.BackgroundColor.ShouldBe(Color.Khaki);
    }

    [AvaloniaTest]
    public void MapLabelPropertiesDialog_DefaultSize_NothingIsClipped()
    {
        EnsureDialogStyles();
        var label = new MapLabel(Project.Current) { Text = "Label", HasBackground = true };
        Project.Current.Elements.Add(label);

        var vm = new MapLabelPropertiesViewModel();
        vm.Initialize(label);
        var dialog = new MapLabelPropertiesDialog { DataContext = vm };
        dialog.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            dialog.UpdateLayout();

            var controls = dialog.GetVisualDescendants()
                .OfType<global::Avalonia.Controls.Control>()
                .Where(c => c.IsVisible && (c is global::Avalonia.Controls.CheckBox || c is global::Avalonia.Controls.ComboBox
                                            || c is global::Avalonia.Controls.Button || c is global::Avalonia.Controls.TextBox
                                            || c is Trizbort.Avalonia.Controls.ColorPickerButton))
                .ToList();

            controls.Count.ShouldBeGreaterThan(8);
            foreach (var control in controls)
            {
                var origin = control.TranslatePoint(new global::Avalonia.Point(0, 0), dialog).ShouldNotBeNull();
                (origin.Y + control.Bounds.Height).ShouldBeLessThanOrEqualTo(dialog.ClientSize.Height);
                (origin.X + control.Bounds.Width).ShouldBeLessThanOrEqualTo(dialog.ClientSize.Width);
            }
        }
        finally
        {
            dialog.Close();
        }
    }

    private static void EnsureDialogStyles()
    {
        // The headless test app doesn't load App.axaml, so add the theme and dialog styles explicitly.
        if (global::Avalonia.Application.Current == null)
        {
            return;
        }

        if (!global::Avalonia.Application.Current.Styles.OfType<FluentTheme>().Any())
        {
            global::Avalonia.Application.Current.Styles.Add(new FluentTheme());
        }

        if (!global::Avalonia.Application.Current.Styles.OfType<global::Avalonia.Markup.Xaml.Styling.StyleInclude>().Any())
        {
            global::Avalonia.Application.Current.Styles.Add(new global::Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("avares://Trizbort.Avalonia/"))
            {
                Source = new Uri("avares://Trizbort.Avalonia/Styles/DialogStyles.axaml")
            });
        }
    }

    [Test]
    public void MapSettingsViewModel_NewFields_RoundTripThroughSettings()
    {
        var savedHandle = Settings.HandleSize;
        var savedSnap = Settings.SnapToElementSize;
        var savedWidth = Settings.LineWidth;
        var savedMargins = Settings.DocumentSpecificMargins;
        var savedHorizontal = Settings.DocHorizontalMargin;
        var savedVertical = Settings.DocVerticalMargin;
        try
        {
            var vm = new MapSettingsViewModel();
            vm.Initialize();
            vm.HandleSize = 20.0f;
            vm.SnapToElementDistance = 24.0f;
            vm.LineWidth = 2.5f;
            vm.DocumentSpecificMargins = true;
            vm.DocHorizontalMargin = 30.0f;
            vm.DocVerticalMargin = 40.0f;
            vm.ApplyToSettings();

            Settings.HandleSize.ShouldBe(20.0f);
            Settings.SnapToElementSize.ShouldBe(24.0f);
            Settings.LineWidth.ShouldBe(2.5f);
            Settings.DocumentSpecificMargins.ShouldBeTrue();
            Settings.DocHorizontalMargin.ShouldBe(30.0f);
            Settings.DocVerticalMargin.ShouldBe(40.0f);

            var reloaded = new MapSettingsViewModel();
            reloaded.Initialize();
            reloaded.HandleSize.ShouldBe(20.0f);
            reloaded.SnapToElementDistance.ShouldBe(24.0f);
            reloaded.LineWidth.ShouldBe(2.5f);
        }
        finally
        {
            Settings.HandleSize = savedHandle;
            Settings.SnapToElementSize = savedSnap;
            Settings.LineWidth = savedWidth;
            Settings.DocumentSpecificMargins = savedMargins;
            Settings.DocHorizontalMargin = savedHorizontal;
            Settings.DocVerticalMargin = savedVertical;
        }
    }

    [Test]
    public void MapSettingsViewModel_Validate_RejectsEmptyAndLetterlessRoomName()
    {
        var vm = new MapSettingsViewModel();
        vm.Initialize();

        vm.DefaultRoomName = "  ";
        vm.Validate().ShouldBeFalse();
        vm.DefaultRoomNameError.ShouldBe(MapSettingsViewModel.EmptyRoomNameMessage);
        vm.HasDefaultRoomNameError.ShouldBeTrue();

        vm.DefaultRoomName = "123";
        vm.Validate().ShouldBeFalse();
        vm.DefaultRoomNameError.ShouldBe(MapSettingsViewModel.NoLetterRoomNameMessage);

        vm.DefaultRoomName = "Cave 2";
        vm.Validate().ShouldBeTrue();
        vm.HasDefaultRoomNameError.ShouldBeFalse();
    }

    [Test]
    public void MapSettingsViewModel_ShapeChoices_UseLegacyNames()
    {
        var vm = new MapSettingsViewModel();
        vm.ShapeChoices.Select(c => c.Display).ShouldBe(
            new[] { "Straight Edges", "Rounded Corners", "Ellipse", "Octagonal" });
        vm.ShapeChoices.Select(c => c.Value).Distinct().Count().ShouldBe(4);
    }

    [Test]
    public void MapSettingsViewModel_RenameAndDeleteRegion_UpdatesRooms()
    {
        var savedRegions = Settings.Regions.ToList();
        try
        {
            Settings.Regions = new List<Region>
            {
                new() { RegionName = Region.DefaultRegion },
                new() { RegionName = "Forest" },
                new() { RegionName = "Castle" }
            };
            var inForest = new Room(Project.Current) { Name = "Glade", Region = "Forest" };
            var inCastle = new Room(Project.Current) { Name = "Keep", Region = "Castle" };
            Project.Current.Elements.Add(inForest);
            Project.Current.Elements.Add(inCastle);

            var vm = new MapSettingsViewModel();
            vm.Initialize();
            vm.Regions.Single(r => r.Name == "Forest").Name = "Woods";
            vm.SelectedRegion = vm.Regions.Single(r => r.Name == "Castle");
            vm.DeleteSelectedRegion();
            vm.ApplyToSettings();

            inForest.Region.ShouldBe("Woods");
            inCastle.Region.ShouldBe(Region.DefaultRegion);
            Settings.Regions.Select(r => r.RegionName).ShouldBe(new[] { Region.DefaultRegion, "Woods" });
        }
        finally
        {
            Settings.Regions = savedRegions;
        }
    }

    [Test]
    public void MapSettingsViewModel_SwappedRegionNames_MoveRoomsCorrectly()
    {
        var savedRegions = Settings.Regions.ToList();
        try
        {
            Settings.Regions = new List<Region>
            {
                new() { RegionName = Region.DefaultRegion },
                new() { RegionName = "North" },
                new() { RegionName = "South" }
            };
            var north = new Room(Project.Current) { Name = "N", Region = "North" };
            var south = new Room(Project.Current) { Name = "S", Region = "South" };
            Project.Current.Elements.Add(north);
            Project.Current.Elements.Add(south);

            var vm = new MapSettingsViewModel();
            vm.Initialize();
            vm.Regions.Single(r => r.Name == "North").Name = "South";
            vm.Regions.First(r => r.OriginalName == "South").Name = "North";
            vm.ApplyToSettings();

            north.Region.ShouldBe("South");
            south.Region.ShouldBe("North");
        }
        finally
        {
            Settings.Regions = savedRegions;
        }
    }

    [Test]
    public void MapSettingsViewModel_RegionValidation_ReportsMessagesAndSanitizes()
    {
        var savedRegions = Settings.Regions.ToList();
        try
        {
            Settings.Regions = new List<Region>
            {
                new() { RegionName = Region.DefaultRegion },
                new() { RegionName = "Forest" },
                new() { RegionName = "Castle" }
            };
            var vm = new MapSettingsViewModel();
            vm.Initialize();
            var forest = vm.Regions.Single(r => r.Name == "Forest");
            var castle = vm.Regions.Single(r => r.Name == "Castle");

            forest.Name = "  ";
            vm.ValidateRegions(false).ShouldBeFalse();
            forest.NameError.ShouldBe(RegionNaming.EmptyMessage);

            forest.Name = "a_b";
            vm.ValidateRegions(false).ShouldBeFalse();
            forest.NameError.ShouldBe(RegionNaming.InvalidCharacterMessage);

            forest.Name = "castle";
            vm.ValidateRegions(false).ShouldBeFalse();
            forest.HasNameError.ShouldBeTrue();
            castle.HasNameError.ShouldBeTrue();
            vm.HasRegionErrors.ShouldBeTrue();

            forest.Name = " The \"Wood\" ";
            vm.ValidateRegions(true).ShouldBeTrue();
            forest.Name.ShouldBe("The 'Wood'");
            vm.HasRegionErrors.ShouldBeFalse();
        }
        finally
        {
            Settings.Regions = savedRegions;
        }
    }

    [Test]
    public void MapSettingsViewModel_DefaultRegion_CannotBeDeletedOrRenamed_AndNewRegionsAreNumbered()
    {
        var savedRegions = Settings.Regions.ToList();
        try
        {
            Settings.Regions = new List<Region> { new() { RegionName = Region.DefaultRegion } };
            var vm = new MapSettingsViewModel();
            vm.Initialize();

            vm.SelectedRegion = vm.Regions.Single(r => r.IsDefaultRegion);
            vm.DeleteSelectedRegionCommand.CanExecute(null).ShouldBeFalse();
            vm.DeleteSelectedRegion();
            vm.Regions.Count.ShouldBe(1);

            vm.AddRegion();
            vm.AddRegion();
            vm.Regions.Select(r => r.Name).ShouldBe(new[] { Region.DefaultRegion, "Region1", "Region2" });
            vm.SelectedRegion!.FillColor.ToArgb().ShouldBe(Color.White.ToArgb());
            vm.SelectedRegion.TextColor.ToArgb().ShouldBe(Settings.Color[Colors.Subtitle].ToArgb());
            vm.DeleteSelectedRegionCommand.CanExecute(null).ShouldBeTrue();
        }
        finally
        {
            Settings.Regions = savedRegions;
        }
    }

    [Test]
    public void MapSettingsViewModel_ApplyInsideSession_IsOneUndoStep()
    {
        var savedRegions = Settings.Regions.ToList();
        using var session = new EditorSession();
        try
        {
            Settings.Regions = new List<Region>
            {
                new() { RegionName = Region.DefaultRegion },
                new() { RegionName = "Forest" }
            };
            Project.Current.Elements.Add(new Room(Project.Current) { Name = "Glade", Region = "Forest" });
            session.Undo.Clear();

            var vm = new MapSettingsViewModel();
            vm.Initialize();
            vm.Regions.Single(r => r.Name == "Forest").Name = "Woods";
            session.Execute("Edit map settings", vm.ApplyToSettings);

            session.Undo.UndoTitle.ShouldBe("Edit map settings");
            Project.Current.Elements.OfType<Room>().Single().Region.ShouldBe("Woods");

            session.PerformUndo();

            Project.Current.Elements.OfType<Room>().Single().Region.ShouldBe("Forest");
            Settings.Regions.Select(r => r.RegionName).ShouldContain("Forest");
            Settings.Regions.Select(r => r.RegionName).ShouldNotContain("Woods");
            session.Undo.CanUndo.ShouldBeFalse();
        }
        finally
        {
            Settings.Regions = savedRegions;
        }
    }

    [Test]
    public void AppSettings_PortAdjustAndSmartSave_RoundTrip()
    {
        var settings = ApplicationSettingsController.AppSettings;
        var saved = (settings.PortAdjustDetail, settings.SaveAt100, settings.SaveToPDF, settings.SaveToImage,
            settings.SaveTadstoAdv3Lite, settings.DefaultImageType);
        try
        {
            var vm = new AppSettingsViewModel();
            vm.Initialize();
            vm.PortAdjustDetail = 2;
            vm.SaveAt100 = true;
            vm.SaveToPdf = true;
            vm.SaveToImage = true;
            vm.SaveTadsToAdv3Lite = true;
            vm.DefaultImageType = 1;
            vm.ApplyToSettings();

            var vm2 = new AppSettingsViewModel();
            vm2.Initialize();
            vm2.PortAdjustDetail.ShouldBe(2);
            vm2.SaveAt100.ShouldBeTrue();
            vm2.SaveToPdf.ShouldBeTrue();
            vm2.SaveToImage.ShouldBeTrue();
            vm2.SaveTadsToAdv3Lite.ShouldBeTrue();
            vm2.DefaultImageType.ShouldBe(1);
        }
        finally
        {
            settings.PortAdjustDetail = saved.PortAdjustDetail;
            settings.SaveAt100 = saved.SaveAt100;
            settings.SaveToPDF = saved.SaveToPDF;
            settings.SaveToImage = saved.SaveToImage;
            settings.SaveTadstoAdv3Lite = saved.SaveTadstoAdv3Lite;
            settings.DefaultImageType = saved.DefaultImageType;
            ApplicationSettingsController.SaveSettings();
        }
    }

    [Test]
    public void AppSettings_TooltipMasterUntickedAndOk_TurnsBothChildrenOff()
    {
        var settings = ApplicationSettingsController.AppSettings;
        var savedDescriptions = settings.ShowDescriptionsInTooltips;
        var savedObjects = settings.ShowObjectsInTooltips;
        try
        {
            var vm = new AppSettingsViewModel();
            vm.Initialize();
            vm.ShowTooltips = false;
            vm.ApplyToSettings();

            settings.ShowTooltips.ShouldBeFalse();
            settings.ShowDescriptionsInTooltips.ShouldBeFalse();
            settings.ShowObjectsInTooltips.ShouldBeFalse();

            var reopened = new AppSettingsViewModel();
            reopened.Initialize();
            reopened.ShowTooltips.ShouldBeFalse();
        }
        finally
        {
            settings.ShowDescriptionsInTooltips = savedDescriptions;
            settings.ShowObjectsInTooltips = savedObjects;
            ApplicationSettingsController.SaveSettings();
        }
    }

    [Test]
    public void AppSettings_TooltipMasterReticked_RestoresPreviousChildren()
    {
        var settings = ApplicationSettingsController.AppSettings;
        var savedDescriptions = settings.ShowDescriptionsInTooltips;
        var savedObjects = settings.ShowObjectsInTooltips;
        try
        {
            settings.ShowDescriptionsInTooltips = true;
            settings.ShowObjectsInTooltips = false;
            var vm = new AppSettingsViewModel();
            vm.Initialize();
            vm.ShowTooltips.ShouldBeTrue();

            vm.ShowTooltips = false;
            vm.ShowDescriptionsInTooltips.ShouldBeFalse();
            vm.ShowObjectsInTooltips.ShouldBeFalse();

            vm.ShowTooltips = true;
            vm.ShowDescriptionsInTooltips.ShouldBeTrue();
            vm.ShowObjectsInTooltips.ShouldBeFalse();
        }
        finally
        {
            settings.ShowDescriptionsInTooltips = savedDescriptions;
            settings.ShowObjectsInTooltips = savedObjects;
        }
    }

    [Test]
    public void AppSettings_TooltipMasterTickedWithBothChildrenOff_OkTurnsBothOn()
    {
        var settings = ApplicationSettingsController.AppSettings;
        var savedDescriptions = settings.ShowDescriptionsInTooltips;
        var savedObjects = settings.ShowObjectsInTooltips;
        try
        {
            var vm = new AppSettingsViewModel();
            vm.Initialize();
            vm.ShowTooltips = true;
            vm.ShowDescriptionsInTooltips = false;
            vm.ShowObjectsInTooltips = false;
            vm.ApplyToSettings();

            settings.ShowTooltips.ShouldBeTrue();
            settings.ShowDescriptionsInTooltips.ShouldBeTrue();
            settings.ShowObjectsInTooltips.ShouldBeTrue();
        }
        finally
        {
            settings.ShowDescriptionsInTooltips = savedDescriptions;
            settings.ShowObjectsInTooltips = savedObjects;
            ApplicationSettingsController.SaveSettings();
        }
    }

    [Test]
    public void AppSettings_TooltipDependencies_FollowMasterDescriptionsAndLimits()
    {
        var vm = new AppSettingsViewModel();
        vm.Initialize();

        vm.ShowTooltips = false;
        vm.CanEditTooltipOptions.ShouldBeFalse();
        vm.CanEditDescriptionLimits.ShouldBeFalse();
        vm.CanEditRoomCharacterCount.ShouldBeFalse();
        vm.CanEditConnectionCharacterCount.ShouldBeFalse();

        vm.ShowTooltips = true;
        vm.ShowDescriptionsInTooltips = false;
        vm.LimitRoomDescriptionCharactersInTooltip = true;
        vm.LimitConnectionDescriptionCharactersInTooltip = true;
        vm.CanEditTooltipOptions.ShouldBeTrue();
        vm.CanEditDescriptionLimits.ShouldBeFalse();
        vm.CanEditRoomCharacterCount.ShouldBeFalse();
        vm.CanEditConnectionCharacterCount.ShouldBeFalse();

        vm.ShowDescriptionsInTooltips = true;
        vm.CanEditDescriptionLimits.ShouldBeTrue();
        vm.CanEditRoomCharacterCount.ShouldBeTrue();
        vm.CanEditConnectionCharacterCount.ShouldBeTrue();

        vm.LimitRoomDescriptionCharactersInTooltip = false;
        vm.CanEditRoomCharacterCount.ShouldBeFalse();
        vm.CanEditConnectionCharacterCount.ShouldBeTrue();
    }

    [Test]
    public void ConnectionPropertiesViewModel_SelectingPreset_FillsStartAndEnd_LeavesMiddle()
    {
        var vm = new ConnectionPropertiesViewModel { MidText = "through" };

        vm.SelectedPreset = TextPreset.UpDown;
        vm.StartText.ShouldBe("up");
        vm.EndText.ShouldBe("down");

        vm.SelectedPreset = TextPreset.DownUp;
        vm.StartText.ShouldBe("down");
        vm.EndText.ShouldBe("up");
        vm.SelectedPreset.ShouldBe(TextPreset.DownUp);

        vm.SelectedPreset = TextPreset.InOut;
        vm.StartText.ShouldBe("in");
        vm.EndText.ShouldBe("out");

        vm.SelectedPreset = TextPreset.OutIn;
        vm.StartText.ShouldBe("out");
        vm.EndText.ShouldBe("in");

        vm.MidText.ShouldBe("through");
    }

    [Test]
    public void ConnectionPropertiesViewModel_TypingText_TracksPreset()
    {
        var vm = new ConnectionPropertiesViewModel();
        vm.StartText = "up";
        vm.EndText = "down";
        vm.SelectedPreset.ShouldBe(TextPreset.UpDown);

        vm.StartText = "down";
        vm.EndText = "up";
        vm.SelectedPreset.ShouldBe(TextPreset.DownUp);

        vm.StartText = "north";
        vm.SelectedPreset.ShouldBe(TextPreset.Custom);

        vm.StartText = "down";
        vm.MidText = "x";
        vm.SelectedPreset.ShouldBe(TextPreset.Custom);
    }

    [Test]
    public void ConnectionPropertiesViewModel_ClearText_EmptiesAllAndSelectsCustom()
    {
        var vm = new ConnectionPropertiesViewModel();
        vm.SelectedPreset = TextPreset.InOut;
        vm.MidText = "m";

        vm.ClearText();

        vm.StartText.ShouldBeEmpty();
        vm.MidText.ShouldBeEmpty();
        vm.EndText.ShouldBeEmpty();
        vm.SelectedPreset.ShouldBe(TextPreset.Custom);
    }
}

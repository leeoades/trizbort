using System.Drawing;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views.Dialogs;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Avalonia.Tests;

/// <summary>Lee, 24 Nov 2026: several rooms' darkness, shapes and colours, together; mixed values left alone.</summary>
[TestFixture]
[NonParallelizable]
public class RoomsPropertiesDialogTests
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

    private static Room Room(bool dark, System.Drawing.Color fill, RoomShape shape)
    {
        var room = new Room(Project.Current) { IsDark = dark, RoomFillColor = fill, Shape = shape };
        Project.Current.Elements.Add(room);
        return room;
    }

    [AvaloniaTest]
    public void MixedValues_ShowAsMixed_AndAreLeftAlone_ChangedOnesSetOnAll()
    {
        var a = Room(true, System.Drawing.Color.Red, RoomShape.Octagonal);
        var b = Room(false, System.Drawing.Color.Blue, RoomShape.Octagonal);
        var vm = new RoomsPropertiesViewModel();
        vm.Initialize(new[] { a, b });

        vm.CountText.ShouldBe("2 rooms selected");
        vm.IsDark.ShouldBeNull();
        vm.FillMixed.ShouldBeTrue();
        vm.Shape.ShouldBe(RoomShape.Octagonal);

        vm.BorderStyle = BorderDashStyle.Dash;
        vm.ToEdit().ApplyTo(new[] { a, b });

        a.IsDark.ShouldBeTrue();
        b.IsDark.ShouldBeFalse();
        a.RoomFillColor.ToArgb().ShouldBe(System.Drawing.Color.Red.ToArgb());
        b.RoomFillColor.ToArgb().ShouldBe(System.Drawing.Color.Blue.ToArgb());
        a.BorderStyle.ShouldBe(BorderDashStyle.Dash);
        b.BorderStyle.ShouldBe(BorderDashStyle.Dash);
    }

    [AvaloniaTest]
    public void PickingAColourForMixedRooms_SetsItOnAll_EvenTheMapDefault()
    {
        var a = Room(false, System.Drawing.Color.Red, RoomShape.SquareCorners);
        var b = Room(false, System.Drawing.Color.Blue, RoomShape.SquareCorners);
        var vm = new RoomsPropertiesViewModel();
        vm.Initialize(new[] { a, b });

        vm.FillColor = System.Drawing.Color.Transparent;
        vm.FillMixed.ShouldBeFalse();
        vm.ToEdit().ApplyTo(new[] { a, b });

        a.RoomFillColor.ToArgb().ShouldBe(System.Drawing.Color.Transparent.ToArgb());
        b.RoomFillColor.ToArgb().ShouldBe(System.Drawing.Color.Transparent.ToArgb());
    }

    [AvaloniaTest]
    public void TheDialogOpens()
    {
        var vm = new RoomsPropertiesViewModel();
        vm.Initialize(new[] { Room(true, System.Drawing.Color.Red, RoomShape.Ellipse), Room(false, System.Drawing.Color.Red, RoomShape.Ellipse) });
        var dialog = new RoomsPropertiesDialog { DataContext = vm };
        dialog.Show();
        global::Avalonia.Controls.ControlExtensions.FindControl<global::Avalonia.Controls.CheckBox>(dialog, "DarkCheck")!.IsChecked.ShouldBeNull();
        dialog.Close();
    }

    /// <summary>Lee, 25 Nov 2026: the same tabs as one room's dialog, so it isn't disorientating.</summary>
    [AvaloniaTest]
    public void HasTheSameTabs_AsOneRoomsDialog()
    {
        var rooms = new[] { Room(true, System.Drawing.Color.Red, RoomShape.Ellipse), Room(false, System.Drawing.Color.Red, RoomShape.Ellipse) };
        var several = new RoomsPropertiesViewModel();
        several.Initialize(rooms);
        var one = new RoomPropertiesViewModel();
        one.Initialize(rooms[0]);

        static string[] Tabs(global::Avalonia.Controls.Window window)
        {
            window.Show();
            try
            {
                return global::Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(window)
                    .OfType<global::Avalonia.Controls.TabControl>().First()
                    .Items.OfType<global::Avalonia.Controls.TabItem>().Select(tab => tab.Header?.ToString() ?? "").ToArray();
            }
            finally
            {
                window.Close();
            }
        }

        var expected = Tabs(new RoomPropertiesDialog { DataContext = one });
        expected.Length.ShouldBe(4);
        Tabs(new RoomsPropertiesDialog { DataContext = several }).ShouldBe(expected);
    }

    [AvaloniaTest]
    public void Moves_AndPuttingTheObjectListsBack_AreSetOnAll()
    {
        var a = Room(false, System.Drawing.Color.Red, RoomShape.Ellipse);
        var b = Room(false, System.Drawing.Color.Red, RoomShape.Ellipse);
        a.Moves = true;
        a.ObjectsPlaced = true;
        a.ObjectsPosition = Trizbort.Domain.Misc.CompassPoint.North;
        var vm = new RoomsPropertiesViewModel();
        vm.Initialize(new[] { a, b });
        vm.Moves.ShouldBeNull();
        vm.ObjectListText.ShouldStartWith("Placed by hand in 1 of the rooms");
        vm.CanResetObjectLists.ShouldBeTrue();

        vm.Moves = true;
        vm.ResetObjectListPositionsCommand.Execute(null);
        vm.ObjectListText.ShouldStartWith("Automatic");
        vm.ToEdit().ApplyTo(new[] { a, b });

        b.Moves.ShouldBeTrue();
        a.ObjectsPlaced.ShouldBeFalse();
        a.ObjectsPosition.ShouldBe(Trizbort.Domain.Misc.CompassPoint.South);
    }

    /// <summary>Lee, 25 Nov 2026: Add New Region... in the region list, as on the room's right-click menu.</summary>
    [AvaloniaTest]
    public void AddNewRegion_MakesOne_AndChoosesIt_KeptOnlyOnOK()
    {
        var a = Room(false, System.Drawing.Color.Red, RoomShape.Ellipse);
        var b = Room(false, System.Drawing.Color.Red, RoomShape.Ellipse);
        var vm = new RoomsPropertiesViewModel();
        vm.Initialize(new[] { a, b });
        vm.AvailableRegions.Last().ShouldBe(NewRegionChoice.Chosen);
        var before = vm.Region;
        var kept = false;
        vm.Regions.AddRegion = () => Task.FromResult<NewRegionChoice?>(new NewRegionChoice("Cellar", () => kept = true));

        vm.Region = NewRegionChoice.Chosen;
        global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        vm.Region.ShouldBe("Cellar");
        vm.AvailableRegions.Last().ShouldBe(NewRegionChoice.Chosen);
        vm.AvailableRegions.ShouldContain("Cellar");
        kept.ShouldBeFalse();
        vm.Regions.Keep();
        kept.ShouldBeTrue();
        vm.ToEdit().Region.ShouldBe("Cellar");

        // cancelled: as it was
        vm.Regions.AddRegion = () => Task.FromResult<NewRegionChoice?>(null);
        vm.Region = before;
        vm.Region = NewRegionChoice.Chosen;
        global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        vm.Region.ShouldBe(before);
    }

    [AvaloniaTest]
    public void AddNewRegion_InOneRoomsDialog_Too()
    {
        var room = Room(false, System.Drawing.Color.Red, RoomShape.Ellipse);
        var vm = new RoomPropertiesViewModel();
        vm.Initialize(room);
        vm.AvailableRegions.Last().ShouldBe(NewRegionChoice.Chosen);
        vm.Regions.AddRegion = () => Task.FromResult<NewRegionChoice?>(new NewRegionChoice("Attic", () => { }));

        vm.SelectedRegion = NewRegionChoice.Chosen;
        global::Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        vm.SelectedRegion.ShouldBe("Attic");
        vm.ApplyTo(room);
        room.Region.ShouldBe("Attic");
    }

    [AvaloniaTest]
    public void RegionList_ShowsAddNewRegion()
    {
        new Trizbort.Avalonia.Converters.RegionDisplayNameConverter()
            .Convert(NewRegionChoice.Chosen, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture)
            .ShouldBe("Add New Region...");
    }
}


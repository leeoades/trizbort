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

        vm.Heading.ShouldStartWith("2 rooms.");
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
        dialog.FindControl<global::Avalonia.Controls.CheckBox>("DarkCheck")!.IsChecked.ShouldBeNull();
        dialog.Close();
    }
}

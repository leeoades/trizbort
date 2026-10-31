using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Vector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

/// <summary>Ticket 131: trackpad scroll pans, mouse wheel zooms, and the scroll setting persists.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class TrackpadNavigationTests
{
    private Project? _previousProject;
    private ScrollWheelMode _savedMode;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
        _savedMode = ApplicationSettingsController.AppSettings.ScrollWheelBehavior;
        ApplicationSettingsController.AppSettings.ScrollWheelBehavior = ScrollWheelMode.Automatic;
    }

    [TearDown]
    public void TearDown()
    {
        ApplicationSettingsController.AppSettings.ScrollWheelBehavior = _savedMode;
        var created = Project.Current;
        Project.Current = _previousProject!;
        created.Dispose();
    }

    private static (Window Window, CommandService Service) CreateCanvas()
    {
        var service = new CommandService();
        var canvas = new MapCanvasControl { CommandService = service };
        var window = new Window { Content = canvas };
        window.Show();
        window.UpdateLayout();
        canvas.Focus();
        service.SetView(Vector.Zero, 1.0f);
        return (window, service);
    }

    [AvaloniaTest]
    public void FractionalWheelDelta_PansInsteadOfZooming()
    {
        var (window, service) = CreateCanvas();
        try
        {
            var p = new Point(300, 300);
            window.MouseMove(p);
            var originBefore = service.Origin;

            window.MouseWheel(p, new global::Avalonia.Vector(0, 0.25));

            service.ZoomFactor.ShouldBe(1.0f, 0.0001f);
            service.Origin.Y.ShouldNotBe(originBefore.Y);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void HorizontalWheelDelta_Pans()
    {
        var (window, service) = CreateCanvas();
        try
        {
            var p = new Point(300, 300);
            window.MouseMove(p);
            var originBefore = service.Origin;

            window.MouseWheel(p, new global::Avalonia.Vector(1, 0));

            service.ZoomFactor.ShouldBe(1.0f, 0.0001f);
            service.Origin.X.ShouldNotBe(originBefore.X);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void WholeWheelDelta_StillZooms()
    {
        var (window, service) = CreateCanvas();
        try
        {
            var p = new Point(300, 300);
            window.MouseMove(p);

            window.MouseWheel(p, new global::Avalonia.Vector(0, 1));

            service.ZoomFactor.ShouldBeGreaterThan(1.0f);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void ScrollWheelBehavior_RoundTripsThroughAppSettings()
    {
        var vm = new AppSettingsViewModel();
        vm.Initialize();
        vm.ScrollWheelBehavior.ShouldBe((int)ScrollWheelMode.Automatic);

        vm.ScrollWheelBehavior = (int)ScrollWheelMode.AlwaysPan;
        vm.ApplyToSettings();
        ApplicationSettingsController.AppSettings.ScrollWheelBehavior.ShouldBe(ScrollWheelMode.AlwaysPan);

        var reopened = new AppSettingsViewModel();
        reopened.Initialize();
        reopened.ScrollWheelBehavior.ShouldBe((int)ScrollWheelMode.AlwaysPan);

        ApplicationSettingsController.AppSettings.ScrollWheelBehavior = _savedMode;
        ApplicationSettingsController.SaveSettings();
    }
}

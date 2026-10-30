using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views.Dialogs;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Vector = Trizbort.Domain.Misc.Vector;
using Rect = Avalonia.Rect;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class ConnectionPropertiesDialogLayoutTests
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
        if (!ReferenceEquals(created, _previousProject))
        {
            created.Dispose();
        }
    }

    private static void EnsureStyles()
    {
        var app = Application.Current!;
        if (!app.Styles.OfType<FluentTheme>().Any())
        {
            app.Styles.Add(new FluentTheme());
        }

        var hasDialogStyles = app.Styles.OfType<global::Avalonia.Markup.Xaml.Styling.StyleInclude>()
            .Any(s => s.Source != null && s.Source.ToString().EndsWith("DialogStyles.axaml"));
        if (!hasDialogStyles)
        {
            app.Styles.Add(new global::Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("avares://Trizbort.Avalonia/"))
            {
                Source = new Uri("avares://Trizbort.Avalonia/Styles/DialogStyles.axaml")
            });
        }
    }

    private static ConnectionPropertiesDialog ShowDialog(ConnectionPropertiesViewModel vm)
    {
        EnsureStyles();
        var window = new ConnectionPropertiesDialog { DataContext = vm, ShowInTaskbar = false };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        return window;
    }

    private static void AssertNoClipping(Window window)
    {
        foreach (var scroll in window.GetVisualDescendants().OfType<ScrollViewer>())
        {
            scroll.Extent.Height.ShouldBeLessThanOrEqualTo(scroll.Viewport.Height + 1);
        }

        var bounds = new Rect(window.Bounds.Size);
        var controls = window.GetVisualDescendants()
            .Where(v => v is CheckBox || v is TextBox || v is Button || v is ColorPickerButton)
            .Cast<Control>()
            .Where(c => c.IsVisible && c.Bounds.Width > 0);
        foreach (var control in controls)
        {
            var topLeft = control.TranslatePoint(new Point(0, 0), window);
            topLeft.ShouldNotBeNull();
            var bottomRight = control.TranslatePoint(new Point(control.Bounds.Width, control.Bounds.Height), window);
            bottomRight.ShouldNotBeNull();
            bounds.Contains(topLeft!.Value).ShouldBeTrue(control.GetType().Name + " top-left outside window");
            bounds.Inflate(1).Contains(bottomRight!.Value).ShouldBeTrue(control.GetType().Name + " bottom-right outside window");
        }
    }

    [AvaloniaTest]
    public void ConnectionDialog_DefaultSize_NothingClipped_DoorOff()
    {
        var vm = new ConnectionPropertiesViewModel { HasDoor = false };
        var window = ShowDialog(vm);
        try
        {
            AssertNoClipping(window);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void ConnectionDialog_DefaultSize_NothingClipped_DoorOn()
    {
        var vm = new ConnectionPropertiesViewModel { HasDoor = true };
        var window = ShowDialog(vm);
        try
        {
            AssertNoClipping(window);
            var texts = window.GetVisualDescendants().OfType<CheckBox>().Select(c => c.Content?.ToString()).ToList();
            texts.ShouldContain("Open_able");
            texts.ShouldContain("_Open");
            texts.ShouldContain("Locka_ble");
            texts.ShouldContain("Loc_ked");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void ConnectionDialog_DescriptionIsMultilineAndColorRowPresent()
    {
        var vm = new ConnectionPropertiesViewModel();
        var window = ShowDialog(vm);
        try
        {
            var box = window.GetVisualDescendants().OfType<TextBox>().First(t => t.Name == "DescriptionBox");
            box.AcceptsReturn.ShouldBeTrue();
            box.TextWrapping.ShouldBe(global::Avalonia.Media.TextWrapping.Wrap);
            window.GetVisualDescendants().OfType<ColorPickerButton>().Count().ShouldBe(1);

            var headers = window.GetVisualDescendants().OfType<HeaderedContentControl>().Select(h => h.Header?.ToString()).ToList();
            headers.ShouldContain("Style");
            headers.ShouldContain("Text");
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public void ConnectionPropertiesViewModel_Description_RoundTripsMultilineText()
    {
        var connection = new Connection(Project.Current, new Vertex(new Vector(0, 0)), new Vertex(new Vector(10, 10)));
        Project.Current.Elements.Add(connection);
        var vm = new ConnectionPropertiesViewModel();
        vm.Initialize(connection);
        vm.ConnectionDescription = "line one\nline two\n\nline four";
        vm.ApplyTo(connection);

        var vm2 = new ConnectionPropertiesViewModel();
        vm2.Initialize(connection);
        vm2.ConnectionDescription.ShouldBe("line one\nline two\n\nline four");
    }
}

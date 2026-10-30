using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views;
using Trizbort.Avalonia.Views.Dialogs;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Elements;
using Trizbort.Setup;

namespace Trizbort.Avalonia.Tests;

/// <summary>
/// T0.5: renders every Avalonia dialog (all tabs and key dependent states) to PNG.
/// Skipped unless TRIZBORT_CAPTURE_OUTPUT is set. Output goes directly to &lt;root&gt;.
///
/// Dependent states: CheckBoxes whose Content is listed in <see cref="ToggleCheckBoxes"/> are captured
/// checked and unchecked (file suffix "__&lt;name&gt;-on" / "__&lt;name&gt;-off"). Not captured: RadioButtons and
/// non-checkbox drivers (e.g. the "!AllCornersEqual" corner fields are covered via the "All corners equal"
/// checkbox; AutomapSettings "Rooms with same name" is driven by the "Transcript is verbose" checkbox).
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("Capture")]
public class DialogCaptureTests
{
    private static readonly HashSet<string> ToggleCheckBoxes = new()
    {
        "Show Canvas Tooltips",
        "Limit Room Description Characters",
        "Limit Connection Description Characters",
        "Specify General Margins for Export",
        "Has Door",
        "Draw Outline",
        "Fill Background",
        "All corners equal",
        "Transcript is verbose"
    };

    private Project? _previousProject;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
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

    [AvaloniaTest]
    public void CaptureAvaloniaDialogScreenshots()
    {
        var root = Environment.GetEnvironmentVariable("TRIZBORT_CAPTURE_OUTPUT");
        if (string.IsNullOrWhiteSpace(root))
        {
            Assert.Ignore("Set TRIZBORT_CAPTURE_OUTPUT to generate Avalonia dialog captures.");
        }

        var output = root;
        Directory.CreateDirectory(output);

        // The headless test application has no styles; dialogs need a theme to render controls.
        if (Application.Current != null && !Application.Current.Styles.OfType<FluentTheme>().Any())
        {
            Application.Current.Styles.Add(new FluentTheme());
        }

        using var project = new Project();
        Project.Current = project;
        Settings.Reset();
        Load(project, Path.Combine(FindManualFixtureDirectory(), "object-testing.trizbort"));

        var room = project.Elements.OfType<Room>().OrderBy(r => r.Id).First();
        var connection = project.Elements.OfType<Connection>().OrderBy(c => c.Id).First();
        // object-testing.trizbort has no label, so use the first one if present, else a new one.
        var label = project.Elements.OfType<MapLabel>().OrderBy(l => l.Id).FirstOrDefault()
                    ?? new MapLabel(project) { Text = "Sample label" };

        var services = App.ConfigureServices();
        var files = new List<string>();

        using (var scope = services.CreateScope())
        {
            var sp = scope.ServiceProvider;

            var roomVm = sp.GetRequiredService<RoomPropertiesViewModel>();
            roomVm.Initialize(room);
            CaptureDialog(output, files, "RoomProperties", new RoomPropertiesDialog { DataContext = roomVm });

            var connVm = sp.GetRequiredService<ConnectionPropertiesViewModel>();
            connVm.Initialize(connection);
            CaptureDialog(output, files, "ConnectionProperties", new ConnectionPropertiesDialog { DataContext = connVm });

            var labelVm = sp.GetRequiredService<MapLabelPropertiesViewModel>();
            labelVm.Initialize(label);
            CaptureDialog(output, files, "MapLabelProperties", new MapLabelPropertiesDialog { DataContext = labelVm });

            var mapSettingsVm = sp.GetRequiredService<MapSettingsViewModel>();
            mapSettingsVm.Initialize();
            CaptureDialog(output, files, "MapSettings", new MapSettingsDialog { DataContext = mapSettingsVm });

            var themeVm = sp.GetRequiredService<MapThemeViewModel>();
            themeVm.Initialize();
            CaptureDialog(output, files, "MapTheme", new MapThemeDialog { DataContext = themeVm });

            var appSettingsVm = sp.GetRequiredService<AppSettingsViewModel>();
            appSettingsVm.Initialize();
            CaptureDialog(output, files, "AppSettings", new AppSettingsDialog { DataContext = appSettingsVm });

            var automapVm = sp.GetRequiredService<AutomapSettingsViewModel>();
            automapVm.Initialize(ApplicationSettingsController.AppSettings.Automap);
            CaptureDialog(output, files, "AutomapSettings", new AutomapSettingsDialog { DataContext = automapVm });
        }

        CaptureDialog(output, files, "About", new AboutDialog());

        var index = new StringBuilder();
        index.AppendLine("# Avalonia dialog screenshots");
        index.AppendLine();
        index.AppendLine("Fixture: tests/manual/object-testing.trizbort (first room, first connection, new label).");
        index.AppendLine("Naming: `<Dialog>__<tab>__<state>.png`.");
        index.AppendLine();
        foreach (var file in files)
        {
            index.AppendLine($"- {file}");
        }

        File.WriteAllText(Path.Combine(output, "index.md"), index.ToString(), Encoding.UTF8);
    }

    private static void CaptureDialog(string output, List<string> files, string name, Window window)
    {
        window.ShowInTaskbar = false;
        if (double.IsNaN(window.Width))
        {
            window.Width = 800;
        }

        if (double.IsNaN(window.Height))
        {
            window.Height = 600;
        }

        window.Show();
        try
        {
            Settle(window);

            var tabControl = window.GetVisualDescendants().OfType<TabControl>().FirstOrDefault();
            if (tabControl == null)
            {
                CaptureStates(output, files, name, "main", window);
                return;
            }

            var items = tabControl.Items.OfType<TabItem>().ToList();
            for (var i = 0; i < items.Count; i++)
            {
                tabControl.SelectedIndex = i;
                Settle(window);
                var header = items[i].Header?.ToString();
                CaptureStates(output, files, name, string.IsNullOrWhiteSpace(header) ? "tab" + i : header, window);
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static void CaptureStates(string output, List<string> files, string dialog, string tab, Window window)
    {
        Save(output, files, $"{Sanitize(dialog)}__{Sanitize(tab)}__default.png", window);

        var toggles = window.GetVisualDescendants()
            .OfType<CheckBox>()
            .Where(c => c.Content is string text && ToggleCheckBoxes.Contains(text))
            .ToList();

        foreach (var checkBox in toggles)
        {
            var original = checkBox.IsChecked;
            var checkName = Sanitize((string)checkBox.Content!);
            foreach (var on in new[] { true, false })
            {
                checkBox.IsChecked = on;
                Settle(window);
                Save(output, files, $"{Sanitize(dialog)}__{Sanitize(tab)}__{checkName}-{(on ? "on" : "off")}.png", window);
            }

            checkBox.IsChecked = original;
            Settle(window);
        }
    }

    private static void Save(string output, List<string> files, string fileName, Window window)
    {
        var width = Math.Max(1, (int)Math.Ceiling(window.Bounds.Width));
        var height = Math.Max(1, (int)Math.Ceiling(window.Bounds.Height));
        using var bitmap = new RenderTargetBitmap(new PixelSize(width, height));
        bitmap.Render(window);
        bitmap.Save(Path.Combine(output, fileName));
        files.Add(fileName);
    }

    private static void Settle(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private static string Sanitize(string value)
    {
        var cleaned = Regex.Replace(value, "[^A-Za-z0-9-]+", "-").Trim('-');
        return cleaned.Length == 0 ? "x" : cleaned;
    }

    private static string FindManualFixtureDirectory()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "tests", "manual");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        Assert.Fail("Could not find tests/manual from " + TestContext.CurrentContext.TestDirectory);
        return string.Empty;
    }

    private static void Load(Project project, string path)
    {
        Exception? failure = null;
        var loaded = new LegacyMapFileEngine(
            project,
            error => failure = error,
            _ => { },
            (_, _) => { }).Load(path);
        loaded.ShouldBeTrue(failure?.ToString());
    }
}

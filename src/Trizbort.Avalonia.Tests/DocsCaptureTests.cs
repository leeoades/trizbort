using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Themes.Fluent;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Trizbort.Automap;
using Trizbort.Avalonia.Controls;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Setup;
using AutomapEngine = Trizbort.Automap.Automap;
using CoreVector = Trizbort.Domain.Misc.Vector;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///   The pictures in the user guide (Docs/): the app as it is, drawn from a small invented adventure, The Lighthouse,
///   automapped from a transcript, so every picture shows the same world. Skipped unless TRIZBORT_DOCS_CAPTURE names a
///   folder to write them to; scripts/update-docs-images copies the ones the guide uses into Docs/images. Lee, 24 Nov
///   2026: keep the guide's screenshots up to date.
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("Docs")]
public class DocsCaptureTests
{
    /// <summary>A short game: a beach, a lighthouse and a keeper's cottage, with a cave, and a way up and down.</summary>
    public const string Lighthouse = """
        >look

        Beach
        You are on a wide sandy beach. The sea is to the south. Dunes rise to the north, and rock pools glitter to the east.

        >north

        Dunes
        Grassy dunes roll away inland. A lighthouse stands to the east; the beach is south.

        >east

        Outside the Lighthouse
        A tall white lighthouse towers above you. Its door is to the north, and a cottage lies to the east.

        >north

        Lighthouse Base
        The round base of the lighthouse. An iron stair spirals up.

        You can see an oil can here.

        >tb see oil can

        >up

        Spiral Stair
        Iron steps wind round and round, up into the light and down into the dark.

        >up

        Lamp Room
        The great lamp sits in its glass cage, cold. Windows look out over the sea.

        >down

        Spiral Stair
        Iron steps wind round and round, up into the light and down into the dark.

        >down

        Lighthouse Base
        The round base of the lighthouse. An iron stair spirals up.

        >south

        Outside the Lighthouse
        A tall white lighthouse towers above you. Its door is to the north, and a cottage lies to the east.

        >east

        Keeper's Cottage
        A snug cottage with a fireplace. A kitchen is to the north.

        >north

        Kitchen
        Pots and pans hang over a scrubbed table. A trapdoor in the floor leads down.

        >down

        Cellar
        A damp cellar. Shelves of jars line the walls.

        You can see a brass key here.

        >tb see brass key

        >up

        Kitchen
        Pots and pans hang over a scrubbed table. A trapdoor in the floor leads down.

        >south

        Keeper's Cottage
        A snug cottage with a fireplace. A kitchen is to the north.

        >west

        Outside the Lighthouse
        A tall white lighthouse towers above you. Its door is to the north, and a cottage lies to the east.

        >west

        Dunes
        Grassy dunes roll away inland. A lighthouse stands to the east; the beach is south.

        >south

        Beach
        You are on a wide sandy beach. The sea is to the south. Dunes rise to the north, and rock pools glitter to the east.

        >east

        Rock Pools
        Rock pools glitter among the boulders. A dark cave mouth opens to the north. The beach is west.

        >north

        Sea Cave
        Water drips in the darkness. The cave goes on to the east, into the cliff.

        >
        """;

    /// <summary>A label written in Markdown: a heading, a table, and links to rooms.</summary>
    public const string TreasureLabel = """
        ## Treasures
        | Treasure | Where |
        |---|---|
        | Oil can | [[Lighthouse Base]] |
        | Brass key | [[Cellar]] |
        | **Pearl** | *not found yet* |
        """;

    private Project? _previous;
    private FluentTheme? _theme;

    [SetUp]
    public void SetUp()
    {
        _previous = Project.Current;
        if (Application.Current != null && !Application.Current.Styles.OfType<FluentTheme>().Any())
        {
            _theme = new FluentTheme();
            Application.Current.Styles.Add(_theme);
        }
    }

    [TearDown]
    public void TearDown()
    {
        MapCanvasControl.HereRoomOverride = null;
        if (_theme != null) Application.Current?.Styles.Remove(_theme);
        Project.Current = _previous!;
    }

    private static string Output()
    {
        var root = Environment.GetEnvironmentVariable("TRIZBORT_DOCS_CAPTURE");
        if (string.IsNullOrWhiteSpace(root)) Assert.Ignore("Set TRIZBORT_DOCS_CAPTURE to a folder to draw the guide's pictures.");
        Directory.CreateDirectory(root);
        return root;
    }

    private static async Task WaitFor(Func<bool> condition, string what, int seconds = 15)
    {
        var clock = Stopwatch.StartNew();
        while (!condition())
        {
            if (clock.Elapsed.TotalSeconds > seconds) Assert.Fail("Timed out waiting for " + what);
            await Task.Delay(10);
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static void Settle(Window window)
    {
        for (var i = 0; i < 5; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    private static void Save(Window window, string path)
    {
        Settle(window);
        using var frame = window.CaptureRenderedFrame();
        frame?.Save(path);
    }

    /// <summary>
    ///   The main window, part way through automapping The Lighthouse: the map, the token where you are, the automap
    ///   panel with the game screen, and a Markdown label of treasures.
    /// </summary>
    [AvaloniaTest]
    public async Task TheMainWindow_Automapping_TheLighthouse()
    {
        var root = Output();
        var transcript = Path.Combine(Path.GetTempPath(), "lighthouse-" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(transcript, Lighthouse.Replace("\r\n", "\n"));
        Settings.Reset();
        var services = App.ConfigureServices();
        var vm = services.GetRequiredService<MainWindowViewModel>();
        var window = new MainWindow { DataContext = vm, Width = 1280, Height = 800 };
        try
        {
            window.Show();
            Settle(window);
            var settings = AutomapSettings.Default;
            settings.FileName = transcript;
            settings.AddObjectCommand = "tb see";
            settings.AutoTidy = true;
            settings.GuessExits = true;
            settings.VerboseTranscript = true;
            await vm.StartAutomapAsync(settings);
            await WaitFor(() => AutomapEngine.Instance.CurrentRoom?.Name == "Sea Cave", "the sea cave");
            await Task.Delay(400);

            // the treasures, written in Markdown, beside the map
            var project = Project.Current;
            var rooms = project.Elements.OfType<Room>().ToList();
            var right = rooms.Max(room => room.InnerBounds.Right);
            var top = rooms.Min(room => room.InnerBounds.Top);
            var label = new MapLabel(project)
            {
                Markdown = true,
                Text = TreasureLabel.Replace("\r\n", "\n"),
                Position = new CoreVector(right + 64, top)
            };
            label.Size = new CoreVector(220, 60);
            label.Size = Trizbort.Avalonia.Drawing.MapLabelDrawingAdapter.FitText(label, label.Size);
            project.Elements.Add(label);
            vm.CommandService.Session.Viewport.FitToMap(project.Elements);
            vm.CommandService.Select(rooms.First(room => room.Name == "Lighthouse Base"));
            vm.RefreshAutomapPanel();
            Save(window, Path.Combine(root, "main-window.png"));
        }
        finally
        {
            vm.StopAutomap();
            window.Close();
            try { File.Delete(transcript); } catch (IOException) { }
        }
    }

    /// <summary>
    ///   Automap's guess in a maze: west from the coins room, which the map shows leading to the keys room; the panel
    ///   says so, with Wrong Room: Choose… and That's Right.
    /// </summary>
    [AvaloniaTest]
    public async Task TheAutomapPanel_SaysWhatItGuessed_InAMaze()
    {
        var root = Output();
        const string Alike = "You are in a maze of twisty little passages, all alike.";
        Settings.Reset();
        var project = new Project();
        Project.Current = project;
        var food = new Room(project) { Name = "Maze", Position = new CoreVector(0, 0), Objects = "food" };
        var keys = new Room(project) { Name = "Maze", Position = new CoreVector(300, 0), Objects = "keys" };
        var coins = new Room(project) { Name = "Maze", Position = new CoreVector(600, 0), Objects = "coins" };
        foreach (var room in new[] { food, keys, coins })
        {
            room.AddDescription(Alike);
            project.Elements.Add(room);
        }

        // south from the hall into the coins room, and the keys room east of the food room's neighbour: as Lee mapped it
        var hall = new Room(project) { Name = "Hall of Mists", Position = new CoreVector(600, -300) };
        hall.AddDescription("A huge hall, full of mist.");
        project.Elements.Add(hall);
        var joins = new AutomapCanvas(project);
        joins.Connect(keys, MappableDirection.East, coins, true);
        joins.Connect(hall, MappableDirection.South, coins, true);
        var transcript = Path.Combine(Path.GetTempPath(), "maze-" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(transcript,
            $">look\n\nHall of Mists\nA huge hall, full of mist.\n\n>south\n\nMaze\n{Alike}\n\n>west\n\nMaze\n{Alike}\n\n>");

        var vm = new MainWindowViewModel(new CommandService(), null!, null!, new AvaloniaAutomapCanvas());
        var window = new Window { DataContext = vm, Width = 300, Height = 760, Content = new AutomapPanel() };
        try
        {
            window.Show();
            var settings = AutomapSettings.Default;
            settings.FileName = transcript;
            settings.AutoTidy = false;
            settings.VerboseTranscript = true;
            await vm.StartAutomapAsync(settings);
            await WaitFor(() => AutomapEngine.Instance.CurrentRoom == keys, "taken back to the keys room");
            vm.RefreshAutomapPanel();
            Save(window, Path.Combine(root, "automap-guess.png"));
        }
        finally
        {
            vm.StopAutomap();
            window.Close();
            try { File.Delete(transcript); } catch (IOException) { }
        }
    }
}

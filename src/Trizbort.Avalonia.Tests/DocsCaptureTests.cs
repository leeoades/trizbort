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
using Trizbort.Avalonia.Views.Dialogs;
using Trizbort.Editor;
using Trizbort.Navigation;
using Trizbort.Avalonia.Drawing;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Setup;
using AutomapEngine = Trizbort.Automap.Automap;
using CoreVector = Trizbort.Domain.Misc.Vector;
using CompassPoint = Trizbort.Domain.Misc.CompassPoint;

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
    private readonly List<global::Avalonia.Styling.IStyle> _addedStyles = new();
    private readonly List<object> _addedResources = new();

    /// <summary>
    ///   The app as users see it: the real app's styles and resources (its theme, dialog styles and toolbar icons),
    ///   which the test application doesn't have; and a fresh map for each picture.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _previous = Project.Current;
        Project.Current = new Project();
        var current = Application.Current!;
        // the test application is a bare Application: the app's own App.axaml isn't loaded, so its styles are added
        // as the app adds them, and its icons read from it
        void AddStyle(global::Avalonia.Styling.IStyle style)
        {
            current.Styles.Add(style);
            _addedStyles.Add(style);
        }

        if (!current.Styles.OfType<FluentTheme>().Any()) AddStyle(new FluentTheme());
        var package = new Uri("avares://Trizbort.Avalonia/");
        AddStyle(new global::Avalonia.Markup.Xaml.Styling.StyleInclude(package)
            { Source = new Uri("avares://Avalonia.Controls.ColorPicker/Themes/Fluent/Fluent.xaml") });
        AddStyle(new global::Avalonia.Markup.Xaml.Styling.StyleInclude(package)
            { Source = new Uri("avares://Trizbort.Avalonia/Styles/DialogStyles.axaml") });

        var xaml = System.Xml.Linq.XDocument.Load(AppXaml());
        foreach (var icon in xaml.Descendants().Where(element => element.Name.LocalName == "StreamGeometry"))
        {
            var key = icon.Attributes().First(attribute => attribute.Name.LocalName == "Key").Value;
            if (current.Resources.ContainsKey(key)) continue;
            current.Resources[key] = global::Avalonia.Media.StreamGeometry.Parse(icon.Value);
            _addedResources.Add(key);
        }
    }

    /// <summary>App.axaml, from the source tree the tests run in.</summary>
    private static string AppXaml()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
        {
            var path = Path.Combine(folder.FullName, "src", "Trizbort.Avalonia", "App.axaml");
            if (File.Exists(path)) return path;
        }

        throw new FileNotFoundException("App.axaml, above " + AppContext.BaseDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        MapCanvasControl.HereRoomOverride = null;
        var current = Application.Current!;
        foreach (var style in _addedStyles) current.Styles.Remove(style);
        foreach (var key in _addedResources) current.Resources.Remove(key);
        _addedStyles.Clear();
        _addedResources.Clear();
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
        var folder = Directory.CreateTempSubdirectory("trizbort-docs-").FullName;
        var transcript = Path.Combine(folder, "lighthouse.txt");
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
            var bottom = rooms.Max(room => room.InnerBounds.Bottom);
            var label = new MapLabel(project)
            {
                Markdown = true,
                Text = TreasureLabel.Replace("\r\n", "\n"),
                Position = new CoreVector(right + 48, bottom - 160)
            };
            label.Size = new CoreVector(220, 60);
            label.Size = Trizbort.Avalonia.Drawing.MapLabelDrawingAdapter.FitText(label, label.Size);
            project.Elements.Add(label);
            vm.CommandService.Session.Viewport.FitToMap(project.Elements);
            vm.CommandService.Select(rooms.First(room => room.Name == "Lighthouse Base"));
            vm.RefreshAutomapPanel();
            vm.UpdateState();
            Save(window, Path.Combine(root, "main-window.png"));
        }
        finally
        {
            vm.StopAutomap();
            window.Close();
            try { Directory.Delete(folder, true); } catch (IOException) { }
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
        var folder = Directory.CreateTempSubdirectory("trizbort-docs-").FullName;
        var transcript = Path.Combine(folder, "adventure.txt");
        File.WriteAllText(transcript,
            $">look\n\nHall of Mists\nA huge hall, full of mist.\n\n>south\n\nMaze\n{Alike}\n\n>west\n\nMaze\n{Alike}\n\n>");

        var vm = new MainWindowViewModel(new CommandService(), null!, null!, new AvaloniaAutomapCanvas());
        // the top of the panel: where you are, and what automap guessed
        var window = new Window { DataContext = vm, Width = 300, Height = 440, Content = new AutomapPanel() };
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
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }
    }

    /// <summary>The Lighthouse, automapped from its transcript by automap's engine: a map to show in the dialogs.</summary>
    private static async Task<Project> LighthouseAsync()
    {
        var project = new Project();
        Project.Current = project;
        Settings.Reset();
        var folder = Directory.CreateTempSubdirectory("trizbort-docs-").FullName;
        var transcript = Path.Combine(folder, "lighthouse.txt");
        File.WriteAllText(transcript, Lighthouse.Replace("\r\n", "\n"));
        var engine = new AutomapEngine((_, _) => { }, NonInteractiveEditorPrompts.Instance);
        var settings = AutomapSettings.Default;
        settings.FileName = transcript;
        settings.AddObjectCommand = "tb see";
        settings.AutoTidy = true;
        settings.VerboseTranscript = true;
        var run = engine.Start(new AutomapCanvas(project, engine), settings);
        try
        {
            await WaitFor(() => engine.CurrentRoom?.Name == "Sea Cave", "the sea cave");
        }
        finally
        {
            engine.Stop();
            await run;
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }

        return project;
    }

    private static Room Called(Project project, string name) =>
        project.Elements.OfType<Room>().First(room => room.Name == name);

    /// <summary>A dialog, shown at its own size, saved as it first appears.</summary>
    private static void Capture(Window dialog, string path)
    {
        dialog.ShowInTaskbar = false;
        dialog.Show();
        try
        {
            Save(dialog, path);
        }
        finally
        {
            dialog.Close();
        }
    }

    /// <summary>Room Properties for the Lighthouse Base: its name, description and the oil can.</summary>
    [AvaloniaTest]
    public async Task TheRoomAndJoinDialogs_ForTheLighthouse()
    {
        var root = Output();
        var project = await LighthouseAsync();
        var services = App.ConfigureServices();
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var room = Called(project, "Lighthouse Base");
        var roomVm = sp.GetRequiredService<RoomPropertiesViewModel>();
        roomVm.Initialize(room);
        Capture(new RoomPropertiesDialog { DataContext = roomVm }, Path.Combine(root, "room-properties.png"));

        // up the spiral stair
        var stair = Called(project, "Spiral Stair");
        var join = project.Elements.OfType<Connection>().First(connection =>
            (connection.GetSourceRoom() == room && connection.GetTargetRoom() == stair) ||
            (connection.GetSourceRoom() == stair && connection.GetTargetRoom() == room));
        var joinVm = sp.GetRequiredService<ConnectionPropertiesViewModel>();
        joinVm.Initialize(join);
        Capture(new ConnectionPropertiesDialog { DataContext = joinVm }, Path.Combine(root, "join-properties.png"));

        // the treasures label, being written: the Markdown toolbar, and Add Link listing the rooms
        var label = new MapLabel(project) { Markdown = true, Text = TreasureLabel.Replace("\r\n", "\n") };
        project.Elements.Add(label);
        var labelVm = sp.GetRequiredService<MapLabelPropertiesViewModel>();
        labelVm.Initialize(label);
        labelVm.LinkPanelOpen = true;
        Capture(new MapLabelPropertiesDialog { DataContext = labelVm }, Path.Combine(root, "label-editor.png"));
    }

    /// <summary>Directions: the way from the beach up to the lamp room, glowing on the map.</summary>
    [AvaloniaTest]
    public async Task Directions_GlowOnTheMap()
    {
        var root = Output();
        var project = await LighthouseAsync();
        var service = new CommandService();
        service.SetViewportSize(760, 560);
        var canvas = new MapCanvasControl { CommandService = service };
        var window = new Window { Width = 760, Height = 560, Content = canvas };
        try
        {
            window.Show();
            service.Session.Viewport.FitToMap(project.Elements);
            RouteGlow.Route = RouteFinder.Find(project.Elements, Called(project, "Beach"), Called(project, "Lamp Room"));
            Save(window, Path.Combine(root, "directions.png"));
        }
        finally
        {
            RouteGlow.Route = null;
            window.Close();
        }
    }

    /// <summary>
    ///   A few rooms and joins in their different looks, for Drawing the map: objects (one inside another), a dark
    ///   room, a second fill, a dotted border; a one-way join, up and down labels, a middle label, and a dotted join
    ///   bent with its middle handle. Lee, 26 Nov 2026.
    /// </summary>
    [AvaloniaTest]
    public void RoomsAndJoins_InTheirDifferentLooks()
    {
        var root = Output();
        var project = new Project();
        Project.Current = project;
        Settings.Reset();

        Room Add(string name, double x, double y)
        {
            var room = new Room(project) { Name = name, Size = new CoreVector(128, 64) };
            room.Position = new CoreVector(x, y);
            project.Elements.Add(room);
            return room;
        }

        Connection Join(Room from, CompassPoint fromSide, Room to, CompassPoint toSide)
        {
            var join = new Connection(project, new Vertex(from.PortAt(fromSide)), new Vertex(to.PortAt(toSide)));
            project.Elements.Add(join);
            return join;
        }

        var hall = Add("Hall", 0, 0);
        hall.Objects = "coat stand\n- umbrella\nletter";
        var kitchen = Add("Kitchen", 320, 0);
        var cellar = Add("Cellar", 320, 224);
        cellar.IsDark = true;
        cellar.Objects = "wine rack";
        var garden = Add("Garden", 0, 224);
        garden.SecondFillColor = System.Drawing.Color.FromArgb(0x9C, 0xCC, 0x8C);
        garden.SecondFillLocation = "Bottom";
        var shed = Add("Shed", -320, 224);
        shed.BorderStyle = BorderDashStyle.Dot;
        shed.SubTitle = "not seen yet";

        Join(hall, CompassPoint.East, kitchen, CompassPoint.West);
        var stairs = Join(kitchen, CompassPoint.South, cellar, CompassPoint.North);
        stairs.StartText = "d";
        stairs.EndText = "u";
        var window = Join(hall, CompassPoint.South, garden, CompassPoint.North);
        window.Flow = ConnectionFlow.OneWay;
        window.MidText = "climb out of window";
        var grating = Join(garden, CompassPoint.East, cellar, CompassPoint.West);
        grating.Style = ConnectionStyle.Dashed;
        grating.MidText = "grating?";
        grating.SetCurveWaypoint(CurveWaypoint.Middle, new CoreVector(224, 352));
        Join(shed, CompassPoint.East, garden, CompassPoint.West);

        var service = new CommandService();
        service.SetViewportSize(760, 440);
        var canvas = new MapCanvasControl { CommandService = service };
        var shown = new Window { Width = 760, Height = 440, Content = canvas };
        try
        {
            shown.Show();
            service.Session.Viewport.FitToMap(project.Elements);
            Save(shown, Path.Combine(root, "rooms-and-joins.png"));
        }
        finally
        {
            shown.Close();
        }
    }

    /// <summary>
    ///   Backgrounds behind The Lighthouse: the shore, the lighthouse itself and the keeper's cottage, each set apart
    ///   in its own colour; and a background's properties. Lee, 24 Nov 2026.
    /// </summary>
    [AvaloniaTest]
    public async Task Backgrounds_SetPartsOfTheMapApart()
    {
        var root = Output();
        var project = await LighthouseAsync();
        var service = new CommandService();
        service.SetViewportSize(760, 560);
        var session = service.Session;

        async Task<Background> Behind(System.Drawing.Color fill, RoomShape shape, params string[] rooms)
        {
            session.Selection.Set(rooms.Select(name => (Element)Called(project, name)).ToArray());
            var background = await session.AddBackgroundAsync(null, false);
            background.FillColor = fill;
            background.Shape = shape;
            return background;
        }

        await Behind(System.Drawing.Color.FromArgb(0xDD, 0xEC, 0xF7), RoomShape.RoundedCorners,
            "Beach", "Dunes", "Rock Pools", "Sea Cave");
        var tower = await Behind(System.Drawing.Color.FromArgb(0xFA, 0xEF, 0xD2), RoomShape.RoundedCorners,
            "Outside the Lighthouse", "Lighthouse Base", "Spiral Stair", "Lamp Room");
        tower.BorderStyle = BorderDashStyle.Dash;
        tower.BorderColor = System.Drawing.Color.FromArgb(0xC8, 0xA4, 0x5A);
        await Behind(System.Drawing.Color.FromArgb(0xE3, 0xF1, 0xDF), RoomShape.RoundedCorners,
            "Keeper's Cottage", "Kitchen", "Cellar");
        session.ClearSelection();

        var canvas = new MapCanvasControl { CommandService = service };
        var window = new Window { Width = 760, Height = 560, Content = canvas };
        try
        {
            window.Show();
            session.Viewport.FitToMap(project.Elements);
            Save(window, Path.Combine(root, "backgrounds.png"));
        }
        finally
        {
            window.Close();
        }

        var vm = new BackgroundPropertiesViewModel();
        vm.Initialize(tower);
        Capture(new BackgroundPropertiesDialog { DataContext = vm }, Path.Combine(root, "background-properties.png"));
    }
}

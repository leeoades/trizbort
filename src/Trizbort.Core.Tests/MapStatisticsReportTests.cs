using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 087: the Map Statistics report text and its commands.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class MapStatisticsReportTests
{
  private static readonly MapDimensions _dimensions = new(200, 100, 140, 40);

  private Project _previousProject;
  private Project _project;
  private List<Region> _previousRegions;
  private EditorSession _session;
  private ReportPrompts _prompts;
  private string _tempFolder;

  private sealed class ReportPrompts : IEditorPrompts
  {
    public string ReportTitle { get; private set; }

    public string ReportText { get; private set; }

    public string SuggestedPath { get; private set; }

    public string PickedPath { get; set; }

    public List<string> Messages { get; } = new();

    public Task<Trizbort.Automap.AutomapSameDirectionResult> ChooseSameDirectionAsync(
      Room existing, string newRoomName, CancellationToken token = default)
    {
      return NonInteractiveEditorPrompts.Instance.ChooseSameDirectionAsync(existing, newRoomName, token);
    }

    public Task<RoomDisambiguation> DisambiguateRoomAsync(
      string transcriptLine, string roomDescription, IReadOnlyList<Room> candidates, CancellationToken token = default)
    {
      return NonInteractiveEditorPrompts.Instance.DisambiguateRoomAsync(transcriptLine, roomDescription, candidates, token);
    }

    public Task ShowTextReportAsync(string title, string text, CancellationToken token = default)
    {
      ReportTitle = title;
      ReportText = text;
      return Task.CompletedTask;
    }

    public Task<string> PickSaveFileAsync(string title, string suggestedPath, string extension, CancellationToken token = default)
    {
      SuggestedPath = suggestedPath;
      return Task.FromResult(PickedPath);
    }

    public Task MessageAsync(string message, string title, CancellationToken token = default)
    {
      Messages.Add(title + "|" + message);
      return Task.CompletedTask;
    }
  }

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _previousRegions = Settings.Regions;
    Settings.Regions = new List<Region> { new() { RegionName = Region.DefaultRegion } };
    _session = new EditorSession();
    _prompts = new ReportPrompts();
    _session.Prompts = _prompts;
    _tempFolder = Path.Combine(Path.GetTempPath(), "trizbort-stats-" + Path.GetRandomFileName());
    Directory.CreateDirectory(_tempFolder);
  }

  [TearDown]
  public void TearDown()
  {
    _session.Dispose();
    Settings.Regions = _previousRegions;
    var current = Project.Current;
    Project.Current = _previousProject;
    if (current != null && current != _previousProject) current.Dispose();
    if (_project != current) _project.Dispose();
    if (Directory.Exists(_tempFolder)) Directory.Delete(_tempFolder, true);
  }

  private Room AddRoom(string name, float x = 0, float y = 0)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y), Size = new Vector(96, 64) };
    Project.Current.Elements.Add(room);
    return room;
  }

  [Test]
  public void EmptyMap_SaysThereAreNoRooms()
  {
    MapStatisticsReport.Build(_dimensions).ShouldBe("No rooms to take stats of.");
  }

  [Test]
  public void Report_StartsWithRoomCountsAndHasStableLayout()
  {
    var start = AddRoom("Hall");
    start.IsStartRoom = true;
    AddRoom("Cellar", 200, 0).IsDark = true;

    var report = MapStatisticsReport.Build(_dimensions);

    report.ShouldStartWith("# of Rooms: 2\n# of Dark Rooms: 1\n# of Unconnected Rooms: 2\n\nStart room = Hall\nNo end room.\n\n");
    report.ShouldContain("Dimensions with margins: height 100, width 200\nDimensions without margins: height 40, width 140\n");
    report.ShouldContain("\n==============================Odd stuff below here\n");
    report.ShouldNotContain("\r");
    report.ShouldEndWith("No rooms with duplicate down exits.\n");
  }

  [Test]
  public void Report_UsesSingularAndPluralWording()
  {
    AddRoom("Hall");

    var report = MapStatisticsReport.Build(_dimensions);

    report.ShouldContain("0 doors, 0 locked");
    report.ShouldContain("0 up-down connections\n");
  }

  [Test]
  public void Report_ListsDuplicateRooms()
  {
    AddRoom("Hall");
    AddRoom("hall", 200, 0);

    MapStatisticsReport.Build(_dimensions).ShouldContain("Duplicate rooms: Hall(2).\n");
  }

  [Test]
  public void Report_ListsRegionsAndTheirRooms()
  {
    Settings.Regions = new List<Region>
    {
      new() { RegionName = Region.DefaultRegion },
      new() { RegionName = "Cellars" },
      new() { RegionName = "Attic" },
      new() { RegionName = "Garden" }
    };
    AddRoom("Wine store").Region = "Cellars";
    AddRoom("Coal store", 200, 0).Region = "Cellars";
    AddRoom("Box room", 400, 0).Region = "Attic";

    var report = MapStatisticsReport.Build(_dimensions);

    report.ShouldContain("# of Regions: 3\n# of Rooms without a region: 0\nRegions:\nAttic (1 room)\nCellars (2 rooms)\nGarden (0 rooms)\n");
    report.ShouldContain("Only room in Attic: Box room\n");
    report.ShouldContain("List of rooms in Cellars: Coal store, Wine store\n");
    report.ShouldContain("Garden has no rooms.\n");
  }

  [Test]
  public async Task Show_SendsTheReportToThePrompts()
  {
    AddRoom("Hall");

    await MapStatisticsCommands.ShowAsync(_session, _dimensions);

    _prompts.ReportTitle.ShouldBe("Map Statistics");
    _prompts.ReportText.ShouldBe(MapStatisticsReport.Build(_dimensions));
  }

  [Test]
  public void DefaultLogPath_ReplacesTheMapExtensionWithLog()
  {
    _project.FileName = Path.Combine(_tempFolder, "cave.trizbort");

    MapStatisticsCommands.DefaultLogPath(_project).ShouldBe(Path.Combine(_tempFolder, "cave.log"));
  }

  [Test]
  public void DefaultLogPath_UnsavedMapUsesTrizbortLogInHome()
  {
    Path.GetFileName(MapStatisticsCommands.DefaultLogPath(_project)).ShouldBe("trizbort.log");
  }

  [Test]
  public async Task Export_WritesTheReportAndReportsThePath()
  {
    AddRoom("Hall");
    _project.FileName = Path.Combine(_tempFolder, "cave.trizbort");
    var target = Path.Combine(_tempFolder, "out.log");
    _prompts.PickedPath = target;

    await MapStatisticsCommands.ExportAsync(_session, _dimensions);

    _prompts.SuggestedPath.ShouldBe(Path.Combine(_tempFolder, "cave.log"));
    File.ReadAllText(target).ShouldBe(MapStatisticsReport.Build(_dimensions));
    _prompts.Messages.ShouldBe(new[] { "Log file written|Wrote log to " + target });
  }

  [Test]
  public async Task Export_CancelledPickerWritesNothing()
  {
    AddRoom("Hall");
    _prompts.PickedPath = null;

    await MapStatisticsCommands.ExportAsync(_session, _dimensions);

    _prompts.Messages.ShouldBeEmpty();
  }
}

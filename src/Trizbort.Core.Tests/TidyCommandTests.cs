using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Editor.Commands;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trizbort.Core.Tests;

/// <summary>Edit > Rooms > Tidy Map...: the whole map or the selected rooms, step by step or with the solver.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class TidyCommandTests
{
  private Project _previousProject;
  private EditorSession _session;
  private CommandCatalogue _catalogue;

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    Project.Current = new Project();
    _session = new EditorSession();
    _catalogue = CommandCatalogue.CreateDefault();
  }

  [TearDown]
  public void TearDown()
  {
    _session.Dispose();
    Project.Current = _previousProject;
  }

  /// <summary>Answers Tidy Map's question with <see cref="Answer" /> and records what it was asked.</summary>
  private sealed class TidyPrompts : IEditorPrompts
  {
    public TidyChoice? Answer;
    public TidyChoice Suggested;
    public int SelectedRooms = -1;
    public string ProgressTitle;
    public bool CancelProgress;

    public Task<AutomapSameDirectionResult> ChooseSameDirectionAsync(Room existing, string newRoomName, CancellationToken token = default) =>
      Task.FromResult(AutomapSameDirectionResult.KeepRoom1);

    public Task<RoomDisambiguation> DisambiguateRoomAsync(string transcriptLine, string roomDescription, IReadOnlyList<Room> candidates, CancellationToken token = default) =>
      Task.FromResult(RoomDisambiguation.Choose(candidates[0]));

    public Task<TidyChoice?> ChooseTidyAsync(TidyChoice suggested, int selectedRooms, CancellationToken token = default)
    {
      Suggested = suggested;
      SelectedRooms = selectedRooms;
      return Task.FromResult(Answer);
    }

    public Task<T> RunWithProgressAsync<T>(string title, string message, int expectedMs, System.Func<CancellationToken, T> work, CancellationToken token = default)
    {
      ProgressTitle = title;
      if (!CancelProgress) return Task.Run(() => work(token));
      var cancelled = new CancellationTokenSource();
      cancelled.Cancel();
      // the work notices it was cancelled and the result is dropped
      work(cancelled.Token);
      return Task.FromResult<T>(default);
    }
  }

  private TidyPrompts Prompts(bool selectedOnly, TidyMethod method)
  {
    var prompts = new TidyPrompts { Answer = new TidyChoice(selectedOnly, method) };
    _session.Prompts = prompts;
    return prompts;
  }

  private static Room Room(string name, float column, float row)
  {
    var room = new Room(Project.Current) { Name = name };
    room.Position = new Vector(column * 160, row * 128);
    Project.Current.Elements.Add(room);
    return room;
  }

  private static void Join(Room from, CompassPoint fromSide, Room to, CompassPoint toSide) =>
    Project.Current.Elements.Add(new Connection(Project.Current, new Vertex(from.PortAt(fromSide)), new Vertex(to.PortAt(toSide))));

  private (Room Hall, Room Garden, Room Shed) AMessyMap()
  {
    // north from the Hall to the Garden, east from the Garden to the Shed; drawn all over the place
    var hall = Room("Hall", 0, 0);
    var garden = Room("Garden", 3, 2);
    var shed = Room("Shed", -2, 1);
    Join(hall, CompassPoint.North, garden, CompassPoint.South);
    Join(garden, CompassPoint.East, shed, CompassPoint.West);
    return (hall, garden, shed);
  }

  [Test]
  public void TidyMap_PutsEveryJoinTheWayItPoints_AsOneUndoStep()
  {
    var (hall, garden, shed) = AMessyMap();
    LayoutTidy.NaturalJoins(Project.Current.Elements.ToList()).Natural.ShouldBe(0);
    var before = (hall.Position, garden.Position, shed.Position);

    var tidy = _catalogue.Find("map.tidy");
    tidy.CanExecute(_session).ShouldBeTrue();
    tidy.ExecuteAsync(_session).Wait();

    LayoutTidy.NaturalJoins(Project.Current.Elements.ToList()).ShouldBe((2, 2));
    _session.PerformUndo();
    // (undo puts back copies of the rooms as they were)
    Vector At(string name) => Project.Current.Elements.OfType<Room>().Single(room => room.Name == name).Position;
    (At("Hall"), At("Garden"), At("Shed")).ShouldBe(before);
  }

  [Test]
  public void TidyMapWithSolver_PutsEveryJoinTheWayItPoints_AsOneUndoStep()
  {
    var (hall, garden, shed) = AMessyMap();
    var before = (hall.Position, garden.Position, shed.Position);

    var prompts = Prompts(false, TidyMethod.Thorough);
    _catalogue.Find("map.tidy").ExecuteAsync(_session).Wait();
    prompts.ProgressTitle.ShouldBe("Tidy map");

    LayoutTidy.NaturalJoins(Project.Current.Elements.ToList()).ShouldBe((2, 2));
    _session.PerformUndo();
    Vector At(string name) => Project.Current.Elements.OfType<Room>().Single(room => room.Name == name).Position;
    (At("Hall"), At("Garden"), At("Shed")).ShouldBe(before);
  }

  [Test]
  public void TidySelectedRoomsWithSolver_MovesOnlyTheSelection()
  {
    var (hall, garden, shed) = AMessyMap();
    var hallBefore = hall.Position;

    _session.Selection.Set(new Element[] { garden, shed });
    Prompts(true, TidyMethod.Thorough);
    _catalogue.Find("map.tidy").ExecuteAsync(_session).Wait();

    hall.Position.ShouldBe(hallBefore);
    LayoutTidy.NaturalJoins(Project.Current.Elements.ToList()).ShouldBe((2, 2));
  }

  [Test]
  public void DraggingARoom_MarksItPlacedByHand_AndTheMarkIsSavedWithTheMap()
  {
    var (hall, garden, _) = AMessyMap();
    _session.Selection.Set(new Element[] { garden });
    _session.NudgeSelected(new Vector(32, 0));
    garden.PlacedByHand.ShouldBeTrue();
    hall.PlacedByHand.ShouldBeFalse();

    var fileName = System.IO.Path.GetTempFileName();
    try
    {
      new LegacyMapFileEngine(Project.Current).Save(fileName).ShouldBeTrue();
      System.IO.File.ReadAllText(fileName).ShouldContain("placedByHand");
      Project.Current = new Project();
      new LegacyMapFileEngine(Project.Current).Load(fileName).ShouldBeTrue();
      var rooms = Project.Current.Elements.OfType<Room>().ToList();
      rooms.Single(room => room.Name == "Garden").PlacedByHand.ShouldBeTrue();
      rooms.Single(room => room.Name == "Hall").PlacedByHand.ShouldBeFalse();
    }
    finally
    {
      System.IO.File.Delete(fileName);
    }
  }

  [Test]
  public void TidyMap_ClearsThePlacedByHandMark()
  {
    var (hall, garden, shed) = AMessyMap();
    garden.PlacedByHand = true;
    shed.PlacedByHand = true;

    _catalogue.Find("map.tidy").ExecuteAsync(_session).Wait();

    Project.Current.Elements.OfType<Room>().Any(room => room.PlacedByHand).ShouldBeFalse();
    LayoutTidy.NaturalJoins(Project.Current.Elements.ToList()).ShouldBe((2, 2));
  }

  [Test]
  public void TidySelectedRooms_MovesOnlyTheSelection()
  {
    var (hall, garden, shed) = AMessyMap();
    var hallBefore = hall.Position;

    var tidy = _catalogue.Find("map.tidy");
    _session.Selection.Set(new Element[] { garden, shed });
    var prompts = Prompts(true, TidyMethod.Quick);
    tidy.CanExecute(_session).ShouldBeTrue();
    tidy.ExecuteAsync(_session).Wait();

    prompts.SelectedRooms.ShouldBe(2);
    prompts.ProgressTitle.ShouldBe("Tidy selected rooms");
    hall.Position.ShouldBe(hallBefore);
    LayoutTidy.NaturalJoins(Project.Current.Elements.ToList()).ShouldBe((2, 2));
  }

  [Test]
  public void TidyMap_SuggestsTheSelectedRooms_OnlyWhenMoreThanOneIsSelected()
  {
    var (hall, garden, _) = AMessyMap();
    var prompts = Prompts(false, TidyMethod.Quick);
    prompts.Answer = null;

    _session.Selection.Set(new Element[] { hall });
    _catalogue.Find("map.tidy").ExecuteAsync(_session).Wait();
    prompts.SelectedRooms.ShouldBe(1);
    prompts.Suggested.SelectedOnly.ShouldBeFalse();

    _session.Selection.Set(new Element[] { hall, garden });
    _catalogue.Find("map.tidy").ExecuteAsync(_session).Wait();
    prompts.Suggested.SelectedOnly.ShouldBeTrue();
  }

  [Test]
  public void CancellingTheDialog_OrTheProgress_ChangesNothing()
  {
    var (hall, garden, shed) = AMessyMap();
    var before = (hall.Position, garden.Position, shed.Position);

    var prompts = Prompts(false, TidyMethod.Quick);
    prompts.Answer = null;
    _catalogue.Find("map.tidy").ExecuteAsync(_session).Wait();
    (hall.Position, garden.Position, shed.Position).ShouldBe(before);
    prompts.ProgressTitle.ShouldBeNull();

    foreach (var method in new[] { TidyMethod.Quick, TidyMethod.Thorough })
    {
      prompts = Prompts(false, method);
      prompts.CancelProgress = true;
      _catalogue.Find("map.tidy").ExecuteAsync(_session).Wait();
      (hall.Position, garden.Position, shed.Position).ShouldBe(before);
    }

    _session.Undo.CanUndo.ShouldBeFalse();
  }

  [Test]
  public void WorkingOutTheTidy_DoesntTouchTheMap_UntilItIsApplied()
  {
    var (hall, garden, shed) = AMessyMap();
    var before = (hall.Position, garden.Position, shed.Position);
    var elements = Project.Current.Elements.ToList();

    foreach (var method in new[] { TidyMethod.Quick, TidyMethod.Thorough })
    {
      var plan = LayoutTidy.PlanTidy(elements, elements.OfType<Room>(), method);
      var moves = plan.Run();
      (hall.Position, garden.Position, shed.Position).ShouldBe(before);
      moves.Count.ShouldBeGreaterThan(0);
    }
  }
}

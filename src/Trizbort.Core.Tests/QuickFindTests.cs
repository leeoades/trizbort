using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests;

/// <summary>Tickets 044 and 086: Quick Find search, command logic and prompt wiring.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class QuickFindTests
{
  private Project _previousProject;
  private Project _project;
  private EditorSession _session;
  private FindPrompts _prompts;

  private sealed class FindPrompts : IEditorPrompts
  {
    public string Answer { get; set; }

    public int Calls { get; private set; }

    public IReadOnlyList<string> Suggestions { get; private set; }

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

    public Task<string> QuickFindAsync(IReadOnlyList<string> suggestions, CancellationToken token = default)
    {
      Calls++;
      Suggestions = suggestions;
      return Task.FromResult(Answer);
    }
  }

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _session = new EditorSession();
    _prompts = new FindPrompts();
    _session.Prompts = _prompts;
  }

  [TearDown]
  public void TearDown()
  {
    _session.Dispose();
    var current = Project.Current;
    Project.Current = _previousProject;
    if (current != null && current != _previousProject) current.Dispose();
    if (_project != current) _project.Dispose();
  }

  private Room AddRoom(string name, float x = 0, float y = 0)
  {
    var room = new Room(Project.Current) { Name = name, Position = new Vector(x, y), Size = new Vector(96, 64) };
    Project.Current.Elements.Add(room);
    return room;
  }

  [Test]
  public void Search_MatchesName_Description_Objects_AndSubtitle()
  {
    var byName = AddRoom("Kitchen");
    var byDescription = AddRoom("A");
    byDescription.Descriptions.Add("A cosy kitchenette");
    var byObjects = AddRoom("B");
    byObjects.Objects = "kitchen knife";
    var bySubtitle = AddRoom("C");
    bySubtitle.SubTitle = "Next to the KITCHEN";
    AddRoom("Cellar");

    QuickFind.Search(Project.Current, "kitchen").ShouldBe(new[] { byName, byDescription, byObjects, bySubtitle });
  }

  [Test]
  public void Search_IsCaseInsensitive()
  {
    var room = AddRoom("Grand Hall");

    QuickFind.Search(Project.Current, "gRaNd").ShouldBe(new[] { room });
  }

  [Test]
  public void Search_ListsARoomOnceWhenSeveralFieldsMatch()
  {
    var room = AddRoom("Lamp room");
    room.Objects = "lamp";
    room.SubTitle = "lamp store";

    QuickFind.Search(Project.Current, "lamp").Count.ShouldBe(1);
  }

  [TestCase(null)]
  [TestCase("")]
  [TestCase("   ")]
  public void Search_EmptyTextFindsNothing(string text)
  {
    AddRoom("Kitchen");

    QuickFind.Search(Project.Current, text).ShouldBeEmpty();
  }

  [Test]
  public void Suggestions_AreDistinctAndSorted()
  {
    AddRoom("Zoo");
    AddRoom("apple");
    var room = AddRoom("ZOO");
    room.Objects = "banana";

    QuickFind.Suggestions(Project.Current).ShouldBe(new[] { "apple", "banana", "Zoo" });
  }

  [Test]
  public async Task Run_AsksWithSuggestions_SelectsMatchesAndCentresOnFirst()
  {
    AddRoom("Cellar", 0, 0);
    var first = AddRoom("Hall One", 1000, 500);
    var second = AddRoom("Hall Two", 2000, 900);
    _prompts.Answer = "hall";

    await QuickFind.RunAsync(_session);

    _prompts.Calls.ShouldBe(1);
    _prompts.Suggestions.ShouldContain("Cellar");
    _session.Selection.Rooms.ToList().ShouldBe(new[] { first, second });
    _session.Viewport.Origin.ShouldBe(first.InnerBounds.Center);
  }

  [Test]
  public async Task Run_DoesNotCreateAnUndoStep()
  {
    AddRoom("Hall");
    _prompts.Answer = "hall";
    var canUndo = _session.Undo.CanUndo;

    await QuickFind.RunAsync(_session);

    _session.Undo.CanUndo.ShouldBe(canUndo);
  }

  [TestCase(null)]
  [TestCase("")]
  public async Task Run_CancelledOrEmptyDoesNothing(string answer)
  {
    var room = AddRoom("Hall");
    _session.Selection.Set(new Element[] { room });
    _prompts.Answer = answer;

    await QuickFind.RunAsync(_session);

    _session.Selection.Count.ShouldBe(1);
  }

  [Test]
  public async Task Run_NoMatchClearsSelectionAndPostsStatus()
  {
    var room = AddRoom("Hall");
    _session.Selection.Set(new Element[] { room });
    _prompts.Answer = "dungeon";
    string status = null;
    _session.StatusMessagePosted += message => status = message;

    await QuickFind.RunAsync(_session);

    _session.Selection.Count.ShouldBe(0);
    status.ShouldBe("No rooms match 'dungeon'");
  }
}

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests.Automap;

/// <summary>
///   While automapping, the view keeps the player's room near the middle, also when tidying the map moves it (Lee,
///   15 Nov 2026: the view sometimes stopped with the room stuck at the side).
/// </summary>
[TestFixture]
[Category("Unit")]
public class KeepTheRoomInTheMiddleTests : IsolatedProjectTests
{
  [Test]
  public async Task ANewLayoutThatMovesThePlayersRoom_BringsItBackToTheMiddle()
  {
    using var session = new EditorSession();
    session.Viewport.SetSize(800, 600);
    var engine = new Trizbort.Automap.Automap((_, _) => { }, NonInteractiveEditorPrompts.Instance);
    var canvas = new AutomapCanvas(Project.Current, engine, session);
    var settings = AutomapSettings.Default;
    settings.FileName = Files.File("game.txt");
    settings.AutoTidy = false;
    settings.GuessExits = false;
    File.WriteAllText(settings.FileName, ">look\n\nHall\nA grand hall.\n\n>");
    await engine.StartCl(canvas, settings);
    var hall = Project.Current.Elements.OfType<Room>().Single();
    engine.CurrentRoom.ShouldBe(hall);

    // the new layout puts the hall far off to the right, as a background rethink can
    var plan = LayoutTidy.PlanRethink(Project.Current.Elements.ToList(), new[] { hall }, 10);
    var moves = new System.Collections.Generic.Dictionary<Room, Vector> { [hall] = new Vector(1600, 0) };
    canvas.FinishRethink(plan, moves).ShouldBe(1);

    session.Viewport.TargetOrigin.X.ShouldBe(hall.InnerBounds.Center.X, 1);
    session.Viewport.TargetOrigin.Y.ShouldBe(hall.InnerBounds.Center.Y, 1);
  }
}

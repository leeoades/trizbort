using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests.Editor;

/// <summary>The view glides (eases in and out) to where automap or Current Room moves it, so the move can be followed.</summary>
[TestFixture]
[Category("Unit")]
public class ViewportGlideTests
{
  private static Viewport AViewport(bool glide)
  {
    var viewport = new Viewport { GlideEnabled = glide };
    viewport.SetSize(800, 600);
    viewport.SetView(new Vector(0, 0), 1);
    return viewport;
  }

  [Test]
  public void WithoutAnimation_AGlideJumpsStraightThere()
  {
    var viewport = AViewport(glide: false);
    viewport.GlideTo(new Vector(400, 0));
    viewport.Origin.ShouldBe(new Vector(400, 0));
    viewport.IsGliding.ShouldBeFalse();
  }

  [Test]
  public void AGlide_EasesInAndOut_AndArrives()
  {
    var viewport = AViewport(glide: true);
    var started = 0;
    viewport.GlideStarted += (_, _) => started++;
    viewport.GlideTo(new Vector(400, 0));
    started.ShouldBe(1);
    viewport.Origin.ShouldBe(new Vector(0, 0));
    viewport.TargetOrigin.ShouldBe(new Vector(400, 0));

    // slow at first, then faster, then slowing to a stop
    var steps = new System.Collections.Generic.List<float>();
    var last = 0f;
    while (viewport.AdvanceGlide(16))
    {
      steps.Add(viewport.Origin.X - last);
      last = viewport.Origin.X;
    }

    viewport.Origin.ShouldBe(new Vector(400, 0));
    steps.Count.ShouldBeGreaterThan(10);
    steps[0].ShouldBeLessThan(steps[steps.Count / 2]);
    steps[^1].ShouldBeLessThan(steps[steps.Count / 2]);
  }

  [Test]
  public void PanningDuringAGlide_EndsIt()
  {
    var viewport = AViewport(glide: true);
    viewport.GlideTo(new Vector(400, 0));
    viewport.AdvanceGlide(50);
    viewport.Pan(new Vector(0, 10));
    viewport.IsGliding.ShouldBeFalse();
    viewport.AdvanceGlide(1000).ShouldBeFalse();
  }

  [Test]
  public void BringingARoomIntoView_GlidesFromWhereTheViewIsGoing()
  {
    var session = new EditorSession();
    session.Viewport.GlideEnabled = true;
    session.Viewport.SetSize(800, 600);
    session.Viewport.SetView(new Vector(0, 0), 1);
    var room = new Trizbort.Domain.Elements.Room(session.Project) { Position = new Vector(2000, 0) };
    session.Project.Elements.Add(room);

    session.EnsureVisible(room);

    session.Viewport.IsGliding.ShouldBeTrue();
    while (session.Viewport.AdvanceGlide(16)) { }
    session.Viewport.VisibleWorldBounds.Contains(room.InnerBounds.Center).ShouldBeTrue();
  }
}

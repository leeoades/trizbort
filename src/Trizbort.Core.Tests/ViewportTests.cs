using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests;

[TestFixture]
[NonParallelizable]
public class ViewportTests
{
  private static Viewport NewViewport()
  {
    var viewport = new Viewport();
    viewport.SetSize(800, 600);
    return viewport;
  }

  [Test]
  public void ClientToCanvas_RoundTrips()
  {
    var viewport = NewViewport();
    viewport.SetView(new Vector(120, -40), 2.0f);

    var world = new Vector(33, 77);
    var back = viewport.ClientToCanvas(viewport.CanvasToClient(world));

    back.X.ShouldBe(world.X, 0.001f);
    back.Y.ShouldBe(world.Y, 0.001f);
  }

  [Test]
  public void ZoomBy_KeepsWorldPointUnderCursor()
  {
    var viewport = NewViewport();
    viewport.SetView(new Vector(50, 50), 1.0f);
    var cursor = new Vector(200, 150);
    var before = viewport.ClientToCanvas(cursor);

    viewport.ZoomBy(2.0f, cursor);

    var after = viewport.ClientToCanvas(cursor);
    after.X.ShouldBe(before.X, 0.01f);
    after.Y.ShouldBe(before.Y, 0.01f);
    viewport.ZoomFactor.ShouldBe(2.0f, 0.0001f);
  }

  [Test]
  public void ZoomIn_ThenOut_ReturnsToOriginalZoom()
  {
    var viewport = NewViewport();

    viewport.ZoomIn();
    viewport.ZoomFactor.ShouldBeGreaterThan(1.0f);
    viewport.ZoomOut();

    viewport.ZoomFactor.ShouldBe(1.0f, 0.001f);
  }

  [Test]
  public void Zoom_ClampsTo_0_1_And_10()
  {
    var viewport = NewViewport();

    viewport.SetView(Vector.Zero, 100f);
    viewport.ZoomFactor.ShouldBe(10.0f);

    viewport.SetView(Vector.Zero, 0.001f);
    viewport.ZoomFactor.ShouldBe(0.1f);
  }

  [Test]
  public void ZoomWheel_Invert_ReversesDirection()
  {
    var normal = NewViewport();
    var inverted = NewViewport();
    var point = new Vector(400, 300);

    normal.ZoomWheel(1, point, false, false);
    inverted.ZoomWheel(1, point, false, true);

    normal.ZoomFactor.ShouldBeGreaterThan(1.0f);
    inverted.ZoomFactor.ShouldBeLessThan(1.0f);
  }

  [Test]
  public void ZoomWheel_Micro_Steps_0_01()
  {
    var viewport = NewViewport();

    viewport.ZoomWheel(1, new Vector(400, 300), true, false);
    viewport.ZoomFactor.ShouldBe(1.01f, 0.0001f);

    viewport.ZoomWheel(-1, new Vector(400, 300), true, false);
    viewport.ZoomFactor.ShouldBe(1.0f, 0.0001f);
  }

  [Test]
  public void FitToMap_Empty_ResetsOriginAndZoom()
  {
    var viewport = NewViewport();
    viewport.SetView(new Vector(500, 500), 3.0f);

    viewport.FitToMap(new List<Element>());

    viewport.Origin.ShouldBe(Vector.Zero);
    viewport.ZoomFactor.ShouldBe(1.0f);
  }

  [Test]
  public void FitToMap_NeverZoomsInPast1()
  {
    var viewport = NewViewport();
    var project = new Project();
    var room = new Room(project);

    viewport.FitToMap(new Element[] { room });

    viewport.ZoomFactor.ShouldBeLessThanOrEqualTo(1.0f);
  }

  [Test]
  public void CenterOn_SetsOriginToCentreAndKeepsZoom()
  {
    var viewport = NewViewport();
    viewport.SetView(Vector.Zero, 2.0f);

    viewport.CenterOn(new Rect(100, 200, 40, 60));

    viewport.Origin.X.ShouldBe(120f, 0.001f);
    viewport.Origin.Y.ShouldBe(230f, 0.001f);
    viewport.ZoomFactor.ShouldBe(2.0f);
  }

  [Test]
  public void Changed_RaisedOnPanAndZoom()
  {
    var viewport = NewViewport();
    var raised = 0;
    viewport.Changed += (_, _) => raised++;

    viewport.Pan(new Vector(10, 10));
    raised.ShouldBe(1);

    viewport.ZoomBy(1.5f, new Vector(100, 100));
    raised.ShouldBe(2);
  }
}

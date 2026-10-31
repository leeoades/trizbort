using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Export;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

[TestFixture]
[NonParallelizable]
public class ImageExportPlannerTests
{
  private Project _previousProject;
  private Project _project;
  private bool _previousDocMargins;
  private float _previousDocH;
  private float _previousDocV;

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _previousDocMargins = Settings.DocumentSpecificMargins;
    _previousDocH = Settings.DocHorizontalMargin;
    _previousDocV = Settings.DocVerticalMargin;
    Settings.DocumentSpecificMargins = false;
  }

  [TearDown]
  public void TearDown()
  {
    Settings.DocumentSpecificMargins = _previousDocMargins;
    Settings.DocHorizontalMargin = _previousDocH;
    Settings.DocVerticalMargin = _previousDocV;
    var current = Project.Current;
    Project.Current = _previousProject;
    if (current != null && current != _previousProject) current.Dispose();
    if (_project != current) _project.Dispose();
  }

  private Room AddRoom(float x, float y)
  {
    var room = new Room(_project) { Position = new Vector(x, y), Size = new Vector(100, 100) };
    _project.Elements.Add(room);
    return room;
  }

  private static ApplicationSettings Margins(float h, float v)
  {
    return new ApplicationSettings { SpecifyGenMargins = true, GenHorizontalMargin = (int)h, GenVerticalMargin = (int)v };
  }

  [Test]
  public void EmptyProject_PlanIsEmpty()
  {
    var plan = ImageExportPlanner.Plan(_project, new Selection(), null, new ImageExportOptions(), Margins(10, 10));
    plan.IsEmpty.ShouldBeTrue();
  }

  [Test]
  public void ConnectionOnlyMap_ZeroMargins_FlatExtentStillExportsAtMinimumThickness()
  {
    // I-025 R8; legacy Canvas.cs:314-317 clamps each side to at least 16 rather than rejecting a flat extent.
    Settings.DocumentSpecificMargins = true;
    Settings.DocHorizontalMargin = 0;
    Settings.DocVerticalMargin = 0;
    _project.Elements.Add(new Connection(_project, new Vertex(new Vector(-100, 64)), new Vertex(new Vector(300, 64))));

    var plan = ImageExportPlanner.Plan(_project, new Selection(), null, new ImageExportOptions { Scale = 1.0 }, Margins(10, 10));

    plan.IsEmpty.ShouldBeFalse();
    plan.Width.ShouldBe(400);
    plan.Height.ShouldBe(ImageExportPlanner.MinDimension);
    plan.Scale.ShouldBe(1.0, 0.001);
  }

  [Test]
  public void ExplicitScale_IsUsed()
  {
    AddRoom(0, 0);
    var plan = ImageExportPlanner.Plan(_project, new Selection(), null, new ImageExportOptions { Scale = 2.0 }, Margins(10, 10));
    plan.Scale.ShouldBe(2.0, 0.01);
    plan.Width.ShouldBeGreaterThan(200);
  }

  [Test]
  public void ScaleUnset_SaveAt100_UsesOne()
  {
    AddRoom(0, 0);
    var viewport = new Viewport();
    viewport.SetView(new Vector(0, 0), 2.0f);
    var settings = Margins(10, 10);
    settings.SaveAt100 = true;
    var plan = ImageExportPlanner.Plan(_project, new Selection(), viewport, new ImageExportOptions(), settings);
    plan.Scale.ShouldBe(1.0, 0.01);
  }

  [Test]
  public void ScaleUnset_NotSaveAt100_UsesCurrentZoom()
  {
    AddRoom(0, 0);
    var viewport = new Viewport();
    viewport.SetView(new Vector(0, 0), 2.0f);
    var settings = Margins(10, 10);
    settings.SaveAt100 = false;
    var plan = ImageExportPlanner.Plan(_project, new Selection(), viewport, new ImageExportOptions(), settings);
    plan.Scale.ShouldBe(2.0, 0.01);
  }

  [Test]
  public void HugeMap_IsClampedProportionally()
  {
    AddRoom(0, 0);
    AddRoom(20000, 0);
    var plan = ImageExportPlanner.Plan(_project, new Selection(), null, new ImageExportOptions { Scale = 1.0 }, Margins(10, 10));
    plan.Width.ShouldBeLessThanOrEqualTo(ImageExportPlanner.MaxDimension);
    plan.Height.ShouldBeLessThanOrEqualTo(ImageExportPlanner.MaxDimension);
    plan.Scale.ShouldBeLessThan(1.0);
  }

  [Test]
  public void TinyScale_IsRaisedToMinimumSize()
  {
    AddRoom(0, 0);
    var plan = ImageExportPlanner.Plan(_project, new Selection(), null, new ImageExportOptions { Scale = 0.01 }, Margins(0, 0));
    plan.Width.ShouldBe(ImageExportPlanner.MinDimension);
    plan.Height.ShouldBe(ImageExportPlanner.MinDimension);
  }

  [Test]
  public void SelectionOnly_UsesSelectedBoundsAndElements()
  {
    var a = AddRoom(0, 0);
    AddRoom(1000, 0);
    var selection = new Selection();
    selection.Set(new Element[] { a });

    var whole = ImageExportPlanner.Plan(_project, selection, null, new ImageExportOptions { Scale = 1.0 }, Margins(10, 10));
    var only = ImageExportPlanner.Plan(_project, selection, null, new ImageExportOptions { Scale = 1.0, SelectionOnly = true }, Margins(10, 10));

    only.SelectionOnly.ShouldBeTrue();
    only.Elements.Count.ShouldBe(1);
    only.Width.ShouldBeLessThan(whole.Width);
    only.Message.ShouldBeNull();
  }

  [Test]
  public void SelectionOnly_EmptySelection_FallsBackToWholeMapWithMessage()
  {
    AddRoom(0, 0);
    AddRoom(1000, 0);
    var whole = ImageExportPlanner.Plan(_project, new Selection(), null, new ImageExportOptions { Scale = 1.0 }, Margins(10, 10));
    var plan = ImageExportPlanner.Plan(_project, new Selection(), null, new ImageExportOptions { Scale = 1.0, SelectionOnly = true }, Margins(10, 10));

    plan.Width.ShouldBe(whole.Width);
    plan.SelectionOnly.ShouldBeFalse();
    plan.Elements.ShouldBeNull();
    plan.Message.ShouldBe(ImageExportPlanner.EmptySelectionMessage);
  }

  [Test]
  public void Margins_GeneralMarginsChangeSize()
  {
    AddRoom(0, 0);
    var small = ImageExportPlanner.Plan(_project, new Selection(), null, new ImageExportOptions { Scale = 1.0 }, Margins(10, 10));
    var big = ImageExportPlanner.Plan(_project, new Selection(), null, new ImageExportOptions { Scale = 1.0 }, Margins(60, 40));
    (big.Width - small.Width).ShouldBeInRange(99, 101);
    (big.Height - small.Height).ShouldBeInRange(59, 61);
  }

  [Test]
  public void Margins_DocumentMarginsOverrideGeneralMargins()
  {
    AddRoom(0, 0);
    Settings.DocumentSpecificMargins = true;
    Settings.DocHorizontalMargin = 50;
    Settings.DocVerticalMargin = 20;
    var plan = ImageExportPlanner.Plan(_project, new Selection(), null, new ImageExportOptions { Scale = 1.0 }, Margins(500, 500));
    var bare = ImageExportPlanner.UnionBounds(_project.Elements);
    plan.Bounds.Width.ShouldBe(bare.Width + 100, 0.01f);
    plan.Bounds.Height.ShouldBe(bare.Height + 40, 0.01f);
  }

  [Test]
  public void SelectedSubset_IncludesConnectionBetweenSelectedRooms()
  {
    var a = AddRoom(0, 0);
    var b = AddRoom(300, 0);
    var c = AddRoom(600, 0);
    var ab = new Connection(_project, new Vertex(a.PortAt(CompassPoint.East)), new Vertex(b.PortAt(CompassPoint.West)));
    var bc = new Connection(_project, new Vertex(b.PortAt(CompassPoint.East)), new Vertex(c.PortAt(CompassPoint.West)));
    _project.Elements.Add(ab);
    _project.Elements.Add(bc);
    var selection = new Selection();
    selection.Set(new Element[] { a, b });

    var subset = ImageExportPlanner.SelectedSubset(_project, selection);
    subset.ShouldContain(ab);
    subset.ShouldNotContain(bc);
    subset.ShouldNotContain(c);
  }

  [Test]
  public void DefaultPaddingUsesTheMeasuredLineHeightWhenAvailable()
  {
    try
    {
      ImageExportPlanner.LineHeightMeasurer = font => font.Size * 1.5f;
      var objects = Settings.ObjectFontMetadata?.Size ?? 11f;
      ImageExportPlanner.DefaultPadding().ShouldBe(objects * 1.5f * 4, 0.01f);

      ImageExportPlanner.LineHeightMeasurer = _ => 0f;
      ImageExportPlanner.DefaultPadding().ShouldBe(objects * ImageExportPlanner.LineHeightFactor * 4, 0.01f);
    }
    finally
    {
      ImageExportPlanner.LineHeightMeasurer = null;
    }
  }
}

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public class HandDrawnTests {
  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _previousTheme = MapTheme.Capture("Previous");
    _previousDirty = _previousProject.IsDirty;
    Project.Current = new Project();
    Settings.Reset();
    Settings.HandDrawn = false;
  }

  [TearDown]
  public void TearDown()
  {
    Project.FileWatcher.StopWatcher();
    Project.Current.Dispose();
    Settings.Reset();
    Project.Current = _previousProject;
    _previousTheme.Apply(false);
    _previousProject.IsDirty = _previousDirty;
  }

  private Project _previousProject;
  private MapTheme _previousTheme;
  private bool _previousDirty;

  [Test]
  public void RoomStyle_FollowsMapSettingUnlessOverridden()
  {
    var mapDefault = new Room(Project.Current);
    var handDrawn = new Room(Project.Current) { HandDrawnStyle = HandDrawnStyle.HandDrawn };
    var straight = new Room(Project.Current) { HandDrawnStyle = HandDrawnStyle.Straight };
    mapDefault.HandDrawnStyle.ShouldBe(HandDrawnStyle.MapDefault);

    mapDefault.IsHandDrawn.ShouldBeFalse();
    handDrawn.IsHandDrawn.ShouldBeTrue();
    straight.IsHandDrawn.ShouldBeFalse();

    Settings.HandDrawn = true;
    mapDefault.IsHandDrawn.ShouldBeTrue();
    handDrawn.IsHandDrawn.ShouldBeTrue();
    straight.IsHandDrawn.ShouldBeFalse();
  }

  [Test]
  public void MapSettingAndRoomOverrides_RoundTripThroughMapFile()
  {
    Settings.HandDrawn = true;
    AddRooms(HandDrawnStyle.MapDefault, HandDrawnStyle.HandDrawn, HandDrawnStyle.Straight);

    var loaded = SaveAndReload(xml => xml);

    Settings.HandDrawn.ShouldBeTrue();
    loaded.Elements.OfType<Room>().OrderBy(room => room.Name).Select(room => room.HandDrawnStyle)
          .ShouldBe(new[] { HandDrawnStyle.MapDefault, HandDrawnStyle.HandDrawn, HandDrawnStyle.Straight });
  }

  [Test]
  public void LegacyMaps_LoadHandDrawnRoomsAsOverrides_AndTheRestAsMapDefault()
  {
    AddRooms(HandDrawnStyle.HandDrawn, HandDrawnStyle.Straight);

    var loaded = SaveAndReload(xml => {
      xml = Regex.Replace(xml, @"\s*handDrawnStyle=""[^""]*""", "");
      return Regex.Replace(xml, @"\s*<handDrawn>[^<]*</handDrawn>", "");
    });

    Settings.HandDrawn.ShouldBeFalse();
    loaded.Elements.OfType<Room>().OrderBy(room => room.Name).Select(room => room.HandDrawnStyle)
          .ShouldBe(new[] { HandDrawnStyle.HandDrawn, HandDrawnStyle.MapDefault });
  }

  [Test]
  public void Theme_CapturesAndAppliesHandDrawn_AndOlderThemesWithoutItStillLoad()
  {
    Settings.HandDrawn = true;
    var theme = MapTheme.Capture("Sketchy");
    theme.HandDrawn.ShouldBeTrue();

    var json = JObject.Parse(JsonConvert.SerializeObject(theme));
    json.Remove("HandDrawn");
    var fileName = Path.GetTempFileName();
    try {
      File.WriteAllText(fileName, json.ToString());
      var older = MapTheme.Load(fileName);
      older.HandDrawn.ShouldBeFalse();
      older.Apply(false);
      Settings.HandDrawn.ShouldBeFalse();
    }
    finally {
      File.Delete(fileName);
    }

    theme.Apply(false);
    Settings.HandDrawn.ShouldBeTrue();
  }

  [Test]
  public void RoomOverride_CountsAsIndividualStyle_AndIsClearedByReplace()
  {
    var room = new Room(Project.Current) { HandDrawnStyle = HandDrawnStyle.Straight };
    Project.Current.Elements.Add(room);
    MapTheme.HasIndividualStyles(Project.Current).ShouldBeTrue();

    MapTheme.BuiltInThemes().Single(theme => theme.Name == "Sketch").Apply(true);

    room.HandDrawnStyle.ShouldBe(HandDrawnStyle.MapDefault);
    room.IsHandDrawn.ShouldBeTrue();
  }

  [TestCase(8f)]
  [TestCase(20f)]
  [TestCase(64f)]
  [TestCase(400f)]
  public void SketchedLine_KeepsEndpoints_AndStaysCloseToTheStraightLine(float length)
  {
    var start = new PointF(10, 20);
    var end = new PointF(10 + length, 20);
    var points = Sketch.Line(start, end, Sketch.Seeded(42));

    points.First().ShouldBe(start);
    points.Last().ShouldBe(end);
    var maxDeviation = points.Max(point => Math.Abs(point.Y - 20));
    maxDeviation.ShouldBeLessThanOrEqualTo(Math.Max(0.6f, Math.Min(3.5f, length * 0.035f)));
    points.Zip(points.Skip(1), (a, b) => b.X - a.X).ShouldAllBe(step => step > 0 && step <= 4);
  }

  [Test]
  public void SketchedShapes_AreDeterministic_AndStayNearTheOutline()
  {
    var rect = new RectangleF(0, 0, 96, 64);
    var ellipse = Sketch.ClosedCurve(Sketch.Ellipse(rect), Sketch.Seeded(7));
    Sketch.ClosedCurve(Sketch.Ellipse(rect), Sketch.Seeded(7)).ShouldBe(ellipse);

    foreach (var point in ellipse) {
      var dx = (point.X - 48) / 48;
      var dy = (point.Y - 32) / 32;
      var radial = Math.Sqrt(dx * dx + dy * dy);
      radial.ShouldBeInRange(0.92, 1.08);
    }

    var rounded = Sketch.ClosedCurve(Sketch.RoundedRectangle(rect, 12, 12, 12, 12), Sketch.Seeded(7));
    rounded.ShouldAllBe(point => point.X > -3 && point.X < 99 && point.Y > -3 && point.Y < 67);
  }

  private void AddRooms(params HandDrawnStyle[] styles)
  {
    for (var index = 0; index < styles.Length; index++)
      Project.Current.Elements.Add(
        new Room(Project.Current) {
          Name = $"Room {index}", Position = new Vector(index * 200, 0), HandDrawnStyle = styles[index]
        });
  }

  private Project SaveAndReload(Func<string, string> transform)
  {
    var fileName = Path.GetTempFileName();
    try {
      new LegacyMapFileEngine(Project.Current).Save(fileName).ShouldBeTrue();
      File.WriteAllText(fileName, transform(File.ReadAllText(fileName)));
      Settings.Reset();
      Settings.HandDrawn = !Settings.HandDrawn;
      Project.Current.Dispose();
      Project.Current = new Project();
      new LegacyMapFileEngine(Project.Current).Load(fileName).ShouldBeTrue();
      return Project.Current;
    }
    finally {
      File.Delete(fileName);
    }
  }
}
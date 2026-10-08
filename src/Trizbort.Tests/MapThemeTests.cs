using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using Trizbort.UI;
using Region = Trizbort.Domain.Misc.Region;

namespace Trizbort.Tests {
  [TestFixture]
  [Apartment(ApartmentState.STA)]
  [NonParallelizable]
  public class MapThemeTests {
    private Project previousProject;
    private MapTheme previousTheme;
    private bool previousDirty;
    private (string Name, bool Snap, float SnapDistance, float Handle, float RoomDistance,
      Keys Creation, Keys Unexplored, bool Start, bool End) previousInteraction;

    [SetUp]
    public void SetUp() {
      previousProject = Project.Current;
      previousTheme = MapTheme.Capture("Previous");
      previousDirty = previousProject.IsDirty;
      previousInteraction = (Settings.DefaultRoomName, Settings.SnapToGrid, Settings.SnapToElementSize,
        Settings.HandleSize, Settings.PreferredDistanceBetweenRooms, Settings.KeypadNavigationCreationModifier,
        Settings.KeypadNavigationUnexploredModifier, Settings.StartRoomLoaded, Settings.EndRoomLoaded);
      Project.Current = new Project();
      Settings.Reset();
      Settings.DefaultRoomShape = RoomShape.SquareCorners;
    }

    [TearDown]
    public void TearDown() {
      Project.Current.Dispose();
      Settings.Reset();
      Project.Current = previousProject;
      previousTheme.Apply(false);
      Settings.DefaultRoomName = previousInteraction.Name;
      Settings.SnapToGrid = previousInteraction.Snap;
      Settings.SnapToElementSize = previousInteraction.SnapDistance;
      Settings.HandleSize = previousInteraction.Handle;
      Settings.PreferredDistanceBetweenRooms = previousInteraction.RoomDistance;
      Settings.KeypadNavigationCreationModifier = previousInteraction.Creation;
      Settings.KeypadNavigationUnexploredModifier = previousInteraction.Unexplored;
      Settings.StartRoomLoaded = previousInteraction.Start;
      Settings.EndRoomLoaded = previousInteraction.End;
      previousProject.IsDirty = previousDirty;
    }

    [Test]
    public void ExportImport_RoundTripsEveryVisualSetting_WithoutMapContentOrInteractionSettings() {
      var source = MapTheme.BuiltInThemes()[1];
      source.RoomFont.Style = FontStyle.Bold | FontStyle.Italic;
      source.ObjectFont.Size = 17;
      source.Regions.Add(new ThemeRegion { Name = "Forest & lake", Fill = "#ABCDEF", Text = "#123456" });
      source.LineWidth = 3.5f;
      source.ArrowSize = 21;
      source.TextOffset = 11;
      source.DarknessStripeSize = 17;
      source.ObjectListOffset = 6;
      source.ConnectionStalkLength = 27;
      source.GridSize = 48;
      source.GridVisible = false;
      source.ShowOrigin = false;
      source.DocumentSpecificMargins = true;
      source.HorizontalMargin = 12;
      source.VerticalMargin = 15;
      source.WrapTextAtDashes = false;
      source.Apply(false);
      var room = new Room(Project.Current) { Name = "Private room name", Objects = "private object" };
      Project.Current.Elements.Add(room);
      Project.Current.Title = "Private title";
      var captured = MapTheme.Capture("Shared theme");
      var fileName = Path.GetTempFileName();
      try {
        captured.Save(fileName);
        var json = File.ReadAllText(fileName);
        json.ShouldNotContain("Private");
        json.ShouldNotContain("private object");
        json.ShouldNotContain("SnapToGrid");
        json.ShouldNotContain("DefaultRoomName");
        MapTheme.BuiltInThemes()[0].Apply(false);
        var imported = MapTheme.Load(fileName);
        imported.Apply(false);
        JsonConvert.SerializeObject(MapTheme.Capture(imported.Name)).ShouldBe(JsonConvert.SerializeObject(captured));
        Settings.RoomNameFont.Unit.ShouldBe(GraphicsUnit.World);
      } finally {
        File.Delete(fileName);
      }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Apply_PreservesContentGeometryAndGameProperties_AndHonoursStyleChoice(bool replaceStyles) {
      var project = Project.Current;
      project.Title = "Title";
      project.Author = "Author";
      project.Description = "Description";
      project.History = "History";
      project.FileName = "original.trizbort";
      project.MustHaveDescription = true;
      Settings.DefaultRoomName = "New room";
      Settings.SnapToGrid = false;
      Settings.SnapToElementSize = 19;
      Settings.HandleSize = 7;
      Settings.PreferredDistanceBetweenRooms = 100;
      Settings.KeypadNavigationCreationModifier = Keys.Shift;
      Settings.Regions.Add(new Region { RegionName = "Map-only", RColor = Color.Green, TextColor = Color.Purple });
      Settings.Regions.Add(new Region { RegionName = "Forest", RColor = Color.Red });
      var room = new Room(project) {
        Name = "Library", SubTitle = "Upstairs", Objects = "book", Region = "Map-only",
        Position = new Vector(200, 300), Size = new Vector(160, 120), Shape = RoomShape.Ellipse,
        RoomFillColor = Color.Red, RoomBorderColor = Color.Blue, RoomNameColor = Color.Yellow,
        RoomSubtitleColor = Color.Green, RoomObjectTextColor = Color.Purple, SecondFillColor = Color.Gray,
        BorderStyle = BorderDashStyle.Dot, HandDrawnStyle = HandDrawnStyle.HandDrawn,
        IsDark = true, IsStartRoom = true, ObjectsPosition = CompassPoint.East
      };
      room.AddDescription("Room description");
      project.Elements.Add(room);
      var label = new MapLabel(project) {
        Text = "Notes", Position = new Vector(500, 600), Shape = RoomShape.Ellipse,
        HasBackground = false, BorderStyle = BorderDashStyle.None,
        TextColor = Color.Red, BorderColor = Color.Blue, BackgroundColor = Color.Green
      };
      project.Elements.Add(label);
      var connection = new Connection(project, new Vertex(room.PortAt(CompassPoint.East)), new Vertex(label.PortAt(CompassPoint.West))) {
        ConnectionColor = Color.Red, Style = ConnectionStyle.Dashed, Flow = ConnectionFlow.OneWay
      };
      connection.SetCurveWaypoint(CurveWaypoint.Middle, new Vector(400, 400));
      project.Elements.Add(connection);
      var theme = MapTheme.BuiltInThemes()[2];
      theme.Regions.Add(new ThemeRegion { Name = "FOREST", Fill = "#123456", Text = "#ABCDEF" });
      project.IsDirty = false;

      theme.Apply(replaceStyles);

      project.IsDirty.ShouldBeTrue();
      project.Elements.Count.ShouldBe(3);
      project.Title.ShouldBe("Title");
      project.Author.ShouldBe("Author");
      project.Description.ShouldBe("Description");
      project.History.ShouldBe("History");
      project.FileName.ShouldBe("original.trizbort");
      project.MustHaveDescription.ShouldBeTrue();
      Settings.DefaultRoomName.ShouldBe("New room");
      Settings.SnapToGrid.ShouldBeFalse();
      Settings.SnapToElementSize.ShouldBe(19);
      Settings.HandleSize.ShouldBe(7);
      Settings.PreferredDistanceBetweenRooms.ShouldBe(100);
      Settings.KeypadNavigationCreationModifier.ShouldBe(Keys.Shift);
      room.Name.ShouldBe("Library");
      room.SubTitle.ShouldBe("Upstairs");
      room.Objects.ShouldBe("book");
      room.PrimaryDescription.ShouldBe("Room description");
      room.Position.ShouldBe(new Vector(200, 300));
      room.Size.ShouldBe(new Vector(160, 120));
      room.Region.ShouldBe("Map-only");
      room.IsDark.ShouldBeTrue();
      room.IsStartRoom.ShouldBeTrue();
      room.ObjectsPosition.ShouldBe(CompassPoint.East);
      Settings.Regions.Single(region => region.RegionName == "Map-only").RColor.ShouldBe(Color.Green);
      Settings.Regions.Single(region => region.RegionName == "Forest").RColor.ToArgb().ShouldBe(ColorTranslator.FromHtml("#123456").ToArgb());
      label.Text.ShouldBe("Notes");
      label.Position.ShouldBe(new Vector(500, 600));
      label.Shape.ShouldBe(RoomShape.Ellipse);
      label.HasBackground.ShouldBeFalse();
      label.BorderStyle.ShouldBe(BorderDashStyle.None);
      connection.Style.ShouldBe(ConnectionStyle.Dashed);
      connection.Flow.ShouldBe(ConnectionFlow.OneWay);
      connection.VertexList[0].Port.Owner.ShouldBeSameAs(room);
      connection.VertexList[1].Port.Owner.ShouldBeSameAs(label);
      connection.GetCurveWaypoint(CurveWaypoint.Middle).ShouldBe(new Vector(400, 400));
      room.RoomFillColor.ShouldBe(replaceStyles ? Color.Transparent : Color.Red);
      room.RoomBorderColor.ShouldBe(replaceStyles ? Color.Transparent : Color.Blue);
      room.RoomNameColor.ShouldBe(replaceStyles ? Color.Transparent : Color.Yellow);
      room.RoomSubtitleColor.ShouldBe(replaceStyles ? Color.Transparent : Color.Green);
      room.RoomObjectTextColor.ShouldBe(replaceStyles ? Color.Transparent : Color.Purple);
      room.SecondFillColor.ShouldBe(replaceStyles ? Color.Transparent : Color.Gray);
      room.Shape.ShouldBe(replaceStyles ? theme.DefaultRoomShape : RoomShape.Ellipse);
      room.BorderStyle.ShouldBe(replaceStyles ? BorderDashStyle.Solid : BorderDashStyle.Dot);
      (room.HandDrawnStyle == HandDrawnStyle.HandDrawn).ShouldBe(!replaceStyles);
      connection.ConnectionColor.ShouldBe(replaceStyles ? Color.Transparent : Color.Red);
      label.TextColor.ToArgb().ShouldBe((replaceStyles ? ColorTranslator.FromHtml(theme.Colors["lineText"]) : Color.Red).ToArgb());
      new Room(project).Shape.ShouldBe(theme.DefaultRoomShape);
    }

    [Test]
    public void BuiltIns_AreIndependentOfCurrentSettings_AndCanBeAppliedAndExported() {
      var before = JsonConvert.SerializeObject(MapTheme.BuiltInThemes());
      Settings.GridSize = 80;
      Settings.RoomNameFont = new Font("Arial", 20);
      JsonConvert.SerializeObject(MapTheme.BuiltInThemes()).ShouldBe(before);
      foreach (var theme in MapTheme.BuiltInThemes()) {
        theme.Apply(true);
        var captured = MapTheme.Capture(theme.Name);
        JsonConvert.SerializeObject(captured).ShouldBe(JsonConvert.SerializeObject(theme));
      }
    }

    [TestCase("color")]
    [TestCase("missingColor")]
    [TestCase("grid")]
    [TestCase("nan")]
    [TestCase("font")]
    [TestCase("shape")]
    [TestCase("regions")]
    [TestCase("regionCase")]
    [TestCase("version")]
    public void InvalidTheme_IsRejectedBeforeAnyMapChanges(string invalidValue) {
      var theme = MapTheme.BuiltInThemes()[2];
      switch (invalidValue) {
        case "color": theme.Colors["grid"] = "not-a-colour"; break;
        case "missingColor": theme.Colors.Remove("border"); break;
        case "grid": theme.GridSize = 0; break;
        case "nan": theme.ArrowSize = float.NaN; break;
        case "font": theme.RoomFont.Size = 1000; break;
        case "shape": theme.DefaultRoomShape = (RoomShape)123; break;
        case "regions": theme.Regions.Add(theme.Regions[0]); break;
        case "regionCase": theme.Regions.Add(new ThemeRegion { Name = "noregion", Fill = "White", Text = "Blue" }); break;
        case "version": theme.Version = 2; break;
      }
      var before = JsonConvert.SerializeObject(MapTheme.Capture("Before"));
      Project.Current.IsDirty = false;
      Should.Throw<InvalidDataException>(() => theme.Apply(true));
      JsonConvert.SerializeObject(MapTheme.Capture("Before")).ShouldBe(before);
      Project.Current.IsDirty.ShouldBeFalse();
    }

    [Test]
    public void UnavailableFont_DoesNotPartiallyApplyTheme() {
      var theme = MapTheme.BuiltInThemes()[2];
      theme.SubtitleFont.Name = "Trizbort missing font " + Guid.NewGuid();
      var before = JsonConvert.SerializeObject(MapTheme.Capture("Before"));
      Project.Current.IsDirty = false;
      Should.Throw<InvalidDataException>(() => theme.Apply(false));
      JsonConvert.SerializeObject(MapTheme.Capture("Before")).ShouldBe(before);
      Project.Current.IsDirty.ShouldBeFalse();
    }

    [Test]
    public void Import_RejectsMalformedAndIncompleteFiles() {
      var fileName = Path.GetTempFileName();
      try {
        File.WriteAllText(fileName, "not json");
        Should.Throw<JsonException>(() => MapTheme.Load(fileName));
        File.WriteAllText(fileName, "{}");
        Should.Throw<JsonException>(() => MapTheme.Load(fileName));
        File.WriteAllText(fileName, "null");
        Should.Throw<InvalidDataException>(() => MapTheme.Load(fileName));
        var json = JObject.FromObject(MapTheme.BuiltInThemes()[0]);
        json.Remove("GridVisible");
        File.WriteAllText(fileName, json.ToString());
        Should.Throw<JsonException>(() => MapTheme.Load(fileName));
      } finally {
        File.Delete(fileName);
      }
    }

    [Test]
    public void AppliedTheme_IsSavedInOrdinaryMapSettings_AndReloadsWithoutThemeFile() {
      var project = Project.Current;
      var room = new Room(project) { Name = "Library", Position = new Vector(210, 310), RoomFillColor = Color.Red };
      project.Elements.Add(room);
      MapTheme.BuiltInThemes()[2].Apply(true);
      var before = JsonConvert.SerializeObject(MapTheme.Capture("Dark"));
      var fileName = Path.GetTempFileName();
      var loaded = new Project();
      try {
        new LegacyMapFileEngine(project).Save(fileName).ShouldBeTrue();
        MapTheme.BuiltInThemes()[0].Apply(false);
        Project.Current = loaded;
        new LegacyMapFileEngine(loaded).Load(fileName).ShouldBeTrue();
        JsonConvert.SerializeObject(MapTheme.Capture("Dark")).ShouldBe(before);
        var loadedRoom = loaded.Elements.OfType<Room>().Single();
        loadedRoom.Name.ShouldBe("Library");
        loadedRoom.Position.ShouldBe(room.Position);
        loadedRoom.Shape.ShouldBe(room.Shape);
        loadedRoom.RoomFillColor.ShouldBe(Color.Transparent);
      } finally {
        Project.FileWatcher.StopWatcher();
        Project.Current = project;
        loaded.Dispose();
        File.Delete(fileName);
      }
    }

    [Test]
    public void DefaultElementsAndGameProperties_DoNotCountAsIndividualStyles() {
      var project = Project.Current;
      MapTheme.HasIndividualStyles(project).ShouldBeFalse();
      var room = new Room(project) {
        HandDrawnStyle = HandDrawnStyle.MapDefault, IsDark = true, IsStartRoom = true, Region = "Forest",
        ObjectsPosition = CompassPoint.East
      };
      project.Elements.Add(room);
      var label = new MapLabel(project) { Shape = RoomShape.Ellipse, HasBackground = true, BorderStyle = BorderDashStyle.Dot };
      project.Elements.Add(label);
      project.Elements.Add(new Connection(project, new Vertex(room.PortAt(CompassPoint.East)), new Vertex(label.PortAt(CompassPoint.West))) {
        Style = ConnectionStyle.Dashed, Flow = ConnectionFlow.OneWay
      });
      MapTheme.HasIndividualStyles(project).ShouldBeFalse();
      MapTheme.BuiltInThemes()[2].Apply(true);
      MapTheme.HasIndividualStyles(project).ShouldBeFalse();
      MapTheme.BuiltInThemes()[1].Apply(true);
      MapTheme.HasIndividualStyles(project).ShouldBeFalse();
    }

    [TestCase("fill")]
    [TestCase("borderColor")]
    [TestCase("nameColor")]
    [TestCase("subtitleColor")]
    [TestCase("objectColor")]
    [TestCase("secondFill")]
    [TestCase("shape")]
    [TestCase("borderStyle")]
    [TestCase("handDrawn")]
    [TestCase("corners")]
    [TestCase("unequalCorners")]
    [TestCase("connectionColor")]
    [TestCase("labelText")]
    [TestCase("labelBorder")]
    [TestCase("labelBackground")]
    public void IndividualOverrides_RequireAStyleChoice(string style) {
      var project = Project.Current;
      var room = new Room(project) { HandDrawnStyle = HandDrawnStyle.MapDefault };
      project.Elements.Add(room);
      var label = new MapLabel(project);
      project.Elements.Add(label);
      var connection = new Connection(project);
      project.Elements.Add(connection);
      switch (style) {
        case "fill": room.RoomFillColor = Color.Red; break;
        case "borderColor": room.RoomBorderColor = Color.Red; break;
        case "nameColor": room.RoomNameColor = Color.Red; break;
        case "subtitleColor": room.RoomSubtitleColor = Color.Red; break;
        case "objectColor": room.RoomObjectTextColor = Color.Red; break;
        case "secondFill": room.SecondFillColor = Color.Red; break;
        case "shape": room.Shape = RoomShape.Ellipse; break;
        case "borderStyle": room.BorderStyle = BorderDashStyle.None; break;
        case "handDrawn": room.HandDrawnStyle = HandDrawnStyle.HandDrawn; break;
        case "corners": room.Corners.TopLeft = 20; break;
        case "unequalCorners": room.AllCornersEqual = false; break;
        case "connectionColor": connection.ConnectionColor = Color.Red; break;
        case "labelText": label.TextColor = Color.Red; break;
        case "labelBorder": label.BorderColor = Color.Red; break;
        case "labelBackground": label.BackgroundColor = Color.Red; break;
      }
      MapTheme.HasIndividualStyles(project).ShouldBeTrue();
    }

    private static Room AddRoom(Project project, string region = null, Color? fill = null, Color? name = null, RoomShape shape = RoomShape.Octagonal) {
      var room = new Room(project) { HandDrawnStyle = HandDrawnStyle.MapDefault, Shape = shape, Region = region };
      if (fill.HasValue) room.RoomFillColor = fill.Value;
      if (name.HasValue) room.RoomNameColor = name.Value;
      project.Elements.Add(room);
      return room;
    }

    [Test]
    public void InferRoomStyle_PromotesStrongMajority_AndRemovesOnlyRedundantOverrides() {
      var project = Project.Current;
      var cave = ColorTranslator.FromHtml("#400000");
      var grey = ColorTranslator.FromHtml("#808080");
      var styled = Enumerable.Range(0, 8).Select(_ => AddRoom(project, fill: cave, name: grey)).ToList();
      var exception = AddRoom(project, fill: Color.Green, name: grey, shape: RoomShape.Ellipse);
      var unstyled = AddRoom(project);
      unstyled.RoomBorderColor = Color.MidnightBlue;
      project.IsDirty = false;

      var inference = RoomStyleInference.Analyze(project);
      inference.InferredShape.ShouldBe(RoomShape.Octagonal);
      inference.Changes.Count.ShouldBe(3);
      inference.Apply();

      Settings.DefaultRoomShape.ShouldBe(RoomShape.Octagonal);
      var noRegion = Settings.Regions.Single(region => region.RegionName == Region.DefaultRegion);
      noRegion.RColor.ToArgb().ShouldBe(cave.ToArgb());
      noRegion.TextColor.ToArgb().ShouldBe(grey.ToArgb());
      styled.ShouldAllBe(room => room.RoomFillColor == Color.Transparent && room.RoomNameColor == Color.Transparent && room.Shape == RoomShape.Octagonal);
      exception.RoomFillColor.ShouldBe(Color.Green);
      exception.RoomNameColor.ShouldBe(Color.Transparent);
      exception.Shape.ShouldBe(RoomShape.Ellipse);
      unstyled.RoomBorderColor.ShouldBe(Color.Transparent);
      project.IsDirty.ShouldBeTrue();
      RoomStyleInference.Analyze(project).HasChanges.ShouldBeFalse();

      var exported = MapTheme.Capture("Cave");
      exported.DefaultRoomShape.ShouldBe(RoomShape.Octagonal);
      exported.Regions.Single(region => region.Name == Region.DefaultRegion).Fill.ShouldBe("#400000");
    }

    [Test]
    public void InferRoomStyle_UsesEachRegionsOwnMajority() {
      var project = Project.Current;
      Settings.Regions.Add(new Region { RegionName = "Forest", RColor = Color.White, TextColor = Color.Black });
      var forest = Enumerable.Range(0, 3).Select(_ => AddRoom(project, "forest", Color.Green)).ToList();
      var plain = Enumerable.Range(0, 3).Select(_ => AddRoom(project, fill: Color.Blue)).ToList();

      RoomStyleInference.Analyze(project).Apply();

      Settings.Regions.Single(region => region.RegionName == "Forest").RColor.ToArgb().ShouldBe(Color.Green.ToArgb());
      Settings.Regions.Single(region => region.RegionName == Region.DefaultRegion).RColor.ToArgb().ShouldBe(Color.Blue.ToArgb());
      forest.Concat(plain).ShouldAllBe(room => room.RoomFillColor == Color.Transparent);
    }

    [Test]
    public void InferRoomStyle_WithoutStrongMajority_ChangesNothing() {
      var project = Project.Current;
      var defaultFill = Settings.Regions.Single(region => region.RegionName == Region.DefaultRegion).RColor;
      AddRoom(project, fill: Color.Red, shape: RoomShape.Ellipse);
      AddRoom(project, fill: Color.Red, shape: RoomShape.Ellipse);
      AddRoom(project, fill: Color.Blue, shape: RoomShape.Octagonal);
      AddRoom(project, fill: Color.Green, shape: RoomShape.RoundedCorners);
      project.IsDirty = false;

      var inference = RoomStyleInference.Analyze(project);
      inference.HasChanges.ShouldBeFalse();
      inference.Apply();

      Settings.DefaultRoomShape.ShouldBe(RoomShape.SquareCorners);
      Settings.Regions.Single(region => region.RegionName == Region.DefaultRegion).RColor.ShouldBe(defaultFill);
      project.Elements.OfType<Room>().Count(room => room.RoomFillColor == Color.Red).ShouldBe(2);
      project.IsDirty.ShouldBeFalse();
    }

    [Test]
    public void ThemesMenu_IsNextToMapSettings_WithPresetsAndImportExport() {
      var previousForm = TrizbortApplication.MainForm;
      try {
        using (var form = new MainForm()) {
          var tools = form.MainMenuStrip.Items.OfType<ToolStripMenuItem>().Single(item => item.Text == "&Tools");
          var items = tools.DropDownItems.OfType<ToolStripMenuItem>().ToList();
          var settingsIndex = items.FindIndex(item => item.Text == "Map &Settings...");
          var menu = items[settingsIndex + 1];
          menu.Text.ShouldBe("&Themes");
          menu.DropDownItems.OfType<ToolStripMenuItem>().Select(item => item.Text).ShouldBe(new[] {
            "Classic", "Parchment", "Dark", "High contrast", "Sketch", "&Import theme...", "&Export current theme...",
            "Infer default room style from &rooms..."
          });
          var room = new Room(Project.Current) { HandDrawnStyle = HandDrawnStyle.MapDefault };
          Project.Current.Elements.Add(room);
          menu.DropDownItems.OfType<ToolStripMenuItem>().Single(item => item.Text == "Dark").PerformClick();
          room.Shape.ShouldBe(MapTheme.BuiltInThemes()[2].DefaultRoomShape);
          MapTheme.HasIndividualStyles(Project.Current).ShouldBeFalse();
          form.Canvas.BackColor.ToArgb().ShouldBe(ColorTranslator.FromHtml(MapTheme.BuiltInThemes()[2].Colors["canvas"]).ToArgb());
          menu.DropDownItems.OfType<ToolStripMenuItem>().Single(item => item.Text == "Classic").PerformClick();
          room.Shape.ShouldBe(RoomShape.SquareCorners);
          MapTheme.HasIndividualStyles(Project.Current).ShouldBeFalse();
        }
      } finally {
        TrizbortApplication.MainForm = previousForm;
      }
    }
  }
}

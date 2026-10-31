using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Enums;
using Trizbort.Editor.Commands;
using Trizbort.Editor.Input;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

/// <summary>
///   Ticket 067: the catalogue must match the approved shortcut map (agent-docs/plans/avalonia-parity/shortcut-map.md,
///   section 2 and the macOS variants of section 4, with Lee's decisions of section 5). The expected table below is
///   the map; counts are never asserted, so commands added by other tickets do not break these tests. A row whose
///   command is not registered yet is <c>Pending</c>: it is checked as soon as a command with that id exists.
/// </summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class ShortcutMapConformanceTests
{
  private static readonly string[] _directions = { "north", "south", "east", "west", "northWest", "southWest", "northEast", "southEast" };

  private static readonly List<Row> _rows = BuildRows();

  private ModifierKeys _previousCreation;
  private ModifierKeys _previousUnexplored;

  /// <summary>
  ///   One command of the map. <see cref="Gestures" /> are the Windows/Linux gestures, <see cref="MacGestures" /> the
  ///   macOS ones (null = same). <see cref="HostDependent" /> rows are built from the host OS (debug keys, F11), so
  ///   they are only checked for the OS the tests run on.
  /// </summary>
  private sealed record Row(
    string Id, CommandScope Scope, string[] Gestures, string[] MacGestures = null, bool HostDependent = false,
    bool Pending = false)
  {
    public string[] GesturesFor(bool macOS)
    {
      return macOS && MacGestures != null ? MacGestures : Gestures;
    }

    public override string ToString()
    {
      return Id;
    }
  }

  private static Row Window(string id, params string[] gestures)
  {
    return new Row(id, CommandScope.Window, gestures);
  }

  private static Row Canvas(string id, params string[] gestures)
  {
    return new Row(id, CommandScope.Canvas, gestures);
  }

  private static List<Row> BuildRows()
  {
    var rows = new List<Row>
    {
      // History
      Window("edit.undo", "Primary+Z"),
      Window("edit.redo", "Primary+Y", "Primary+Shift+Z") with { MacGestures = new[] { "Primary+Shift+Z" } },

      // Clipboard and delete
      Window("edit.copy", "Primary+C"),
      Window("edit.cut", "Primary+X"),
      Window("edit.paste", "Primary+V"),
      Window("edit.delete", "Delete"),
      // Map: Backspace, macOS also Cmd+Backspace. The catalogue binds Primary+Backspace on every OS (canvas only, harmless).
      Canvas("edit.deleteBackspace", "Backspace", "Primary+Backspace"),
      Window("edit.selectAll", "Primary+A"),
      Canvas("edit.selectNone", "Esc"),
      Canvas("select.regions", "Primary+Shift+A"),
      Canvas("select.startRoom", "0", "NumPad0"),
      Canvas("edit.enter", "Enter"),
      Window("edit.properties", "Alt+Enter"),

      // Add / rename
      Canvas("project.addRoom", "R"),
      Canvas("project.addLabel", "L"),
      Canvas("room.rename", "F2"),
      Canvas("room.changeRegion", "Shift+F2"),

      // Lighting, start/end, shapes
      Canvas("room.toggleDark", "K"),
      Window("room.forceDark", "Primary+K"),
      Window("room.forceLit", "Primary+Shift+K"),
      Window("room.setStart", "Primary+F5") with { MacGestures = new[] { "Primary+Shift+F5" } },
      Canvas("room.setEnd", "Shift+F5"),
      Window("room.shape.square", "Primary+H") with { MacGestures = new[] { "Ctrl+H" } },
      Window("room.shape.ellipse", "Primary+E"),
      Window("room.shape.rounded", "Primary+R"),
      Window("room.shape.octagonal", "Primary+8"),

      // Swaps and joins
      Canvas("room.join", "J"),
      Canvas("room.swap.objects", "W"),
      Window("room.swap.names", "Primary+W") with { MacGestures = new[] { "Ctrl+W" } },
      Canvas("room.swap.formats", "Shift+W"),
      Canvas("room.swap.regions", "Alt+W"),

      // Connections
      Canvas("connection.plain", "P"),
      Canvas("connection.toggleDotted", "T"),
      Canvas("connection.toggleOneWay", "A"),
      Canvas("connection.label.up", "U"),
      Canvas("connection.label.down", "D"),
      Canvas("connection.label.in", "I"),
      Canvas("connection.label.out", "O"),
      Canvas("connection.reverse", "V"),
      Canvas("connection.rotate.cw", "OemOpenBrackets"),
      Canvas("connection.rotate.ccw", "OemCloseBrackets"),
      Canvas("connection.rotate.ctrlCw", "Primary+OemOpenBrackets"),
      Canvas("connection.rotate.ctrlCcw", "Primary+OemCloseBrackets"),
      Canvas("room.adjustPorts", "OemSemicolon", "Oem5"),
      Window("room.cyclePortDetail", "Primary+OemSemicolon", "Primary+Oem5"),
      Canvas("room.deleteAllConnections", "Alt+Delete"),

      // Active element and toggles
      Canvas("selection.nextActive", "F3"),
      Canvas("selection.previousActive", "Shift+F3"),
      Window("view.toggleText", "Primary+F4"),

      // View: Primary+= / Primary++ and the bare +/- keys (which deliberately accept any modifier).
      Window("view.zoomIn", "Primary+Plus", "Primary+NumPadAdd"),
      Window("view.zoomOut", "Primary+Minus", "Primary+NumPadSubtract"),
      Window("view.resetZoom", "Primary+0", "Primary+NumPad0"),
      Window("view.fitToMap", "Primary+Home"),
      Canvas("view.resetOrigin", "Home"),
      Canvas("view.zoomIn.key", "Plus", "NumPadAdd", "Shift+Plus", "Shift+NumPadAdd"),
      Canvas("view.zoomOut.key", "Minus", "NumPadSubtract", "Shift+Minus", "Shift+NumPadSubtract"),

      // Automap keys. F11 is host dependent: Primary+Shift+F11 on macOS.
      Canvas("automap.run", "F5"),
      new Row("automap.step", CommandScope.Canvas, new[] { "F11" }, new[] { "Primary+Shift+F11" }, HostDependent: true),

      // Debug toggles (Lee, Q6): reference keys; none on macOS (Developer menu only); gridPolyline never has a key.
      new Row("debug.fps", CommandScope.Canvas, new[] { "Ctrl+F1" }, new string[0], HostDependent: true),
      new Row("debug.mouseCoords", CommandScope.Canvas, new[] { "Shift+F1" }, new string[0], HostDependent: true),
      new Row("debug.elementRendering", CommandScope.Canvas, new[] { "Ctrl+F2" }, new string[0], HostDependent: true),
      new Row("debug.lineRendering", CommandScope.Canvas, new[] { "Ctrl+F3" }, new string[0], HostDependent: true),
      Canvas("debug.gridPolyline"),

      // Code export to file. The map's section 2 hugo row mentions a macOS variant, but section 4 says the file
      // export is unchanged and only the clipboard sibling moves; section 4 is followed.
      Window("file.export.alan", "Primary+Shift+L"),
      Window("file.export.adventuron"),
      Window("file.export.hugo", "Primary+Shift+H"),
      Window("file.export.inform7", "Primary+Shift+7"),
      Window("file.export.inform6", "Primary+Shift+6"),
      Window("file.export.tads", "Primary+Shift+T"),
      Window("file.export.zil", "Primary+Shift+I"),
      Window("file.export.quest", "Primary+Shift+Q") with { MacGestures = new[] { "Primary+Alt+Shift+Q" } },

      // Code export to clipboard
      Window("export.clipboard.alan", "Primary+Alt+L"),
      Window("export.clipboard.adventuron", "Primary+Alt+A"),
      Window("export.clipboard.hugo", "Primary+Alt+H") with { MacGestures = new[] { "Primary+Alt+Shift+H" } },
      Window("export.clipboard.inform7", "Primary+Alt+7"),
      Window("export.clipboard.inform6", "Primary+Alt+6"),
      Window("export.clipboard.tads", "Primary+Alt+T"),
      Window("export.clipboard.zil", "Primary+Alt+Z"),
      Window("export.clipboard.quest"),
      Window("export.clipboard.questRooms", "Primary+Alt+Q"),

      // In the map but not registered at the time of writing (ids follow the existing naming). Checked as soon as
      // a command with the id appears; see PendingRows_AreListed.
      Window("edit.duplicate", "Primary+D") with { Pending = true },
      Window("edit.copyColor", "Primary+Alt+C") with { Pending = true },
      Window("project.backup", "Primary+B") with { Pending = true }
    };

    // Arrow keys: plain move/pan, Primary+Alt resize.
    foreach (var arrow in new[] { "Left", "Right", "Up", "Down" })
    {
      rows.Add(Canvas("selection.move." + arrow.ToLowerInvariant(), arrow));
      rows.Add(Canvas("selection.resize." + arrow.ToLowerInvariant(), "Primary+Alt+" + arrow));
    }

    // Keypad and block navigation (K011, K013, K014, K032-K034, K049-K051). The keypad creation modifier is the
    // default (Ctrl, i.e. Primary); the setting is pinned in SetUp.
    var keypad = new Dictionary<string, string>
    {
      ["north"] = "NumPad8", ["south"] = "NumPad2", ["east"] = "NumPad6", ["west"] = "NumPad4",
      ["northWest"] = "NumPad7", ["southWest"] = "NumPad1", ["northEast"] = "NumPad9", ["southEast"] = "NumPad3"
    };
    var arrows = new Dictionary<string, string> { ["north"] = "Up", ["south"] = "Down", ["east"] = "Right", ["west"] = "Left" };
    var blocks = new Dictionary<string, string>
    {
      ["northWest"] = "Home", ["southWest"] = "End", ["northEast"] = "PageUp", ["southEast"] = "PageDown"
    };
    foreach (var direction in _directions)
    {
      var pad = keypad[direction];
      rows.Add(Canvas("nav.select." + direction, pad));

      var shifted = new List<string> { "Shift+" + pad };
      if (arrows.TryGetValue(direction, out var arrow)) shifted.Add("Shift+" + arrow);
      if (blocks.TryGetValue(direction, out var block)) shifted.Add("Shift+" + block);
      rows.Add(Canvas("nav.selectConnection." + direction, shifted.ToArray()));

      var added = new List<string> { "Primary+" + pad };
      if (arrows.TryGetValue(direction, out arrow)) added.Add("Primary+" + arrow);
      rows.Add(Canvas("nav.addOrConnect." + direction, added.ToArray()));
    }

    return rows;
  }

  public static IEnumerable<string> RowIds()
  {
    return _rows.Where(r => !r.Pending).Select(r => r.Id);
  }

  [SetUp]
  public void SetUp()
  {
    _previousCreation = Settings.KeypadNavigationCreationModifier;
    _previousUnexplored = Settings.KeypadNavigationUnexploredModifier;
    Settings.KeypadNavigationCreationModifier = ModifierKeys.Control;
    Settings.KeypadNavigationUnexploredModifier = ModifierKeys.Alt;
  }

  [TearDown]
  public void TearDown()
  {
    Settings.KeypadNavigationCreationModifier = _previousCreation;
    Settings.KeypadNavigationUnexploredModifier = _previousUnexplored;
  }

  /// <summary>The Core catalogue as the host would build it, with the macOS variants forced on for <paramref name="macOS" />.</summary>
  private static CommandCatalogue Build(bool macOS)
  {
    var catalogue = CommandCatalogue.CreateDefault();
    if (macOS) CommandCatalogue.ApplyMacVariants(catalogue);
    ExportCommands.Register(catalogue, null, macOS);
    return catalogue;
  }

  private static KeyGesture Parse(string text)
  {
    KeyGesture.TryParse(text, out var gesture).ShouldBeTrue("Cannot parse gesture '" + text + "'");
    return gesture;
  }

  private static bool IsChecked(Row row, bool macOS)
  {
    return !row.HostDependent || macOS == OperatingSystem.IsMacOS();
  }

  private static void CheckRow(Row row, bool macOS)
  {
    var catalogue = Build(macOS);
    var command = catalogue.Find(row.Id);
    if (command == null)
    {
      row.Pending.ShouldBeTrue("The shortcut map lists command '" + row.Id + "' but it is not registered.");
      return;
    }

    command.Scope.ShouldBe(row.Scope, row.Id + " scope");
    command.Gestures.Select(g => g.ToString())
      .ShouldBe(row.GesturesFor(macOS).Select(g => Parse(g).ToString()), true, row.Id + " gestures");
  }

  [TestCaseSource(nameof(RowIds))]
  public void Command_MatchesMap_OnWindowsAndLinux(string id)
  {
    Assume.That(!OperatingSystem.IsMacOS(), "Windows/Linux gestures are only built on those hosts.");
    CheckRow(_rows.First(r => r.Id == id), false);
  }

  [TestCaseSource(nameof(RowIds))]
  public void Command_MatchesMap_OnMacOS(string id)
  {
    var row = _rows.First(r => r.Id == id);
    Assume.That(IsChecked(row, true), "Built from the host OS; only checked on macOS.");
    CheckRow(row, true);
  }

  [Test]
  public void PendingRows_AreListed()
  {
    var catalogue = Build(false);
    var missing = _rows.Where(r => r.Pending && catalogue.Find(r.Id) == null).Select(r => r.Id).ToList();
    TestContext.WriteLine("Map commands not registered yet: " + string.Join(", ", missing));
    foreach (var row in _rows.Where(r => r.Pending && catalogue.Find(r.Id) != null))
      CheckRow(row, false);
  }

  [TestCase(false)]
  [TestCase(true)]
  public void Catalogue_HasNoDuplicateGestures(bool macOS)
  {
    CatalogueValidator.Validate(Build(macOS)).ShouldBeEmpty();
  }

  [TestCase(false)]
  [TestCase(true)]
  public void Map_ItselfHasNoDuplicateGestures(bool macOS)
  {
    var owners = new Dictionary<string, string>();
    var clashes = new List<string>();
    foreach (var row in _rows)
    {
      foreach (var gesture in row.GesturesFor(macOS).Select(g => Parse(g).ToString()))
      {
        if (owners.TryGetValue(gesture, out var other)) clashes.Add(gesture + ": " + other + " and " + row.Id);
        else owners[gesture] = row.Id;
      }
    }
    clashes.ShouldBeEmpty();
  }

  [Test]
  public void BareKeyCommands_NeverFireWithOtherModifiers()
  {
    var catalogue = Build(false);
    var probes = new[]
    {
      ModifierKeys.Primary, ModifierKeys.Alt, ModifierKeys.Shift,
      ModifierKeys.Primary | ModifierKeys.Alt, ModifierKeys.Primary | ModifierKeys.Shift,
      ModifierKeys.Alt | ModifierKeys.Shift, ModifierKeys.Control
    };
    var failures = new List<string>();

    foreach (var row in _rows.Where(r => !r.Pending && !r.HostDependent))
    {
      var command = catalogue.Find(row.Id);
      if (command == null) continue;
      var listed = row.Gestures.Select(g => Parse(g)).ToList();

      // Bare keys only: letters, digits, brackets, semicolon, backslash, Enter, F-keys, arrows, Home/End, NumPad.
      foreach (var bare in listed.Where(g => g.Modifiers == ModifierKeys.None && g.Key != InputKey.OemPlus &&
                                              g.Key != InputKey.OemMinus && g.Key != InputKey.Add && g.Key != InputKey.Subtract))
      {
        foreach (var modifiers in probes)
        {
          var probe = new KeyGesture(bare.Key, modifiers);
          if (listed.Contains(probe)) continue;
          foreach (var scope in new[] { CommandScope.Canvas, CommandScope.Window })
          {
            var hit = catalogue.FindByGesture(probe, scope);
            if (hit != null && hit.Id == row.Id) failures.Add(row.Id + " fires on " + probe);
          }
        }
      }
    }

    failures.ShouldBeEmpty();
  }

  [Test]
  public void PrimaryV_IsPaste_NotReverse()
  {
    var catalogue = Build(false);
    catalogue.FindByGesture(new KeyInput(InputKey.V, ModifierKeys.Primary), CommandScope.Window).Id.ShouldBe("edit.paste");
    catalogue.FindByGesture(new KeyInput(InputKey.V, ModifierKeys.Primary), CommandScope.Canvas).ShouldBeNull();
    catalogue.FindByGesture(new KeyInput(InputKey.V, ModifierKeys.None), CommandScope.Canvas).Id.ShouldBe("connection.reverse");
    catalogue.FindByGesture(new KeyInput(InputKey.V, ModifierKeys.None), CommandScope.Window).ShouldBeNull();
  }

  [Test]
  public void PrimaryD_IsNotDown_AndBareDIsLabelDown()
  {
    var catalogue = Build(false);
    var bare = catalogue.FindByGesture(new KeyInput(InputKey.D, ModifierKeys.None), CommandScope.Canvas);
    bare.Id.ShouldBe("connection.label.down");

    // Primary+D is reserved for Duplicate: it must be unbound, or Duplicate, never the label or the arrow.
    foreach (var scope in new[] { CommandScope.Canvas, CommandScope.Window })
    {
      var hit = catalogue.FindByGesture(new KeyInput(InputKey.D, ModifierKeys.Primary), scope);
      if (hit != null) hit.Id.ShouldBe("edit.duplicate");
    }
    catalogue.FindByGesture(new KeyInput(InputKey.Down, ModifierKeys.Primary), CommandScope.Canvas).Id.ShouldBe("nav.addOrConnect.south");
  }

  [Test]
  public void PrimaryShiftA_IsSelectRegions_NotSelectAll()
  {
    var catalogue = Build(false);
    catalogue.FindByGesture(new KeyInput(InputKey.A, ModifierKeys.Primary | ModifierKeys.Shift), CommandScope.Canvas)
      .Id.ShouldBe("select.regions");
    catalogue.FindByGesture(new KeyInput(InputKey.A, ModifierKeys.Primary | ModifierKeys.Shift), CommandScope.Window).ShouldBeNull();
    catalogue.FindByGesture(new KeyInput(InputKey.A, ModifierKeys.Primary), CommandScope.Window).Id.ShouldBe("edit.selectAll");
    catalogue.FindByGesture(new KeyInput(InputKey.A, ModifierKeys.None), CommandScope.Canvas).Id.ShouldBe("connection.toggleOneWay");
  }

  [Test]
  public void PrimaryShiftZ_IsRedo_AndZilExportIsPrimaryShiftI()
  {
    var catalogue = Build(false);
    catalogue.FindByGesture(new KeyInput(InputKey.Z, ModifierKeys.Primary | ModifierKeys.Shift), CommandScope.Window)
      .Id.ShouldBe("edit.redo");
    catalogue.FindByGesture(new KeyInput(InputKey.I, ModifierKeys.Primary | ModifierKeys.Shift), CommandScope.Window)
      .Id.ShouldBe("file.export.zil");
  }

  [Test]
  public void PrimaryComma_AndAltDelete_AreNotOverlapping()
  {
    var catalogue = Build(false);
    catalogue.FindByGesture(new KeyInput(InputKey.Delete, ModifierKeys.None), CommandScope.Window).Id.ShouldBe("edit.delete");
    catalogue.FindByGesture(new KeyInput(InputKey.Delete, ModifierKeys.Alt), CommandScope.Canvas).Id
      .ShouldBe("room.deleteAllConnections");
  }

  [Test]
  public void MacVariants_ResolveToTheMappedCommand()
  {
    var catalogue = Build(true);
    catalogue.FindByGesture(new KeyInput(InputKey.Z, ModifierKeys.Primary | ModifierKeys.Shift), CommandScope.Window)
      .Id.ShouldBe("edit.redo");
    catalogue.FindByGesture(new KeyInput(InputKey.Y, ModifierKeys.Primary), CommandScope.Window).ShouldBeNull();
    catalogue.FindByGesture(new KeyInput(InputKey.H, ModifierKeys.Control), CommandScope.Window).Id.ShouldBe("room.shape.square");
    catalogue.FindByGesture(new KeyInput(InputKey.H, ModifierKeys.Primary), CommandScope.Window).ShouldBeNull();
    catalogue.FindByGesture(new KeyInput(InputKey.W, ModifierKeys.Control), CommandScope.Window).Id.ShouldBe("room.swap.names");
    catalogue.FindByGesture(new KeyInput(InputKey.F5, ModifierKeys.Primary | ModifierKeys.Shift), CommandScope.Window)
      .Id.ShouldBe("room.setStart");
    catalogue.FindByGesture(new KeyInput(InputKey.Q, ModifierKeys.Primary | ModifierKeys.Alt | ModifierKeys.Shift), CommandScope.Window)
      .Id.ShouldBe("file.export.quest");
    catalogue.FindByGesture(new KeyInput(InputKey.H, ModifierKeys.Primary | ModifierKeys.Alt | ModifierKeys.Shift), CommandScope.Window)
      .Id.ShouldBe("export.clipboard.hugo");
  }

  [Test]
  public void ApplyMacVariants_IsIdempotent()
  {
    var catalogue = Build(true);
    var before = catalogue.All.Count;
    CommandCatalogue.ApplyMacVariants(catalogue);
    catalogue.All.Count.ShouldBe(before);
    catalogue.Find("edit.redo").Gestures.Count.ShouldBe(1);
  }
}

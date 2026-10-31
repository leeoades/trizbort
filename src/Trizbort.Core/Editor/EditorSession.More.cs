using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Editor;

// Region, port, z-order, validation and toggle-text operations (tickets 024, 033, 035, 036, 054).
public sealed partial class EditorSession
{
  /// <summary>The last short notice for the status bar (for example the port detail level). Null if none.</summary>
  public string StatusMessage { get; private set; }

  /// <summary>Raised after <see cref="StatusMessage" /> changes.</summary>
  public event Action<string> StatusMessagePosted;

  private void PostStatusMessage(string message)
  {
    StatusMessage = message;
    StatusMessagePosted?.Invoke(message);
  }

  // ---- Regions (024) ----

  /// <summary>Region names: the default region first, then the others A-Z.</summary>
  public IReadOnlyList<string> RegionNames()
  {
    var names = new List<string> { Region.DefaultRegion };
    names.AddRange(Settings.Regions
      .Select(r => r.RegionName)
      .Where(n => !string.IsNullOrEmpty(n) && n != Region.DefaultRegion)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
    return names;
  }

  /// <summary>True when the map has more than one region to choose from.</summary>
  public bool HasMultipleRegions => RegionNames().Count > 1;

  /// <summary>Sets the region of every selected room as one undo step.</summary>
  public void SetSelectedRoomsRegion(string region)
  {
    var rooms = SelectedRooms.ToList();
    if (rooms.Count == 0 || string.IsNullOrEmpty(region)) return;

    Execute("Set region", () =>
    {
      foreach (var room in rooms) room.Region = region;
    }, rooms);
    RaiseChanged(EditorChange.Model);
  }

  /// <summary>Asks the user for a region (via <see cref="Prompts" />) and applies it to the selected rooms.</summary>
  public async Task ChangeRegionAsync()
  {
    if (SelectedRooms.Count == 0) return;

    var current = PrimaryRoom?.Region;
    var chosen = await Prompts.ChooseRegionAsync(RegionNames(), current);
    if (!string.IsNullOrEmpty(chosen)) SetSelectedRoomsRegion(chosen);
  }

  // ---- Ports (033) ----

  public void AdjustPorts()
  {
    var rooms = SelectedRooms;
    if (rooms.Count != 1) return;

    var room = rooms[0];
    var touched = new List<Element> { room };
    touched.AddRange(room.GetConnections());
    Execute("Adjust ports", () => room.AdjustAllRoomConnections(), touched);
    RaiseChanged(EditorChange.Model);
  }

  /// <summary>Cycles the port-adjust detail 0 (4 ports), 1 (8), 2 (16) and tells the user the new level.</summary>
  public void CyclePortDetail()
  {
    var settings = ApplicationSettingsController.AppSettings;
    settings.PortAdjustDetail = (settings.PortAdjustDetail + 1) % 3;
    PostStatusMessage(PortDetailMessage(settings.PortAdjustDetail));
  }

  public static string PortDetailMessage(int level)
  {
    switch (level)
    {
      case 0: return "Port adjust detail: 4 ports";
      case 1: return "Port adjust detail: 8 ports";
      default: return "Port adjust detail: 16 ports";
    }
  }

  public void DeleteAllConnectionsOfRoom()
  {
    var rooms = SelectedRooms;
    if (rooms.Count != 1) return;

    var room = rooms[0];
    var connections = room.GetConnections();
    if (connections.Count == 0) return;

    Execute("Delete all connections", () =>
    {
      foreach (var connection in connections) Project.Elements.Remove(connection);
    }, connections);
    Selection.Set(new Element[] { room });
    RaiseChanged(EditorChange.Model);
  }

  // ---- Z-order (035) ----

  private List<Element> SelectedRoomsAndLabels()
  {
    return Selection.Items.Where(e => e is Room || e is MapLabel).ToList();
  }

  public bool HasRoomOrLabelSelected => Selection.Items.Any(e => e is Room || e is MapLabel);

  public void BringSelectionToFront()
  {
    var items = SelectedRoomsAndLabels();
    if (items.Count == 0) return;

    var top = Project.Elements.Max(e => e.ZOrder);
    Execute("Bring to front", () =>
    {
      for (var i = 0; i < items.Count; i++) items[i].ZOrder = top + 1 + i;
    }, items);
    RaiseChanged(EditorChange.Model);
  }

  public void SendSelectionToBack()
  {
    var items = SelectedRoomsAndLabels();
    if (items.Count == 0) return;

    var bottom = Project.Elements.Min(e => e.ZOrder);
    Execute("Send to back", () =>
    {
      for (var i = 0; i < items.Count; i++) items[i].ZOrder = bottom - items.Count + i;
    }, items);
    RaiseChanged(EditorChange.Model);
  }

  // ---- Validation (036) ----

  /// <summary>
  ///   Flips one of the four project validation flags. Not an undo step: the flags are not part of the map
  ///   snapshot (not saved with the map, and carried over unchanged when undo restores a step).
  /// </summary>
  public void ToggleValidation(ValidationFlag flag)
  {
    var project = Project;
    switch (flag)
    {
      case ValidationFlag.UniqueNames: project.MustHaveUniqueNames = !project.MustHaveUniqueNames; break;
      case ValidationFlag.Description: project.MustHaveDescription = !project.MustHaveDescription; break;
      case ValidationFlag.Subtitle: project.MustHaveSubtitle = !project.MustHaveSubtitle; break;
      case ValidationFlag.NoDangling:
        project.MustHaveNoDanglingConnectors = !project.MustHaveNoDanglingConnectors;
        break;
    }

    RaiseChanged(EditorChange.Model);
  }

  public bool IsValidationEnabled(ValidationFlag flag)
  {
    var project = Project;
    switch (flag)
    {
      case ValidationFlag.UniqueNames: return project.MustHaveUniqueNames;
      case ValidationFlag.Description: return project.MustHaveDescription;
      case ValidationFlag.Subtitle: return project.MustHaveSubtitle;
      default: return project.MustHaveNoDanglingConnectors;
    }
  }

  // ---- Toggle text (054) ----

  public void ToggleTextRendering()
  {
    var settings = ApplicationSettingsController.AppSettings;
    settings.DebugDisableTextRendering = !settings.DebugDisableTextRendering;
    RaiseChanged(EditorChange.Viewport);
  }
}

public enum ValidationFlag
{
  UniqueNames,
  Description,
  Subtitle,
  NoDangling
}

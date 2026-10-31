using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Trizbort.Domain.Application;
using Trizbort.Domain.Controllers;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor.Commands;
using Trizbort.Setup;

namespace Trizbort.Editor;

// Connection, selection, delete and clipboard operations (tickets 021, 030, 032, 038, 039, 040).
public sealed partial class EditorSession
{
  /// <summary>Clipboard shared by sessions until a host supplies its own through <see cref="Clipboard" />.</summary>
  internal static readonly InMemoryClipboard DefaultClipboard = new();

  private IClipboard _clipboard = DefaultClipboard;
  private bool _clipboardHasData = true;
  private string _lastPasteText;
  private int _pasteRepeat;

  /// <summary>Flow, style and label for newly drawn connections; changed by the line-style commands.</summary>
  public NewConnectionDefaults NewConnection { get; } = new();

  /// <summary>The clipboard used by Copy, Cut and Paste. Never null.</summary>
  public IClipboard Clipboard
  {
    get => _clipboard;
    set => _clipboard = value ?? DefaultClipboard;
  }

  /// <summary>
  ///   True when Paste has something to paste. For the in-memory clipboard this is exact; for a host clipboard it is
  ///   the state last seen by <see cref="RefreshClipboardStateAsync" />, Copy, Cut or Paste (true until first known).
  /// </summary>
  public bool CanPaste => _clipboard is InMemoryClipboard memory ? IsTrizbortClipboardText(memory.Text) : _clipboardHasData;

  // ---- Selection (021) ----

  public void SelectSpecial(SelectSpecialKind kind)
  {
    Selection.Set(SelectCommands.Find(Project, kind));
  }

  // ---- Connection styles (030) ----

  /// <summary>Apply a line style to the selected connections and make it the default for new ones.</summary>
  public void SetConnectionStyle(ConnectionStyle style)
  {
    var connections = Selection.Connections.ToList();
    if (connections.Count > 0)
    {
      Execute("Line style", () =>
      {
        foreach (var connection in connections) connection.Style = style;
      }, connections);
      RaiseChanged(EditorChange.Model);
    }
    NewConnection.Style = style;
  }

  /// <summary>Apply a flow to the selected connections and make it the default for new ones.</summary>
  public void SetConnectionFlow(ConnectionFlow flow)
  {
    var connections = Selection.Connections.ToList();
    if (connections.Count > 0)
    {
      Execute("Line direction", () =>
      {
        foreach (var connection in connections) connection.Flow = flow;
      }, connections);
      RaiseChanged(EditorChange.Model);
    }
    NewConnection.Flow = flow;
  }

  /// <summary>Plain, two-way, unlabelled, default-coloured lines for the selection and for new connections.</summary>
  public void ApplyPlainConnectionSettings()
  {
    var connections = Selection.Connections.ToList();
    if (connections.Count > 0)
    {
      Execute("Plain line", () =>
      {
        foreach (var connection in connections)
        {
          connection.Style = ConnectionStyle.Solid;
          connection.Flow = ConnectionFlow.TwoWay;
          connection.SetText(ConnectionLabel.None);
          connection.MidText = string.Empty;
          connection.ConnectionColor = Color.Transparent;
        }
      }, connections);
      RaiseChanged(EditorChange.Model);
    }
    NewConnection.Style = ConnectionStyle.Solid;
    NewConnection.Flow = ConnectionFlow.TwoWay;
    NewConnection.Label = ConnectionLabel.None;
  }

  // ---- Reverse and rotate (032) ----

  public void ReverseSelectedConnections()
  {
    var connections = Selection.Connections.ToList();
    if (connections.Count == 0) return;

    Execute("Reverse line", () =>
    {
      foreach (var connection in connections) connection.Reverse();
    }, connections);
    RaiseChanged(EditorChange.Model);
  }

  /// <summary>Rotate the connector of the single selected connection; <paramref name="ctrlFlag" /> picks the other end.</summary>
  public void RotateSelectedConnector(bool clockwise, bool ctrlFlag)
  {
    if (Selection.Count != 1 || Selection.Primary is not Connection connection) return;

    Execute("Rotate connector", () => connection.RotateConnector(!ctrlFlag, clockwise), new Element[] { connection });
    RaiseChanged(EditorChange.Model);
  }

  // ---- Add label (038) ----

  /// <summary>Add a label centred on <paramref name="centre" /> (or the viewport centre) and select it.</summary>
  public MapLabel AddLabel(Vector? centre)
  {
    var point = centre ?? Viewport.Origin;
    var label = new MapLabel(Project);
    label.Position = Settings.Snap(new Vector(point.X - label.Size.X / 2, point.Y - label.Size.Y / 2));
    Execute("Add label", () => Project.Elements.Add(label), new Element[] { label });
    Selection.Set(new Element[] { label });
    Project.ActiveSelectedElement = label;
    RaiseChanged(EditorChange.Model);
    return label;
  }

  // ---- Clipboard (040) ----

  /// <summary>The JSON for the selection (or active element), or null when there is nothing to copy.</summary>
  private string BuildCopyText()
  {
    var selected = GetSelectedOrActive();
    if (selected.Count == 0) return null;

    var copyObj = new CopyController().CreateCopyObject(selected);
    return JsonConvert.SerializeObject(copyObj, Formatting.Indented,
      new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
  }

  /// <summary>Synchronous copy into the shared in-memory clipboard; hosts without <see cref="Clipboard" /> use this.</summary>
  public string CopySelected()
  {
    var json = BuildCopyText();
    if (json == null) return null;

    DefaultClipboard.Text = json;
    _lastPasteText = null;
    PostStatusMessage($"Copied {GetSelectedOrActive().Count} element(s)");
    return json;
  }

  public string CutSelected()
  {
    var json = CopySelected();
    if (json != null)
    {
      // DeleteSelected opens its own transaction; the outer one makes the whole cut a single "Cut" step.
      var count = GetSelectedOrActive().Count;
      Execute("Cut", () => DeleteSelected());
      PostStatusMessage($"Cut {count} element(s)");
    }
    return json;
  }

  /// <summary>Copy the selection to <see cref="Clipboard" />. Returns false when nothing was selected.</summary>
  public async Task<bool> CopyAsync()
  {
    var json = BuildCopyText();
    if (json == null) return false;

    await _clipboard.SetTextAsync(json);
    _clipboardHasData = true;
    _lastPasteText = null;
    PostStatusMessage($"Copied {GetSelectedOrActive().Count} element(s)");
    return true;
  }

  /// <summary>Copy the selection to <see cref="Clipboard" /> then delete it, as one undo step.</summary>
  public async Task<bool> CutAsync()
  {
    if (!await CopyAsync()) return false;

    var count = GetSelectedOrActive().Count;
    Execute("Cut", () => DeleteSelected());
    PostStatusMessage($"Cut {count} element(s)");
    return true;
  }

  /// <summary>Paste from <see cref="Clipboard" />. <paramref name="target" /> is where the first element lands.</summary>
  public async Task<bool> PasteAsync(Vector? target)
  {
    var text = await _clipboard.GetTextAsync();
    _clipboardHasData = IsTrizbortClipboardText(text);
    return PasteText(text, target);
  }

  /// <summary>Re-read the clipboard so <see cref="CanPaste" /> is current (hosts call this when a menu opens).</summary>
  public async Task RefreshClipboardStateAsync()
  {
    var text = await _clipboard.GetTextAsync();
    var has = IsTrizbortClipboardText(text);
    if (has == _clipboardHasData) return;

    _clipboardHasData = has;
    RaiseChanged(EditorChange.Selection);
  }

  public void Paste(string clipboardText, Vector? target)
  {
    var text = !string.IsNullOrEmpty(clipboardText) ? clipboardText : DefaultClipboard.Text;
    PasteText(text, target);
  }

  /// <summary>Whether <paramref name="text" /> is Trizbort copy data (elements or colours).</summary>
  public static bool IsTrizbortClipboardText(string text)
  {
    if (string.IsNullOrWhiteSpace(text)) return false;

    try
    {
      if (text.Contains("\"CopyType\": 1"))
      {
        return JsonConvert.DeserializeObject<CopyController.CopyColorsObj>(text) != null;
      }

      var copyObj = JsonConvert.DeserializeObject<CopyController.CopyObject>(text);
      return copyObj != null &&
             ((copyObj.Rooms != null && copyObj.Rooms.Count > 0) ||
              (copyObj.Labels != null && copyObj.Labels.Count > 0) ||
              (copyObj.Connections != null && copyObj.Connections.Count > 0));
    }
    catch (JsonException)
    {
      return false;
    }
  }

  private bool PasteText(string text, Vector? target)
  {
    if (!IsTrizbortClipboardText(text)) return false;

    try
    {
      var controller = new CopyController();
      if (text.Contains("\"CopyType\": 1"))
      {
        var colorsObj = JsonConvert.DeserializeObject<CopyController.CopyColorsObj>(text);
        var rooms = Selection.Rooms.ToList();
        Execute("Paste", () =>
        {
          foreach (var room in rooms)
          {
            controller.SetRoomColors(room, colorsObj);
          }
        }, rooms);
        RaiseChanged(EditorChange.Model);
        PostStatusMessage("Pasted colours");
        return true;
      }

      // Pasting the same text again steps the copy one grid cell further each time.
      if (target == null && text == _lastPasteText) _pasteRepeat++;
      else _pasteRepeat = 0;
      _lastPasteText = text;

      var copyObj = JsonConvert.DeserializeObject<CopyController.CopyObject>(text);
      var step = Settings.GridSize * (_pasteRepeat + 1);
      Execute("Paste", () => PasteElements(copyObj, controller, target, step));
      PostStatusMessage("Pasted " + ((copyObj.Rooms?.Count ?? 0) + (copyObj.Labels?.Count ?? 0) + (copyObj.Connections?.Count ?? 0)) + " element(s)");
      return true;
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Paste failed: {ex}");
      return false;
    }
  }

  private void PasteElements(CopyController.CopyObject copyObj, CopyController controller, Vector? targetPosition, float step)
  {
    var project = Project;
    var newRooms = new List<Room>();
    var newLabels = new List<MapLabel>();
    var copiedNodes = new Dictionary<int, Element>();
    var newConnections = new List<Connection>();

    var firstElement = true;
    float offsetX = 0;
    float offsetY = 0;

    foreach (var room in copyObj.Rooms ?? new List<CopyController.CopyRoomObj>())
    {
      var newRoom = new Room(project);
      project.Elements.Add(newRoom);
      newRooms.Add(newRoom);

      if (firstElement)
      {
        var firstX = room.Position.X;
        var firstY = room.Position.Y;
        var targetX = targetPosition?.X ?? (firstX + step);
        var targetY = targetPosition?.Y ?? (firstY + step);
        firstElement = false;
        offsetX = firstX - targetX;
        offsetY = firstY - targetY;
      }

      newRoom.Position = new Vector(room.Position.X - offsetX, room.Position.Y - offsetY);
      controller.SetRoom(newRoom, room);
      copiedNodes[room.OldId] = newRoom;
    }

    foreach (var label in copyObj.Labels ?? new List<CopyController.CopyLabelObj>())
    {
      var newLabel = new MapLabel(project);
      project.Elements.Add(newLabel);
      if (firstElement)
      {
        var targetX = targetPosition?.X ?? (label.Position.X + step);
        var targetY = targetPosition?.Y ?? (label.Position.Y + step);
        offsetX = label.Position.X - targetX;
        offsetY = label.Position.Y - targetY;
        firstElement = false;
      }

      controller.SetLabel(newLabel, label);
      newLabel.Position = label.Position - new Vector(offsetX, offsetY);
      copiedNodes[label.OldId] = newLabel;
      newLabels.Add(newLabel);
    }

    foreach (var room in newRooms)
    {
      room.ReferenceRoomId = copiedNodes.TryGetValue(room.ReferenceRoomId, out var reference) && reference is Room
        ? reference.Id
        : -1;
    }

    newConnections.AddRange(
      controller.PasteConnections(project, copyObj.Connections ?? new List<CopyController.CopyConnectionObj>(),
        copiedNodes, new Vector(offsetX, offsetY)));

    var pasted = new List<Element>();
    pasted.AddRange(newRooms);
    pasted.AddRange(newLabels);
    pasted.AddRange(newConnections);
    Selection.Set(pasted);

    RaiseChanged(EditorChange.Model);
  }
}

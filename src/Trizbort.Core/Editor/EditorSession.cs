using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Trizbort.Domain.Application;
using Trizbort.Domain.Controllers;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Editor;

/// <summary>
///   Owns the editing state of one map window: the project, the selection and the viewport. UI hosts render it
///   and forward input to it; they listen to <see cref="Changed" /> for everything that needs a repaint.
/// </summary>
public sealed class EditorSession : IDisposable
{
  // Fallback clipboard until an IClipboard abstraction exists. Static to match the previous CommandService behaviour.
  private static string _internalClipboard;

  private bool _showGrid = true;
  private Vector? _pointerMapPosition;
  private bool _suppressSelectionEvents;
  private bool _lastDirty;
  private bool _disposed;

  public EditorSession() : this(new UndoStack())
  {
  }

  /// <summary>Create a session around a specific undo stack (for example one with a smaller capacity).</summary>
  public EditorSession(UndoStack undoStack)
  {
    Selection = new Selection();
    Viewport = new Viewport();
    Undo = undoStack ?? throw new ArgumentNullException(nameof(undoStack));
    Undo.Attach(this);
    Undo.Changed += OnUndoChanged;

    Selection.Changed += OnSelectionChanged;
    Viewport.Changed += OnViewportChanged;
    Project.ProjectChanged += OnProjectChanged;

    Project.SelectedElementsProvider = () => Selection.Items;

    if (CopyController.SetClipboardTextHandler == null)
    {
      CopyController.SetClipboardTextHandler = text => _internalClipboard = text;
    }
    if (CopyController.GetClipboardTextHandler == null)
    {
      CopyController.GetClipboardTextHandler = () => _internalClipboard ?? string.Empty;
    }

    _lastDirty = Project.Current != null && Project.Current.IsDirty;
  }

  public Project Project => Project.Current;

  public Selection Selection { get; }

  public Viewport Viewport { get; }

  /// <summary>Undo/redo history. Edits should run inside <see cref="Execute" /> or <see cref="BeginTransaction" />.</summary>
  public UndoStack Undo { get; }

  public bool ShowGrid
  {
    get => _showGrid;
    set
    {
      if (_showGrid == value) return;
      _showGrid = value;
      RaiseChanged(EditorChange.Viewport);
    }
  }

  /// <summary>Map-space point under the pointer while it is over the canvas; null when it is not.</summary>
  public Vector? PointerMapPosition => _pointerMapPosition;

  /// <summary>Record the pointer position in canvas client pixels, converted to map space through <see cref="Viewport" />.</summary>
  public void SetPointerClientPosition(double x, double y)
  {
    SetPointerMapPosition(Viewport.ClientToCanvas(new Vector((float)x, (float)y)));
  }

  /// <summary>Forget the pointer position (for example when it leaves the canvas).</summary>
  public void ClearPointerPosition()
  {
    SetPointerMapPosition(null);
  }

  public event Action<EditorChange> Changed;

  public void Dispose()
  {
    if (_disposed) return;
    _disposed = true;

    Selection.Changed -= OnSelectionChanged;
    Viewport.Changed -= OnViewportChanged;
    Project.ProjectChanged -= OnProjectChanged;
    Undo.Changed -= OnUndoChanged;
  }

  /// <summary>
  ///   Open an undo transaction; disposing the handle records one step if the map changed. Call
  ///   <see cref="UndoTransaction.Cancel" /> to roll back without recording.
  /// </summary>
  public UndoTransaction BeginTransaction(string title, IEnumerable<Element> touched = null)
  {
    return Undo.Begin(title, touched);
  }

  /// <summary>
  ///   Run <paramref name="edit" /> as one undo step. If it throws, the map is rolled back, nothing is recorded and
  ///   the exception is rethrown. (The ticket sketched an <c>IDisposable</c> return value; there is nothing left
  ///   to dispose once the step is recorded, so this returns nothing.)
  /// </summary>
  public void Execute(string title, Action edit, IEnumerable<Element> touched = null)
  {
    if (edit == null) throw new ArgumentNullException(nameof(edit));

    var transaction = Undo.Begin(title, touched);
    try
    {
      edit();
    }
    catch
    {
      transaction.Cancel();
      transaction.Dispose();
      throw;
    }
    transaction.Dispose();
  }

  /// <summary>
  ///   Undo the last step. (Named <c>PerformUndo</c> because C# does not allow a method called <c>Undo</c> next to
  ///   the <see cref="Undo" /> property.)
  /// </summary>
  public void PerformUndo()
  {
    Undo.Undo(this);
  }

  /// <summary>Redo the last undone step.</summary>
  public void PerformRedo()
  {
    Undo.Redo(this);
  }

  /// <summary>Record that the current state has been saved to disk.</summary>
  public void MarkSaved()
  {
    Undo.MarkSaved();
  }

  /// <summary>
  ///   Swap the current project for <paramref name="project" /> (which becomes <c>Project.Current</c>). Raises
  ///   <see cref="EditorChange.Model" /> and clears the selection. Does not touch the undo history.
  /// </summary>
  public void ReplaceProject(Project project)
  {
    if (project == null) throw new ArgumentNullException(nameof(project));

    if (ReferenceEquals(Project, project))
    {
      RaiseChanged(EditorChange.Model);
      return;
    }

    // The Project.Current setter raises ProjectChanged, so OnProjectChanged raises Model | Selection.
    Project.Current = project;
  }

  /// <summary>Raise <see cref="Changed" /> for a change made outside the session.</summary>
  public void Invalidate(EditorChange change)
  {
    RaiseChanged(change);
  }

  /// <summary>Raise <see cref="EditorChange.Dirty" /> if <c>Project.IsDirty</c> changed since the last check.</summary>
  public void CheckDirty()
  {
    var dirty = Project != null && Project.IsDirty;
    if (dirty == _lastDirty) return;

    _lastDirty = dirty;
    RaiseChanged(EditorChange.Dirty);
  }

  public void NewProject()
  {
    Settings.Reset();
    Selection.Clear();
    // The Project.Current setter raises Project.ProjectChanged, which notifies listeners of this session.
    Project.Current = new Project();
    // A new document starts with no history and counts as saved.
    Undo.Clear();
  }

  public Room AddRoom(Vector? position)
  {
    var pos = position ?? Viewport.Origin;
    var room = new Room(Project)
    {
      Position = Settings.Snap(pos)
    };
    Execute("Add room", () => Project.Elements.Add(room), new Element[] { room });
    Selection.Set(new Element[] { room });
    Project.ActiveSelectedElement = room;
    RaiseChanged(EditorChange.Model);
    return room;
  }

  public MapLabel AddMapLabel(Vector? position)
  {
    var pos = position ?? Viewport.Origin;
    var label = new MapLabel(Project)
    {
      Position = Settings.Snap(pos)
    };
    Execute("Add label", () => Project.Elements.Add(label), new Element[] { label });
    Selection.Set(new Element[] { label });
    Project.ActiveSelectedElement = label;
    RaiseChanged(EditorChange.Model);
    return label;
  }

  public int DeleteSelected()
  {
    var selected = GetSelectedOrActive();
    if (selected.Count == 0) return 0;

    Execute("Delete", () =>
    {
      foreach (var element in selected)
      {
        Project.Elements.Remove(element);
      }
    }, selected);
    // Undo restores a new project object, so the selection is cleared against whichever project is current now.
    Selection.Clear();
    Project.ActiveSelectedElement = null;
    RaiseChanged(EditorChange.Model);
    return selected.Count;
  }

  public void Select(Element element, bool toggle, bool addToSelection)
  {
    Selection.Click(element, toggle, addToSelection);
  }

  public void Select(IEnumerable<Element> elements, bool clearExisting)
  {
    var list = elements == null ? new List<Element>() : elements.ToList();
    if (clearExisting)
    {
      Selection.Set(list);
      return;
    }

    foreach (var element in list)
    {
      Selection.Add(element);
    }
  }

  public void Deselect(Element element)
  {
    Selection.Remove(element);
  }

  public void SelectAll()
  {
    Selection.Set(Project.Elements);
  }

  public void ClearSelection()
  {
    Selection.Clear();
    Project.ActiveSelectedElement = null;
  }

  public void NudgeSelected(Vector delta)
  {
    if (delta == Vector.Zero || Selection.Count == 0) return;

    var items = Selection.Items.ToList();
    Execute("Move", () => MapEditing.Move(Project.Elements, items, delta), items);
    RaiseChanged(EditorChange.Model);
  }

  /// <summary>
  ///   Resize the selected rooms by <paramref name="delta" /> (the Ctrl+Alt+arrow gesture), keeping at least one grid
  ///   cell. One undo step per call, so key repeat makes one step per repeat.
  /// </summary>
  public void ResizeSelected(Vector delta)
  {
    if (delta == Vector.Zero || Selection.Count == 0) return;

    var sizeables = Selection.Items.OfType<ISizeable>().ToList();
    if (sizeables.Count == 0) return;

    Execute("Resize", () =>
    {
      foreach (var sizeable in sizeables)
      {
        var width = sizeable.Width + delta.X;
        var height = sizeable.Height + delta.Y;
        // Shrinking never goes below one grid cell; the axis is left unchanged instead.
        if (delta.X < 0 && width < Settings.GridSize) width = sizeable.Width;
        if (delta.Y < 0 && height < Settings.GridSize) height = sizeable.Height;
        sizeable.Size = new Vector(width, height);
      }
    }, sizeables.OfType<Element>());
    RaiseChanged(EditorChange.Model);
  }

  /// <summary>Run <paramref name="apply"/> (typically a dialog's OK handler) as one undo step.</summary>
  public void Apply(string title, Element element, Action apply)
  {
    Execute(title, apply, new[] { element });
    RaiseChanged(EditorChange.Model);
  }

  public string CopySelected()
  {
    var selected = GetSelectedOrActive();
    if (selected.Count == 0) return null;

    var controller = new CopyController();
    var copyObj = controller.CreateCopyObject(selected);
    var json = JsonConvert.SerializeObject(copyObj, Formatting.Indented,
      new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
    _internalClipboard = json;
    return json;
  }

  public string CutSelected()
  {
    var json = CopySelected();
    if (json != null)
    {
      // DeleteSelected opens its own transaction; the outer one makes the whole cut a single "Cut" step.
      Execute("Cut", () => DeleteSelected());
    }
    return json;
  }

  public void Paste(string clipboardText, Vector? target)
  {
    var text = !string.IsNullOrEmpty(clipboardText) ? clipboardText : _internalClipboard;
    if (string.IsNullOrEmpty(text)) return;

    try
    {
      var controller = new CopyController();
      if (text.Contains("\"CopyType\": 1"))
      {
        var colorsObj = JsonConvert.DeserializeObject<CopyController.CopyColorsObj>(text);
        if (colorsObj != null)
        {
          var rooms = Selection.Rooms.ToList();
          Execute("Paste", () =>
          {
            foreach (var room in rooms)
            {
              controller.SetRoomColors(room, colorsObj);
            }
          }, rooms);
          RaiseChanged(EditorChange.Model);
        }
      }
      else
      {
        var copyObj = JsonConvert.DeserializeObject<CopyController.CopyObject>(text);
        if (copyObj != null)
        {
          Execute("Paste", () => PasteRooms(copyObj, controller, target));
        }
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Paste failed: {ex}");
    }
  }

  private void PasteRooms(CopyController.CopyObject copyObj, CopyController controller, Vector? targetPosition)
  {
    var project = Project;
    var newRooms = new List<Room>();
    var newLabels = new List<MapLabel>();
    var copiedNodes = new Dictionary<int, Element>();
    var newConnections = new List<Connection>();

    var firstElement = true;
    float offsetX = 0;
    float offsetY = 0;

    foreach (var room in copyObj.Rooms)
    {
      var newRoom = new Room(project);
      project.Elements.Add(newRoom);
      newRooms.Add(newRoom);

      if (firstElement)
      {
        var firstX = room.Position.X;
        var firstY = room.Position.Y;
        var targetX = targetPosition?.X ?? (firstX + Settings.GridSize);
        var targetY = targetPosition?.Y ?? (firstY + Settings.GridSize);
        firstElement = false;
        offsetX = firstX - targetX;
        offsetY = firstY - targetY;
      }

      newRoom.Position = new Vector(room.Position.X - offsetX, room.Position.Y - offsetY);
      controller.SetRoom(newRoom, room);
      copiedNodes[room.OldId] = newRoom;
    }

    foreach (var label in copyObj.Labels)
    {
      var newLabel = new MapLabel(project);
      project.Elements.Add(newLabel);
      if (firstElement)
      {
        var targetX = targetPosition?.X ?? (label.Position.X + Settings.GridSize);
        var targetY = targetPosition?.Y ?? (label.Position.Y + Settings.GridSize);
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
      controller.PasteConnections(project, copyObj.Connections, copiedNodes, new Vector(offsetX, offsetY)));

    var pasted = new List<Element>();
    pasted.AddRange(newRooms);
    pasted.AddRange(newLabels);
    pasted.AddRange(newConnections);
    Selection.Set(pasted);

    RaiseChanged(EditorChange.Model);
  }

  private List<Element> GetSelectedOrActive()
  {
    if (Selection.Count > 0) return Selection.Items.ToList();

    var active = Project.ActiveSelectedElement;
    var result = new List<Element>();
    if (active != null) result.Add(active);
    return result;
  }

  private void OnSelectionChanged(object sender, EventArgs e)
  {
    if (Project != null)
    {
      Project.ActiveSelectedElement = Selection.Primary;
    }
    if (_suppressSelectionEvents) return;
    RaiseChanged(EditorChange.Selection);
  }

  private void OnUndoChanged(object sender, EventArgs e)
  {
    // The stack position is the single source of truth for the dirty flag.
    if (Project != null) Project.IsDirty = Undo.IsDirty;
    _lastDirty = Undo.IsDirty;
    RaiseChanged(EditorChange.Dirty);
  }

  private void OnViewportChanged(object sender, EventArgs e)
  {
    RaiseChanged(EditorChange.Viewport);
  }

  private void OnProjectChanged(object sender, ProjectChangedEventArgs e)
  {
    _suppressSelectionEvents = true;
    try
    {
      Selection.Clear();
    }
    finally
    {
      _suppressSelectionEvents = false;
    }

    _lastDirty = e.NewProject != null && e.NewProject.IsDirty;
    RaiseChanged(EditorChange.Model | EditorChange.Selection);
  }

  private void SetPointerMapPosition(Vector? position)
  {
    if (_pointerMapPosition == position) return;

    _pointerMapPosition = position;
    RaiseChanged(EditorChange.Pointer);
  }

  private void RaiseChanged(EditorChange change)
  {
    var handler = Changed;
    handler?.Invoke(change);
  }
}

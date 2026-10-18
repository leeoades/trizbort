using Newtonsoft.Json;
using Trizbort.Domain.Application;
using Trizbort.Domain.Controllers;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Avalonia.Services;

public class CommandService : ICommandService
{
    private float _zoomFactor = 1.0f;
    private Vector _origin = Vector.Zero;
    private bool _showGrid = true;
    private readonly HashSet<Element> _selectedElements = [];
    private static string? _internalClipboard;

    public Project Project => Project.Current;

    public IReadOnlySet<Element> SelectedElements => _selectedElements;

    public double ViewportWidth { get; private set; } = 800;
    public double ViewportHeight { get; private set; } = 600;

    public float ZoomFactor
    {
        get => _zoomFactor;
        set => SetView(_origin, value);
    }

    public Vector Origin
    {
        get => _origin;
        set => SetView(value, _zoomFactor);
    }

    public void SetViewportSize(double width, double height)
    {
        if (width > 0 && height > 0)
        {
            ViewportWidth = width;
            ViewportHeight = height;
        }
    }

    public void SetView(Vector origin, float zoomFactor)
    {
        var clampedZoom = Math.Clamp(zoomFactor, 0.1f, 10.0f);
        var zoomChanged = Math.Abs(_zoomFactor - clampedZoom) > 0.0001f;
        var originChanged = _origin != origin;

        if (zoomChanged || originChanged)
        {
            _zoomFactor = clampedZoom;
            _origin = origin;
            if (zoomChanged)
            {
                ZoomChanged?.Invoke(this, _zoomFactor);
            }
            InvalidateCanvas();
        }
    }

    public bool ShowGrid
    {
        get => _showGrid;
        set
        {
            if (_showGrid != value)
            {
                _showGrid = value;
                InvalidateCanvas();
            }
        }
    }

    public event EventHandler? ProjectChanged;
    public event EventHandler? SelectionChanged;
    public event EventHandler<float>? ZoomChanged;
    public event EventHandler? CanvasInvalidated;

    public CommandService()
    {
        Project.SelectedElementsProvider = () => _selectedElements;
        Project.ProjectChanged += OnCoreProjectChanged;
        if (CopyController.SetClipboardTextHandler == null)
        {
            CopyController.SetClipboardTextHandler = text => _internalClipboard = text;
        }
        if (CopyController.GetClipboardTextHandler == null)
        {
            CopyController.GetClipboardTextHandler = () => _internalClipboard ?? string.Empty;
        }
    }

    private void OnCoreProjectChanged(object? sender, ProjectChangedEventArgs e)
    {
        _selectedElements.Clear();
        ProjectChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        InvalidateCanvas();
    }

    public void NewMap()
    {
        Settings.Reset();
        _selectedElements.Clear();
        Project.Current = new Project();
        // Project.Current setter fires Project.ProjectChanged which notifies listeners
    }

    public Room AddRoom(Vector? position = null)
    {
        var pos = position ?? Origin;
        var room = new Room(Project.Current)
        {
            Position = Settings.Snap(pos)
        };
        Project.Current.Elements.Add(room);
        Project.Current.IsDirty = true;
        _selectedElements.Clear();
        _selectedElements.Add(room);
        Project.Current.ActiveSelectedElement = room;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        InvalidateCanvas();
        return room;
    }

    public MapLabel AddMapLabel(Vector? position = null)
    {
        var pos = position ?? Origin;
        var label = new MapLabel(Project.Current)
        {
            Position = Settings.Snap(pos)
        };
        Project.Current.Elements.Add(label);
        Project.Current.IsDirty = true;
        _selectedElements.Clear();
        _selectedElements.Add(label);
        Project.Current.ActiveSelectedElement = label;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        InvalidateCanvas();
        return label;
    }

    public int DeleteSelected()
    {
        var selected = _selectedElements.Count > 0
            ? _selectedElements.ToList()
            : (Project.Current.ActiveSelectedElement != null ? [Project.Current.ActiveSelectedElement] : new List<Element>());

        if (selected.Count > 0)
        {
            foreach (var element in selected)
            {
                Project.Current.Elements.Remove(element);
            }
            _selectedElements.Clear();
            Project.Current.ActiveSelectedElement = null;
            Project.Current.IsDirty = true;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            InvalidateCanvas();
        }
        return selected.Count;
    }

    public void Select(Element element, bool toggle = false, bool addToSelection = false)
    {
        if (toggle)
        {
            if (_selectedElements.Contains(element))
            {
                _selectedElements.Remove(element);
                if (Project.Current.ActiveSelectedElement == element)
                {
                    Project.Current.ActiveSelectedElement = _selectedElements.LastOrDefault();
                }
            }
            else
            {
                _selectedElements.Add(element);
                Project.Current.ActiveSelectedElement = element;
            }
        }
        else if (addToSelection)
        {
            _selectedElements.Add(element);
            Project.Current.ActiveSelectedElement = element;
        }
        else
        {
            _selectedElements.Clear();
            _selectedElements.Add(element);
            Project.Current.ActiveSelectedElement = element;
        }

        SelectionChanged?.Invoke(this, EventArgs.Empty);
        InvalidateCanvas();
    }

    public void Select(IEnumerable<Element> elements, bool clearExisting = true)
    {
        if (clearExisting)
        {
            _selectedElements.Clear();
        }

        Element? last = null;
        foreach (var element in elements)
        {
            _selectedElements.Add(element);
            last = element;
        }

        if (last != null)
        {
            Project.Current.ActiveSelectedElement = last;
        }
        else if (clearExisting)
        {
            Project.Current.ActiveSelectedElement = null;
        }

        SelectionChanged?.Invoke(this, EventArgs.Empty);
        InvalidateCanvas();
    }

    public void Deselect(Element element)
    {
        if (_selectedElements.Remove(element))
        {
            if (Project.Current.ActiveSelectedElement == element)
            {
                Project.Current.ActiveSelectedElement = _selectedElements.LastOrDefault();
            }
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            InvalidateCanvas();
        }
    }

    public void SelectAll()
    {
        _selectedElements.Clear();
        foreach (var element in Project.Current.Elements)
        {
            _selectedElements.Add(element);
        }
        if (Project.Current.Elements.Count > 0)
        {
            Project.Current.ActiveSelectedElement = Project.Current.Elements[^1];
        }
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        InvalidateCanvas();
    }

    public void ClearSelection()
    {
        _selectedElements.Clear();
        Project.Current.ActiveSelectedElement = null;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        InvalidateCanvas();
    }

    public void NudgeSelected(Vector delta)
    {
        if (delta == Vector.Zero || _selectedElements.Count == 0) return;

        MapEditing.Move(Project.Current.Elements, _selectedElements.ToList(), delta);
        Project.Current.IsDirty = true;
        InvalidateCanvas();
    }

    public string? CopySelected()
    {
        var selected = _selectedElements.Count > 0
            ? _selectedElements.ToList()
            : (Project.Current.ActiveSelectedElement != null ? [Project.Current.ActiveSelectedElement] : new List<Element>());

        if (selected.Count == 0) return null;

        var controller = new CopyController();
        var copyObj = controller.CreateCopyObject(selected);
        var json = JsonConvert.SerializeObject(copyObj, Formatting.Indented,
            new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
        _internalClipboard = json;
        return json;
    }

    public string? CutSelected()
    {
        var json = CopySelected();
        if (json != null)
        {
            DeleteSelected();
        }
        return json;
    }

    public void Paste(string? clipboardText = null, Vector? targetPosition = null)
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
                    foreach (var room in _selectedElements.OfType<Room>())
                    {
                        controller.SetRoomColors(room, colorsObj);
                    }
                    Project.Current.IsDirty = true;
                    InvalidateCanvas();
                }
            }
            else
            {
                var copyObj = JsonConvert.DeserializeObject<CopyController.CopyObject>(text);
                if (copyObj != null)
                {
                    PasteRooms(copyObj, controller, targetPosition);
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
        var newRooms = new List<Room>();
        var newLabels = new List<MapLabel>();
        var copiedNodes = new Dictionary<int, Element>();
        var newConnections = new List<Connection>();

        var firstElement = true;
        float offsetX = 0;
        float offsetY = 0;

        foreach (var room in copyObj.Rooms)
        {
            var newRoom = new Room(Project.Current);
            Project.Current.Elements.Add(newRoom);
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
            var newLabel = new MapLabel(Project.Current);
            Project.Current.Elements.Add(newLabel);
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
            controller.PasteConnections(Project.Current, copyObj.Connections, copiedNodes, new Vector(offsetX, offsetY)));

        _selectedElements.Clear();
        foreach (var elem in newRooms) _selectedElements.Add(elem);
        foreach (var elem in newLabels) _selectedElements.Add(elem);
        foreach (var elem in newConnections) _selectedElements.Add(elem);

        if (_selectedElements.Count > 0)
        {
            Project.Current.ActiveSelectedElement = _selectedElements.Last();
        }

        Project.Current.IsDirty = true;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        InvalidateCanvas();
    }

    public void ZoomIn()
    {
        SetView(Origin, (float)Math.Round(ZoomFactor * 1.25f, 2));
    }

    public void ZoomOut()
    {
        SetView(Origin, (float)Math.Round(ZoomFactor / 1.25f, 2));
    }

    public void ResetZoom()
    {
        SetView(Vector.Zero, 1.0f);
    }

    public void FitToMap()
    {
        var transform = new CanvasTransform
        {
            ViewportWidth = ViewportWidth > 0 ? ViewportWidth : 800,
            ViewportHeight = ViewportHeight > 0 ? ViewportHeight : 600
        };
        transform.FitToMap(Project.Current.Elements);
        SetView(transform.Origin, transform.ZoomFactor);
    }

    public void ToggleGrid()
    {
        ShowGrid = !ShowGrid;
    }

    public void InvalidateCanvas()
    {
        CanvasInvalidated?.Invoke(this, EventArgs.Empty);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.Application;
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

    public Project Project => Project.Current;

    public float ZoomFactor
    {
        get => _zoomFactor;
        set
        {
            var clamped = Math.Clamp(value, 0.1f, 10.0f);
            if (Math.Abs(_zoomFactor - clamped) > 0.0001f)
            {
                _zoomFactor = clamped;
                ZoomChanged?.Invoke(this, _zoomFactor);
                InvalidateCanvas();
            }
        }
    }

    public Vector Origin
    {
        get => _origin;
        set
        {
            if (_origin != value)
            {
                _origin = value;
                InvalidateCanvas();
            }
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

    public void ZoomIn()
    {
        ZoomFactor = (float)Math.Round(ZoomFactor * 1.25f, 2);
    }

    public void ZoomOut()
    {
        ZoomFactor = (float)Math.Round(ZoomFactor / 1.25f, 2);
    }

    public void ResetZoom()
    {
        ZoomFactor = 1.0f;
        Origin = Vector.Zero;
    }

    public void FitToMap()
    {
        if (Project.Current.Elements.Count == 0)
        {
            ResetZoom();
            return;
        }

        var bounds = Rect.Empty;
        foreach (var element in Project.Current.Elements)
        {
            bounds = element.UnionBoundsWith(bounds, true);
        }

        if (bounds.Width > 0 && bounds.Height > 0)
        {
            Origin = new Vector(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
            ZoomFactor = 1.0f;
        }
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

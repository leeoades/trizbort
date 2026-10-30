using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Avalonia.Services;

/// <summary>
///   Thin adapter that exposes an <see cref="EditorSession" /> through the legacy <see cref="ICommandService" />
///   surface. All editing, selection and viewport logic lives in Core.
/// </summary>
public class CommandService : ICommandService
{
    private Project _lastProject;
    private Vector _lastOrigin;
    private float _lastZoom;
    private double _lastWidth;
    private double _lastHeight;

    public CommandService() : this(new EditorSession())
    {
    }

    public CommandService(EditorSession session)
    {
        Session = session;
        SelectedElements = new SelectionSetView(session.Selection);
        _lastProject = session.Project;
        _lastOrigin = session.Viewport.Origin;
        _lastZoom = session.Viewport.ZoomFactor;
        _lastWidth = session.Viewport.Width;
        _lastHeight = session.Viewport.Height;
        session.Changed += OnSessionChanged;
    }

    public EditorSession Session { get; }

    public Project Project => Session.Project;

    public IReadOnlySet<Element> SelectedElements { get; }

    public double ViewportWidth => Session.Viewport.Width;
    public double ViewportHeight => Session.Viewport.Height;

    public float ZoomFactor
    {
        get => Session.Viewport.ZoomFactor;
        set => SetView(Origin, value);
    }

    public Vector Origin
    {
        get => Session.Viewport.Origin;
        set => SetView(value, ZoomFactor);
    }

    public bool ShowGrid
    {
        get => Session.ShowGrid;
        set => Session.ShowGrid = value;
    }

    public event EventHandler? ProjectChanged;
    public event EventHandler? SelectionChanged;
    public event EventHandler<float>? ZoomChanged;
    public event EventHandler? CanvasInvalidated;
    public event EventHandler? PointerMoved;
    public event EventHandler? DirtyChanged;

    public void SetViewportSize(double width, double height) => Session.Viewport.SetSize(width, height);
    public void SetView(Vector origin, float zoomFactor) => Session.Viewport.SetView(origin, zoomFactor);

    public Vector? PointerMapPosition => Session.PointerMapPosition;
    public void SetPointerClientPosition(double x, double y) => Session.SetPointerClientPosition(x, y);
    public void ClearPointerPosition() => Session.ClearPointerPosition();

    public void NewMap() => Session.NewProject();
    public Room AddRoom(Vector? position = null) => Session.AddRoom(position);
    public MapLabel AddMapLabel(Vector? position = null) => Session.AddMapLabel(position);
    public int DeleteSelected() => Session.DeleteSelected();

    public void Select(Element element, bool toggle = false, bool addToSelection = false) =>
        Session.Select(element, toggle, addToSelection);

    public void Select(IEnumerable<Element> elements, bool clearExisting = true) =>
        Session.Select(elements, clearExisting);

    public void Deselect(Element element) => Session.Deselect(element);
    public void SelectAll() => Session.SelectAll();
    public void ClearSelection() => Session.ClearSelection();
    public void NudgeSelected(Vector delta) => Session.NudgeSelected(delta);
    public void ResizeSelected(Vector delta) => Session.ResizeSelected(delta);

    public string? CopySelected() => Session.CopySelected();
    public string? CutSelected() => Session.CutSelected();
    public void Paste(string? clipboardText = null, Vector? targetPosition = null) =>
        Session.Paste(clipboardText, targetPosition);

    public void ZoomIn() => Session.Viewport.ZoomIn();
    public void ZoomOut() => Session.Viewport.ZoomOut();
    public void ResetZoom() => Session.Viewport.ResetZoom();
    public void FitToMap() => Session.Viewport.FitToMap(Session.Project.Elements);
    public void ToggleGrid() => ShowGrid = !ShowGrid;

    public void InvalidateCanvas() => Session.Invalidate(EditorChange.Adorners);

    private void OnSessionChanged(EditorChange change)
    {
        if ((change & EditorChange.Pointer) != 0)
        {
            PointerMoved?.Invoke(this, EventArgs.Empty);
            change &= ~EditorChange.Pointer;
            if (change == EditorChange.None) return;
        }

        if ((change & EditorChange.Viewport) != 0 && !HasVisibleViewportChange())
        {
            // Only the viewport size changed: the legacy service never raised events for that.
            change &= ~EditorChange.Viewport;
            if (change == EditorChange.None) return;
        }

        if ((change & EditorChange.Model) != 0 && !ReferenceEquals(_lastProject, Session.Project))
        {
            _lastProject = Session.Project;
            ProjectChanged?.Invoke(this, EventArgs.Empty);
        }

        if ((change & EditorChange.Dirty) != 0)
        {
            DirtyChanged?.Invoke(this, EventArgs.Empty);
        }

        if ((change & EditorChange.Selection) != 0)
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        if ((change & EditorChange.Viewport) != 0 && Math.Abs(_lastZoom - Session.Viewport.ZoomFactor) > 0.0001f)
        {
            _lastZoom = Session.Viewport.ZoomFactor;
            ZoomChanged?.Invoke(this, _lastZoom);
        }

        _lastOrigin = Session.Viewport.Origin;
        _lastZoom = Session.Viewport.ZoomFactor;
        CanvasInvalidated?.Invoke(this, EventArgs.Empty);
    }

    private bool HasVisibleViewportChange()
    {
        var viewport = Session.Viewport;
        var sizeChanged = _lastWidth != viewport.Width || _lastHeight != viewport.Height;
        _lastWidth = viewport.Width;
        _lastHeight = viewport.Height;
        if (!sizeChanged) return true;
        return _lastOrigin != viewport.Origin || Math.Abs(_lastZoom - viewport.ZoomFactor) > 0.0001f;
    }

    private sealed class SelectionSetView(Selection selection) : IReadOnlySet<Element>
    {
        public int Count => selection.Count;

        public bool Contains(Element item) => selection.Contains(item);

        public IEnumerator<Element> GetEnumerator() => selection.Items.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public bool IsProperSubsetOf(IEnumerable<Element> other) => Snapshot().IsProperSubsetOf(other);
        public bool IsProperSupersetOf(IEnumerable<Element> other) => Snapshot().IsProperSupersetOf(other);
        public bool IsSubsetOf(IEnumerable<Element> other) => Snapshot().IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<Element> other) => Snapshot().IsSupersetOf(other);
        public bool Overlaps(IEnumerable<Element> other) => Snapshot().Overlaps(other);
        public bool SetEquals(IEnumerable<Element> other) => Snapshot().SetEquals(other);

        private HashSet<Element> Snapshot() => new(selection.Items);
    }
}

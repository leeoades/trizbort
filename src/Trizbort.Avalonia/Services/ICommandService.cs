using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Avalonia.Services;

public interface ICommandService
{
    /// <summary>The editor session that owns the project, selection and undo history.</summary>
    EditorSession Session { get; }

    Project Project { get; }

    IReadOnlySet<Element> SelectedElements { get; }

    float ZoomFactor { get; set; }
    Vector Origin { get; set; }
    bool ShowGrid { get; set; }
    double ViewportWidth { get; }
    double ViewportHeight { get; }

    void SetView(Vector origin, float zoomFactor);
    void SetViewportSize(double width, double height);

    /// <summary>Map-space point under the pointer while it is over the canvas; null when it is not.</summary>
    Vector? PointerMapPosition { get; }
    void SetPointerClientPosition(double x, double y);
    void ClearPointerPosition();

    void NewMap();
    Room AddRoom(Vector? position = null);
    MapLabel AddMapLabel(Vector? position = null);
    int DeleteSelected();
    void Select(Element element, bool toggle = false, bool addToSelection = false);
    void Select(IEnumerable<Element> elements, bool clearExisting = true);
    void Deselect(Element element);
    void SelectAll();
    void ClearSelection();
    void NudgeSelected(Vector delta);
    void ResizeSelected(Vector delta);

    string? CopySelected();
    string? CutSelected();
    void Paste(string? clipboardText = null, Vector? targetPosition = null);

    void ZoomIn();
    void ZoomOut();
    void ResetZoom();
    void FitToMap();
    void ToggleGrid();

    event EventHandler? ProjectChanged;
    event EventHandler? SelectionChanged;
    event EventHandler<float>? ZoomChanged;
    event EventHandler? CanvasInvalidated;

    /// <summary>The pointer moved over the map (<see cref="PointerMapPosition" /> changed); no redraw needed.</summary>
    event EventHandler? PointerMoved;

    /// <summary>The unsaved-changes flag changed (undo stack moved away from or back to the saved state, or the map was saved).</summary>
    event EventHandler? DirtyChanged;

    void InvalidateCanvas();
}

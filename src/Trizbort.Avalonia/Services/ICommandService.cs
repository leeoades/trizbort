using System;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

namespace Trizbort.Avalonia.Services;

public interface ICommandService
{
    Project Project { get; }

    IReadOnlySet<Element> SelectedElements { get; }

    float ZoomFactor { get; set; }
    Vector Origin { get; set; }
    bool ShowGrid { get; set; }
    double ViewportWidth { get; }
    double ViewportHeight { get; }

    void SetView(Vector origin, float zoomFactor);
    void SetViewportSize(double width, double height);

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

    void InvalidateCanvas();
}

using System;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

namespace Trizbort.Avalonia.Services;

public interface ICommandService
{
    Project Project { get; }

    float ZoomFactor { get; set; }
    Vector Origin { get; set; }
    bool ShowGrid { get; set; }

    void NewMap();
    Room AddRoom(Vector? position = null);
    MapLabel AddMapLabel(Vector? position = null);
    void DeleteSelected();
    void SelectAll();
    void ClearSelection();

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

using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.UI.Controls;

namespace Trizbort.Domain.Controllers;

public class CanvasController
{
  private readonly Canvas _canvas;


  public CanvasController() : this(TrizbortApplication.MainForm.Canvas)
  {
  }

  internal CanvasController(Canvas canvas)
  {
    _canvas = canvas;
  }

  public void EnsureVisible(Element element)
  {
    if (element == null) return;
    var rect = Rect.Empty;
    rect = element.UnionBoundsWith(rect, false);
    if (rect != Rect.Empty) _canvas.Origin = rect.Center;
  }

  public void SelectElements(List<Element> elements)
  {
    _canvas.SelectElements(elements);
  }

  public void SelectRoomClosestToCenterOfViewport()
  {
    // select the room closest to the center of the viewport
    var viewportCenter = _canvas.Viewport.Center;
    Room closestRoom = null;
    var closestDistance = float.MaxValue;
    foreach (var element in Project.Current.Elements.OfType<Room>())
    {
      var roomCenter = element.InnerBounds.Center;
      var distance = roomCenter.Distance(viewportCenter);

      if (!(distance < closestDistance)) continue;
      closestRoom = element;
      closestDistance = distance;
    }

    _canvas.SelectedElement = closestRoom;
    EnsureVisible(_canvas.SelectedElement);
  }

  public void SelectStartRoom()
  {
    var startRoom = Project.Current.Elements.OfType<Room>().FirstOrDefault(p => p.IsStartRoom);
    if (startRoom != null)
    {
      _canvas.SelectedElement = startRoom;
      EnsureVisible(startRoom);
    }
  }

  public void SetConnectionFlow(ConnectionFlow connectionFlow)
  {
    var elements = _canvas.SelectedConnections;
    SetConnectionFlow(elements, connectionFlow);
  }

  public void SetConnectionLabel(ConnectionLabel label)
  {
    var elements = _canvas.SelectedConnections;
    SetConnectionLabel(elements, label);
  }

  public void SetConnectionStyle(ConnectionStyle style)
  {
    var elements = _canvas.SelectedConnections;
    SetConnectionStyle(elements, style);
  }

  private void SetConnectionFlow(List<Connection> connections, ConnectionFlow connectionFlow)
  {
    connections.ForEach(p => p.Flow = connectionFlow);
    _canvas.NewConnectionFlow = connectionFlow;
  }

  private void SetConnectionLabel(List<Connection> connections, ConnectionLabel label)
  {
    connections.ForEach(p => p.SetText(label));
    _canvas.NewConnectionLabel = label;
  }

  private void SetConnectionStyle(List<Connection> connections, ConnectionStyle style)
  {
    connections.ForEach(p => p.Style = style);
    _canvas.NewConnectionStyle = style;
  }
}
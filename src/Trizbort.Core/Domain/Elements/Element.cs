using System;
using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json;
using Trizbort.Domain.Application;
using Trizbort.Domain.Misc;

namespace Trizbort.Domain.Elements;

public class Element : IComparable<Element>
{
  private int _id;
  private int _zOrder;

  public Element()
  {
    InitElement(null);
  }

  public Element(Project project)
  {
    InitElement(project);
    var id = GetNextId();
    Id = id;
  }

  public Element(Project project, int totalIDs)
  {
    InitElement(project);
    Id = totalIDs;
  }

  /// <summary>
  ///   Get the drawing priority of this element.
  /// </summary>
  public virtual Depth Depth => Depth.Low;

  /// <summary>
  ///   Get/set whether this element is flagged. For temporary use by the Canvas when traversing elements.
  /// </summary>
  public bool Flagged { get; set; }

  /// <summary>
  ///   Get whether the element has a properties dialog which may be displayed.
  /// </summary>
  public virtual bool HasDialog => false;

  /// <summary>
  ///   Get the unique identifier of this element.
  /// </summary>
  public int Id {
    get { return _id; }
    set {
      if (Project != null)
        if (!Project.IsElementIdInUse(value))
          _id = value;
    }
  }

  public virtual string Name { get; set; }

  /// <summary>
  ///   Get the collection of ports on the element.
  /// </summary>
  [JsonIgnore]
  public List<Port> PortList { get; set; } = new();

  public virtual Vector Position { get; set; }

  [JsonIgnore] public Project Project { get; set; }

  public virtual int ZOrder {
    get { return _zOrder; }
    set {
      _zOrder = value;
      RaiseChanged();
    }
  }

  /// <summary>
  ///   Get/set whether to raise change events.
  /// </summary>
  protected bool RaiseChangedEvents { get; set; } = true;

  public int CompareTo(Element element)
  {
    var delta = Depth.CompareTo(element.Depth);
    if (delta == 0)
    {
      delta = ZOrder.CompareTo(element.ZOrder);
      if (delta == 0)
        delta = Id.CompareTo(element.Id);
    }

    return delta;
  }

  /// <summary>
  ///   Event raised when the element changes.
  /// </summary>
  public event EventHandler Changed;

  public virtual void Dispose()
  {
    //There is nothing to clean. 
    Disposed?.Invoke(this, EventArgs.Empty);
  }

  public event EventHandler Disposed;

  /// <summary>
  ///   Get the distance from this element to the given point.
  /// </summary>
  public virtual float Distance(Vector pos, bool includeMargins)
  {
    return 0.0f;
  }

  public int GetNextId()
  {
    // Build the set of used ids once (O(n)) rather than rescanning every element for each candidate id.
    var used = new HashSet<int>();
    foreach (var existing in Project.Elements)
      used.Add(existing.Id);
    var id = 1;
    while (used.Contains(id))
      ++id;
    return id;
  }

  /// <summary>
  ///   Get the position of a given port on the element.
  /// </summary>
  public virtual Vector GetPortPosition(Port port)
  {
    return new Vector(0, 0);
  }

  /// <summary>
  ///   Get the position of the end of the "stalk" on the given port; or the port position if none.
  /// </summary>
  public virtual Vector GetPortStalkPosition(Port port)
  {
    return new Vector(0, 0);
  }

  public RoomShape GetRoomType()
  {
    if (this is Room xx) return xx.Shape;
    if (this is MapLabel label) return label.Shape;
    return RoomShape.NotARoom;
  }

  public virtual Color GetToolTipColor()
  {
    return Color.Blue;
  }

  public virtual string GetToolTipFooter()
  {
    return string.Empty;
  }

  public virtual string GetToolTipHeader()
  {
    return string.Empty;
  }

  public virtual string GetToolTipText()
  {
    return string.Empty;
  }

  public virtual bool HasTooltip()
  {
    return false;
  }

  /// <summary>
  ///   Get whether this element intersects the given rectangle.
  /// </summary>
  public virtual bool Intersects(Rect rect)
  {
    return false;
  }

  /// <summary>
  ///   Enlarge the given rectangle so as to fully incorporate this element.
  /// </summary>
  public virtual Rect UnionBoundsWith(Rect rect, bool includeMargins)
  {
    return new Rect();
  }

  /// <summary>
  ///   Raise the Changed event.
  /// </summary>
  protected void RaiseChanged()
  {
    if (!RaiseChangedEvents) return;
    var changed = Changed;
    changed?.Invoke(this, EventArgs.Empty);
  }

  private void InitElement(Project project)
  {
    Project = project;
  }
}

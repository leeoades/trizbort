using System;
using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.Elements;

namespace Trizbort.Editor;

/// <summary>
///   Ordered set of selected elements. The last item is the primary element.
/// </summary>
public sealed class Selection
{
  private readonly List<Element> _items = new();

  public IReadOnlyList<Element> Items => _items;

  public Element Primary => _items.Count > 0 ? _items[_items.Count - 1] : null;

  public int Count => _items.Count;

  public IEnumerable<Room> Rooms => _items.OfType<Room>();

  public IEnumerable<Connection> Connections => _items.OfType<Connection>();

  public event EventHandler Changed;

  public bool Contains(Element element)
  {
    return element != null && _items.Contains(element);
  }

  public bool Set(IEnumerable<Element> elements)
  {
    var newItems = new List<Element>();
    if (elements != null)
    {
      foreach (var element in elements)
      {
        if (element != null && !newItems.Contains(element))
        {
          newItems.Add(element);
        }
      }
    }

    if (_items.SequenceEqual(newItems)) return false;

    _items.Clear();
    _items.AddRange(newItems);
    RaiseChanged();
    return true;
  }

  public bool Add(Element element)
  {
    if (element == null) return false;

    // Already the primary element: nothing changes.
    if (_items.Count > 0 && _items[_items.Count - 1] == element) return false;

    _items.Remove(element);
    _items.Add(element);
    RaiseChanged();
    return true;
  }

  public bool Remove(Element element)
  {
    if (element == null || !_items.Remove(element)) return false;

    RaiseChanged();
    return true;
  }

  public bool Toggle(Element element)
  {
    if (element == null) return false;

    return Contains(element) ? Remove(element) : Add(element);
  }

  public bool Clear()
  {
    if (_items.Count == 0) return false;

    _items.Clear();
    RaiseChanged();
    return true;
  }

  /// <summary>
  ///   Apply a click on an element: toggle, add to the selection, or replace the selection.
  /// </summary>
  public bool Click(Element element, bool toggle, bool add)
  {
    if (element == null) return false;

    if (toggle) return Toggle(element);
    if (add) return Add(element);
    return Set(new[] { element });
  }

  private void RaiseChanged()
  {
    var handler = Changed;
    handler?.Invoke(this, EventArgs.Empty);
  }
}

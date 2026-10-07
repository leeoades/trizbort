using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Trizbort.Domain.Misc {
  public class BoundList<T> : Collection<T> {
    public event ItemEventHandler<T> Added;

    public void AddRange<U>(IEnumerable<U> list) where U : T {
      foreach (var item in list) Add(item);
    }

    public event ItemEventHandler<T> Removed;

    public void RemoveRange<U>(IEnumerable<U> list) where U : T {
      foreach (var item in list) Remove(item);
    }

    public void Reverse() {
      var list = new List<T>(this);
      list.Reverse();
      Clear();
      foreach (var item in list) Add(item);
    }

    protected override void InsertItem(int index, T item) {
      base.InsertItem(index, item);
      RaiseAdded(item);
    }

    protected override void RemoveItem(int index) {
      var element = Items[index];
      base.RemoveItem(index);
      RaiseRemoved(element);
    }

    private void RaiseAdded(T item) {
      var added = Added;
      if (added != null) added(this, new ItemEventArgs<T>(item));
    }

    private void RaiseRemoved(T item) {
      var removed = Removed;
      if (removed != null) removed(this, new ItemEventArgs<T>(item));
    }
  }
}
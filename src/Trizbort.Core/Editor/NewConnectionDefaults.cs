using System;
using Trizbort.Domain.Elements;

namespace Trizbort.Editor;

/// <summary>Flow, line style and label that newly drawn connections start with. Not part of the undo history.</summary>
public sealed class NewConnectionDefaults
{
  private ConnectionFlow _flow = ConnectionFlow.TwoWay;
  private ConnectionStyle _style = ConnectionStyle.Solid;
  private ConnectionLabel _label = ConnectionLabel.None;

  public ConnectionFlow Flow
  {
    get => _flow;
    set
    {
      if (_flow == value) return;
      _flow = value;
      RaiseChanged();
    }
  }

  public ConnectionStyle Style
  {
    get => _style;
    set
    {
      if (_style == value) return;
      _style = value;
      RaiseChanged();
    }
  }

  public ConnectionLabel Label
  {
    get => _label;
    set
    {
      if (_label == value) return;
      _label = value;
      RaiseChanged();
    }
  }

  public event EventHandler Changed;

  private void RaiseChanged()
  {
    var handler = Changed;
    handler?.Invoke(this, EventArgs.Empty);
  }
}

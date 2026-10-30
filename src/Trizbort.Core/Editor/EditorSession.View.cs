using System;
using System.Linq;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Editor;

public sealed partial class EditorSession
{
  /// <summary>
  ///   Set by the canvas interaction: returns true while a drag (or a pending port press) owns the map. Undo and Redo
  ///   are unavailable then.
  /// </summary>
  public Func<bool> InteractionActiveProbe { get; set; }

  public bool IsInteractionActive => InteractionActiveProbe != null && InteractionActiveProbe();

  /// <summary>Distance one arrow key press moves or resizes the selection: the grid size with snap, else 2 units.</summary>
  public float ArrowStep => Settings.SnapToGrid ? Settings.GridSize : 2.0f;

  /// <summary>
  ///   Plain arrow key: moves the selection by <see cref="ArrowStep" />; with nothing selected pans the view by 1/10 of
  ///   the viewport (1/5 when <paramref name="fastPan" />). <paramref name="unit" /> is e.g. (1, 0) for right.
  /// </summary>
  public void ArrowMoveOrPan(Vector unit, bool fastPan = false)
  {
    if (Selection.Count == 0)
    {
      Viewport.PanByViewportFraction(unit, fastPan ? 0.2 : 0.1);
      return;
    }

    NudgeSelected(unit * ArrowStep);
  }

  /// <summary>Reset View: puts the map origin back at (0, 0) and keeps the current zoom (unlike Reset Zoom).</summary>
  public void ResetViewOrigin()
  {
    Viewport.SetView(Vector.Zero, Viewport.ZoomFactor);
  }

  /// <summary>Whether the mini map is shown (the persisted <c>ShowMiniMap</c> application setting).</summary>
  public bool ShowMiniMap
  {
    get => ApplicationSettingsController.AppSettings.ShowMiniMap;
    set
    {
      var settings = ApplicationSettingsController.AppSettings;
      if (settings.ShowMiniMap == value) return;

      settings.ShowMiniMap = value;
      RaiseChanged(EditorChange.Viewport);
    }
  }

  /// <summary>True when at least one selected element can be resized.</summary>
  public bool CanResizeSelection => Selection.Items.OfType<ISizeable>().Any();

  /// <summary>Primary+Alt+arrow: resize the selected rooms by one <see cref="ArrowStep" /> (never below one grid cell).</summary>
  public void ArrowResize(Vector unit)
  {
    ResizeSelected(unit * ArrowStep);
  }
}

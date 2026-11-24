using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Editor;

// Backgrounds: coloured shapes behind the map. Lee, 24 Nov 2026.
public sealed partial class EditorSession
{
  /// <summary>
  ///   Adds a background centred on <paramref name="centre" /> (or the middle of the view), selected, as one undo
  ///   step; then its properties, when <paramref name="openProperties" />, as another (cancelled, it stays as added).
  ///   Rooms selected first: it's drawn round them instead, with a grid square to spare.
  /// </summary>
  public async Task<Background> AddBackgroundAsync(Vector? centre, bool openProperties = true)
  {
    var background = new Background(Project);
    var around = SelectedRooms.Cast<Element>().Concat(Selection.Items.OfType<MapLabel>()).ToList();
    if (around.Count > 0)
    {
      var bounds = Rect.Empty;
      foreach (var element in around) bounds = element.UnionBoundsWith(bounds, false);
      bounds.Inflate(Settings.GridSize);
      background.Position = Settings.Snap(new Vector(bounds.X, bounds.Y));
      background.Size = new Vector(bounds.Width, bounds.Height);
    }
    else
    {
      var point = centre ?? Viewport.Origin;
      background.Position = Settings.Snap(new Vector(point.X - background.Size.X / 2, point.Y - background.Size.Y / 2));
    }

    // on top of the others already there (a small one inside a big one stays in view)
    var backgrounds = Project.Elements.OfType<Background>().ToList();
    if (backgrounds.Count > 0) background.ZOrder = backgrounds.Max(b => b.ZOrder) + 1;

    Execute("Add background", () => Project.Elements.Add(background), new Element[] { background });
    Selection.Set(new Element[] { background });
    Project.ActiveSelectedElement = background;
    RaiseChanged(EditorChange.Model);

    if (openProperties) await ShowPropertiesAsync(background);
    PostStatusMessage("Added background");
    return background;
  }

  /// <summary>The topmost background the point is on, if any.</summary>
  public Background BackgroundAt(Vector world)
  {
    return Project.Elements.OfType<Background>()
      .Where(background => background.Contains(world))
      .OrderBy(background => background, Comparer<Background>.Create((a, b) => a.CompareTo(b)))
      .LastOrDefault();
  }
}

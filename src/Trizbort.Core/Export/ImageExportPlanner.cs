using System;
using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Editor;
using Trizbort.Setup;

namespace Trizbort.Export;

/// <summary>The size, origin and scale of an image export.</summary>
public sealed class ImageExportPlan
{
  /// <summary>True when there is nothing to export (empty map or zero extent).</summary>
  public bool IsEmpty { get; set; }

  /// <summary>World bounds that the image covers, margins included.</summary>
  public Rect Bounds { get; set; }

  /// <summary>Pixel width of the image.</summary>
  public int Width { get; set; }

  /// <summary>Pixel height of the image.</summary>
  public int Height { get; set; }

  /// <summary>Scale (pixels per world unit) after clamping to the size limits.</summary>
  public double Scale { get; set; }

  /// <summary>The elements to draw, or null to draw the whole map.</summary>
  public IReadOnlyCollection<Element> Elements { get; set; }

  /// <summary>True when only the selection is exported.</summary>
  public bool SelectionOnly { get; set; }

  /// <summary>An explanation for the user when the plan differs from the request, otherwise null.</summary>
  public string Message { get; set; }
}

/// <summary>Works out how big an exported image is and which part of the map it shows.</summary>
public static class ImageExportPlanner
{
  public const int MaxDimension = 8192;
  public const int MinDimension = 16;

  /// <summary>Approximation of the legacy <c>Font.GetHeight()</c> line height relative to the font size.</summary>
  public const float LineHeightFactor = 1.2f;

  public const string EmptySelectionMessage = "Nothing is selected, so the whole map was exported.";

  public static ImageExportPlan Plan(
    Project project,
    Selection selection,
    Viewport viewport,
    ImageExportOptions options,
    ApplicationSettings settings)
  {
    var plan = new ImageExportPlan { IsEmpty = true };
    if (project == null || project.Elements.Count == 0) return plan;

    options ??= new ImageExportOptions();
    settings ??= new ApplicationSettings();

    IReadOnlyCollection<Element> subset = null;
    if (options.SelectionOnly)
    {
      subset = SelectedSubset(project, selection);
      if (subset.Count == 0)
      {
        subset = null;
        plan.Message = EmptySelectionMessage;
      }
    }

    var elements = subset ?? (IEnumerable<Element>)project.Elements;
    var bounds = PadBounds(UnionBounds(elements), settings);
    if (bounds == Rect.Empty || bounds.Width <= 0 || bounds.Height <= 0) return plan;

    var requested = options.Scale;
    if (requested <= 0)
    {
      var zoom = viewport != null ? viewport.ZoomFactor : 1.0;
      requested = settings.SaveAt100 ? 1.0 : zoom;
    }

    if (requested <= 0) requested = 1.0;

    var effective = ClampScale(bounds, requested, out var width, out var height);
    plan.IsEmpty = false;
    plan.Bounds = bounds;
    plan.Width = width;
    plan.Height = height;
    plan.Scale = effective;
    plan.Elements = subset;
    plan.SelectionOnly = subset != null;
    return plan;
  }

  /// <summary>
  ///   Scales <paramref name="bounds" /> by <paramref name="requested" />, shrinking proportionally so neither side
  ///   exceeds <see cref="MaxDimension" /> and never going below <see cref="MinDimension" /> pixels.
  /// </summary>
  public static double ClampScale(Rect bounds, double requested, out int width, out int height)
  {
    var targetW = bounds.Width * requested;
    var targetH = bounds.Height * requested;
    var fit = Math.Min(1.0, Math.Min(MaxDimension / targetW, MaxDimension / targetH));
    var effective = requested * fit;

    width = Math.Clamp((int)Math.Ceiling(bounds.Width * effective), MinDimension, MaxDimension);
    height = Math.Clamp((int)Math.Ceiling(bounds.Height * effective), MinDimension, MaxDimension);

    // Recompute against the clamped pixel size so the whole of the bounds fits inside the image.
    return Math.Min((double)width / bounds.Width, (double)height / bounds.Height);
  }

  /// <summary>
  ///   Selected rooms and labels, plus selected connections and connections whose ends are both selected rooms.
  /// </summary>
  public static IReadOnlyCollection<Element> SelectedSubset(Project project, Selection selection)
  {
    var result = new List<Element>();
    if (project == null || selection == null || selection.Count == 0) return result;

    var selected = new HashSet<Element>(selection.Items);
    foreach (var element in project.Elements)
    {
      if (element is Connection connection)
      {
        var source = connection.GetSourceRoom();
        var target = connection.GetTargetRoom();
        var bothEnds = source != null && target != null && selected.Contains(source) && selected.Contains(target);
        if (selected.Contains(connection) || bothEnds) result.Add(connection);
      }
      else if (selected.Contains(element))
      {
        result.Add(element);
      }
    }

    return result;
  }

  public static Rect UnionBounds(IEnumerable<Element> elements)
  {
    if (elements == null) return Rect.Empty;
    var bounds = Rect.Empty;
    var any = false;
    foreach (var element in elements)
    {
      bounds = element.UnionBoundsWith(bounds, true);
      any = true;
    }

    return any ? bounds : Rect.Empty;
  }

  /// <summary>
  ///   Adds the export margin as the legacy canvas does: the map's own margins when enabled, otherwise the
  ///   application's general margins when enabled, otherwise a fudge allowing for overhanging text.
  /// </summary>
  public static Rect PadBounds(Rect bounds, ApplicationSettings settings)
  {
    if (bounds == Rect.Empty) return bounds;

    if (Settings.DocumentSpecificMargins)
    {
      bounds.Inflate(Settings.DocHorizontalMargin, Settings.DocVerticalMargin);
    }
    else if (settings != null && settings.SpecifyGenMargins)
    {
      bounds.Inflate(settings.GenHorizontalMargin, settings.GenVerticalMargin);
    }
    else
    {
      bounds.Inflate(DefaultPadding());
    }

    return bounds;
  }

  /// <summary>The legacy overhang allowance: the larger of the subtitle line height and four object line heights.</summary>
  public static float DefaultPadding()
  {
    var subtitle = (Settings.SubtitleFontMetadata?.Size ?? 9f) * LineHeightFactor;
    var objects = (Settings.ObjectFontMetadata?.Size ?? 11f) * LineHeightFactor * 4;
    return Math.Max(subtitle, objects);
  }
}

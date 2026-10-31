namespace Trizbort.Export;

/// <summary>What the user asked for when exporting the map as an image.</summary>
public sealed class ImageExportOptions
{
  /// <summary>
  ///   Scale factor (1.0 = 100%). Zero or less means "use the application setting": 100% when
  ///   <c>SaveAt100</c> is on, otherwise the current canvas zoom.
  /// </summary>
  public double Scale { get; set; }

  /// <summary>Export only the selected elements (falls back to the whole map when nothing is selected).</summary>
  public bool SelectionOnly { get; set; }

  public ImageFormat Format { get; set; } = ImageFormat.Png;
}

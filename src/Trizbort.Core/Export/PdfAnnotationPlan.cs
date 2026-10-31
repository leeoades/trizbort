using System;
using System.Collections.Generic;
using System.Linq;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

namespace Trizbort.Export;

/// <summary>One note annotation for the PDF: a square in PDF page coordinates (origin bottom left) and its text.</summary>
public sealed class PdfAnnotationEntry
{
  /// <summary>Left edge in points.</summary>
  public double X { get; set; }

  /// <summary>Bottom edge in points (PDF Y axis points up).</summary>
  public double Y { get; set; }

  public double Width { get; set; }

  public double Height { get; set; }

  /// <summary>The room's primary description.</summary>
  public string Text { get; set; }
}

/// <summary>
///   Works out where the room-description note annotations go in an exported PDF (ticket 124). Mirrors the legacy
///   <c>MapPdfExporter</c>: one square per room that has a description, a quarter of the room's shorter side, centred
///   horizontally on the room unless the room has square corners (then at its left edge), flipped to PDF coordinates.
/// </summary>
public static class PdfAnnotationPlan
{
  /// <summary>
  ///   Builds the annotation list.
  /// </summary>
  /// <param name="project">The project whose rooms are annotated.</param>
  /// <param name="mapBounds">World bounds the PDF page covers (the export plan bounds).</param>
  /// <param name="scale">Page points per world unit (1 when the page is at 100%).</param>
  /// <param name="pageHeight">Height of the PDF page in points; used to flip Y.</param>
  public static IReadOnlyList<PdfAnnotationEntry> Build(Project project, Rect mapBounds, double scale, double pageHeight)
  {
    var result = new List<PdfAnnotationEntry>();
    if (project == null) return result;

    foreach (var room in project.Elements.OfType<Room>().Where(room => room.HasDescription))
    {
      var side = Math.Min(room.Width, room.Height) / 4.0;
      var x = room.X - mapBounds.Left + (room.Shape == RoomShape.SquareCorners ? 0.0 : room.Width / 2.0 - side / 2.0);
      var top = room.Y - mapBounds.Top;
      result.Add(new PdfAnnotationEntry
      {
        X = x * scale,
        Y = pageHeight - (top + side) * scale,
        Width = side * scale,
        Height = side * scale,
        Text = room.PrimaryDescription
      });
    }

    return result;
  }
}

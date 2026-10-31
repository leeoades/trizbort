using System.Threading.Tasks;
using Trizbort.Domain.Application;

namespace Trizbort.Export;

/// <summary>Writes the map as a single-page PDF. The implementation lives in the UI project (it draws the map).</summary>
public interface IMapPdfWriter
{
  /// <summary>
  ///   Writes the whole map of <paramref name="project" /> to <paramref name="path" />. Throws when the map is empty
  ///   or the file cannot be written.
  /// </summary>
  Task WriteAsync(Project project, string path, PdfMetadata metadata);
}

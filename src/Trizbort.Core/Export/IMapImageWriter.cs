using System.Threading.Tasks;
using Trizbort.Domain.Application;

namespace Trizbort.Export;

/// <summary>Writes the whole map as a raster image. The implementation lives in the UI project (it draws the map).</summary>
public interface IMapImageWriter
{
  /// <summary>Writes the whole map of <paramref name="project" /> to <paramref name="path" />. Throws when the map is empty or the file cannot be written.</summary>
  Task WriteAsync(Project project, string path, ImageFormat format);
}

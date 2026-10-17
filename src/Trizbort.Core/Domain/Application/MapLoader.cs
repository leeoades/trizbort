using System;
using System.IO;

namespace Trizbort.Domain.Application;

public class MapLoader
{
  private readonly MapFileEngine _loader;
  private readonly Action<string> _reportUnknownFile;

  public static Action<string> DefaultUnknownFileReporter { get; set; }

  public MapLoader(Project project) : this(
    project,
    message => DefaultUnknownFileReporter?.Invoke(message))
  {
  }

  public MapLoader(Project project, Action<string> reportUnknownFile, MapFileEngine loader = null)
  {
    _loader = loader ?? new LegacyMapFileEngine(project);
    _reportUnknownFile = reportUnknownFile ?? (message => DefaultUnknownFileReporter?.Invoke(message));
  }

  public bool LoadMap(string fileName)
  {
    if (Path.GetExtension(fileName) == ".trizbort") return _loader.Load(fileName);

    _reportUnknownFile($"'{fileName}' is not a known Trizbort file.");
    return false;
  }

  public bool LoadMap(Uri url)
  {
    return LoadMap(url.AbsoluteUri);
  }
}

using System.IO;

namespace Trizbort.Domain.Application;

public class MapSaver {
  private readonly Project _project;
  private MapFileEngine _engine;

  public MapSaver(Project project)
  {
    _project = project;
  }

  public bool SaveMap(string fileName)
  {
    if (Path.GetExtension(fileName) == ".trizbort") {
      _engine = new LegacyMapFileEngine(_project);
      return _engine.Save(fileName);
    }

    return false;
  }
}
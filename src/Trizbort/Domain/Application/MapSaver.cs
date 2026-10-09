using System.IO;

namespace Trizbort.Domain.Application {
  public class MapSaver {
    private MapFileEngine _engine;
    private readonly Project _project;

    public MapSaver(Project project) {
      this._project = project;
    }

    public bool SaveMap(string fileName) {
      if (Path.GetExtension(fileName) == ".trizbort")
      {
          _engine = new LegacyMapFileEngine(_project);
          return _engine.Save(fileName);
      }
      return false;
    }
  }
}
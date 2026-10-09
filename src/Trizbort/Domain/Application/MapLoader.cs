using Trizbort.UI;
using System;
using System.IO;
using System.Windows.Forms;

namespace Trizbort.Domain.Application {
  public class MapLoader {
    private readonly MapFileEngine _loader;
    private readonly Action<string> _reportUnknownFile;

    public MapLoader(Project project) : this(project, message => UserInteraction.ShowMessage(message, "Not a valid file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)) { }

    internal MapLoader(Project project, Action<string> reportUnknownFile, MapFileEngine loader = null) {
      this._loader = loader ?? new LegacyMapFileEngine(project);
      this._reportUnknownFile = reportUnknownFile;
    }

    public bool LoadMap(string fileName) {
      if (Path.GetExtension(fileName) == ".trizbort") {
        return _loader.Load(fileName);
      }

      _reportUnknownFile($"'{fileName}' is not a known Trizbort file.");
      return false;
    }

    public bool LoadMap(Uri url) {

      return LoadMap(url.AbsoluteUri);
    }
  }
}
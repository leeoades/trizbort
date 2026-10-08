using Trizbort.UI;
using System;
using System.IO;
using System.Windows.Forms;
using Trizbort.Setup;

namespace Trizbort.Domain.Application {
  public class MapLoader {
    private readonly MapFileEngine loader;
    private readonly Action<string> reportUnknownFile;

    public MapLoader(Project project) : this(project, message => UserInteraction.ShowMessage(message, "Not a valid file", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)) { }

    internal MapLoader(Project project, Action<string> reportUnknownFile, MapFileEngine loader = null) {
      this.loader = loader ?? new LegacyMapFileEngine(project);
      this.reportUnknownFile = reportUnknownFile;
    }

    public bool LoadMap(string fileName) {
      
      // empty file
      if (isEmptyFile(fileName)) {
        Settings.Reset();
        return false;
      }

      if (Path.GetExtension(fileName) == ".trizbort") {
        return loader.Load(fileName);
      }

      reportUnknownFile($"'{fileName}' is not a known Trizbort file.");
      return false;
    }

    private bool isEmptyFile(string fileName) {
      if (Uri.IsWellFormedUriString(fileName, UriKind.RelativeOrAbsolute))
        return false;

      return new FileInfo(fileName).Length == 0;
    }

    public bool LoadMap(Uri url) {

      return LoadMap(url.AbsoluteUri);
    }
  }
}
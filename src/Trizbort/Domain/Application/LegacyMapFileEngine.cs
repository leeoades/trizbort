using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using Trizbort.Domain.Elements;
using Trizbort.Setup;
using Trizbort.UI;
using Trizbort.Util;

namespace Trizbort.Domain.Application;

public class LegacyMapFileEngine : MapFileEngine
{
  private readonly Action<Project> _checkVersion;
  private readonly Project _project;
  private readonly Action<Exception> _reportError;
  private readonly Action<string, string> _reportWarning;

  public LegacyMapFileEngine(Project project) : this(project, ShowError, loaded => loaded.CheckDocVersion())
  {
  }

  internal LegacyMapFileEngine(
    Project project,
    Action<Exception> reportError,
    Action<Project> checkVersion,
    Action<string, string> reportWarning = null)
  {
    _project = project;
    _reportError = reportError;
    _checkVersion = checkVersion;
    _reportWarning = reportWarning ?? ((message, title) => UserInteraction.ShowMessage(message, title));
  }

  public override bool Load(string fileName)
  {
    try
    {
      var isLocalFile = File.Exists(fileName);
      if (isLocalFile && new FileInfo(fileName).Length == 0)
      {
        // this is an empty file, probably thanks to our Explorer New->Trizbort Map menu option.
        Settings.Reset(false);
        _project.Title = _project.Author = _project.History = _project.Description = "";
        _project.InitFileWWatcher(Path.GetFullPath(fileName));
        return true;
      }

      var doc = new XmlDocument();
      doc.Load(fileName);
      var root = new XmlElementReader(doc.DocumentElement);

      if (!root.HasName("trizbort"))
        throw new InvalidDataException($"Not a {System.Windows.Forms.Application.ProductName} map file.");

      //reset checks: we may make this into a function if we ever wish to verify a Trizbort file first.
      Settings.StartRoomLoaded = false;
      Settings.EndRoomLoaded = false;

      // file version
      var versionNumber = root.Attribute("version").Text;
      _project.SetVersion(versionNumber);
      _checkVersion(_project);

      // load info
      _project.Title = root["info"]["title"].Text;
      _project.Author = root["info"]["author"].Text;
      _project.Description = root["info"]["description"].Text;
      _project.History = root["info"]["history"].Text;

      // load all elements
      var map = root["map"];
      var mapConnectionToLoadState = new Dictionary<Connection, object>();
      foreach (var element in map.Children)
        if (element.HasName("room"))
        {
          // Changed the constructor used for elements when loading a file for a significant speed increase
          var room = new Room(_project, _project.Elements.Count + 1);
          room.Id = element.Attribute("id").ToInt(room.Id);
          room.Load(element, _reportWarning);
          _project.Elements.Add(room);
        }
        else if (element.HasName("label"))
        {
          var label = new MapLabel(_project, _project.Elements.Count + 1);
          label.Id = element.Attribute("id").ToInt(label.Id);
          label.Load(element);
          _project.Elements.Add(label);
        }
        else if (element.HasName("line") || element.HasName("labelLine"))
        {
          // Changed the constructor used for elements when loading a file for a significant speed increase
          var connection = new Connection(_project, _project.Elements.Count + 1);
          connection.Id = element.Attribute("id").ToInt(connection.Id);
          var loadState = connection.BeginLoad(element);
          if (loadState != null)
            mapConnectionToLoadState.Add(connection, loadState);
          _project.Elements.Add(connection);
        }

      // connect them together
      foreach (var pair in mapConnectionToLoadState)
      {
        var connection = pair.Key;
        var state = pair.Value;
        connection.EndLoad(state);
      }

      // load settings last, since their load can't be undone
      Settings.Reset(false);
      Settings.Load(root["settings"]);

      // setup filewatcher.
      if (isLocalFile)
        _project.InitFileWWatcher(Path.GetFullPath(fileName));

      return true;
    }
    catch (Exception ex)
    {
      _reportError(ex);
      return false;
    }
  }

  private static void ShowError(Exception ex)
  {
    UserInteraction.ShowMessage(
      Program.MainForm,
      $"There was a problem loading the map:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
      System.Windows.Forms.Application.ProductName,
      MessageBoxButtons.OK,
      MessageBoxIcon.Error);
  }

  public override bool Save(string fileName)
  {
    using var scribe = XmlScribe.Create(fileName);
    scribe.StartElement("trizbort");
    scribe.Attribute("version", typeof(Project).Assembly.GetName().Version.ToString());
    scribe.StartElement("info");
    if (!string.IsNullOrEmpty(_project.Title))
      scribe.Element("title", _project.Title);
    if (!string.IsNullOrEmpty(_project.Author))
      scribe.Element("author", _project.Author);
    if (!string.IsNullOrEmpty(_project.Description))
      scribe.Element("description", _project.Description);
    if (!string.IsNullOrEmpty(_project.History))
      scribe.Element("history", _project.History);
    scribe.EndElement();
    scribe.StartElement("map");
    foreach (var element in _project.Elements)
      SaveElement(scribe, element);
    scribe.EndElement();
    scribe.StartElement("settings");
    Settings.Save(scribe);
    scribe.EndElement();
    return true;
  }

  private void SaveElement(XmlScribe scribe, Element element)
  {
    if (element.GetType() == typeof(Room))
    {
      scribe.StartElement("room");
      scribe.Attribute("id", element.Id);
      ((Room)element).Save(scribe);
      scribe.EndElement();
    }
    else if (element is MapLabel label)
    {
      scribe.StartElement("label");
      scribe.Attribute("id", label.Id);
      label.Save(scribe);
      scribe.EndElement();
    }
    else if (element.GetType() == typeof(Connection))
    {
      var connection = (Connection)element;
      // Older readers must ignore label connectors as well as the labels themselves.
      scribe.StartElement(connection.VertexList.Any(vertex => vertex.Port?.Owner is MapLabel) ? "labelLine" : "line");
      scribe.Attribute("id", element.Id);
      ((Connection)element).Save(scribe);
      scribe.EndElement();
    }
  }
}
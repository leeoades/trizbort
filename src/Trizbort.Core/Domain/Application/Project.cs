using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Domain.Watchers;
using Trizbort.Extensions;

namespace Trizbort.Domain.Application;

public class Project : IDisposable
{
  public static readonly string FilterString = "Trizbort Map Files|*.trizbort";

  public static readonly TrizbortFileWatcher FileWatcher = new();

  private static Project _current = new();

  public static Action<DocumentVersionWarning, Version, Version> DocVersionWarningReporter { get; set; }
  public static Action<string, bool> BackupReporter { get; set; }
  public static Action<Exception> SaveErrorReporter { get; set; }
  public static Action<string> MapReloader { get; set; }
  public static Func<IEnumerable<Element>> SelectedElementsProvider { get; set; }

  public Project()
  {
    Elements.Removed += OnElementRemoved;
  }

  public Element ActiveSelectedElement { get; set; }

  public string Author { get; set; }

  public List<Element> GetSelectedElements()
  {
    return SelectedElementsProvider?.Invoke().ToList() ?? new List<Element>();
  }

  public static Project Current {
    get { return _current; }
    set {
      if (_current == value) return;
      var oldProject = _current;
      // The old project must stop listening for external file changes, and an unsaved project has no file to watch.
      oldProject?.Dispose();
      if (value != null && !value.HasFileName) FileWatcher.StopWatcher();
      _current = value;
      RaiseProjectChanged(oldProject, _current);
    }
  }

  public string Description { get; set; }

  public BoundList<Element> Elements { get; } = new();

  public string FileName { get; set; } = string.Empty;

  public bool HasFileName => !string.IsNullOrEmpty(FileName);

  public string History { get; set; }

  public bool IsDirty { get; set; }

  public bool MustHaveDescription { get; set; }
  public bool MustHaveNoDanglingConnectors { get; set; }
  public bool MustHaveSubtitle { get; set; }
  public bool MustHaveUniqueNames { get; set; }

  public string Name => !HasFileName ? "Untitled" : Path.GetFileNameWithoutExtension(FileName);

  public string Title { get; set; }

  public Version Version { get; private set; }

  public void Dispose()
  {
    FileWatcher.ReloadMap -= ReloadMap;
  }

  public bool AreRoomsConnected(List<Room> selectedRooms)
  {
    if (selectedRooms.Count < 2) return false;
    if (selectedRooms.Count == 2)
    {
      var room1 = selectedRooms.First();
      var room2 = selectedRooms.Last();

      if (room1.IsConnected && room2.IsConnected)
      {
        var con = room1.GetConnections();
        foreach (var connection in con)
        {
          if (connection.GetSourceRoom() == room1)
            if (connection.GetTargetRoom() == room2)
              return true;

          if (connection.GetTargetRoom() == room1)
            if (connection.GetSourceRoom() == room2)
              return true;
        }
      }

      return false;
    }

    return false;
  }

  public string Backup()
  {
    if (HasFileName)
    {
      var nextAvailableFilename = FileName.NextAvailableFilename();
      File.Copy(FileName, nextAvailableFilename);
      BackupReporter?.Invoke(nextAvailableFilename, true);
      return nextAvailableFilename;
    }

    BackupReporter?.Invoke(null, false);
    return null;
  }

  public void CheckDocVersion()
  {
    var appVers = typeof(Project).Assembly.GetName().Version;
    var warning = DocumentVersionPolicy.Compare(Version, appVers);
    if (warning == DocumentVersionWarning.None) return;
    DocVersionWarningReporter?.Invoke(warning, appVers, Version);
  }

  public bool FindElement(int id, out Element element)
  {
    foreach (var existing in Elements)
      if (existing.Id == id)
      {
        element = existing;
        return true;
      }

    element = null;
    return false;
  }

  /// <summary>
  ///   Carry over the state that is not part of the map file when a snapshot is restored into a fresh project:
  ///   the file name (and its reload subscription) and the validation flags.
  /// </summary>
  internal void CopyTransientStateFrom(Project other)
  {
    FileName = other.FileName;
    MustHaveDescription = other.MustHaveDescription;
    MustHaveNoDanglingConnectors = other.MustHaveNoDanglingConnectors;
    MustHaveSubtitle = other.MustHaveSubtitle;
    MustHaveUniqueNames = other.MustHaveUniqueNames;
    if (HasFileName)
    {
      FileWatcher.ReloadMap -= ReloadMap;
      FileWatcher.ReloadMap += ReloadMap;
    }
  }

  public void InitFileWWatcher(string fileName)
  {
    FileWatcher.ReloadMap -= ReloadMap;
    FileWatcher.ReloadMap += ReloadMap;
    FileWatcher.InitializeWatcher(fileName);
  }

  public bool IsElementIdInUse(int id)
  {
    Element element;
    return FindElement(id, out element);
  }

  public static event ProjectChangedEventHandler ProjectChanged;

  public bool Save(bool reload = false)
  {
    FileWatcher.StopWatcher();

    try
    {
      var saver = new MapSaver(this);
      if (saver.SaveMap(FileName))
      {
        if (reload) InitFileWWatcher(FileName);
        IsDirty = false;
        FileWatcher.StartWatcher();
        return true;
      }

      return false;
    }
    catch (Exception ex)
    {
      SaveErrorReporter?.Invoke(ex);
      FileWatcher.StartWatcher();
      return false;
    }
  }

  public void SetVersion(string versionNumber)
  {
    try
    {
      Version = Version.Parse(versionNumber);
    }
    catch (Exception)
    {
      Version = new Version(0, 0, 0, 0);
    }
  }

  private void OnElementRemoved(object sender, ItemEventArgs<Element> e)
  {
    var doomed = new List<Element>();
    foreach (var element in Elements.OfType<Connection>())
    {
      var connection = element;
      foreach (var vertex in connection.VertexList)
        if (vertex.Port != null && vertex.Port.Owner == e.Item)
          doomed.Add(element);
    }

    foreach (var element in doomed)
      Elements.Remove(element);
  }

  private static void RaiseProjectChanged(Project oldProject, Project newProject)
  {
    var projectChanged = ProjectChanged;
    projectChanged?.Invoke(null, new ProjectChangedEventArgs(oldProject, newProject));
  }

  private void ReloadMap(object sender, EventArgs e)
  {
    try
    {
      MapReloader?.Invoke(FileName);
    }
    finally
    {
      FileWatcher.StartWatcher();
    }
  }

  public bool Load()
  {
    FileWatcher.ReloadMap -= ReloadMap;

    var loader = new MapLoader(this);
    return loader.LoadMap(FileName);
  }

  internal bool Load(Uri uri)
  {
    FileWatcher.ReloadMap -= ReloadMap;

    var loader = new MapLoader(this);
    return loader.LoadMap(uri);
  }
}

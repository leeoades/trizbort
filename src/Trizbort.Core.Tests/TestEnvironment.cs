using System;
using System.IO;
using System.Threading;
using System.Xml;
using Newtonsoft.Json;
using NUnit.Framework;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Setup;
using Trizbort.Util;

namespace Trizbort.Core.Tests;

[SetUpFixture]
public class TestEnvironment
{
  private TemporaryDirectory _directory;
  private string _previousDirectory;

  [OneTimeSetUp]
  public void Start()
  {
    _previousDirectory = Environment.CurrentDirectory;
    _directory = new TemporaryDirectory();
    Environment.CurrentDirectory = _directory.Path;
    // Prevent legacy user-settings migration when the controller initializes.
    File.WriteAllText(Path.Combine(_directory.Path, "appsettings.json"), "{}");
    ApplicationSettingsController.ResetSettings();
  }

  [OneTimeTearDown]
  public void Stop()
  {
    Project.FileWatcher.StopWatcher();
    Environment.CurrentDirectory = _previousDirectory;
    _directory.Dispose();
  }
}

internal sealed class TemporaryDirectory : IDisposable
{
  public TemporaryDirectory()
  {
    Directory.CreateDirectory(Path);
  }

  public string Path { get; } = System.IO.Path.Combine(
    System.IO.Path.GetTempPath(),
    "Trizbort.Core.Tests",
    Guid.NewGuid().ToString("N"));

  public void Dispose()
  {
    Directory.Delete(Path, true);
  }

  public string File(string name)
  {
    return System.IO.Path.Combine(Path, name);
  }
}

[NonParallelizable]
public abstract class IsolatedProjectTests
{
  private string _defaultRoomName;
  private float _dragDistance;
  private string _previousAppSettings;
  private Project _previousProject;

  private bool _startLoaded,
    _endLoaded,
    _wrappingChanged;

  private protected TemporaryDirectory Files { get; private set; }

  [SetUp]
  public void Isolate()
  {
    Files = new TemporaryDirectory();
    _previousProject = Project.Current;
    _previousAppSettings = JsonConvert.SerializeObject(ApplicationSettingsController.AppSettings);
    _defaultRoomName = Settings.DefaultRoomName;
    _startLoaded = Settings.StartRoomLoaded;
    _endLoaded = Settings.EndRoomLoaded;
    _wrappingChanged = Settings.WrappingChanged;
    _dragDistance = Settings.DragDistanceToInitiateNewConnection;
    using (var scribe = XmlScribe.Create(Files.File("settings.xml")))
    {
      scribe.StartElement("settings");
      Settings.Save(scribe);
      scribe.EndElement();
    }

    Project.Current = new Project();
    Settings.Reset(false);
    ApplicationSettingsController.ResetSettings();
    ApplicationSettingsController.AppSettings.ShowDescriptionsInTooltips = false;
    ApplicationSettingsController.AppSettings.ShowObjectsInTooltips = false;
  }

  [TearDown]
  public void Restore()
  {
    Project.FileWatcher.StopWatcher();
    Project.Current.Dispose();
    Project.Current = _previousProject;
    var document = new XmlDocument();
    document.Load(Files.File("settings.xml"));
    Settings.Reset(false);
    Settings.Load(new XmlElementReader(document.DocumentElement));
    Settings.DefaultRoomName = _defaultRoomName;
    Settings.StartRoomLoaded = _startLoaded;
    Settings.EndRoomLoaded = _endLoaded;
    Settings.WrappingChanged = _wrappingChanged;
    Settings.DragDistanceToInitiateNewConnection = _dragDistance;
    ApplicationSettingsController.AppSettings.RecentProjects.Clear();
    JsonConvert.PopulateObject(
      _previousAppSettings,
      ApplicationSettingsController.AppSettings,
      new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
    Files.Dispose();
  }
}

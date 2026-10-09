using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using NUnit.Framework;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Setup;
using Trizbort.Util;
using Trizbort.UI;
using System.Windows.Forms;

namespace Trizbort.Tests {
  [SetUpFixture]
  public class TestEnvironment {
    private string previousDirectory;
    private TemporaryDirectory directory;
    private IUserInteraction previousInteraction;

    [OneTimeSetUp]
    public void Start() {
      previousDirectory = Environment.CurrentDirectory;
      directory = new TemporaryDirectory();
      previousInteraction = UserInteraction.Current;
      UserInteraction.Current = new UnexpectedUserInteraction();
      Environment.CurrentDirectory = directory.Path;
      // Prevent legacy user-settings migration when the controller initializes.
      File.WriteAllText(Path.Combine(directory.Path, "appsettings.json"), "{}");
      ApplicationSettingsController.ResetSettings();
    }

    [OneTimeTearDown]
    public void Stop() {
      Project.FileWatcher.StopWatcher();
      UserInteraction.Current = previousInteraction;
      Environment.CurrentDirectory = previousDirectory;
      directory.Dispose();
    }
  }

  internal sealed class UnexpectedUserInteraction : IUserInteraction {
    public DialogResult ShowMessage(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon,
      MessageBoxDefaultButton defaultButton) =>
      throw new AssertionException("Unexpected message box: " + caption + ": " + text);
    public DialogResult ShowDialog(Form dialog, IWin32Window owner) =>
      throw new AssertionException("Unexpected form dialog: " + dialog.GetType().Name);
    public DialogResult ShowDialog(CommonDialog dialog, IWin32Window owner) =>
      throw new AssertionException("Unexpected common dialog: " + dialog.GetType().Name);
    public string GetClipboardText() => throw new AssertionException("Unexpected system clipboard read");
    public void SetClipboardText(string text, TextDataFormat format) =>
      throw new AssertionException("Unexpected system clipboard write");
  }

  internal static class TestDialog {
    public static DialogResult Show(Form dialog) {
      dialog.ShowInTaskbar = false;
      dialog.Opacity = 0;
      return dialog.ShowDialog();
    }
  }

  internal sealed class TemporaryDirectory : IDisposable {
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Trizbort.Tests", Guid.NewGuid().ToString("N"));

    public TemporaryDirectory() {
      Directory.CreateDirectory(Path);
    }

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose() {
      Directory.Delete(Path, true);
    }
  }

  [Apartment(ApartmentState.STA)]
  [NonParallelizable]
  public abstract class IsolatedProjectTests {
    private Project previousProject;
    private string previousAppSettings;
    private IUserInteraction previousInteraction;
    private string defaultRoomName;
    private bool startLoaded, endLoaded, wrappingChanged;
    private float dragDistance;
    private protected TemporaryDirectory Files { get; private set; }

    [SetUp]
    public void Isolate() {
      Files = new TemporaryDirectory();
      previousInteraction = UserInteraction.Current;
      previousProject = Project.Current;
      previousAppSettings = JsonConvert.SerializeObject(ApplicationSettingsController.AppSettings);
      defaultRoomName = Settings.DefaultRoomName;
      startLoaded = Settings.StartRoomLoaded;
      endLoaded = Settings.EndRoomLoaded;
      wrappingChanged = Settings.WrappingChanged;
      dragDistance = Settings.DragDistanceToInitiateNewConnection;
      using (var scribe = XmlScribe.Create(Files.File("settings.xml"))) {
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
    public void Restore() {
      Project.FileWatcher.StopWatcher();
      UserInteraction.Current = previousInteraction;
      Project.Current.Dispose();
      Project.Current = previousProject;
      var document = new System.Xml.XmlDocument();
      document.Load(Files.File("settings.xml"));
      Settings.Reset(false);
      Settings.Load(new XmlElementReader(document.DocumentElement));
      Settings.DefaultRoomName = defaultRoomName;
      Settings.StartRoomLoaded = startLoaded;
      Settings.EndRoomLoaded = endLoaded;
      Settings.WrappingChanged = wrappingChanged;
      Settings.DragDistanceToInitiateNewConnection = dragDistance;
      ApplicationSettingsController.AppSettings.RecentProjects.Clear();
      JsonConvert.PopulateObject(previousAppSettings, ApplicationSettingsController.AppSettings,
        new JsonSerializerSettings {ObjectCreationHandling = ObjectCreationHandling.Replace});
      Files.Dispose();
    }
  }
}

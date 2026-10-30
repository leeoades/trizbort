using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.AppSettings;

namespace Trizbort.Core.Tests;

[TestFixture]
[NonParallelizable]
public class AppPathsTests
{
  private TemporaryDirectory _directory;
  private string _previousDirectory;
  private string _previousOverride;
  private string _previousSettings;

  [SetUp]
  public void SetUp()
  {
    _directory = new TemporaryDirectory();
    _previousDirectory = Environment.CurrentDirectory;
    _previousOverride = AppPaths.OverrideConfigFolder;
    _previousSettings = JsonConvert.SerializeObject(ApplicationSettingsController.AppSettings);
    Environment.CurrentDirectory = _directory.Path;
    AppPaths.OverrideConfigFolder = _directory.File("config");
  }

  [TearDown]
  public void TearDown()
  {
    Environment.CurrentDirectory = _previousDirectory;
    AppPaths.OverrideConfigFolder = _previousOverride;
    JsonConvert.PopulateObject(
      _previousSettings,
      ApplicationSettingsController.AppSettings,
      new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
    _directory.Dispose();
  }

  [Test]
  public void Windows_uses_roaming_app_data()
  {
    AppPaths.ResolveConfigFolder(true, false, () => Path.Combine("base", "Roaming"), _ => null)
      .ShouldBe(Path.Combine("base", "Roaming", "Trizbort"));
  }

  [Test]
  public void MacOS_uses_application_support()
  {
    AppPaths.ResolveConfigFolder(false, true, () => "home", _ => null)
      .ShouldBe(Path.Combine("home", "Library", "Application Support", "Trizbort"));
  }

  [Test]
  public void Linux_honours_xdg_config_home()
  {
    var xdg = Path.Combine(Path.GetTempPath(), "xdg");
    AppPaths.ResolveConfigFolder(false, false, () => "home", name => name == "XDG_CONFIG_HOME" ? xdg : null)
      .ShouldBe(Path.Combine(xdg, "trizbort"));
  }

  [Test]
  public void Linux_falls_back_to_dot_config()
  {
    AppPaths.ResolveConfigFolder(false, false, () => "home", _ => "")
      .ShouldBe(Path.Combine("home", ".config", "trizbort"));
  }

  [Test]
  public void Override_is_used_and_folder_is_created()
  {
    AppPaths.ConfigFolder.ShouldBe(_directory.File("config"));
    Directory.Exists(_directory.File("config")).ShouldBeTrue();
    AppPaths.SettingsFilePath.ShouldBe(Path.Combine(_directory.File("config"), "appsettings.json"));
  }

  [Test]
  public void Existing_file_in_config_folder_is_loaded()
  {
    File.WriteAllText(AppPaths.SettingsFilePath, "{\"InvertMouseWheel\": true}");

    ApplicationSettingsController.LoadSettings();

    ApplicationSettingsController.AppSettings.InvertMouseWheel.ShouldBeTrue();
  }

  [Test]
  public void Working_directory_file_is_imported_once_when_new_file_is_absent()
  {
    var legacyFile = _directory.File("appsettings.json");
    File.WriteAllText(legacyFile, "{\"InvertMouseWheel\": true}");

    ApplicationSettingsController.LoadSettings();

    ApplicationSettingsController.AppSettings.InvertMouseWheel.ShouldBeTrue();
    File.Exists(AppPaths.SettingsFilePath).ShouldBeTrue();

    // The new file now wins over the working-directory file.
    File.WriteAllText(legacyFile, "{\"InvertMouseWheel\": false}");
    ApplicationSettingsController.LoadSettings();
    ApplicationSettingsController.AppSettings.InvertMouseWheel.ShouldBeTrue();
  }

  [Test]
  public void Corrupt_file_is_renamed_and_defaults_are_loaded()
  {
    File.WriteAllText(AppPaths.SettingsFilePath, "{ this is not json");

    Should.NotThrow(() => ApplicationSettingsController.LoadSettings());

    File.Exists(AppPaths.SettingsFilePath + ".bad").ShouldBeTrue();
    ApplicationSettingsController.AppSettings.ShowMiniMap.ShouldBeTrue();
    ApplicationSettingsController.AppSettings.SaveAt100.ShouldBeTrue();
  }

  [Test]
  public void Save_writes_to_config_folder_without_leaving_temp_file()
  {
    ApplicationSettingsController.AppSettings.InvertMouseWheel = true;

    ApplicationSettingsController.SaveSettings();

    File.Exists(AppPaths.SettingsFilePath).ShouldBeTrue();
    File.ReadAllText(AppPaths.SettingsFilePath).ShouldContain("InvertMouseWheel");
    Directory.GetFiles(AppPaths.ConfigFolder).Select(Path.GetFileName).ShouldBe(new[] { "appsettings.json" });
    File.Exists(_directory.File("appsettings.json")).ShouldBeFalse();
  }
}

using System;
using System.IO;

namespace Trizbort.Domain.AppSettings;

/// <summary>
///   Locations of per-user files. Settings live in the OS per-user config folder, never the working directory.
/// </summary>
public static class AppPaths
{
  public const string SettingsFileName = "appsettings.json";

  /// <summary>
  ///   When set (tests, --config-dir), replaces the OS config folder.
  /// </summary>
  public static string OverrideConfigFolder { get; set; }

  /// <summary>
  ///   The config folder. Created on demand.
  /// </summary>
  public static string ConfigFolder
  {
    get
    {
      var folder = !string.IsNullOrEmpty(OverrideConfigFolder)
        ? OverrideConfigFolder
        : ResolveConfigFolder(
          OperatingSystem.IsWindows(),
          OperatingSystem.IsMacOS(),
          GetDefaultBase,
          Environment.GetEnvironmentVariable);
      Directory.CreateDirectory(folder);
      return folder;
    }
  }

  public static string SettingsFilePath => Path.Combine(ConfigFolder, SettingsFileName);

  /// <summary>
  ///   Pure resolution of the per-OS folder.
  ///   <paramref name="getBase" /> returns the roaming application data folder on Windows and the home folder elsewhere.
  /// </summary>
  public static string ResolveConfigFolder(
    bool isWindows,
    bool isMacOS,
    Func<string> getBase,
    Func<string, string> getEnvironmentVariable)
  {
    if (isWindows) return Path.Combine(getBase(), "Trizbort");
    if (isMacOS) return Path.Combine(getBase(), "Library", "Application Support", "Trizbort");

    var xdg = getEnvironmentVariable("XDG_CONFIG_HOME");
    if (!string.IsNullOrEmpty(xdg) && Path.IsPathRooted(xdg)) return Path.Combine(xdg, "trizbort");
    return Path.Combine(getBase(), ".config", "trizbort");
  }

  private static string GetDefaultBase()
  {
    return OperatingSystem.IsWindows()
      ? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
      : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
  }
}

namespace Trizbort.Editor;

/// <summary>
///   Builds the map window title as <c>&lt;name&gt;[*] - &lt;app&gt; - &lt;version&gt;</c>. The name is the file name
///   when <c>showFullPath</c> is set and the map has one, otherwise the project name. A map without a file is
///   named "Untitled". The asterisk marks unsaved changes and sits directly after the name.
/// </summary>
public static class TitleFormatter
{
  public const string UntitledName = "Untitled";

  private const string DirtyMarker = "*";
  private const char BuildMetadataSeparator = '+';

  public static string Format(string projectName, string fileName, bool isDirty, bool showFullPath, string appName, string version)
  {
    var hasFile = !string.IsNullOrEmpty(fileName);
    var name = !hasFile
      ? UntitledName
      : showFullPath
        ? fileName
        : (string.IsNullOrEmpty(projectName) ? UntitledName : projectName);

    var marker = isDirty ? DirtyMarker : string.Empty;
    var shortVersion = StripBuildMetadata(version);
    var title = $"{name}{marker} - {appName}";

    return string.IsNullOrEmpty(shortVersion) ? title : $"{title} - {shortVersion}";
  }

  /// <summary>Drop build metadata such as "+commit-hash" so only the release version is shown.</summary>
  private static string StripBuildMetadata(string version)
  {
    if (string.IsNullOrEmpty(version)) return string.Empty;

    var metadataStart = version.IndexOf(BuildMetadataSeparator);
    return metadataStart > 0 ? version.Substring(0, metadataStart) : version;
  }
}

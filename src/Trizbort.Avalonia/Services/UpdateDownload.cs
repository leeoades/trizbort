using System.Runtime.InteropServices;

namespace Trizbort.Avalonia.Services;

/// <summary>
///   Which release download installs Recast so it updates itself, on the computer it's running on (Lee, 26 Nov 2026).
///   Shown to a plain copy (a zip, or a build) so it names just the one download, not all of them.
///   Names follow vpk's output in release.yml: <c>TrizbortRecast-&lt;channel&gt;-Setup.exe</c> /
///   <c>-Portable.zip</c>; the AppImage's name hasn't been checked yet, so it's described rather than named.
/// </summary>
public static class UpdateDownload
{
    public enum Platform { Windows, Linux, MacAppleSilicon, MacIntel }

    public static Platform ThisComputer =>
        OperatingSystem.IsMacOS()
            ? RuntimeInformation.OSArchitecture == Architecture.Arm64 ? Platform.MacAppleSilicon : Platform.MacIntel
            : OperatingSystem.IsLinux() ? Platform.Linux : Platform.Windows;

    /// <summary>What to download from the releases page and what to do with it, as a sentence.</summary>
    public static string HowToInstall(Platform platform) => platform switch
    {
        Platform.Linux => "download the .AppImage from the Releases page, make it executable and run it",
        Platform.MacAppleSilicon =>
            "download TrizbortRecast-osx-arm64-Portable.zip from the Releases page, unzip it and drag Trizbort Recast to Applications",
        Platform.MacIntel =>
            "download TrizbortRecast-osx-x64-Portable.zip from the Releases page, unzip it and drag Trizbort Recast to Applications",
        _ => "download TrizbortRecast-win-x64-Setup.exe from the Releases page and run it",
    };

    public static string HowToInstall() => HowToInstall(ThisComputer);
}

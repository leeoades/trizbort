using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Trizbort.Avalonia.Services;

/// <summary>
///   Double-clicking a .trizbort file opens it in Recast (Lee, 26 Nov 2026): App Settings > General, on by default.
///   Windows: per-user registry keys (no administrator needed), pointing at the launcher Velopack keeps outside its
///   versioned folder, so they survive updates; removed on uninstall. Linux: a .desktop file, the MIME type and the
///   icon under ~/.local/share, pointing at the AppImage (or the unpacked program). macOS: the app's Info.plist
///   declares the type, so there's nothing to do (Finder's Get Info > Open With chooses).
/// </summary>
public static class FileAssociation
{
    public const string Extension = ".trizbort";
    public const string ProgId = "TrizbortRecast.Map";
    public const string MimeType = "application/x-trizbort";
    private const string DesktopFile = "trizbort-recast.desktop";

    /// <summary>True where Recast sets this itself (not macOS, where the app bundle declares it).</summary>
    public static bool Supported => OperatingSystem.IsWindows() || OperatingSystem.IsLinux();

    /// <summary>
    ///   The program a double-click should run: the AppImage on Linux; on Windows, Velopack's launcher beside its
    ///   "current" folder when installed (it stays put across updates), else this program.
    /// </summary>
    public static string? ProgramPath()
    {
        var appImage = Environment.GetEnvironmentVariable("APPIMAGE");
        if (OperatingSystem.IsLinux() && !string.IsNullOrEmpty(appImage)) return appImage;
        var path = Environment.ProcessPath;
        if (string.IsNullOrEmpty(path)) return null;
        var folder = Path.GetDirectoryName(path);
        if (folder != null && string.Equals(Path.GetFileName(folder), "current", StringComparison.OrdinalIgnoreCase))
        {
            var launcher = Path.Combine(Path.GetDirectoryName(folder)!, Path.GetFileName(path));
            if (File.Exists(launcher)) return launcher;
        }

        return path;
    }

    /// <summary>Opens .trizbort files with this copy of Recast (on), or stops doing so (off). Never throws.</summary>
    public static void Apply(bool on)
    {
        try
        {
            var program = ProgramPath();
            if (OperatingSystem.IsWindows())
            {
                if (on && program != null) RegisterWindows(Registry.CurrentUser, @"Software\Classes", program);
                else UnregisterWindows(Registry.CurrentUser, @"Software\Classes");
                SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero); // file associations changed
            }
            else if (OperatingSystem.IsLinux())
            {
                var share = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
                if (on && program != null) RegisterLinux(share, program, RunQuietly);
                else UnregisterLinux(share, RunQuietly);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Could not {(on ? "set" : "remove")} the .trizbort file association: {ex.Message}");
        }
    }

    [SupportedOSPlatform("windows")]
    public static void RegisterWindows(RegistryKey root, string classes, string program)
    {
        using (var type = root.CreateSubKey($@"{classes}\{ProgId}"))
        {
            type.SetValue(string.Empty, "Trizbort map");
            using var icon = type.CreateSubKey("DefaultIcon");
            icon.SetValue(string.Empty, $"\"{program}\",0");
            using var command = type.CreateSubKey(@"shell\open\command");
            command.SetValue(string.Empty, $"\"{program}\" \"%1\"");
        }

        using var extension = root.CreateSubKey($@"{classes}\{Extension}");
        extension.SetValue(string.Empty, ProgId);
        using var openWith = extension.CreateSubKey("OpenWithProgids");
        openWith.SetValue(ProgId, Array.Empty<byte>(), RegistryValueKind.None);
    }

    [SupportedOSPlatform("windows")]
    public static void UnregisterWindows(RegistryKey root, string classes)
    {
        root.DeleteSubKeyTree($@"{classes}\{ProgId}", false);
        using var extension = root.OpenSubKey($@"{classes}\{Extension}", true);
        if (extension == null) return;
        // leave another app's claim alone
        if (extension.GetValue(string.Empty) as string == ProgId) extension.DeleteValue(string.Empty, false);
        using var openWith = extension.OpenSubKey("OpenWithProgids", true);
        openWith?.DeleteValue(ProgId, false);
    }

    /// <summary>Writes the .desktop file, MIME type and icon under <paramref name="share" /> (~/.local/share).</summary>
    public static void RegisterLinux(string share, string program, Action<string, string> run)
    {
        var applications = Directory.CreateDirectory(Path.Combine(share, "applications")).FullName;
        var mime = Directory.CreateDirectory(Path.Combine(share, "mime", "packages")).FullName;
        var icons = Directory.CreateDirectory(Path.Combine(share, "icons", "hicolor", "256x256", "apps")).FullName;

        File.WriteAllText(Path.Combine(applications, DesktopFile),
            "[Desktop Entry]\nType=Application\nName=Trizbort: Recast\nGenericName=Interactive fiction map editor\n"
            + "Comment=Map editor for interactive fiction\n"
            + $"Exec=\"{program.Replace("\"", "\\\"")}\" %f\nIcon=trizbort-recast\nTerminal=false\n"
            + $"Categories=Graphics;Utility;\nMimeType={MimeType};\n");
        File.WriteAllText(Path.Combine(mime, "trizbort-recast.xml"),
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
            + "<mime-info xmlns=\"http://www.freedesktop.org/standards/shared-mime-info\">\n"
            + $"  <mime-type type=\"{MimeType}\">\n    <comment>Trizbort map</comment>\n"
            + $"    <glob pattern=\"*{Extension}\"/>\n  </mime-type>\n</mime-info>\n");
        using (var icon = global::Avalonia.Platform.AssetLoader.Open(
                   new Uri("avares://Trizbort.Avalonia/Assets/AppIcon/trizbort-recast-256.png")))
        using (var file = File.Create(Path.Combine(icons, "trizbort-recast.png")))
        {
            icon.CopyTo(file);
        }

        run("update-mime-database", Path.Combine(share, "mime"));
        run("update-desktop-database", applications);
        run("xdg-mime", $"default {DesktopFile} {MimeType}");
    }

    public static void UnregisterLinux(string share, Action<string, string> run)
    {
        foreach (var file in new[]
                 {
                     Path.Combine(share, "applications", DesktopFile),
                     Path.Combine(share, "mime", "packages", "trizbort-recast.xml")
                 })
            if (File.Exists(file)) File.Delete(file);
        run("update-mime-database", Path.Combine(share, "mime"));
        run("update-desktop-database", Path.Combine(share, "applications"));
    }

    /// <summary>Runs a desktop tool, if it's there, without waiting long or minding if it fails.</summary>
    private static void RunQuietly(string program, string arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(program, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            process?.WaitForExit(5000);
        }
        catch (Exception)
        {
            // not every desktop has these tools
        }
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);
}

using System.Runtime.Versioning;
using Avalonia.Headless.NUnit;
using Microsoft.Win32;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Services;

namespace Trizbort.Avalonia.Tests;

/// <summary>Open .trizbort files with Recast (Lee, 26 Nov 2026).</summary>
[TestFixture]
public class FileAssociationTests
{
    [Test]
    [Platform("Win")]
    [SupportedOSPlatform("windows")]
    public void Windows_RegistersTheMapType_AndRemovesOnlyItsOwn()
    {
        const string root = @"Software\TrizbortRecastTests";
        var classes = root + @"\Classes";
        try
        {
            FileAssociation.RegisterWindows(Registry.CurrentUser, classes, @"C:\Apps\TrizbortRecast\TrizbortRecast.exe");
            using (var extension = Registry.CurrentUser.OpenSubKey(classes + @"\.trizbort")!)
                extension.GetValue(string.Empty).ShouldBe(FileAssociation.ProgId);
            using (var command = Registry.CurrentUser.OpenSubKey($@"{classes}\{FileAssociation.ProgId}\shell\open\command")!)
                command.GetValue(string.Empty).ShouldBe("\"C:\\Apps\\TrizbortRecast\\TrizbortRecast.exe\" \"%1\"");

            FileAssociation.UnregisterWindows(Registry.CurrentUser, classes);
            Registry.CurrentUser.OpenSubKey($@"{classes}\{FileAssociation.ProgId}").ShouldBeNull();
            using (var extension = Registry.CurrentUser.OpenSubKey(classes + @"\.trizbort")!)
                extension.GetValue(string.Empty).ShouldBeNull();

            // another app's claim on .trizbort stays
            using (var extension = Registry.CurrentUser.CreateSubKey(classes + @"\.trizbort"))
                extension.SetValue(string.Empty, "Trizbort.Map");
            FileAssociation.UnregisterWindows(Registry.CurrentUser, classes);
            using (var extension = Registry.CurrentUser.OpenSubKey(classes + @"\.trizbort")!)
                extension.GetValue(string.Empty).ShouldBe("Trizbort.Map");
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(root, false);
        }
    }

    [AvaloniaTest]
    public void Linux_WritesTheDesktopFile_MimeType_AndIcon_ThenRemovesThem()
    {
        var share = Path.Combine(Path.GetTempPath(), "trizbort-share-" + Guid.NewGuid().ToString("N"));
        var ran = new List<string>();
        try
        {
            FileAssociation.RegisterLinux(share, "/home/me/Apps/Trizbort Recast.AppImage", (program, args) => ran.Add($"{program} {args}"));

            var desktop = File.ReadAllText(Path.Combine(share, "applications", "trizbort-recast.desktop"));
            desktop.ShouldContain("Exec=\"/home/me/Apps/Trizbort Recast.AppImage\" %f");
            desktop.ShouldContain("MimeType=application/x-trizbort;");
            File.ReadAllText(Path.Combine(share, "mime", "packages", "trizbort-recast.xml")).ShouldContain("*.trizbort");
            new FileInfo(Path.Combine(share, "icons", "hicolor", "256x256", "apps", "trizbort-recast.png")).Length.ShouldBeGreaterThan(0);
            ran.ShouldContain("xdg-mime default trizbort-recast.desktop application/x-trizbort");

            FileAssociation.UnregisterLinux(share, (_, _) => { });
            File.Exists(Path.Combine(share, "applications", "trizbort-recast.desktop")).ShouldBeFalse();
            File.Exists(Path.Combine(share, "mime", "packages", "trizbort-recast.xml")).ShouldBeFalse();
        }
        finally
        {
            if (Directory.Exists(share)) Directory.Delete(share, true);
        }
    }
}

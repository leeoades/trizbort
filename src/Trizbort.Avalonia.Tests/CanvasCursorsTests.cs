using Avalonia.Headless.NUnit;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Controls;
using Trizbort.Editor;
using DrawingColor = System.Drawing.Color;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
public class CanvasCursorsTests
{
    [Test]
    public void IsDark_FollowsReferenceRule()
    {
        CanvasCursors.IsDark(DrawingColor.White).ShouldBeFalse();
        CanvasCursors.IsDark(DrawingColor.FromArgb(40, 40, 40)).ShouldBeTrue();
        CanvasCursors.IsDark(DrawingColor.FromArgb(10, 200, 30)).ShouldBeFalse();
    }

    [AvaloniaTest]
    public void Get_ReturnsCursorForEveryKind_OnLightAndDarkCanvas()
    {
        foreach (var kind in Enum.GetValues<CursorKind>())
        {
            CanvasCursors.Get(kind, false).ShouldNotBeNull();
            CanvasCursors.Get(kind, true).ShouldNotBeNull();
        }
    }

    [Test]
    public void CursorAssets_AreBundledForBothLineCursorsAndTheirDarkVariants()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        string? folder = null;
        while (dir != null && folder == null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Trizbort.Avalonia", "Assets", "Cursors");
            if (Directory.Exists(candidate)) folder = candidate;
            dir = dir.Parent;
        }

        folder.ShouldNotBeNull();
        foreach (var name in new[] { "DrawLine", "DrawLineInverted", "MoveLine", "MoveLineInverted" })
        {
            File.Exists(Path.Combine(folder!, name + ".png")).ShouldBeTrue(name);
        }
    }
}

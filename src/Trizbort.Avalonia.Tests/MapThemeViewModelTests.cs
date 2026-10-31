using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;
using Trizbort.Setup;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class MapThemeViewModelTests
{
    private Project? _previousProject;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
    }

    [TearDown]
    public void TearDown()
    {
        var created = Project.Current;
        Project.Current = _previousProject!;
        created.Dispose();
    }

    [Test]
    public void Initialize_ListsCurrentSettingsFirstAndSelectsIt()
    {
        var vm = new MapThemeViewModel();
        vm.Initialize();

        vm.Themes[0].Name.ShouldBe(MapThemeViewModel.CurrentThemeName);
        vm.SelectedTheme.ShouldBeSameAs(vm.CurrentTheme);
        vm.Themes.Count.ShouldBe(MapTheme.BuiltInThemes().Count + 1);
        vm.CurrentMatchText.ShouldNotBeEmpty();
    }

    [Test]
    public void SameLook_IgnoresNameAndRestoresIt()
    {
        var a = MapTheme.Capture("one");
        var b = MapTheme.Capture("two");

        MapTheme.SameLook(a, b).ShouldBeTrue();
        a.Name.ShouldBe("one");
        b.Name.ShouldBe("two");
    }

    [Test]
    public void ExportCurrentTheme_WritesFileEqualToCapture()
    {
        var path = Path.Combine(Path.GetTempPath(), "trizbort-theme-" + Guid.NewGuid().ToString("N") + ".trizbort-theme");
        try
        {
            var vm = new MapThemeViewModel();
            vm.Initialize();
            vm.ExportCurrentTheme(path);

            var loaded = MapTheme.Load(path);
            loaded.Name.ShouldBe(Path.GetFileNameWithoutExtension(path));
            MapTheme.SameLook(loaded, MapTheme.Capture("x")).ShouldBeTrue();
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public void ApplyWarning_MentionsUndoAndStyles()
    {
        MapThemeViewModel.ApplyWarning(true).ShouldContain("Edit > Undo");
        MapThemeViewModel.ApplyWarning(true).ShouldContain("custom styling");
        MapThemeViewModel.ApplyWarning(false).ShouldNotContain("custom styling");
    }
}

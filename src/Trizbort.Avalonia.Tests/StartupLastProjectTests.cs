using System.Reflection;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class StartupLastProjectTests
{
    private Project? _previousProject;
    private string _folder = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
        _folder = Path.Combine(Path.GetTempPath(), "trizbort-last-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TearDown]
    public void TearDown()
    {
        var created = Project.Current;
        Project.Current = _previousProject!;
        created?.Dispose();
        try
        {
            Directory.Delete(_folder, true);
        }
        catch (IOException)
        {
            // best effort
        }
    }

    private string MakeFile(string name)
    {
        var path = Path.Combine(_folder, name);
        File.WriteAllText(path, "x");
        return path;
    }

    private static async Task<StartupTests.RecordingProxy> RunAsync(string[] args, ApplicationSettings settings)
    {
        var storage = DispatchProxy.Create<IStorageService, StartupTests.RecordingProxy>();
        var dialogs = DispatchProxy.Create<IDialogService, StartupTests.RecordingProxy>();
        var vm = new MainWindowViewModel(new CommandService(), storage, dialogs, new AvaloniaAutomapCanvas());

        await StartupActions.RunAsync(vm, CommandLineParsing.Parse(args), storage, dialogs, null, settings);
        return (StartupTests.RecordingProxy)(object)storage;
    }

    [Test]
    public async Task SettingOn_ExistingFile_Loads()
    {
        var file = MakeFile("last.trizbort");
        var settings = new ApplicationSettings { LoadLastProjectOnStart = true, LastProjectFileName = file };

        var storage = await RunAsync(Array.Empty<string>(), settings);

        storage.Calls.ShouldContain(nameof(IStorageService.OpenMapFromPathAsync));
        storage.LastArgs[0].ShouldBe(file);
    }

    [Test]
    public async Task SettingOn_MissingFile_LeavesEmptyMap()
    {
        var settings = new ApplicationSettings
        {
            LoadLastProjectOnStart = true,
            LastProjectFileName = Path.Combine(_folder, "gone.trizbort")
        };

        var storage = await RunAsync(Array.Empty<string>(), settings);

        storage.Calls.ShouldBeEmpty();
    }

    [Test]
    public async Task ExplicitFile_BeatsLastProject()
    {
        var last = MakeFile("last.trizbort");
        var explicitFile = MakeFile("named.trizbort");
        var settings = new ApplicationSettings { LoadLastProjectOnStart = true, LastProjectFileName = last };

        var storage = await RunAsync(new[] { explicitFile }, settings);

        storage.Calls.Count(c => c == nameof(IStorageService.OpenMapFromPathAsync)).ShouldBe(1);
        storage.LastArgs[0].ShouldBe(Path.GetFullPath(explicitFile));
    }

    [Test]
    public async Task SettingOff_DoesNothing()
    {
        var file = MakeFile("last.trizbort");
        var settings = new ApplicationSettings { LoadLastProjectOnStart = false, LastProjectFileName = file };

        var storage = await RunAsync(Array.Empty<string>(), settings);

        storage.Calls.ShouldBeEmpty();
    }

    [Test]
    public async Task LoadLastProjectFlag_LoadsEvenWhenSettingOff()
    {
        var file = MakeFile("last.trizbort");
        var settings = new ApplicationSettings { LoadLastProjectOnStart = false, LastProjectFileName = file };

        var storage = await RunAsync(new[] { "--loadlastproject" }, settings);

        storage.Calls.ShouldContain(nameof(IStorageService.OpenMapFromPathAsync));
    }
}

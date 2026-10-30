using System.Reflection;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class StartupTests
{
    /// <summary>Records calls and returns benign defaults, so fakes survive interface growth.</summary>
    public class RecordingProxy : DispatchProxy
    {
        public List<string> Calls { get; } = new();
        public List<object?> LastArgs { get; } = new();
        public bool OpenResult { get; set; } = true;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null) return null;
            Calls.Add(targetMethod.Name);
            LastArgs.Clear();
            if (args != null) LastArgs.AddRange(args);

            var type = targetMethod.ReturnType;
            if (type == typeof(Task<bool>)) return Task.FromResult(OpenResult);
            if (type == typeof(Task)) return Task.CompletedTask;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var inner = type.GetGenericArguments()[0];
                var value = inner.IsValueType ? Activator.CreateInstance(inner) : null;
                return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(inner).Invoke(null, new[] { value });
            }
            return null;
        }
    }

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
        created?.Dispose();
    }

    private static string FindSample()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            var candidate = Path.Combine(dir, "samples", "escape-from-summerland.trizbort");
            if (File.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        throw new FileNotFoundException("samples/escape-from-summerland.trizbort not found");
    }

    private static (MainWindowViewModel Vm, RecordingProxy Storage, RecordingProxy Dialogs) Create()
    {
        var storage = DispatchProxy.Create<IStorageService, RecordingProxy>();
        var dialogs = DispatchProxy.Create<IDialogService, RecordingProxy>();
        var vm = new MainWindowViewModel(new CommandService(), storage, dialogs, new AvaloniaAutomapCanvas());
        return (vm, (RecordingProxy)(object)storage, (RecordingProxy)(object)dialogs);
    }

    [Test]
    public async Task ExistingFile_IsOpenedThroughStorage()
    {
        var sample = FindSample();
        var (vm, storage, dialogs) = Create();

        await StartupActions.RunAsync(
            vm, CommandLineParsing.Parse(new[] { sample }),
            (IStorageService)(object)storage, (IDialogService)(object)dialogs, null);

        storage.Calls.ShouldContain(nameof(IStorageService.OpenMapFromPathAsync));
        storage.LastArgs[0].ShouldBe(Path.GetFullPath(sample));
        dialogs.Calls.ShouldBeEmpty();
    }

    [Test]
    public async Task MissingFile_ShowsMessageAndDoesNotOpen()
    {
        var (vm, storage, dialogs) = Create();

        await StartupActions.RunAsync(
            vm, CommandLineParsing.Parse(new[] { "no-such-map.trizbort" }),
            (IStorageService)(object)storage, (IDialogService)(object)dialogs, null);

        storage.Calls.ShouldBeEmpty();
        dialogs.Calls.ShouldContain(nameof(IDialogService.ShowMessageAsync));
        ((string)dialogs.LastArgs[2]!).ShouldStartWith("File not found: ");
    }

    [Test]
    public async Task NoFileName_DoesNothing()
    {
        var (vm, storage, dialogs) = Create();

        await StartupActions.RunAsync(
            vm, CommandLineParsing.Parse(new[] { "--debug" }),
            (IStorageService)(object)storage, (IDialogService)(object)dialogs, null);

        storage.Calls.ShouldBeEmpty();
        dialogs.Calls.ShouldBeEmpty();
    }
}

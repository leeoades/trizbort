using System.Reflection;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Services;
using Trizbort.Domain.Application;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Domain.Recovery;
using Trizbort.Editor;

namespace Trizbort.Avalonia.Tests;

[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class RecoveryTests
{
    private string _folder = string.Empty;
    private string? _previousOverride;
    private Project? _previousProject;
    private EditorSession? _session;
    private RecoveryStore? _store;
    private RecoveryService? _service;
    private bool _enabled = true;

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "trizbort-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _previousOverride = AppPaths.OverrideConfigFolder;
        AppPaths.OverrideConfigFolder = Path.Combine(_folder, "config");
        _previousProject = Project.Current;
        Project.Current = new Project();
        _enabled = true;
        _session = new EditorSession();
        _store = new RecoveryStore(Path.Combine(_folder, "recovery"), "mine");
        _service = new RecoveryService(_session, _store, IsEnabled, "1.0", TimeSpan.FromMilliseconds(40));
    }

    [TearDown]
    public void TearDown()
    {
        RecoveryService.Current = null;
        CrashReporter.MessageSink = null;
        _service?.Dispose();
        _store?.Dispose(); // also disposed by the service; disposing twice is safe
        _session?.Dispose();
        var current = Project.Current;
        Project.Current = _previousProject!;
        if (current != null && current != _previousProject) current.Dispose();
        AppPaths.OverrideConfigFolder = _previousOverride!;
        try
        {
            Directory.Delete(_folder, true);
        }
        catch (IOException)
        {
            // temp folder; best effort
        }
    }

    private bool IsEnabled() => _enabled;

    private void AddRoom(string name)
    {
        _session!.Execute("Add room", () => Project.Current.Elements.Add(
            new Room(Project.Current) { Name = name, Position = new Vector(0, 0), Size = new Vector(96, 64) }));
    }

    private static bool WaitFor(Func<bool> condition)
    {
        for (var i = 0; i < 250; i++)
        {
            Dispatcher.UIThread.RunJobs();
            if (condition()) return true;
            Thread.Sleep(20);
        }

        return condition();
    }

    [AvaloniaTest]
    public void Edit_WritesCopyAfterDelay_AndSaveRemovesIt()
    {
        AddRoom("Cellar");

        WaitFor(() => _store!.HasCopy).ShouldBeTrue();

        _session!.MarkSaved();
        Project.Current.IsDirty.ShouldBeFalse();
        WaitFor(() => !_store!.HasCopy).ShouldBeTrue();
    }

    [AvaloniaTest]
    public void Undo_BackToSavedState_RemovesCopy()
    {
        AddRoom("Cellar");
        WaitFor(() => _store!.HasCopy).ShouldBeTrue();

        _session!.PerformUndo();

        Project.Current.IsDirty.ShouldBeFalse();
        WaitFor(() => !_store!.HasCopy).ShouldBeTrue();
    }

    [AvaloniaTest]
    public void SettingOff_WritesNothing()
    {
        _enabled = false;

        AddRoom("Cellar");
        WaitFor(() => _store!.HasCopy).ShouldBeFalse();
        _service!.WriteNow(true).ShouldBeFalse();
    }

    [AvaloniaTest]
    public void NewMap_DiscardsCopy()
    {
        AddRoom("Cellar");
        WaitFor(() => _store!.HasCopy).ShouldBeTrue();

        _session!.NewProject();

        WaitFor(() => !_store!.HasCopy).ShouldBeTrue();
    }

    [AvaloniaTest]
    public void Crash_WritesCopyImmediatelyAndALogAndExplainsItself()
    {
        string? message = null;
        CrashReporter.MessageSink = text => message = text;
        RecoveryService.Current = _service;
        // dirty without waiting for the debounce
        _enabled = true;
        AddRoom("Cellar");

        CrashReporter.ReportUiThreadError(new InvalidOperationException("boom"));

        _store!.HasCopy.ShouldBeTrue();
        message.ShouldNotBeNull();
        message.ShouldContain("Something went wrong");
        message.ShouldNotContain("boom"); // the .NET message stays in the log
        message.ShouldContain("A copy of your unsaved changes has been kept.");
        message.ShouldContain("The details are saved in");
        var logs = Directory.GetFiles(Path.Combine(AppPaths.ConfigFolder, RecoveryStore.CrashLogFolderName));
        logs.Length.ShouldBe(1);
        message.ShouldContain(logs[0]);
    }

    [AvaloniaTest]
    public void Crash_WithRecoveryOff_AsksToSaveInsteadOfPromising()
    {
        // PR #26 review: don't say the work was kept when no copy was written
        string? message = null;
        CrashReporter.MessageSink = text => message = text;
        RecoveryService.Current = _service;
        _enabled = false;
        AddRoom("Cellar");

        CrashReporter.ReportUiThreadError(new InvalidOperationException("boom"));

        _store!.HasCopy.ShouldBeFalse();
        message.ShouldNotBeNull();
        message.ShouldNotContain("has been kept");
        message.ShouldContain("Please save your map now.");
    }

    [AvaloniaTest]
    public void Crash_WithNothingUnsaved_SaysNothingAboutWork()
    {
        string? message = null;
        CrashReporter.MessageSink = text => message = text;
        RecoveryService.Current = _service;

        CrashReporter.ReportUiThreadError(new InvalidOperationException("boom"));

        message.ShouldNotBeNull();
        message.ShouldNotContain("kept");
        message.ShouldNotContain("save your map");
    }

    [AvaloniaTest]
    public void Crash_OnAnotherThread_CopiesTheMapOnTheUiThread()
    {
        RecoveryService.Current = _service;
        AddRoom("Cellar");

        var capture = Task.Run(() => CrashReporter.Capture(new InvalidOperationException("boom")));
        for (var i = 0; i < 250 && !capture.IsCompleted; i++)
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }

        capture.IsCompleted.ShouldBeTrue();
        capture.Result.Work.ShouldBe(UnsavedWork.Kept);
        _store!.HasCopy.ShouldBeTrue();
    }

    [AvaloniaTest]
    public void Crash_OnAnotherThread_WhenTheUiThreadIsBusy_LeavesTheLastCopyAlone()
    {
        var previousWait = CrashReporter.UiThreadWait;
        CrashReporter.UiThreadWait = TimeSpan.FromMilliseconds(50);
        try
        {
            RecoveryService.Current = _service;
            AddRoom("Cellar");

            // this (UI) thread is blocked waiting, as it would be if it were stuck
            var capture = Task.Run(() => CrashReporter.Capture(new InvalidOperationException("boom"))).Result;

            capture.Work.ShouldBe(UnsavedWork.NotKept);
        }
        finally
        {
            CrashReporter.UiThreadWait = previousWait;
        }
    }

    private (string Id, string Copy) MakeOrphan(string title, string originalFile)
    {
        // write a copy as another instance would have, then leave it behind without a lock
        using var other = new RecoveryStore(_store!.Folder, "other");
        using var project = new Project { Title = title, FileName = originalFile };
        project.Elements.Add(new Room(project) { Name = "Lost room", Position = new Vector(0, 0), Size = new Vector(96, 64) });
        other.WriteCopy(project, "0.1").ShouldBeTrue();
        var orphanCopy = Path.Combine(_store.Folder, "dead.trizbort");
        File.Copy(other.CopyPath, orphanCopy, true);
        File.Copy(other.MetaPath, Path.Combine(_store.Folder, "dead.json"), true);
        return ("dead", orphanCopy);
    }

    private RecordingDialogs Dialogs(RecoveryChoice choice)
    {
        var dialogs = DispatchProxy.Create<IDialogService, RecordingDialogs>();
        var recorder = (RecordingDialogs)(object)dialogs;
        recorder.Choice = choice;
        return recorder;
    }

    [AvaloniaTest]
    public async Task Startup_Recover_OpensCopyAsUnsavedMapWithOriginalName()
    {
        var (_, copy) = MakeOrphan("Lost Map", "/maps/lost.trizbort");
        var dialogs = Dialogs(RecoveryChoice.Recover);

        var recovered = await RecoveryStartup.RunAsync(_session!, _store!, _service, (IDialogService)(object)dialogs, null);

        recovered.ShouldBeTrue();
        dialogs.Message.ShouldContain("didn't close properly");
        dialogs.Message.ShouldContain("Lost Map");
        Project.Current.IsDirty.ShouldBeTrue();
        Project.Current.FileName.ShouldBe("/maps/lost.trizbort");
        Project.Current.Elements.OfType<Room>().Single().Name.ShouldBe("Lost room");
        File.Exists(copy).ShouldBeFalse();
        _store!.HasCopy.ShouldBeTrue();
        // Unsaved at every point in its history: undoing back to the recovered state must not look saved.
        _session!.Undo.IsDirty.ShouldBeTrue();
    }

    [AvaloniaTest]
    public async Task Startup_Recover_WithRecoveryOff_KeepsTheCopyUntilTheMapIsSaved()
    {
        // PR #26 review: the recovered copy is the only copy of the work until it is saved
        _enabled = false;
        var (_, copy) = MakeOrphan("Lost Map", "/maps/lost.trizbort");
        var dialogs = Dialogs(RecoveryChoice.Recover);

        var recovered = await RecoveryStartup.RunAsync(_session!, _store!, _service, (IDialogService)(object)dialogs, null);

        recovered.ShouldBeTrue();
        File.Exists(copy).ShouldBeTrue();
        AddRoom("Another room"); // edits don't remove it either
        File.Exists(copy).ShouldBeTrue();

        Project.Current.IsDirty = false; // saved
        _session!.CheckDirty();

        WaitFor(() => !File.Exists(copy)).ShouldBeTrue();
    }

    [AvaloniaTest]
    public async Task Startup_Recover_WithRecoveryOff_EditThenUndo_KeepsTheCopy()
    {
        // PR #26 review: undo rebuilds the map and reports it clean for a moment, which deleted the only copy
        _enabled = false;
        var (_, copy) = MakeOrphan("Lost Map", "/maps/lost.trizbort");
        var dialogs = Dialogs(RecoveryChoice.Recover);
        (await RecoveryStartup.RunAsync(_session!, _store!, _service, (IDialogService)(object)dialogs, null))
            .ShouldBeTrue();

        AddRoom("Another room");
        _session!.PerformUndo();
        Dispatcher.UIThread.RunJobs();

        Project.Current.IsDirty.ShouldBeTrue();
        File.Exists(copy).ShouldBeTrue();
    }

    [AvaloniaTest]
    public async Task Startup_Recover_WithRecoveryOff_ClosingNormally_DeletesTheCopy()
    {
        // PR #26 review: closing after choosing Discard left the copy to be offered again
        _enabled = false;
        var (_, copy) = MakeOrphan("Lost Map", "/maps/lost.trizbort");
        var dialogs = Dialogs(RecoveryChoice.Recover);
        (await RecoveryStartup.RunAsync(_session!, _store!, _service, (IDialogService)(object)dialogs, null))
            .ShouldBeTrue();

        _service!.Dispose();

        File.Exists(copy).ShouldBeFalse();
    }

    [AvaloniaTest]
    public async Task Startup_Discard_DeletesCopy()
    {
        var (_, copy) = MakeOrphan("Lost Map", "");
        var dialogs = Dialogs(RecoveryChoice.Discard);

        var recovered = await RecoveryStartup.RunAsync(_session!, _store!, _service, (IDialogService)(object)dialogs, null);

        recovered.ShouldBeFalse();
        File.Exists(copy).ShouldBeFalse();
        Project.Current.Elements.OfType<Room>().ShouldBeEmpty();
    }

    [AvaloniaTest]
    public async Task Startup_DecideLater_KeepsCopy()
    {
        var (_, copy) = MakeOrphan("Lost Map", "");
        var dialogs = Dialogs(RecoveryChoice.DecideLater);

        var recovered = await RecoveryStartup.RunAsync(_session!, _store!, _service, (IDialogService)(object)dialogs, null);

        recovered.ShouldBeFalse();
        File.Exists(copy).ShouldBeTrue();
    }

    [AvaloniaTest]
    public async Task Startup_NoOrphans_DoesNotAsk()
    {
        var dialogs = Dialogs(RecoveryChoice.Recover);

        var recovered = await RecoveryStartup.RunAsync(_session!, _store!, _service, (IDialogService)(object)dialogs, null);

        recovered.ShouldBeFalse();
        dialogs.Asked.ShouldBe(0);
    }

    public class RecordingDialogs : DispatchProxy
    {
        public RecoveryChoice Choice { get; set; }
        public string Message { get; private set; } = string.Empty;
        public int Asked { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IDialogService.ShowRecoverDiscardLaterAsync))
            {
                Asked++;
                Message = (string)args![2]!;
                return Task.FromResult(Choice);
            }

            return Task.CompletedTask;
        }
    }
}

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Domain.Recovery;

namespace Trizbort.Core.Tests;

[TestFixture]
[NonParallelizable]
public class RecoveryStoreTests
{
  private TemporaryDirectory _directory;
  private string _folder;

  [SetUp]
  public void SetUp()
  {
    _directory = new TemporaryDirectory();
    _folder = _directory.File("recovery");
  }

  [TearDown]
  public void TearDown()
  {
    _directory.Dispose();
  }

  private static Project NewProject(string title, string fileName)
  {
    var project = new Project { Title = title, FileName = fileName };
    project.Elements.Add(new Room(project) { Name = "Kitchen", Position = new Vector(0, 0), Size = new Vector(96, 64) });
    return project;
  }

  [Test]
  public void WriteCopy_WritesMapAndDetails_AndLeavesNoTempFiles()
  {
    using var store = new RecoveryStore(_folder, "abc");
    using var project = NewProject("My Map", "/maps/my.trizbort");

    store.WriteCopy(project, "1.2.3").ShouldBeTrue();

    File.Exists(store.CopyPath).ShouldBeTrue();
    File.Exists(store.MetaPath).ShouldBeTrue();
    File.Exists(store.LockPath).ShouldBeTrue();
    Directory.GetFiles(_folder, "*.tmp.*").ShouldBeEmpty();

    // the copy is a normal map
    using var loaded = new Project();
    new MapLoader(loaded).LoadMap(store.CopyPath).ShouldBeTrue();
    loaded.Elements.OfType<Room>().Single().Name.ShouldBe("Kitchen");
  }

  [Test]
  public void WriteCopy_Twice_ReplacesTheCopy()
  {
    using var store = new RecoveryStore(_folder, "abc");
    using var project = NewProject("A", "");
    store.WriteCopy(project, "1").ShouldBeTrue();
    project.Elements.Add(new Room(project) { Name = "Hall", Position = new Vector(200, 0), Size = new Vector(96, 64) });

    store.WriteCopy(project, "1").ShouldBeTrue();

    using var loaded = new Project();
    new MapLoader(loaded).LoadMap(store.CopyPath).ShouldBeTrue();
    loaded.Elements.OfType<Room>().Count().ShouldBe(2);
  }

  [Test]
  public void WriteCopy_CannotThrow_WhenFolderIsUnusable()
  {
    var blocker = _directory.File("blocker");
    File.WriteAllText(blocker, "x");
    using var store = new RecoveryStore(Path.Combine(blocker, "sub"), "abc");
    using var project = NewProject("A", "");

    store.WriteCopy(project, "1").ShouldBeFalse();
    store.WriteCopy(null, "1").ShouldBeFalse();
  }

  [Test]
  public void FindOrphans_IgnoresOwnCopyAndCopiesWhoseLockIsHeld()
  {
    using var running = new RecoveryStore(_folder, "running");
    using var project = NewProject("Live", "");
    running.WriteCopy(project, "1").ShouldBeTrue();

    using var scanner = new RecoveryStore(_folder, "scanner");
    scanner.FindOrphans().ShouldBeEmpty();
  }

  [Test]
  public void FindOrphans_ReportsCopyOfAnInstanceThatEndedWithoutCleaningUp()
  {
    var crashed = new RecoveryStore(_folder, "crashed");
    using (var project = NewProject("Lost Map", "/maps/lost.trizbort"))
    {
      crashed.WriteCopy(project, "9.9").ShouldBeTrue();
    }

    // simulate the process dying: release the lock but keep the files
    ReleaseLockOnly(crashed);

    using var next = new RecoveryStore(_folder, "next");
    var orphan = next.FindOrphans().Single();
    orphan.SessionId.ShouldBe("crashed");
    orphan.Title.ShouldBe("Lost Map");
    orphan.OriginalFileName.ShouldBe("/maps/lost.trizbort");
    orphan.AppVersion.ShouldBe("9.9");
    orphan.SavedUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-5));
    File.Exists(orphan.CopyPath).ShouldBeTrue();

    next.Delete(orphan);
    next.FindOrphans().ShouldBeEmpty();
    Directory.GetFiles(_folder).ShouldBeEmpty();
  }

  [Test]
  public void Dispose_RemovesCopyDetailsAndLock()
  {
    var store = new RecoveryStore(_folder, "abc");
    using (var project = NewProject("A", ""))
    {
      store.WriteCopy(project, "1").ShouldBeTrue();
    }

    store.Dispose();

    Directory.GetFiles(_folder).ShouldBeEmpty();
  }

  [Test]
  public void DeleteCopy_RemovesCopyButKeepsTheLockForTheInstance()
  {
    using var store = new RecoveryStore(_folder, "abc");
    using var project = NewProject("A", "");
    store.WriteCopy(project, "1").ShouldBeTrue();

    store.DeleteCopy();

    store.HasCopy.ShouldBeFalse();
    File.Exists(store.MetaPath).ShouldBeFalse();
    File.Exists(store.LockPath).ShouldBeTrue();
  }

  [Test]
  public void WriteCrashLog_WritesVersionOsAndException()
  {
    var folder = _directory.File("crash-logs");
    Exception thrown;
    try
    {
      throw new InvalidOperationException("boom");
    }
    catch (Exception ex)
    {
      thrown = ex;
    }

    var path = RecoveryStore.WriteCrashLog(thrown, "4.5.6", folder);

    path.ShouldNotBeNull();
    Path.GetDirectoryName(path).ShouldBe(folder);
    var text = File.ReadAllText(path);
    text.ShouldContain("4.5.6");
    text.ShouldContain("OS:");
    text.ShouldContain("InvalidOperationException");
    text.ShouldContain("boom");
  }

  [Test]
  public void WriteCrashLog_TwiceInTheSameMillisecond_DoesNotOverwrite()
  {
    var folder = _directory.File("crash-logs");
    for (var i = 0; i < 5; i++) RecoveryStore.WriteCrashLog(new Exception("x"), "1", folder).ShouldNotBeNull();
    Directory.GetFiles(folder).Length.ShouldBe(5);
  }

  /// <summary>Releases the exclusive lock without deleting the files, as a dead process would.</summary>
  private static void ReleaseLockOnly(RecoveryStore store)
  {
    var field = typeof(RecoveryStore).GetField("_lock", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    var stream = (FileStream)field.GetValue(store);
    stream.Dispose();
    field.SetValue(store, null);
  }
}

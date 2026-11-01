using System.IO;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Export;

namespace Trizbort.Core.Tests;

/// <summary>
///   Saving to a new file (Save As, quick save) must watch that file for outside changes, and must not fail because
///   the previously saved file's folder has gone. Found through a test-order failure: a quick save after an earlier
///   test's temporary folder was deleted reported "Error reading the ... directory".
/// </summary>
[TestFixture]
[NonParallelizable]
public class SaveWatchesSavedFileTests : IsolatedProjectTests
{
  [Test]
  public void QuickSave_ToANewFolder_WatchesTheNewFile_EvenWhenTheOldFolderIsGone()
  {
    var oldFolder = Directory.CreateDirectory(Files.File("old")).FullName;
    var newFolder = Directory.CreateDirectory(Files.File("new")).FullName;
    var first = Path.Combine(oldFolder, "first.trizbort");
    var second = Path.Combine(newFolder, "second.trizbort");

    HeadlessSave.QuickSave(Project.Current, first).ShouldBeNull();
    Project.FileWatcher.IsSetTo(first).ShouldBeTrue();

    Project.FileWatcher.StopWatcher();
    Directory.Delete(oldFolder, true);

    HeadlessSave.QuickSave(Project.Current, second).ShouldBeNull();
    File.Exists(second).ShouldBeTrue();
    Project.FileWatcher.IsSetTo(second).ShouldBeTrue();
    Project.FileWatcher.IsWatching.ShouldBeTrue();
  }
}

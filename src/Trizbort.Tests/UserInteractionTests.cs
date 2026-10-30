using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Controllers;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using Trizbort.UI;
using Trizbort.UI.Controls;

namespace Trizbort.Tests;

internal sealed class RecordingUserInteraction : IUserInteraction
{
  public readonly List<string> Messages = new();
  public MessageBoxButtons Buttons;
  public string ClipboardText = "";
  public MessageBoxDefaultButton DefaultButton;
  public Action<CommonDialog> EditCommonDialog;
  public Action<Form> EditForm;
  public IWin32Window Owner;
  public DialogResult Result = DialogResult.OK;

  public DialogResult ShowMessage(
    IWin32Window owner,
    string text,
    string caption,
    MessageBoxButtons buttons,
    MessageBoxIcon icon,
    MessageBoxDefaultButton defaultButton)
  {
    Owner = owner;
    Buttons = buttons;
    DefaultButton = defaultButton;
    Messages.Add(caption + ": " + text);
    return Result;
  }

  public DialogResult ShowDialog(Form dialog, IWin32Window owner)
  {
    Owner = owner;
    EditForm?.Invoke(dialog);
    return Result;
  }

  public DialogResult ShowDialog(CommonDialog dialog, IWin32Window owner)
  {
    Owner = owner;
    EditCommonDialog?.Invoke(dialog);
    return Result;
  }

  public string GetClipboardText()
  {
    return ClipboardText;
  }

  public void SetClipboardText(string text, TextDataFormat format)
  {
    ClipboardText = text;
  }
}

[TestFixture]
[Category("Integration")]
public class UserInteractionTests : IsolatedProjectTests
{
  [SetUp]
  public void SubstituteUI()
  {
    _interaction = new RecordingUserInteraction();
    UserInteraction.Current = _interaction;
  }

  private RecordingUserInteraction _interaction;

  [Test]
  public void MapRoundTrip_UsesApplicationAssemblyVersionAndNoRunnerMetadata()
  {
    ProjectRegressionTests.AddRoom("Test Room");
    var path = Files.File("map.trizbort");
    new LegacyMapFileEngine(Project.Current).Save(path).ShouldBeTrue();
    XDocument.Load(path).Root.Attribute("version").Value
             .ShouldBe(typeof(Project).Assembly.GetName().Version.ToString());
    using (var loaded = new Project())
    {
      new MapLoader(loaded).LoadMap(path).ShouldBeTrue();
      loaded.Elements.Count.ShouldBe(1);
      loaded.Version.ShouldBe(typeof(Project).Assembly.GetName().Version);
    }

    _interaction.Messages.ShouldBeEmpty();
  }

  [Test]
  public void NewerDocument_ReportsVersionWarningThroughUIBoundary()
  {
    var current = typeof(Project).Assembly.GetName().Version;
    Project.Current.SetVersion(new Version(current.Major + 1, 0, 0, 0).ToString());
    Project.Current.CheckDocVersion();
    _interaction.Messages.ShouldHaveSingleItem().ShouldContain("ahead a major version");
  }

  [Test]
  public void Backup_CopiesSavedFileAndReportsDestinationWithCorrectMessage()
  {
    var path = Files.File("backup.trizbort");
    File.WriteAllText(path, "Saved map contents");
    Project.Current.FileName = path;
    for (var index = 1; index <= 2; index++)
    {
      Project.Current.Backup();
      var backup = Files.File("backup-backup-" + index + ".trizbort");
      File.ReadAllText(backup).ShouldBe("Saved map contents");
      _interaction.Messages[index - 1]
                  .ShouldBe("Project backed up.: Your project has been backed up to " + backup + ".");
    }

    _interaction.Messages.Count.ShouldBe(2);
    File.ReadAllText(path).ShouldBe("Saved map contents");
  }

  [Test]
  public void Backup_UnsavedProjectReportsNothingToBackUpWithoutCreatingFile()
  {
    var files = Directory.GetFiles(Files.Path);
    Project.Current.Backup();
    Directory.GetFiles(Files.Path).ShouldBe(files);
    _interaction.Messages.ShouldHaveSingleItem().ShouldBe(
      "Nothing to backup.: Your project has not yet been saved to a file. There is nothing to backup.");
  }

  [Test]
  public void InvalidLoadAndSave_ReportErrorsWithoutOpeningMessageBoxes()
  {
    var path = Files.File("invalid.trizbort");
    File.WriteAllText(path, "<wrong/>");
    new MapLoader(Project.Current).LoadMap(path).ShouldBeFalse();
    _interaction.Messages.ShouldHaveSingleItem().ShouldContain("problem loading");
    Project.Current.InitFileWWatcher(path);
    Project.Current.FileName = Files.File(@"missing\map.trizbort");
    Project.Current.Save().ShouldBeFalse();
    _interaction.Messages.Count.ShouldBe(2);
    _interaction.Messages[1].ShouldContain("problem saving");
  }

  private Room PrepareExistingMap()
  {
    var room = ProjectRegressionTests.AddRoom("Existing Room");
    Project.Current.Title = "Existing title";
    Project.Current.Author = "Existing author";
    Project.Current.Description = "Existing description";
    Project.Current.History = "Existing history";
    Project.Current.IsDirty = true;
    Settings.GridSize = 90;
    return room;
  }

  private static void AssertExistingMap(Project existing, Room room)
  {
    Project.Current.ShouldBeSameAs(existing);
    existing.Elements.ShouldHaveSingleItem().ShouldBeSameAs(room);
    existing.Title.ShouldBe("Existing title");
    existing.Author.ShouldBe("Existing author");
    existing.Description.ShouldBe("Existing description");
    existing.History.ShouldBe("Existing history");
    existing.IsDirty.ShouldBeTrue();
  }

  private static void AssertWatching(string path)
  {
    Project.FileWatcher.WatchedPath.ShouldBe(Path.GetDirectoryName(path));
    Project.FileWatcher.WatchedFilter.ShouldBe(Path.GetFileName(path));
    Project.FileWatcher.IsWatching.ShouldBeTrue();
  }

  [TestCase(false)]
  [TestCase(true)]
  public void EmptyLocalMap_PublicLoadSucceedsWithoutClearingCurrentMetadata(bool throughProject)
  {
    var room = PrepareExistingMap();
    var existing = Project.Current;
    var path = Files.File("empty.trizbort");
    File.WriteAllText(path, "");
    using (var loaded = new Project {
             FileName = path, Title = "Unused title", Author = "Unused author",
             Description = "Unused description", History = "Unused history"
           })
    {
      var succeeded = throughProject ? loaded.Load() : new MapLoader(loaded).LoadMap(path);
      succeeded.ShouldBeTrue();
      loaded.Elements.ShouldBeEmpty();
      loaded.Title.ShouldBeEmpty();
      loaded.Author.ShouldBeEmpty();
      loaded.Description.ShouldBeEmpty();
      loaded.History.ShouldBeEmpty();
      loaded.IsDirty.ShouldBeFalse();
      Settings.GridSize.ShouldBe(32);
      AssertExistingMap(existing, room);
      AssertWatching(path);
    }

    _interaction.Messages.ShouldBeEmpty();
  }

  [Test]
  public void OpenEmptyLocalMap_ReplacesCurrentProjectWithBlankMap()
  {
    var room = PrepareExistingMap();
    var existing = Project.Current;
    var previousForm = TrizbortApplication.MainForm;
    var path = Files.File("explorer-new.trizbort");
    File.WriteAllText(path, "");
    try
    {
      using var form = new MainForm();
      form.OpenProject(path);
      Project.Current.ShouldNotBeSameAs(existing);
      Project.Current.FileName.ShouldBe(path);
      Project.Current.Elements.ShouldBeEmpty();
      Project.Current.Title.ShouldBeEmpty();
      Project.Current.Author.ShouldBeEmpty();
      Project.Current.Description.ShouldBeEmpty();
      Project.Current.History.ShouldBeEmpty();
      Project.Current.IsDirty.ShouldBeFalse();
      existing.Elements.ShouldHaveSingleItem().ShouldBeSameAs(room);
      existing.Title.ShouldBe("Existing title");
      Settings.GridSize.ShouldBe(32);
      AssertWatching(path);
      _interaction.Messages.ShouldBeEmpty();
    }
    finally
    {
      TrizbortApplication.MainForm = previousForm;
      existing.Dispose();
    }
  }

  [TestCase("empty.txt", "")]
  [TestCase("missing.txt", null)]
  [TestCase("missing.trizbort", null)]
  [TestCase("malformed.trizbort", "<trizbort>")]
  [TestCase("whitespace.trizbort", " \r\n\t")]
  public void FailedPublicLoad_PreservesCurrentMapAndReportsError(string name, string contents)
  {
    var room = PrepareExistingMap();
    var existing = Project.Current;
    var path = Files.File(name);
    if (contents != null) File.WriteAllText(path, contents);
    using var loaded = new Project { FileName = path };
    loaded.Load().ShouldBeFalse();
    AssertExistingMap(existing, room);
    Settings.GridSize.ShouldBe(90);
    _interaction.Messages.ShouldHaveSingleItem().ShouldContain(
      Path.GetExtension(path) == ".trizbort" ? "problem loading" : "not a known Trizbort file");
  }

  [TestCase(false)]
  [TestCase(true)]
  public void RelativeLocalMap_PublicLoadHandlesBlankAndXmlAndWatchesResolvedPath(bool empty)
  {
    var path = Files.File("relative.trizbort");
    File.WriteAllText(
      path,
      empty ? "" : "<trizbort version=\"1.0\"><map><room id=\"1\" name=\"Loaded Room\"/></map></trizbort>");
    var previousDirectory = Environment.CurrentDirectory;
    try
    {
      Environment.CurrentDirectory = Files.Path;
      using var loaded = new Project { FileName = Path.GetFileName(path) };
      loaded.Load().ShouldBeTrue();
      loaded.Elements.Count.ShouldBe(empty ? 0 : 1);
      AssertWatching(path);
      _interaction.Messages.ShouldBeEmpty();
    }
    finally
    {
      Environment.CurrentDirectory = previousDirectory;
    }
  }

  [Test]
  public void DuplicateStartRooms_ReportWarningThroughUIBoundary()
  {
    var path = Files.File("duplicate.trizbort");
    File.WriteAllText(
      path,
      "<trizbort version=\"1.0\"><map><room id=\"1\" name=\"First\" isStartRoom=\"yes\"/>" +
      "<room id=\"2\" name=\"Second\" isStartRoom=\"yes\"/></map></trizbort>");
    new MapLoader(Project.Current).LoadMap(path).ShouldBeTrue();
    _interaction.Messages.ShouldHaveSingleItem().ShouldContain("duplicate start room");
  }

  [TestCase(DialogResult.OK)]
  [TestCase(DialogResult.Cancel)]
  public void RoomProperties_AreAbstractableAndApplyOnlyOnOK(DialogResult result)
  {
    var room = ProjectRegressionTests.AddRoom("Original");
    _interaction.Result = result;
    _interaction.EditForm = form => {
      var dialog = form.ShouldBeOfType<RoomPropertiesDialog>();
      dialog.RoomName.ShouldBe("Original");
      dialog.RoomName = "Updated";
      dialog.IsDark = true;
    };
    new ElementController().ShowElementProperties(room);
    room.Name.ShouldBe(result == DialogResult.OK ? "Updated" : "Original");
    room.IsDark.ShouldBe(result == DialogResult.OK);
  }

  [TestCase(DialogResult.OK)]
  [TestCase(DialogResult.Cancel)]
  public void ConnectionProperties_AreAbstractableAndApplyOnlyOnOK(DialogResult result)
  {
    var line = new Connection(Project.Current) { Name = "Original" };
    _interaction.Result = result;
    _interaction.EditForm = form => {
      var dialog = form.ShouldBeOfType<ConnectionPropertiesDialog>();
      dialog.ConnectionName.ShouldBe("Original");
      dialog.ConnectionName = "Updated";
      dialog.IsDirectional = true;
      dialog.IsDotted = true;
    };
    line.ShowDialog();
    line.Name.ShouldBe(result == DialogResult.OK ? "Updated" : "Original");
    line.Flow.ShouldBe(result == DialogResult.OK ? ConnectionFlow.OneWay : ConnectionFlow.TwoWay);
    line.Style.ShouldBe(result == DialogResult.OK ? ConnectionStyle.Dashed : ConnectionStyle.Solid);
  }

  [TestCase(DialogResult.OK)]
  [TestCase(DialogResult.Cancel)]
  public void ColorPicker_AppliesOnlyAcceptedColorWithoutNativeDialog(DialogResult result)
  {
    _interaction.Result = result;
    _interaction.EditCommonDialog = dialog => dialog.ShouldBeOfType<ColorDialog>().Color = Color.Blue;
    Colors.ShowColorDialog(Color.Red, null).ShouldBe(result == DialogResult.OK ? Color.Blue : Color.Red);
  }

  [Test]
  public void MessageBoundary_PreservesChoiceButtonsOwnerAndDefaultButton()
  {
    using var owner = new Form();
    _interaction.Result = DialogResult.No;
    UserInteraction.ShowMessage(
      owner,
      "Question",
      "Caption",
      MessageBoxButtons.YesNoCancel,
      MessageBoxIcon.Question,
      MessageBoxDefaultButton.Button2).ShouldBe(DialogResult.No);
    _interaction.Owner.ShouldBeSameAs(owner);
    _interaction.Buttons.ShouldBe(MessageBoxButtons.YesNoCancel);
    _interaction.DefaultButton.ShouldBe(MessageBoxDefaultButton.Button2);
  }

  [Test]
  public void ClipboardCopyPaste_UsesSubstitutedStorageRatherThanSystemClipboard()
  {
    var room = ProjectRegressionTests.AddRoom("Copied");
    var controller = new CopyController();
    controller.CopyElements(new List<Element> { room });
    _interaction.ClipboardText.ShouldContain("Copied");
    var copy = controller.PasteElements().ShouldBeOfType<CopyController.CopyObject>();
    copy.Rooms.ShouldHaveSingleItem().Name.ShouldBe("Copied");
  }

  [Test]
  public void ClipboardColors_RoundTripAllSixColorsWithoutChangingRoomContent()
  {
    var source = ProjectRegressionTests.AddRoom("Source");
    source.RoomBorderColor = Color.Red;
    source.RoomFillColor = Color.FromArgb(128, 20, 30, 40);
    source.RoomNameColor = Color.Blue;
    source.RoomObjectTextColor = Color.Green;
    source.RoomSubtitleColor = Color.Transparent;
    source.SecondFillColor = Color.Yellow;
    source.SecondFillLocation = "Top";
    var target = ProjectRegressionTests.AddRoom("Target");
    target.Objects = "lamp";
    var controller = new CopyController();
    controller.CopyColors(source);
    var copy = controller.PasteElements().ShouldBeOfType<CopyController.CopyColorsObj>();
    copy.Colors.Select(color => color.Name).ShouldBe(
      new[] {
        nameof(Room.RoomBorderColor), nameof(Room.RoomFillColor), nameof(Room.RoomNameColor),
        nameof(Room.RoomObjectTextColor), nameof(Room.RoomSubtitleColor), nameof(Room.SecondFillColor)
      });
    copy.SecondFillLocation.ShouldBe("Top");

    using var canvas = new Canvas();
    canvas.SelectedElement = target;
    Project.Current.IsDirty = false;
    canvas.Paste(false);

    target.RoomBorderColor.ToArgb().ShouldBe(source.RoomBorderColor.ToArgb());
    target.RoomFillColor.ToArgb().ShouldBe(source.RoomFillColor.ToArgb());
    target.RoomNameColor.ToArgb().ShouldBe(source.RoomNameColor.ToArgb());
    target.RoomObjectTextColor.ToArgb().ShouldBe(source.RoomObjectTextColor.ToArgb());
    target.RoomSubtitleColor.ToArgb().ShouldBe(source.RoomSubtitleColor.ToArgb());
    target.SecondFillColor.ToArgb().ShouldBe(source.SecondFillColor.ToArgb());
    target.SecondFillLocation.ShouldBe("Top");
    target.Name.ShouldBe("Target");
    target.Objects.ShouldBe("lamp");
    source.Name.ShouldBe("Source");
    Project.Current.IsDirty.ShouldBeTrue();
  }

  [Test]
  public void ClipboardColors_PartialAndUnknownKeysPreserveUnspecifiedProperties()
  {
    var room = ProjectRegressionTests.AddRoom("Target");
    room.RoomFillColor = Color.Blue;
    new CopyController().SetRoomColors(
      room,
      new CopyController.CopyColorsObj {
        Colors = new List<CopyController.CopyColorObj> {
          new() { Name = nameof(Room.RoomBorderColor), Color = Color.Red },
          new() { Name = "FutureColor", Color = Color.Black },
          new() { Name = nameof(Room.Name), Color = Color.Black },
          new() { Name = nameof(Room.RoomBorderColor), Color = Color.Green }
        },
        SecondFillLocation = "Top"
      });
    room.RoomBorderColor.ShouldBe(Color.Green);
    room.RoomFillColor.ShouldBe(Color.Blue);
    room.SecondFillLocation.ShouldBe("Top");
    room.Name.ShouldBe("Target");
  }

  [Test]
  public void UnexpectedUI_FailsImmediatelyRatherThanBlockingOnDialog()
  {
    UserInteraction.Current = new UnexpectedUserInteraction();
    Should.Throw<AssertionException>(() => UserInteraction.ShowMessage("Unexpected"));
    using (var form = new Form())
    {
      Should.Throw<AssertionException>(() => UserInteraction.ShowDialog(form));
    }

    using (var dialog = new OpenFileDialog())
    {
      Should.Throw<AssertionException>(() => UserInteraction.ShowDialog(dialog));
    }

    Should.Throw<AssertionException>(() => UserInteraction.GetClipboardText());
  }
}
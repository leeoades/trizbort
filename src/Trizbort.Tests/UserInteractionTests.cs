using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Controllers;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.UI;

namespace Trizbort.Tests {
  internal sealed class RecordingUserInteraction : IUserInteraction {
    public readonly List<string> Messages = new List<string>();
    public DialogResult Result = DialogResult.OK;
    public Action<Form> EditForm;
    public Action<CommonDialog> EditCommonDialog;
    public string ClipboardText = "";
    public MessageBoxButtons Buttons;
    public MessageBoxDefaultButton DefaultButton;
    public IWin32Window Owner;

    public DialogResult ShowMessage(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon,
      MessageBoxDefaultButton defaultButton) {
      Owner = owner;
      Buttons = buttons;
      DefaultButton = defaultButton;
      Messages.Add(caption + ": " + text);
      return Result;
    }

    public DialogResult ShowDialog(Form dialog, IWin32Window owner) {
      Owner = owner;
      EditForm?.Invoke(dialog);
      return Result;
    }

    public DialogResult ShowDialog(CommonDialog dialog, IWin32Window owner) {
      Owner = owner;
      EditCommonDialog?.Invoke(dialog);
      return Result;
    }

    public string GetClipboardText() => ClipboardText;
    public void SetClipboardText(string text, TextDataFormat format) => ClipboardText = text;
  }

  [TestFixture, Category("Integration")]
  public class UserInteractionTests : IsolatedProjectTests {
    private RecordingUserInteraction interaction;

    [SetUp]
    public void SubstituteUI() {
      interaction = new RecordingUserInteraction();
      UserInteraction.Current = interaction;
    }

    [Test]
    public void MapRoundTrip_UsesApplicationAssemblyVersionAndNoRunnerMetadata() {
      ProjectRegressionTests.AddRoom("Test Room");
      var path = Files.File("map.trizbort");
      new LegacyMapFileEngine(Project.Current).Save(path).ShouldBeTrue();
      XDocument.Load(path).Root.Attribute("version").Value.ShouldBe(typeof(Project).Assembly.GetName().Version.ToString());
      using (var loaded = new Project()) {
        new MapLoader(loaded).LoadMap(path).ShouldBeTrue();
        loaded.Elements.Count.ShouldBe(1);
        loaded.Version.ShouldBe(typeof(Project).Assembly.GetName().Version);
      }
      interaction.Messages.ShouldBeEmpty();
    }

    [Test]
    public void NewerDocument_ReportsVersionWarningThroughUIBoundary() {
      var current = typeof(Project).Assembly.GetName().Version;
      Project.Current.SetVersion(new Version(current.Major + 1, 0, 0, 0).ToString());
      Project.Current.CheckDocVersion();
      interaction.Messages.ShouldHaveSingleItem().ShouldContain("ahead a major version");
    }

    [Test]
    public void InvalidLoadAndSave_ReportErrorsWithoutOpeningMessageBoxes() {
      var path = Files.File("invalid.trizbort");
      File.WriteAllText(path, "<wrong/>");
      new MapLoader(Project.Current).LoadMap(path).ShouldBeFalse();
      interaction.Messages.ShouldHaveSingleItem().ShouldContain("problem loading");
      Project.Current.InitFileWWatcher(path);
      Project.Current.FileName = Files.File(@"missing\map.trizbort");
      Project.Current.Save().ShouldBeFalse();
      interaction.Messages.Count.ShouldBe(2);
      interaction.Messages[1].ShouldContain("problem saving");
    }

    [Test]
    public void DuplicateStartRooms_ReportWarningThroughUIBoundary() {
      var path = Files.File("duplicate.trizbort");
      File.WriteAllText(path, "<trizbort version=\"1.0\"><map><room id=\"1\" name=\"First\" isStartRoom=\"yes\"/>" +
        "<room id=\"2\" name=\"Second\" isStartRoom=\"yes\"/></map></trizbort>");
      new MapLoader(Project.Current).LoadMap(path).ShouldBeTrue();
      interaction.Messages.ShouldHaveSingleItem().ShouldContain("duplicate start room");
    }

    [TestCase(DialogResult.OK)]
    [TestCase(DialogResult.Cancel)]
    public void RoomProperties_AreAbstractableAndApplyOnlyOnOK(DialogResult result) {
      var room = ProjectRegressionTests.AddRoom("Original");
      interaction.Result = result;
      interaction.EditForm = form => {
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
    public void ConnectionProperties_AreAbstractableAndApplyOnlyOnOK(DialogResult result) {
      var line = new Connection(Project.Current) {Name = "Original"};
      interaction.Result = result;
      interaction.EditForm = form => {
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
    public void ColorPicker_AppliesOnlyAcceptedColorWithoutNativeDialog(DialogResult result) {
      interaction.Result = result;
      interaction.EditCommonDialog = dialog => dialog.ShouldBeOfType<ColorDialog>().Color = Color.Blue;
      Colors.ShowColorDialog(Color.Red, null).ShouldBe(result == DialogResult.OK ? Color.Blue : Color.Red);
    }

    [Test]
    public void MessageBoundary_PreservesChoiceButtonsOwnerAndDefaultButton() {
      using (var owner = new Form()) {
        interaction.Result = DialogResult.No;
        UserInteraction.ShowMessage(owner, "Question", "Caption", MessageBoxButtons.YesNoCancel,
          MessageBoxIcon.Question, MessageBoxDefaultButton.Button2).ShouldBe(DialogResult.No);
        interaction.Owner.ShouldBeSameAs(owner);
        interaction.Buttons.ShouldBe(MessageBoxButtons.YesNoCancel);
        interaction.DefaultButton.ShouldBe(MessageBoxDefaultButton.Button2);
      }
    }

    [Test]
    public void ClipboardCopyPaste_UsesSubstitutedStorageRatherThanSystemClipboard() {
      var room = ProjectRegressionTests.AddRoom("Copied");
      var controller = new CopyController();
      controller.CopyElements(new List<Element> {room});
      interaction.ClipboardText.ShouldContain("Copied");
      var copy = controller.PasteElements().ShouldBeOfType<CopyController.CopyObject>();
      copy.Rooms.ShouldHaveSingleItem().Name.ShouldBe("Copied");
    }

    [Test]
    public void UnexpectedUI_FailsImmediatelyRatherThanBlockingOnDialog() {
      UserInteraction.Current = new UnexpectedUserInteraction();
      Should.Throw<AssertionException>(() => UserInteraction.ShowMessage("Unexpected"));
      using (var form = new Form())
        Should.Throw<AssertionException>(() => UserInteraction.ShowDialog(form));
      using (var dialog = new OpenFileDialog())
        Should.Throw<AssertionException>(() => UserInteraction.ShowDialog(dialog));
      Should.Throw<AssertionException>(() => UserInteraction.GetClipboardText());
    }
  }
}

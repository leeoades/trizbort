using System;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.UI;

namespace Trizbort.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public class RoomPropertiesDialogTests : IsolatedProjectTests
{
  [SetUp]
  public void ResetRememberedTab()
  {
    _previousTab = RoomPropertiesDialog.LastClosedTab;
    RoomPropertiesDialog.LastClosedTab = RoomPropertiesDialog.Tab.Objects;
  }

  [TearDown]
  public void RestoreRememberedTab()
  {
    RoomPropertiesDialog.LastClosedTab = _previousTab;
  }

  private RoomPropertiesDialog.Tab _previousTab;

  [TestCase(PropertiesStartType.RoomName, "tabObjects", "txtObjects")]
  [TestCase(PropertiesStartType.Objects, "tabObjects", "txtObjects")]
  [TestCase(PropertiesStartType.Region, "tabRegions", "cboRegion")]
  public void Dialog_OpensWithExpectedTabAndFocus(PropertiesStartType start, string tabName, string controlName)
  {
    using var dialog = CreateDialog(start);
    AssertOpening(dialog, tabName, controlName);
  }

  [TestCase("tabDescription", "m_descriptionTextBox")]
  [TestCase("tabObjects", "txtObjects")]
  [TestCase("tabColors", "m_changeRoomFillButton")]
  [TestCase("tabRegions", "cboRegion")]
  [TestCase("tabRoomShapes", "cboDrawType")]
  public void Dialog_RestoresLastClosedTabAndFocus(string tabName, string controlName)
  {
    using (var previous = CreateDialog(PropertiesStartType.Objects))
    {
      AssertOpening(
        previous,
        "tabObjects",
        "txtObjects",
        beforeClose: form => SelectTab(form, tabName));
    }

    using (var dialog = CreateDialog(PropertiesStartType.Objects))
    {
      AssertOpening(dialog, tabName, controlName);
    }
  }

  [TestCase(DialogResult.OK)]
  [TestCase(DialogResult.Cancel)]
  [TestCase(DialogResult.None)]
  public void Dialog_RemembersTabRegardlessOfCloseResult(DialogResult result)
  {
    using (var previous = CreateDialog(PropertiesStartType.Objects))
    {
      AssertOpening(
        previous,
        "tabObjects",
        "txtObjects",
        result,
        form => SelectTab(form, "tabDescription"));
    }

    using (var dialog = CreateDialog(PropertiesStartType.Objects))
    {
      AssertOpening(dialog, "tabDescription", "m_descriptionTextBox");
    }
  }

  [Test]
  public void Dialog_DoesNotRememberTabUntilClosed()
  {
    using var unshown = CreateDialog(PropertiesStartType.Objects);
    SelectTab(unshown, "tabColors");
    using var dialog = CreateDialog(PropertiesStartType.Objects);
    AssertOpening(dialog, "tabObjects", "txtObjects");
  }

  [Test]
  public void Dialog_RegionShortcutOverridesAndUpdatesRememberedTab()
  {
    using (var previous = CreateDialog(PropertiesStartType.Objects))
    {
      AssertOpening(
        previous,
        "tabObjects",
        "txtObjects",
        beforeClose: form => SelectTab(form, "tabColors"));
    }

    using (var region = CreateDialog(PropertiesStartType.Region))
    {
      AssertOpening(region, "tabRegions", "cboRegion");
    }

    using (var dialog = CreateDialog(PropertiesStartType.Objects))
    {
      AssertOpening(dialog, "tabRegions", "cboRegion");
    }
  }

  [TestCase(false)]
  [TestCase(true)]
  public void Room_NormalOpeningRestoresTabEvenForNewOrLoadedDefaultNamedRoom(bool loaded)
  {
    using (var previous = CreateDialog(PropertiesStartType.Objects))
    {
      AssertOpening(
        previous,
        "tabObjects",
        "txtObjects",
        beforeClose: form => SelectTab(form, "tabDescription"));
    }

    var room = loaded ? new Room(Project.Current, 1) : new Room(Project.Current);
    UserInteraction.Current = new RecordingUserInteraction {
      Result = DialogResult.Cancel,
      EditForm = dialog => AssertOpening(dialog, "tabDescription", "m_descriptionTextBox")
    };
    room.ShowDialog();
    room.ShowDialog();
  }

  [TestCase(Keys.O, "tabObjects", "txtObjects")]
  [TestCase(Keys.E, "tabDescription", "m_descriptionTextBox")]
  public void Dialog_TabShortcutsSelectAndFocusExpectedBox(Keys key, string tabName, string controlName)
  {
    using var dialog = CreateDialog(PropertiesStartType.Objects);
    dialog.Shown += (sender, e) => dialog.PressAltKey(key);
    AssertOpening(dialog, tabName, controlName);
  }

  private static void AssertOpening(
    Form dialog,
    string tabName,
    string controlName,
    DialogResult result = DialogResult.Cancel,
    Action<Form> beforeClose = null)
  {
    string selectedTab = null;
    var focused = false;
    dialog.Shown += (sender, e) => dialog.BeginInvoke(() => {
      var tabs = (TabControl)dialog.Controls.Find("m_tabControl", true)[0];
      selectedTab = tabs.SelectedTab.Name;
      focused = dialog.Controls.Find(controlName, true)[0].Focused;
      beforeClose?.Invoke(dialog);
      dialog.DialogResult = result;
      dialog.Close();
    });

    TestDialog.Show(dialog);

    selectedTab.ShouldBe(tabName);
    focused.ShouldBeTrue();
  }

  private static TestRoomPropertiesDialog CreateDialog(PropertiesStartType start)
  {
    var dialog = new TestRoomPropertiesDialog(start);
    ((ComboBox)dialog.Controls.Find("cboDrawType", true)[0]).SelectedIndex = 0;
    return dialog;
  }

  private class TestRoomPropertiesDialog(PropertiesStartType start) : RoomPropertiesDialog(start, 0)
  {
    public void PressAltKey(Keys key)
    {
      OnKeyUp(new KeyEventArgs(Keys.Alt | key));
    }
  }

  private static void SelectTab(Form dialog, string name)
  {
    var tabs = (TabControl)dialog.Controls.Find("m_tabControl", true)[0];
    tabs.SelectedTab = tabs.TabPages[name];
  }
}
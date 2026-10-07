using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Elements;
using Trizbort.UI;

namespace Trizbort.Tests {
  [TestFixture]
  [Apartment(ApartmentState.STA)]
  public class RoomPropertiesDialogTests {
    [TestCase(PropertiesStartType.RoomName, "tabObjects", "txtName")]
    [TestCase(PropertiesStartType.Objects, "tabObjects", "txtObjects")]
    [TestCase(PropertiesStartType.Region, "tabRegions", "cboRegion")]
    public void Dialog_OpensWithExpectedTabAndFocus(PropertiesStartType start, string tabName, string controlName) {
      using (var dialog = CreateDialog(start)) {
        AssertOpening(dialog, tabName, controlName);
      }
    }

    [Test]
    public void Dialog_ReopensOnObjectsAfterViewingAnotherTab() {
      using (var previous = CreateDialog(PropertiesStartType.Objects)) {
        var tabs = (TabControl)previous.Controls.Find("m_tabControl", true)[0];
        tabs.SelectedTab = tabs.TabPages["tabColors"];
      }

      using (var dialog = CreateDialog(PropertiesStartType.Objects)) {
        AssertOpening(dialog, "tabObjects", "txtObjects");
      }
    }

    [TestCase(Keys.O, "tabObjects", "txtObjects")]
    [TestCase(Keys.E, "tabDescription", "m_descriptionTextBox")]
    public void Dialog_TabShortcutsSelectAndFocusExpectedBox(Keys key, string tabName, string controlName) {
      using (var dialog = CreateDialog(PropertiesStartType.Objects)) {
        dialog.Shown += (sender, e) => typeof(Form)
          .GetMethod("OnKeyUp", BindingFlags.Instance | BindingFlags.NonPublic)
          .Invoke(dialog, new object[] { new KeyEventArgs(Keys.Alt | key) });
        AssertOpening(dialog, tabName, controlName);
      }
    }

    private static void AssertOpening(Form dialog, string tabName, string controlName) {
      string selectedTab = null;
      var focused = false;
      dialog.Shown += (sender, e) => dialog.BeginInvoke(new Action(() => {
        var tabs = (TabControl)dialog.Controls.Find("m_tabControl", true)[0];
        selectedTab = tabs.SelectedTab.Name;
        focused = dialog.Controls.Find(controlName, true)[0].Focused;
        dialog.Close();
      }));

      dialog.ShowDialog();

      selectedTab.ShouldBe(tabName);
      focused.ShouldBeTrue();
    }

    private static Form CreateDialog(PropertiesStartType start) {
      var type = typeof(OnlineHelpDialog).Assembly.GetType("Trizbort.UI.RoomPropertiesDialog", true);
      return (Form)Activator.CreateInstance(type, start, 0);
    }
  }
}

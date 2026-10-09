using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Shouldly;
using Trizbort.UI;

namespace Trizbort.Tests {
  [TestFixture]
  [Apartment(ApartmentState.STA)]
  public class OnlineHelpDialogTests {
    [Test]
    public void Dialog_ListsUserGuideFirstThenOriginalHelp() {
      using var dialog = new OnlineHelpDialog();
      var layout = dialog.Controls.OfType<TableLayoutPanel>().Single();
      var labels = layout.Controls.OfType<Label>().Where(label => !(label is LinkLabel)).ToList();
      labels[0].Text.ShouldContain("Trizbort v2 user guide");
      labels[1].Text.ShouldContain("original Trizbort v1 help");

      var links = layout.Controls.OfType<LinkLabel>().ToList();
      links.Select(link => link.Text).ShouldBe(new[] {
        "https://github.com/leeoades/trizbort/blob/master/Docs/index.md",
        "https://trizbort.genstein.net/help/"
      });
      links.All(link => link.Links.Count == 1 && link.TabStop).ShouldBeTrue();
      dialog.StartPosition.ShouldBe(FormStartPosition.CenterParent);
      dialog.FormBorderStyle.ShouldBe(FormBorderStyle.FixedDialog);
    }

    [Test]
    public void Dialog_AutosizesToKeepAllContentVisible() {
      using var dialog = new OnlineHelpDialog();
      var layout = dialog.Controls.OfType<TableLayoutPanel>().Single();
      Rectangle layoutBounds = Rectangle.Empty;
      Rectangle[] controlBounds = null;
      var labelFits = false;
      dialog.Shown += (sender, e) => dialog.BeginInvoke(new Action(() => {
        layoutBounds = layout.ClientRectangle;
        controlBounds = layout.Controls.Cast<Control>().Select(control => control.Bounds).ToArray();
        labelFits = layout.Controls[0].Height >= layout.Controls[0].PreferredSize.Height;
        dialog.Close();
      }));

      TestDialog.Show(dialog);

      controlBounds.ShouldNotBeNull();
      controlBounds.All(bounds => layoutBounds.Contains(bounds)).ShouldBeTrue();
      labelFits.ShouldBeTrue();
    }

    [TestCase(Keys.Enter)]
    [TestCase(Keys.Escape)]
    public void Dialog_CanBeDismissedWithKeyboard(Keys key) {
      using var dialog = new KeyboardHelpDialog();
      var handled = false;
      var result = DialogResult.None;
      dialog.Shown += (sender, e) => dialog.BeginInvoke(new Action(() => {
        dialog.ActiveControl = (Button)dialog.AcceptButton;
        handled = dialog.PressKey(key);
        result = dialog.DialogResult;
        dialog.Close();
      }));

      TestDialog.Show(dialog).ShouldBe(DialogResult.OK);
      result.ShouldBe(DialogResult.OK);
      handled.ShouldBeTrue();
    }

    private class KeyboardHelpDialog : OnlineHelpDialog {
      public bool PressKey(Keys key) {
        return ProcessDialogKey(key);
      }
    }
  }
}

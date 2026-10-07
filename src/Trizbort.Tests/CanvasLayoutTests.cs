using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Shouldly;
using Trizbort.UI.Controls;

namespace Trizbort.Tests {
  [TestFixture]
  [Apartment(ApartmentState.STA)]
  public class CanvasLayoutTests {
    [TestCase(false)]
    [TestCase(true)]
    public void Canvas_HasNoFloatingControlAtFormerCornerPanelPositionAfterResizing(bool minimapVisible) {
      using (var canvas = new Canvas()) {
        canvas.MinimapVisible = minimapVisible;
        canvas.Controls.OfType<Panel>().ShouldBeEmpty();

        foreach (var size in new[] {new Size(718, 492), new Size(960, 720), new Size(600, 400)}) {
          canvas.Size = size;
          canvas.PerformLayout();

          var formerPanelCentre = new Point(canvas.Width - 263, canvas.Height - 182);
          canvas.GetChildAtPoint(formerPanelCentre).ShouldBeNull();

          var horizontal = canvas.Controls.OfType<HScrollBar>().Single();
          var vertical = canvas.Controls.OfType<VScrollBar>().Single();
          horizontal.Bottom.ShouldBe(canvas.ClientSize.Height);
          vertical.Right.ShouldBe(canvas.ClientSize.Width);
          horizontal.Right.ShouldBe(vertical.Left);
        }
      }
    }

  }
}

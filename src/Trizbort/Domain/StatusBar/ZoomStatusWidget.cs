using System.Drawing;
using System.Windows.Forms;
using Trizbort.Domain.Application;

namespace Trizbort.Domain.StatusBar;

public class ZoomStatusWidget : IStatusWidget {
  private readonly ContextMenuStrip _menu;

  public ZoomStatusWidget()
  {
    _menu = new ContextMenuStrip();
    _menu.Items.Add("Zoom 300%", null, (o, args) => SetZoom(o, 3.00f));
    _menu.Items.Add("Zoom 250%", null, (o, args) => SetZoom(o, 2.50f));
    _menu.Items.Add("Zoom 200%", null, (o, args) => SetZoom(o, 2.00f));
    _menu.Items.Add("Zoom 175%", null, (o, args) => SetZoom(o, 1.75f));
    _menu.Items.Add("Zoom 150%", null, (o, args) => SetZoom(o, 1.50f));
    _menu.Items.Add("Zoom 125%", null, (o, args) => SetZoom(o, 1.25f));
    _menu.Items.Add("Zoom 100%", null, (o, args) => SetZoom(o, 1.00f));
    _menu.Items.Add("Zoom 75%", null, (o, args) => SetZoom(o, 0.75f));
    _menu.Items.Add("Zoom 50%", null, (o, args) => SetZoom(o, 0.50f));
  }

  public StatusItems Id => StatusItems.TsbCapsLock;
  public string Name => "Zoom";
  public string MenuName => "Map Zoom";
  public string HelpText => "Mousewheel Up/Down to change zoom, Ctrl Mousewheel gives finer control.";

  public Color DisplayColor => Color.Black;

  public string DisplayText()
  {
    return Project.Current.Canvas.ZoomFactor.ToString("P");
  }

  public void ClickHandler()
  {
    _menu.Show(Project.Current.Canvas, Cursor.Position);
  }

  private void SetZoom(object sender, float zoomFactor)
  {
    Project.Current.Canvas.ZoomFactor = zoomFactor;
  }
}
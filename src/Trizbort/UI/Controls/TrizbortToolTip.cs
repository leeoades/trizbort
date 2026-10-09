using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Trizbort.UI.Controls;

public class TrizbortToolTip : ToolTip {
  private const int TipWidth = 200;
  private const int LineBuffer = 5;

  private readonly Font _bodyFont;
  private readonly Font _footerFont;
  private readonly Font _headerFont;
  private int _bodyHeight;
  private int _footerHeight;
  private double _headerBottom;
  private int _headerHeight;
  private int _tipHeight;


  public TrizbortToolTip()
  {
    InitialDelay = 500;
    ReshowDelay = 500;
    AutoPopDelay = 5000;
    OwnerDraw = true;
    Popup += OnPopup;
    Draw += OnDraw;
    ForeColor = Color.Black;
    BackColor = Color.LightBlue;
    _bodyFont = new Font("Arial", 8.0f, FontStyle.Regular);
    _headerFont = new Font("Arial", 8.0f, FontStyle.Bold);
    _footerFont = new Font("Arial", 8.0f, FontStyle.Bold);
  }

  public string TitleText { get; set; }
  public string BodyText { get; set; }
  public string FooterText { get; set; }
  public bool IsShown { get; set; }
  public IWin32Window LastOwner { get; set; }
  public Point LastPosition { get; private set; }
  public object HoverElement { get; set; }

  public Color GradientColor { get; set; } = Color.Empty;

  private void OnPopup(object sender, PopupEventArgs e)
  {
    LastOwner = e.AssociatedControl;
    IsShown = true;
    _headerHeight = 0;
    _footerHeight = 0;
    _bodyHeight = 0;

    using Image fakeImage = new Bitmap(1, 1);
    using var graphics = Graphics.FromImage(fakeImage);

    //calc header size
    var headerStringSize = graphics.MeasureString(TitleText, _headerFont);

    //calc body size
    var bodyStringSize = graphics.MeasureString(BodyText, _bodyFont);

    //calc footer size
    var footerStringSize = graphics.MeasureString(FooterText, _footerFont);

    _headerBottom = Math.Ceiling(headerStringSize.Width / TipWidth * (_headerFont.Height + LineBuffer));
    _headerHeight = Convert.ToInt32(_headerBottom) + (int)headerStringSize.Height + LineBuffer;
    if (FooterText != string.Empty)
      _footerHeight =
        Convert.ToInt32(Math.Ceiling(footerStringSize.Width / TipWidth * (_footerFont.Height + LineBuffer))) +
        (int)footerStringSize.Height + LineBuffer;

    if (BodyText != string.Empty)
      _bodyHeight = Convert.ToInt32(Math.Ceiling(bodyStringSize.Width / TipWidth * (_bodyFont.Height + LineBuffer))) +
                    (int)bodyStringSize.Height + LineBuffer;


    _tipHeight = _headerHeight + _bodyHeight + _footerHeight;

    e.ToolTipSize = new Size(TipWidth, _tipHeight);
  }

  private void OnDraw(object sender, DrawToolTipEventArgs e)
  {
    var g = e.Graphics;

    Brush b;
    if (GradientColor != Color.Empty)
      b = new LinearGradientBrush(e.Bounds, BackColor, GradientColor, 45f);
    else
      b = new SolidBrush(BackColor);

    using var textBrush = new SolidBrush(ForeColor);
    g.FillRectangle(b, e.Bounds);

    // draw header
    float titleBoundsY = 0;
    if (TitleText != string.Empty) {
      var titleBounds = new RectangleF(
        new PointF(e.Bounds.X + LineBuffer, e.Bounds.Y + LineBuffer),
        new SizeF(TipWidth - 20, _headerHeight));
      g.DrawString(TitleText, _headerFont, textBrush, titleBounds); // top layer
      titleBoundsY = titleBounds.Y;
    }

    // draw body
    if (BodyText != string.Empty) {
      var bodyBounds = new RectangleF(
        new PointF(e.Bounds.X + LineBuffer + 10, titleBoundsY + _headerFont.Height + 6),
        new SizeF(TipWidth - 20, _bodyHeight));
      g.DrawString(BodyText, _bodyFont, textBrush, bodyBounds); // top layer
    }

    // draw footer
    if (FooterText != string.Empty) {
      var footerBounds = new RectangleF(
        new PointF(e.Bounds.X + LineBuffer, e.Bounds.Y + _headerHeight + _bodyHeight + LineBuffer),
        new SizeF(TipWidth - 20, _footerHeight));
      using (var pen = new Pen(Color.Gray)) {
        g.DrawLine(pen, new PointF(0f, footerBounds.Y), new PointF(TipWidth, footerBounds.Y));
      }

      g.DrawString(
        FooterText,
        _footerFont,
        textBrush,
        new RectangleF(new PointF(footerBounds.Location.X, footerBounds.Location.Y + 2), footerBounds.Size));
    }

    b.Dispose();
  }

  protected override void Dispose(bool disposing)
  {
    if (disposing) {
      _bodyFont.Dispose();
      _headerFont.Dispose();
      _footerFont.Dispose();
    }

    base.Dispose(disposing);
  }

  public bool IsPositionChanged(Point position)
  {
    return position.X != LastPosition.X || position.Y != LastPosition.Y;
  }

  public new void Show(string text, IWin32Window window)
  {
    LastOwner = window;
    base.Show(text, window);
    IsShown = true;
  }

  public new void Show(string text, IWin32Window window, int duration)
  {
    LastOwner = window;
    base.Show(text, window, duration);
    IsShown = true;
  }

  public new void Show(string text, IWin32Window window, Point point)
  {
    LastOwner = window;
    LastPosition = point;
    base.Show(text, window, point);
    IsShown = true;
  }

  public new void Show(string text, IWin32Window window, int x, int y)
  {
    LastOwner = window;
    LastPosition = new Point(x, y);
    base.Show(text, window, x, y);
    IsShown = true;
  }

  public new void Show(string text, IWin32Window window, Point point, int duration)
  {
    LastOwner = window;
    LastPosition = point;
    base.Show(text, window, point, duration);
    IsShown = true;
  }

  public new void Show(string text, IWin32Window window, int x, int y, int duration)
  {
    LastOwner = window;
    LastPosition = new Point(x, y);
    base.Show(text, window, x, y, duration);
    IsShown = true;
  }

  public new void Hide(IWin32Window win)
  {
    LastOwner = null; // not really necessary
    HoverElement = null; // as above - this one is being set outside of the class
    base.Hide(win);
    IsShown = false;
  }
}
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using PdfSharp.Drawing;
using Trizbort.Setup;

namespace Trizbort.Domain.Misc {
  /// <summary>
  ///   A palette of drawing tools such as brushes, pens and fonts.
  /// </summary>
  /// <remarks>
  ///   Centralising such tools in one place avoids tedious management
  ///   of their lifetimes and avoids creating them until necessary,
  ///   and then only once.
  /// </remarks>
  public class Palette : IDisposable {
    private readonly List<IDisposable> _items = new List<IDisposable>();
    private Brush _borderBrush;
    private Pen _borderPen;
    private Brush _canvasBrush;
    private Pen _dashedLinePen;
    private Brush _fillBrush;
    // TODO: private Pen m_fillPen;
    private Pen _gridPen;
    private Pen _hoverDashedLinePen;
    private Brush _hoverLineBrush;
    private Pen _hoverLinePen;

    private Brush _lineBrush;

    private Pen _linePen;
    private Brush _lineTextBrush;
    private Pen _marqueeBorderPen;
    private Brush _marqueeFillBrush;
    private Pen _resizeBorderPen;
    private Pen _selectedDashedLinePen;
    private Brush _selectedLineBrush;
    private Pen _selectedLinePen;
    private Brush _smallTextBrush;
    private Brush _subTitleTextBrush;
    // TODO: private Pen m_subTitleTextPen;

    public Brush BorderBrush => _borderBrush ?? (_borderBrush = Brush(Settings.Color[Colors.Border]));

    public Pen BorderPen => _borderPen ?? (_borderPen = Pen(Settings.Color[Colors.Border]));

    public Brush CanvasBrush => _canvasBrush ?? (_canvasBrush = Brush(Settings.Color[Colors.Canvas]));

    public Pen DashedLinePen {
      get {
        if (_dashedLinePen == null) {
          _dashedLinePen = Pen(Settings.Color[Colors.Line]);
          _dashedLinePen.DashStyle = DashStyle.Dot;
        }

        return _dashedLinePen;
      }
    }

    public Brush FillBrush => _fillBrush ?? (_fillBrush = Brush(Color.White));

    //public Pen FillPen
    //{
    //    get { return m_fillPen ?? (m_fillPen = Pen(Settings.Color[Colors.Fill])); }
    //}

    public Pen GridPen => _gridPen ?? (_gridPen = Pen(Settings.Color[Colors.Grid], 0));

    public Pen HoverDashedLinePen {
      get {
        if (_hoverDashedLinePen == null) {
          _hoverDashedLinePen = Pen(Settings.Color[Colors.HoverLine]);
          _hoverDashedLinePen.DashStyle = DashStyle.Dot;
        }

        return _hoverDashedLinePen;
      }
    }

    public Brush HoverLineBrush => _hoverLineBrush ?? (_hoverLineBrush = Brush(Settings.Color[Colors.HoverLine]));
    public Pen HoverLinePen => _hoverLinePen ?? (_hoverLinePen = Pen(Settings.Color[Colors.HoverLine]));

    public Brush LineBrush => _lineBrush ?? (_lineBrush = Brush(Settings.Color[Colors.Line]));
    public Pen LinePen => _linePen ?? (_linePen = Pen(Settings.Color[Colors.Line]));
    public Brush LineTextBrush => _lineTextBrush ?? (_lineTextBrush = Brush(Settings.Color[Colors.LineText]));

    public Pen MarqueeBorderPen => _marqueeBorderPen ?? (_marqueeBorderPen = Pen(Color.FromArgb(120, Settings.Color[Colors.Border]), 0));
    public Brush MarqueeFillBrush => _marqueeFillBrush ?? (_marqueeFillBrush = Brush(Color.FromArgb(80, Settings.Color[Colors.Border])));

    public Pen ResizeBorderPen => _resizeBorderPen ?? (_resizeBorderPen = Pen(Color.FromArgb(64, Color.SteelBlue), 6));

    public Pen SelectedDashedLinePen {
      get {
        if (_selectedDashedLinePen == null) {
          _selectedDashedLinePen = Pen(Settings.Color[Colors.SelectedLine]);
          _selectedDashedLinePen.DashStyle = DashStyle.Dot;
        }

        return _selectedDashedLinePen;
      }
    }

    public Brush SelectedLineBrush => _selectedLineBrush ?? (_selectedLineBrush = Brush(Settings.Color[Colors.SelectedLine]));
    public Pen SelectedLinePen => _selectedLinePen ?? (_selectedLinePen = Pen(Settings.Color[Colors.SelectedLine]));

    public Brush SmallTextBrush => _smallTextBrush ?? (_smallTextBrush = Brush(Settings.Color[Colors.SmallText]));
    public Brush SubtitleTextBrush => _subTitleTextBrush ?? (_subTitleTextBrush = Brush(Settings.Color[Colors.Subtitle]));

    public void Dispose() {
      foreach (var item in _items) item.Dispose();
    }

    public Brush Brush(Color color) {
      var brush = new SolidBrush(color);
      _items.Add(brush);
      return brush;
    }

    public Font Font(string familyName, float emSize) {
      var font = new Font(familyName, emSize, FontStyle.Regular, GraphicsUnit.World);
      _items.Add(font);
      return font;
    }

    public Font Font(Font prototype, FontStyle newStyle) {
      var font = new Font(prototype, newStyle);
      _items.Add(font);
      return font;
    }

    public Brush GetLineBrush(bool selected, bool hover) {
      if (selected)
        return SelectedLineBrush;
      if (hover) return HoverLineBrush;
      return LineBrush;
    }

    public Pen GetLinePen(bool selected, bool hover, bool dashed) {
      if (selected)
        return dashed ? SelectedDashedLinePen : SelectedLinePen;
      if (hover) return dashed ? HoverDashedLinePen : HoverLinePen;
      return dashed ? DashedLinePen : LinePen;
    }

    public XBrush Gradient(Rect rect, Color color1, Color color2) {
      var brush = new XLinearGradientBrush(rect.ToRectangleF(), color1, color2, XLinearGradientMode.ForwardDiagonal);
      return brush;
    }

    public XGraphicsPath Path() {
      var path = new XGraphicsPath();
      //m_items.Add(path);
      return path;
    }

    public Pen Pen(Color color) {
      return Pen(color, Settings.LineWidth);
    }

    public Pen Pen(Color color, float width) {
      var pen = new Pen(color, width) {StartCap = LineCap.Round, EndCap = LineCap.Round};
      _items.Add(pen);
      return pen;
    }
  }
}
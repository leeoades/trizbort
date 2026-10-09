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
    private readonly List<IDisposable> _mItems = new List<IDisposable>();
    private Brush _mBorderBrush;
    private Pen _mBorderPen;
    private Brush _mCanvasBrush;
    private Pen _mDashedLinePen;
    private Brush _mFillBrush;
    // TODO: private Pen m_fillPen;
    private Pen _mGridPen;
    private Pen _mHoverDashedLinePen;
    private Brush _mHoverLineBrush;
    private Pen _mHoverLinePen;

    private Brush _mLineBrush;

    private Pen _mLinePen;
    private Brush _mLineTextBrush;
    private Pen _mMarqueeBorderPen;
    private Brush _mMarqueeFillBrush;
    private Pen _mResizeBorderPen;
    private Pen _mSelectedDashedLinePen;
    private Brush _mSelectedLineBrush;
    private Pen _mSelectedLinePen;
    private Brush _mSmallTextBrush;
    private Brush _mSubTitleTextBrush;
    // TODO: private Pen m_subTitleTextPen;

    public Brush BorderBrush => _mBorderBrush ?? (_mBorderBrush = Brush(Settings.Color[Colors.Border]));

    public Pen BorderPen => _mBorderPen ?? (_mBorderPen = Pen(Settings.Color[Colors.Border]));

    public Brush CanvasBrush => _mCanvasBrush ?? (_mCanvasBrush = Brush(Settings.Color[Colors.Canvas]));

    public Pen DashedLinePen {
      get {
        if (_mDashedLinePen == null) {
          _mDashedLinePen = Pen(Settings.Color[Colors.Line]);
          _mDashedLinePen.DashStyle = DashStyle.Dot;
        }

        return _mDashedLinePen;
      }
    }

    public Brush FillBrush => _mFillBrush ?? (_mFillBrush = Brush(Color.White));

    //public Pen FillPen
    //{
    //    get { return m_fillPen ?? (m_fillPen = Pen(Settings.Color[Colors.Fill])); }
    //}

    public Pen GridPen => _mGridPen ?? (_mGridPen = Pen(Settings.Color[Colors.Grid], 0));

    public Pen HoverDashedLinePen {
      get {
        if (_mHoverDashedLinePen == null) {
          _mHoverDashedLinePen = Pen(Settings.Color[Colors.HoverLine]);
          _mHoverDashedLinePen.DashStyle = DashStyle.Dot;
        }

        return _mHoverDashedLinePen;
      }
    }

    public Brush HoverLineBrush => _mHoverLineBrush ?? (_mHoverLineBrush = Brush(Settings.Color[Colors.HoverLine]));
    public Pen HoverLinePen => _mHoverLinePen ?? (_mHoverLinePen = Pen(Settings.Color[Colors.HoverLine]));

    public Brush LineBrush => _mLineBrush ?? (_mLineBrush = Brush(Settings.Color[Colors.Line]));
    public Pen LinePen => _mLinePen ?? (_mLinePen = Pen(Settings.Color[Colors.Line]));
    public Brush LineTextBrush => _mLineTextBrush ?? (_mLineTextBrush = Brush(Settings.Color[Colors.LineText]));

    public Pen MarqueeBorderPen => _mMarqueeBorderPen ?? (_mMarqueeBorderPen = Pen(Color.FromArgb(120, Settings.Color[Colors.Border]), 0));
    public Brush MarqueeFillBrush => _mMarqueeFillBrush ?? (_mMarqueeFillBrush = Brush(Color.FromArgb(80, Settings.Color[Colors.Border])));

    public Pen ResizeBorderPen => _mResizeBorderPen ?? (_mResizeBorderPen = Pen(Color.FromArgb(64, Color.SteelBlue), 6));

    public Pen SelectedDashedLinePen {
      get {
        if (_mSelectedDashedLinePen == null) {
          _mSelectedDashedLinePen = Pen(Settings.Color[Colors.SelectedLine]);
          _mSelectedDashedLinePen.DashStyle = DashStyle.Dot;
        }

        return _mSelectedDashedLinePen;
      }
    }

    public Brush SelectedLineBrush => _mSelectedLineBrush ?? (_mSelectedLineBrush = Brush(Settings.Color[Colors.SelectedLine]));
    public Pen SelectedLinePen => _mSelectedLinePen ?? (_mSelectedLinePen = Pen(Settings.Color[Colors.SelectedLine]));

    public Brush SmallTextBrush => _mSmallTextBrush ?? (_mSmallTextBrush = Brush(Settings.Color[Colors.SmallText]));
    public Brush SubtitleTextBrush => _mSubTitleTextBrush ?? (_mSubTitleTextBrush = Brush(Settings.Color[Colors.Subtitle]));

    public void Dispose() {
      foreach (var item in _mItems) item.Dispose();
    }

    public Brush Brush(Color color) {
      var brush = new SolidBrush(color);
      _mItems.Add(brush);
      return brush;
    }

    public Font Font(string familyName, float emSize) {
      var font = new Font(familyName, emSize, FontStyle.Regular, GraphicsUnit.World);
      _mItems.Add(font);
      return font;
    }

    public Font Font(Font prototype, FontStyle newStyle) {
      var font = new Font(prototype, newStyle);
      _mItems.Add(font);
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
      _mItems.Add(pen);
      return pen;
    }
  }
}
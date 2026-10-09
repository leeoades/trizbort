using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using PdfSharp.Drawing;

namespace Trizbort.Domain.Misc {
  internal class TextBlock {
    public static int SRebuildCount;
    private readonly List<string> _mLines = new List<string>();
    private XStringFormat _mActualFormat;

    private Vector _mDelta;

    // cached layout data to speed drawing
    private bool _mInvalidLayout = true;
    private float _mLineHeight;
    private Vector _mOrigin;
    private Vector _mPos;
    private XStringFormat _mRequestedFormat;
    private Vector _mSize;
    private XSize _mSizeChecker;
    private string _mText = string.Empty;

    public static int RebuildCount => SRebuildCount;

    public string Text {
      get => _mText;
      set {
        _mText = value;
        _mInvalidLayout = true;
      }
    }

    /// <summary>
    ///   Draw a multi-line string as it would be drawn by GDI+.
    ///   Compensates for issues and draw-vs-PDF differences in PDFsharp.
    /// </summary>
    /// <param name="graphics">The graphics with which to draw.</param>
    /// <param name="font">The font with which to draw.</param>
    /// <param name="brush">The brush with which to draw.</param>
    /// <param name="pos">The position at which to draw.</param>
    /// <param name="size">The size to which to limit the drawn text; or Vector.Zero for no limit.</param>
    /// <param name="format">The string format to use.</param>
    /// <param name="maxObjects"></param>
    /// <param name="text">The text to draw, which may contain line breaks.</param>
    /// <remarks>
    ///   PDFsharp cannot currently render multi-line text to PDF files; it comes out as single line.
    ///   This method simulates standard Graphics.DrawString() over PDFsharp.
    ///   It always has the effect of StringFormatFlags.LineLimit (which PDFsharp does not support).
    /// </remarks>
    public Rect Draw(XGraphics graphics, Font font, Brush brush, Vector pos, Vector size, XStringFormat format) {
      // do a quick test to see if text is going to get drawn at the same size as last time;
      // if so, assume we don't need to recompute our layout for that reason.
      var sizeChecker = graphics.MeasureString("M q", font);
            if (sizeChecker != _mSizeChecker ||
                pos != _mPos ||
                _mSize != size ||
                _mRequestedFormat.Alignment != format.Alignment ||
                _mRequestedFormat.LineAlignment != format.LineAlignment)
            {
                _mInvalidLayout = true;
            }

            // TODO: removed || m_requestedFormat.FormatFlags != format.FormatFlags from if check based on error 
            // error CS1061: 'XStringFormat' does not contain a definition for 'FormatFlags'
            // This seems relevant : https://forum.pdfsharp.net/viewtopic.php?f=2&t=3898

            _mSizeChecker = sizeChecker;

      if (_mInvalidLayout) {
        // something vital has changed; rebuild our cached layout data
        rebuildCachedLayout(graphics, font, ref pos, ref size, format);
        _mInvalidLayout = false;
      }

      var state = graphics.Save();
      var textRect = new RectangleF(pos.X, pos.Y, size.X, size.Y);
      if (size != Vector.Zero) graphics.IntersectClip(textRect);

      // disable smoothing whilst rendering text;
      // visually this is no different, but is faster
      var smoothingMode = graphics.SmoothingMode;
      graphics.SmoothingMode = XSmoothingMode.HighSpeed;

      var origin = _mOrigin;
      foreach (var t in _mLines) {
        if (size.Y > 0 && size.Y < _mLineHeight)
          break; // not enough remaining vertical space for a whole line

        var line = t;

        graphics.SmoothingMode = XSmoothingMode.HighQuality;


        graphics.DrawString(line, font, brush, origin.X, origin.Y, _mActualFormat);
        origin += _mDelta;
        size.Y -= _mLineHeight;
      }

      graphics.SmoothingMode = smoothingMode;
      graphics.Restore(state);

      var actualTextRect = new Rect(pos.X, _mOrigin.Y, size.X, _mLineHeight * _mLines.Count);
      return actualTextRect;
    }

    private void rebuildCachedLayout(XGraphics graphics, Font font, ref Vector pos, ref Vector size, XStringFormat baseFormat) {
      // for diagnostic purposes
      ++SRebuildCount;

      // store current settings to help us tell if we need a rebuild next time around
      _mRequestedFormat = new XStringFormat();
      _mRequestedFormat.Alignment = baseFormat.Alignment;
      // TODO: m_requestedFormat.FormatFlags = baseFormat.FormatFlags;
      _mRequestedFormat.LineAlignment = baseFormat.LineAlignment;
      _mActualFormat = new XStringFormat();
      _mActualFormat.Alignment = baseFormat.Alignment;
      // TODO: m_actualFormat.FormatFlags = baseFormat.FormatFlags;
      _mActualFormat.LineAlignment = baseFormat.LineAlignment;
      _mPos = pos;
      _mSize = size;

      // PDFsharp renders carriage returns as glyphs instead of treating them as line breaks.
      var text = _mText.Replace("\r\n", "\n").Replace('\r', '\n');
      if (text.IndexOf('\n') == -1 && size.X > 0 && size.Y > 0 && graphics.MeasureString(text, font).Width > size.X) {
        // wrap single-line text to fit in rectangle

        // measure a space, countering the APIs unwillingness to measure spaces
        var spaceLength = (float) (graphics.MeasureString("M M", font).Width - graphics.MeasureString("M", font).Width * 2);
        var hyphenLength = (float) graphics.MeasureString("-", font).Width;

        var wordsStep1 = new List<Word>();
        foreach (var word in text.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries)) {
          if (wordsStep1.Count != 0) wordsStep1.Add(new Word(" ", spaceLength));
          wordsStep1.Add(new Word(word, (float) graphics.MeasureString(word, font).Width));
        }

        bool isSplitDash = Setup.Settings.WrapTextAtDashes;
        var words = new List<Word>();
        foreach (var splits in wordsStep1.Where(p => !string.IsNullOrWhiteSpace(p.Text)).Select(word => isSplitDash ? word.Text.Split('-') : new string[] { word.Text }))
          if (splits.Count() > 1) {
            var tWordList = new List<Word>();
            foreach (var tWord in splits) {
              if (words.Count != 0 && tWordList.Count == 0)
                tWordList.Add(new Word(" ", spaceLength));
              else if (tWordList.Count != 0 && isSplitDash)
                tWordList.Add(new Word("-", hyphenLength));

              tWordList.Add(new Word(tWord, (float) graphics.MeasureString(tWord, font).Width));
            }

            words.AddRange(tWordList);
          } else {
            if (words.Count != 0)
              words.Add(new Word(" ", spaceLength));
            words.Add(new Word(splits[0], (float) graphics.MeasureString(splits[0], font).Width));
          }

        var lineLength = 0.0f;
        var total = string.Empty;
        var line = string.Empty;

        foreach (var word in words)
          if (word.Text != " " && word.Length > Math.Max(0, size.X - lineLength) && lineLength > 0) {
            if (line.Length > 0) {
              if (total.Length > 0) total += "\n";
              total += line;
              lineLength = word.Length + spaceLength;
              line = word.Text;
            }
          } else {
            line += word.Text;
            lineLength += word.Length + spaceLength;
          }

        if (line.Length > 0) {
          if (total.Length > 0) total += "\n";
          total += line;
        }

        text = total;
      }

      _mLineHeight = font.GetHeight();

      _mLines.Clear();
      _mLines.AddRange(text.Split('\n'));

      switch (_mActualFormat.LineAlignment) {
        case XLineAlignment.Near:
        default:
          _mOrigin = pos;
          _mDelta = new Vector(0, _mLineHeight);
          break;
        case XLineAlignment.Far:
          _mOrigin = new Vector(pos.X, pos.Y + size.Y - _mLineHeight);
          if (size.Y > 0) {
            var count = _mLines.Count;
            while (_mOrigin.Y - _mLineHeight >= pos.Y && --count > 0) _mOrigin.Y -= _mLineHeight;
          } else {
            _mOrigin.Y -= (_mLines.Count - 1) * _mLineHeight;
          }

          _mDelta = new Vector(0, _mLineHeight);
          break;
        case XLineAlignment.Center:
          _mOrigin = new Vector(pos.X, pos.Y + size.Y / 2 - (_mLines.Count - 1) * _mLineHeight / 2 - _mLineHeight / 2);
          _mDelta = new Vector(0, _mLineHeight);
          break;
      }

      _mActualFormat.LineAlignment = XLineAlignment.Near;

      switch (_mActualFormat.Alignment) {
        case XStringAlignment.Far:
          _mOrigin.X = pos.X + size.X;
          break;
        case XStringAlignment.Center:
          _mOrigin.X = pos.X + size.X / 2;
          break;
      }
    }

    private struct Word {
      public readonly float Length;
      public readonly string Text;

      public Word(string text, float length) {
        Text = text;
        Length = length;
      }
    }
  }
}
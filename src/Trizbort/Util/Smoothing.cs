using System;
using PdfSharp.Drawing;

namespace Trizbort.Util;

internal class Smoothing : IDisposable
{
  public Smoothing(XGraphics graphics, XSmoothingMode mode)
  {
    Graphics = graphics;
    SmoothingMode = graphics.SmoothingMode;
    graphics.SmoothingMode = mode;
  }

  public XGraphics Graphics { get; }
  public XSmoothingMode SmoothingMode { get; }

  public void Dispose()
  {
    Graphics.SmoothingMode = SmoothingMode;
  }
}
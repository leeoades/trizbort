using System;
using Trizbort.Domain.Misc;

namespace Trizbort.Domain.Elements;

public class CornerRadii
{
  private double _bottomLeft = 15.0;
  private double _bottomRight = 15.0;
  private double _topLeft = 15.0;
  private double _topRight = 15.0;

  public double BottomLeft {
    get { return _bottomLeft; }
    set {
      if (value < 1) _bottomLeft = 1;
      else if (value > 30) _bottomLeft = 30;
      else _bottomLeft = value;
    }
  }

  public double BottomRight {
    get { return _bottomRight; }
    set {
      if (value < 1) _bottomRight = 1;
      else if (value > 30) _bottomRight = 30;
      else _bottomRight = value;
    }
  }

  public double TopLeft {
    get { return _topLeft; }
    set {
      if (value < 1) _topLeft = 1;
      else if (value > 30) _topLeft = 30;
      else _topLeft = value;
    }
  }

  public double TopRight {
    get { return _topRight; }
    set {
      if (value < 1) _topRight = 1;
      else if (value > 30) _topRight = 30;
      else _topRight = value;
    }
  }

  public bool SetCorner(CompassPoint corner, float value)
  {
    switch (corner)
    {
      case CompassPoint.NorthWest:
        if (Math.Abs(TopLeft - value) > 0.001)
        {
          TopLeft = value;
          return true;
        }
        break;
      case CompassPoint.NorthEast:
        if (Math.Abs(TopRight - value) > 0.001)
        {
          TopRight = value;
          return true;
        }
        break;
      case CompassPoint.SouthWest:
        if (Math.Abs(BottomLeft - value) > 0.001)
        {
          BottomLeft = value;
          return true;
        }
        break;
      case CompassPoint.SouthEast:
        if (Math.Abs(BottomRight - value) > 0.001)
        {
          BottomRight = value;
          return true;
        }
        break;
    }
    return false;
  }
}

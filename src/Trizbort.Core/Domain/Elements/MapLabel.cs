using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Trizbort.Domain.Application;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Extensions;
using Trizbort.Setup;
using Trizbort.Util;

namespace Trizbort.Domain.Elements;

public class MapLabel : Element, ISizeable
{
  private string _text = "Label";
  private Color _backgroundColor = Color.White;
  private Color _borderColor = Color.Black;
  private BorderDashStyle _borderStyle = BorderDashStyle.None;
  private bool _hasBackground;
  private Vector _position;
  private RoomShape _shape;
  private Vector _size = new(3 * Settings.GridSize, 2 * Settings.GridSize);
  private Color _textColor = Color.Black;

  public MapLabel(Project project) : base(project)
  {
    Initialize();
  }

  public MapLabel(Project project, int id) : base(project, id)
  {
    Initialize();
  }

  public override Depth Depth => Depth.Medium;
  public override bool HasDialog => true;

  public override string Name {
    get { return Text; }
    set { Text = value; }
  }

  public string Text {
    get { return _text; }
    set {
      value = value ?? string.Empty;
      if (_text == value) return;
      _text = value;
      RaiseChanged();
    }
  }

  public RoomShape Shape {
    get { return _shape; }
    set { SetField(ref _shape, value); }
  }

  public BorderDashStyle BorderStyle {
    get { return _borderStyle; }
    set { SetField(ref _borderStyle, value); }
  }

  public bool HasBackground {
    get { return _hasBackground; }
    set { SetField(ref _hasBackground, value); }
  }

  public Color TextColor {
    get { return _textColor; }
    set { SetField(ref _textColor, value); }
  }

  public Color BorderColor {
    get { return _borderColor; }
    set { SetField(ref _borderColor, value); }
  }

  public Color BackgroundColor {
    get { return _backgroundColor; }
    set { SetField(ref _backgroundColor, value); }
  }

  public override Vector Position {
    get { return _position; }
    set { SetField(ref _position, value); }
  }

  public Vector Size {
    get { return _size; }
    set { SetField(ref _size, value); }
  }

  public float X => Position.X;
  public float Y => Position.Y;
  public float Width => Size.X;
  public float Height => Size.Y;
  public Rect InnerBounds => new(Position, Size);

  private void Initialize()
  {
    Text = "Label";
    for (var point = CompassPoint.Min; point <= CompassPoint.Max; point++)
      PortList.Add(new Room.CompassPort(point, this));
  }

  private void SetField<T>(ref T field, T value)
  {
    if (EqualityComparer<T>.Default.Equals(field, value)) return;
    field = value;
    RaiseChanged();
  }

  public Port PortAt(CompassPoint point)
  {
    return PortList.Cast<Room.CompassPort>().First(port => port.CompassPoint == point);
  }

  public override Vector GetPortPosition(Port port)
  {
    return InnerBounds.GetCorner(((Room.CompassPort)port).CompassPoint, Shape, new CornerRadii());
  }

  public override Vector GetPortStalkPosition(Port port)
  {
    var bounds = InnerBounds;
    bounds.Inflate(Settings.ConnectionStalkLength);
    return bounds.GetCorner(((Room.CompassPort)port).CompassPoint);
  }

  public override float Distance(Vector pos, bool includeMargins)
  {
    return pos.DistanceFromRect(UnionBoundsWith(Rect.Empty, includeMargins));
  }

  public override bool Intersects(Rect rect)
  {
    return InnerBounds.IntersectsWith(rect);
  }

  public override Rect UnionBoundsWith(Rect rect, bool includeMargins)
  {
    var bounds = InnerBounds;
    if (includeMargins) bounds.Inflate(Settings.LineWidth);
    return rect == Rect.Empty ? bounds : rect.Union(bounds);
  }

  public void Save(XmlScribe scribe)
  {
    scribe.Attribute("text", Text);
    scribe.Attribute("x", X);
    scribe.Attribute("y", Y);
    scribe.Attribute("w", Width);
    scribe.Attribute("h", Height);
    scribe.Attribute("shape", Shape.ToString());
    scribe.Attribute("borderstyle", BorderStyle.ToString());
    scribe.Attribute("background", HasBackground);
    scribe.Attribute("textColor", TextColor);
    scribe.Attribute("borderColor", BorderColor);
    scribe.Attribute("backgroundColor", BackgroundColor);
    scribe.Attribute("ZOrder", ZOrder);
  }

  public void Load(XmlElementReader element)
  {
    Text = element.Attribute("text").Text;
    Position = new Vector(element.Attribute("x").ToFloat(), element.Attribute("y").ToFloat());
    Size = new Vector(element.Attribute("w").ToFloat(Width), element.Attribute("h").ToFloat(Height));
    Shape = (RoomShape)Enum.Parse(typeof(RoomShape), element.Attribute("shape").Text);
    BorderStyle = (BorderDashStyle)Enum.Parse(typeof(BorderDashStyle), element.Attribute("borderstyle").Text);
    HasBackground = element.Attribute("background").ToBool();
    TextColor = ColorTranslator.FromHtml(element.Attribute("textColor").Text);
    BorderColor = ColorTranslator.FromHtml(element.Attribute("borderColor").Text);
    BackgroundColor = ColorTranslator.FromHtml(element.Attribute("backgroundColor").Text);
    ZOrder = element.Attribute("ZOrder").ToInt();
  }
}

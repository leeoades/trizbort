using System;
using System.Collections.Generic;
using System.Drawing;
using Trizbort.Domain.Application;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using Trizbort.Util;

namespace Trizbort.Domain.Elements;

/// <summary>
///   A coloured shape behind the map, to set parts of it apart ("Above ground", "The cellars") whatever regions the
///   rooms are in. Drawn below the joins and rooms. Inside it behaves as empty map, so rooms on it can still be
///   clicked and boxed: it's picked up by its edge, or by Alt+click inside. It moves on its own. Lee, 24 Nov 2026.
/// </summary>
public class Background : Element, ISizeable
{
  /// <summary>A pale blue-grey: there to be seen behind rooms, not to compete with them.</summary>
  public static readonly Color DefaultFill = Color.FromArgb(0xE6, 0xEE, 0xF7);

  public static readonly Color DefaultBorder = Color.FromArgb(0x9A, 0xAE, 0xC6);

  private Color _fillColor = DefaultFill;
  private Color _borderColor = DefaultBorder;
  private BorderDashStyle _borderStyle = BorderDashStyle.None;
  private HandDrawnStyle _handDrawnStyle = HandDrawnStyle.MapDefault;
  private Vector _position;
  private RoomShape _shape = RoomShape.RoundedCorners;
  private Vector _size = new(8 * Settings.GridSize, 5 * Settings.GridSize);

  public Background(Project project) : base(project)
  {
  }

  public Background(Project project, int id) : base(project, id)
  {
  }

  /// <summary>Below everything else on the map.</summary>
  public override Depth Depth => Depth.Low;

  public override bool HasDialog => true;

  public override string Name { get => "Background"; set { } }

  public RoomShape Shape {
    get => _shape;
    set => SetField(ref _shape, value == RoomShape.NotARoom ? RoomShape.SquareCorners : value);
  }

  public Color FillColor {
    get => _fillColor;
    set => SetField(ref _fillColor, value);
  }

  /// <summary>None for no border.</summary>
  public BorderDashStyle BorderStyle {
    get => _borderStyle;
    set => SetField(ref _borderStyle, value);
  }

  public Color BorderColor {
    get => _borderColor;
    set => SetField(ref _borderColor, value);
  }

  /// <summary>Hand-drawn, straight, or as the map's Hand-drawn setting.</summary>
  public HandDrawnStyle HandDrawnStyle {
    get => _handDrawnStyle;
    set => SetField(ref _handDrawnStyle, value);
  }

  /// <summary>Drawn hand-drawn, after the map's setting where it follows it.</summary>
  public bool IsHandDrawn => HandDrawnStyle switch
  {
    HandDrawnStyle.HandDrawn => true,
    HandDrawnStyle.Straight => false,
    _ => Settings.HandDrawn
  };

  public override Vector Position {
    get => _position;
    set => SetField(ref _position, value);
  }

  public Vector Size {
    get => _size;
    set => SetField(ref _size, value);
  }

  public float X => Position.X;
  public float Y => Position.Y;
  public float Width => Size.X;
  public float Height => Size.Y;
  public Rect InnerBounds => new(Position, Size);

  private void SetField<T>(ref T field, T value)
  {
    if (EqualityComparer<T>.Default.Equals(field, value)) return;
    field = value;
    RaiseChanged();
  }

  /// <summary>True when the point is on the background (inside its shape).</summary>
  public bool Contains(Vector pos)
  {
    var bounds = InnerBounds;
    if (!bounds.Contains(pos)) return false;
    if (Shape != RoomShape.Ellipse) return true;
    var rx = Width / 2;
    var ry = Height / 2;
    if (rx <= 0 || ry <= 0) return false;
    var dx = (pos.X - bounds.Center.X) / rx;
    var dy = (pos.Y - bounds.Center.Y) / ry;
    return dx * dx + dy * dy <= 1;
  }

  /// <summary>
  ///   How far the point is from the background's edge, inside or out: only the edge picks it up, so inside it the
  ///   map is as empty as anywhere else.
  /// </summary>
  public override float Distance(Vector pos, bool includeMargins)
  {
    var bounds = InnerBounds;
    if (Shape == RoomShape.Ellipse)
    {
      var rx = Math.Max(1f, Width / 2);
      var ry = Math.Max(1f, Height / 2);
      var dx = pos.X - bounds.Center.X;
      var dy = pos.Y - bounds.Center.Y;
      var length = (float)Math.Sqrt(dx * dx + dy * dy);
      if (length < 0.001f) return Math.Min(rx, ry);
      // the ellipse's radius in the point's direction, then how far the point is from there
      var radius = (float)(rx * ry / Math.Sqrt(ry * ry * dx * dx / (length * length) + rx * rx * dy * dy / (length * length)));
      return Math.Abs(length - radius);
    }

    if (!bounds.Contains(pos)) return pos.DistanceFromRect(bounds);
    return Math.Min(Math.Min(pos.X - bounds.Left, bounds.Right - pos.X), Math.Min(pos.Y - bounds.Top, bounds.Bottom - pos.Y));
  }

  /// <summary>Boxed only when the box takes in all of it: a box drawn round rooms on it leaves it be.</summary>
  public override bool Intersects(Rect rect)
  {
    var bounds = InnerBounds;
    return rect.Contains(new Vector(bounds.Left, bounds.Top)) && rect.Contains(new Vector(bounds.Right, bounds.Bottom));
  }

  public override Rect UnionBoundsWith(Rect rect, bool includeMargins)
  {
    var bounds = InnerBounds;
    if (includeMargins) bounds.Inflate(Settings.LineWidth);
    return rect == Rect.Empty ? bounds : rect.Union(bounds);
  }

  public override Vector GetPortPosition(Port port) => Position;

  public override Vector GetPortStalkPosition(Port port) => Position;

  public override bool HasTooltip() => false;

  public void Save(XmlScribe scribe)
  {
    scribe.Attribute("x", X);
    scribe.Attribute("y", Y);
    scribe.Attribute("w", Width);
    scribe.Attribute("h", Height);
    scribe.Attribute("shape", Shape.ToString());
    scribe.Attribute("fillColor", FillColor);
    scribe.Attribute("borderstyle", BorderStyle.ToString());
    scribe.Attribute("borderColor", BorderColor);
    scribe.Attribute("handDrawn", HandDrawnStyle.ToString());
    scribe.Attribute("ZOrder", ZOrder);
  }

  public void Load(XmlElementReader element)
  {
    Position = new Vector(element.Attribute("x").ToFloat(), element.Attribute("y").ToFloat());
    Size = new Vector(element.Attribute("w").ToFloat(Width), element.Attribute("h").ToFloat(Height));
    if (Enum.TryParse<RoomShape>(element.Attribute("shape").Text, out var shape)) Shape = shape;
    if (Enum.TryParse<BorderDashStyle>(element.Attribute("borderstyle").Text, out var border)) BorderStyle = border;
    if (Enum.TryParse<HandDrawnStyle>(element.Attribute("handDrawn").Text, out var hand)) HandDrawnStyle = hand;
    var fill = element.Attribute("fillColor").Text;
    if (!string.IsNullOrEmpty(fill)) FillColor = ColorTranslator.FromHtml(fill);
    var line = element.Attribute("borderColor").Text;
    if (!string.IsNullOrEmpty(line)) BorderColor = ColorTranslator.FromHtml(line);
    ZOrder = element.Attribute("ZOrder").ToInt();
  }
}

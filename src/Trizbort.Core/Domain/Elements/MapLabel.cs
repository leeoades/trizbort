using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Trizbort.Domain.Application;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using Trizbort.Util;

namespace Trizbort.Domain.Elements;

public class MapLabel : Element, ISizeable
{
  private string _text = "Label";
  private Vector _position;
  private Vector _size = new(3 * Settings.GridSize, 2 * Settings.GridSize);

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

  /// <summary>
  ///   The text is Markdown (<see cref="Trizbort.Domain.Misc.LabelMarkdown" />): headings, bold and italic, lists,
  ///   tables, and links to rooms, drawn left-aligned. Off, the text is drawn as written, centred. Saved as the
  ///   optional <c>markdown</c> attribute, which older versions ignore (they show the text as written).
  /// </summary>
  public bool Markdown {
    get { return _markdown; }
    set { SetField(ref _markdown, value); }
  }

  private bool _markdown;

  /// <summary>The label's style (Lee, 24 Nov 2026): its look comes from there, unless the label sets its own.</summary>
  public string StyleId {
    get { return _styleId; }
    set { SetField(ref _styleId, string.IsNullOrEmpty(value) ? LabelStyle.DefaultId : value); }
  }

  private string _styleId = LabelStyle.DefaultId;

  /// <summary>
  ///   This label's own tweaks to its style (null values: from the style). Discouraged: styles are the way. Changed
  ///   through the look properties below, or replaced whole.
  /// </summary>
  public LabelStyle Own {
    get { return _own; }
    set {
      _own = value?.Clone() ?? new LabelStyle();
      RaiseChanged();
    }
  }

  private LabelStyle _own = new();

  /// <summary>True when the label sets any of its look itself rather than from its style.</summary>
  public bool HasOwnLook => !_own.IsEmpty;

  /// <summary>The look as drawn: its own tweaks, then its style, and so on up to Default.</summary>
  public ResolvedLabelStyle Look => LabelStyles.Resolve(StyleId, _own);

  /// <summary>The look its style gives, without its own tweaks.</summary>
  public ResolvedLabelStyle StyleLook => LabelStyles.Resolve(StyleId);

  /// <summary>Clears the label's own tweaks, so it looks as its style does.</summary>
  public void ResetToStyle()
  {
    if (_own.IsEmpty) return;
    _own = new LabelStyle();
    RaiseChanged();
  }

  // Each look property reads the resolved look; setting one sets the label's own value, or clears it when it's what
  // the style gives anyway.
  private void SetOwn<T>(Func<ResolvedLabelStyle, T> fromStyle, T value, Action<T?> set) where T : struct
  {
    var same = EqualityComparer<T>.Default.Equals(fromStyle(StyleLook), value);
    set(same ? null : value);
    RaiseChanged();
  }

  private void SetOwnColor(Func<ResolvedLabelStyle, Color> fromStyle, Color value, Action<Color?> set)
  {
    set(fromStyle(StyleLook).ToArgb() == value.ToArgb() ? null : value);
    RaiseChanged();
  }

  public RoomShape Shape {
    get { return Look.Shape; }
    set { SetOwn(look => look.Shape, value, v => _own.Shape = v); }
  }

  public BorderDashStyle BorderStyle {
    get { return Look.BorderStyle; }
    set { SetOwn(look => look.BorderStyle, value, v => _own.BorderStyle = v); }
  }

  public bool HasBackground {
    get { return Look.HasBackground; }
    set { SetOwn(look => look.HasBackground, value, v => _own.HasBackground = v); }
  }

  public Color TextColor {
    get { return Look.TextColor; }
    set { SetOwnColor(look => look.TextColor, value, v => _own.TextColor = v); }
  }

  public Color BorderColor {
    get { return Look.BorderColor; }
    set { SetOwnColor(look => look.BorderColor, value, v => _own.BorderColor = v); }
  }

  public Color BackgroundColor {
    get { return Look.BackgroundColor; }
    set { SetOwnColor(look => look.BackgroundColor, value, v => _own.BackgroundColor = v); }
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
    // the look as drawn, for older versions, which know nothing of styles
    var look = Look;
    scribe.Attribute("shape", look.Shape.ToString());
    scribe.Attribute("borderstyle", look.BorderStyle.ToString());
    scribe.Attribute("background", look.HasBackground);
    scribe.Attribute("textColor", look.TextColor);
    scribe.Attribute("borderColor", look.BorderColor);
    scribe.Attribute("backgroundColor", look.BackgroundColor);
    scribe.Attribute("ZOrder", ZOrder);
    if (Markdown) scribe.Attribute("markdown", Markdown);
    // the style, and the label's own tweaks to it (Recast reads these, not the look above)
    scribe.Attribute("style", StyleId);
    _own.SaveValues(scribe, "own-");
  }

  public void Load(XmlElementReader element)
  {
    Text = element.Attribute("text").Text;
    Position = new Vector(element.Attribute("x").ToFloat(), element.Attribute("y").ToFloat());
    Size = new Vector(element.Attribute("w").ToFloat(Width), element.Attribute("h").ToFloat(Height));
    ZOrder = element.Attribute("ZOrder").ToInt();
    Markdown = element.Attribute("markdown").ToBool();

    var style = element.Attribute("style").Text;
    if (!string.IsNullOrEmpty(style))
    {
      StyleId = style;
      var own = new LabelStyle();
      own.LoadValues(element, "own-");
      _own = own;
      return;
    }

    // a map from before styles: the Default style, with everything the label had as its own for now; once the
    // map's settings (and so its styles) are loaded, what Default gives anyway is dropped (TrimOwnToStyle)
    StyleId = LabelStyle.DefaultId;
    _own = new LabelStyle {
      Shape = Enum.TryParse<RoomShape>(element.Attribute("shape").Text, out var shape) ? shape : RoomShape.SquareCorners,
      BorderStyle = Enum.TryParse<BorderDashStyle>(element.Attribute("borderstyle").Text, out var border)
        ? border
        : BorderDashStyle.None,
      HasBackground = element.Attribute("background").ToBool(),
      TextColor = element.Attribute("textColor").ToColor(Color.Black),
      BorderColor = element.Attribute("borderColor").ToColor(Color.Black),
      BackgroundColor = element.Attribute("backgroundColor").ToColor(Color.White)
    };
  }

  /// <summary>Drops the label's own values that its style gives anyway, so only real tweaks are left.</summary>
  public void TrimOwnToStyle()
  {
    var style = StyleLook;
    if (_own.Shape == style.Shape) _own.Shape = null;
    if (_own.BorderStyle == style.BorderStyle) _own.BorderStyle = null;
    if (_own.HasBackground == style.HasBackground) _own.HasBackground = null;
    if (_own.TextColor?.ToArgb() == style.TextColor.ToArgb()) _own.TextColor = null;
    if (_own.BorderColor?.ToArgb() == style.BorderColor.ToArgb()) _own.BorderColor = null;
    if (_own.BackgroundColor?.ToArgb() == style.BackgroundColor.ToArgb()) _own.BackgroundColor = null;
    if (_own.FontName == style.FontName) _own.FontName = null;
    if (_own.FontSize == style.FontSize) _own.FontSize = null;
    if (_own.Bold == style.Bold) _own.Bold = null;
    if (_own.Italic == style.Italic) _own.Italic = null;
    if (_own.Align == style.Align) _own.Align = null;
    if (_own.VerticalAlign == style.VerticalAlign) _own.VerticalAlign = null;
  }
}

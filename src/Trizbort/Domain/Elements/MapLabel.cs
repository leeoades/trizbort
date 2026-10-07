using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using PdfSharp.Drawing;
using Trizbort.Domain.Application;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Extensions;
using Trizbort.Setup;
using Trizbort.UI;
using Trizbort.Util;

namespace Trizbort.Domain.Elements {
  public class MapLabel : Element, ISizeable {
    private readonly TextBlock text = new TextBlock();
    private Vector position;
    private Vector size = new Vector(3 * Settings.GridSize, 2 * Settings.GridSize);
    private RoomShape shape;
    private BorderDashStyle borderStyle = BorderDashStyle.None;
    private bool hasBackground;
    private Color textColor = Color.Black;
    private Color borderColor = Color.Black;
    private Color backgroundColor = Color.White;

    public MapLabel(Project project) : base(project) {
      initialize();
    }

    public MapLabel(Project project, int id) : base(project, id) {
      initialize();
    }

    private void initialize() {
      Text = "Label";
      for (var point = CompassPoint.Min; point <= CompassPoint.Max; point++)
        PortList.Add(new Room.CompassPort(point, this));
    }

    public override Depth Depth => Depth.Medium;
    public override bool HasDialog => true;
    public override string Name { get => Text; set => Text = value; }
    public string Text {
      get => text.Text;
      set {
        value = value ?? string.Empty;
        if (text.Text == value) return;
        text.Text = value;
        RaiseChanged();
      }
    }

    public override Vector Position { get => position; set => setField(ref position, value); }
    public Vector Size { get => size; set => setField(ref size, value); }
    public float X => Position.X;
    public float Y => Position.Y;
    public float Width => Size.X;
    public float Height => Size.Y;
    public Rect InnerBounds => new Rect(Position, Size);
    public RoomShape Shape { get => shape; set => setField(ref shape, value); }
    public BorderDashStyle BorderStyle { get => borderStyle; set => setField(ref borderStyle, value); }
    public bool HasBackground { get => hasBackground; set => setField(ref hasBackground, value); }
    public Color TextColor { get => textColor; set => setField(ref textColor, value); }
    public Color BorderColor { get => borderColor; set => setField(ref borderColor, value); }
    public Color BackgroundColor { get => backgroundColor; set => setField(ref backgroundColor, value); }

    private void setField<T>(ref T field, T value) {
      if (EqualityComparer<T>.Default.Equals(field, value)) return;
      field = value;
      RaiseChanged();
    }

    public Port PortAt(CompassPoint point) {
      return PortList.Cast<Room.CompassPort>().First(port => port.CompassPoint == point);
    }

    public override Vector GetPortPosition(Port port) {
      return InnerBounds.GetCorner(((Room.CompassPort) port).CompassPoint, Shape, new CornerRadii());
    }

    public override Vector GetPortStalkPosition(Port port) {
      var bounds = InnerBounds;
      bounds.Inflate(Settings.ConnectionStalkLength);
      return bounds.GetCorner(((Room.CompassPort) port).CompassPoint);
    }

    public override float Distance(Vector pos, bool includeMargins) {
      return pos.DistanceFromRect(UnionBoundsWith(Rect.Empty, includeMargins));
    }

    public override bool Intersects(Rect rect) {
      return InnerBounds.IntersectsWith(rect);
    }

    public override Rect UnionBoundsWith(Rect rect, bool includeMargins) {
      var bounds = InnerBounds;
      if (includeMargins) bounds.Inflate(Settings.LineWidth);
      return rect == Rect.Empty ? bounds : rect.Union(bounds);
    }

    public override void Draw(XGraphics graphics, Palette palette, DrawingContext context) {
      if (Width <= 0 || Height <= 0) return;
      var path = createPath(palette);
      if (HasBackground) graphics.DrawPath(palette.Brush(BackgroundColor), path);
      if (BorderStyle != BorderDashStyle.None) {
        var pen = palette.Pen(BorderColor);
        pen.DashStyle = BorderStyle.ConvertToDashStyle();
        graphics.DrawPath(pen, path);
      }
      if (context.Selected || context.Hover)
        graphics.DrawPath(context.Selected ? palette.SelectedLinePen : palette.HoverLinePen, path);

      var bounds = InnerBounds;
      bounds.Inflate(Shape == RoomShape.Ellipse ? -11.5f : -5);
      if (bounds.Width > 0 && bounds.Height > 0)
        text.Draw(graphics, Settings.RoomNameFont, palette.Brush(TextColor), bounds.Position, bounds.Size, XStringFormats.Center);
    }

    private XGraphicsPath createPath(Palette palette) {
      var path = palette.Path();
      if (Shape == RoomShape.Ellipse) {
        path.AddEllipse(InnerBounds.ToRectangleF());
      } else if (Shape == RoomShape.RoundedCorners) {
        var diameter = Math.Min(30, Math.Min(Width, Height));
        path.AddArc(X, Y, diameter, diameter, 180, 90);
        path.AddArc(X + Width - diameter, Y, diameter, diameter, 270, 90);
        path.AddArc(X + Width - diameter, Y + Height - diameter, diameter, diameter, 0, 90);
        path.AddArc(X, Y + Height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
      } else if (Shape == RoomShape.Octagonal) {
        path.AddPolygon(new[] {
          new PointF(X + Width / 4, Y), new PointF(X + Width * 3 / 4, Y),
          new PointF(X + Width, Y + Height / 4), new PointF(X + Width, Y + Height * 3 / 4),
          new PointF(X + Width * 3 / 4, Y + Height), new PointF(X + Width / 4, Y + Height),
          new PointF(X, Y + Height * 3 / 4), new PointF(X, Y + Height / 4)
        });
      } else {
        path.AddRectangle(InnerBounds.ToRectangleF());
      }
      return path;
    }

    public void Save(XmlScribe scribe) {
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

    public void Load(XmlElementReader element) {
      Text = element.Attribute("text").Text;
      Position = new Vector(element.Attribute("x").ToFloat(), element.Attribute("y").ToFloat());
      Size = new Vector(element.Attribute("w").ToFloat(Width), element.Attribute("h").ToFloat(Height));
      Shape = (RoomShape) Enum.Parse(typeof(RoomShape), element.Attribute("shape").Text);
      BorderStyle = (BorderDashStyle) Enum.Parse(typeof(BorderDashStyle), element.Attribute("borderstyle").Text);
      HasBackground = element.Attribute("background").ToBool();
      TextColor = ColorTranslator.FromHtml(element.Attribute("textColor").Text);
      BorderColor = ColorTranslator.FromHtml(element.Attribute("borderColor").Text);
      BackgroundColor = ColorTranslator.FromHtml(element.Attribute("backgroundColor").Text);
      ZOrder = element.Attribute("ZOrder").ToInt();
    }

    public override void ShowDialog() {
      using (var dialog = new LabelPropertiesDialog(this))
        dialog.ShowDialog(Program.MainForm);
    }
  }
}

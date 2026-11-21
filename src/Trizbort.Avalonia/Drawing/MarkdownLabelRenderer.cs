using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Trizbort.Domain.Misc;
using AvaloniaDrawingContext = Avalonia.Media.DrawingContext;

namespace Trizbort.Avalonia.Drawing;

/// <summary>
///     Lays out and draws a Markdown label (<see cref="LabelMarkdown" />) inside a rectangle: headings, bold and
///     italic, lists, rules, and tables with their columns sized to fit. Remembers where each link was drawn, so a
///     click on the map can follow it (<see cref="LinkAt" />).
/// </summary>
public sealed class MarkdownLabelRenderer
{
    private const double CellPadding = 4;
    private const double ListIndent = 14;

    private readonly List<Action<AvaloniaDrawingContext>> _draws = new();
    private readonly List<(Rect Bounds, string Link)> _links = new();

    /// <summary>The height the text takes.</summary>
    public double Height { get; private set; }

    /// <summary>Where each link was drawn, in map coordinates.</summary>
    public IReadOnlyList<(Rect Bounds, string Link)> Links => _links;

    /// <summary>The link drawn at <paramref name="point" />, or null.</summary>
    public string? LinkAt(Point point) => _links.FirstOrDefault(link => link.Bounds.Contains(point)).Link;

    public static MarkdownLabelRenderer Lay(string text, Rect bounds, Typeface typeface, double fontSize, IBrush brush,
        IBrush linkBrush, IPen rulePen)
    {
        var renderer = new MarkdownLabelRenderer();
        renderer.Layout(LabelMarkdown.Parse(text), bounds, typeface, fontSize, brush, linkBrush, rulePen);
        return renderer;
    }

    public void Draw(AvaloniaDrawingContext context)
    {
        foreach (var draw in _draws) draw(context);
    }

    private void Layout(IReadOnlyList<MarkdownBlock> blocks, Rect bounds, Typeface typeface, double fontSize,
        IBrush brush, IBrush linkBrush, IPen rulePen)
    {
        var y = bounds.Top;
        var width = Math.Max(1, bounds.Width);
        foreach (var block in blocks)
            switch (block.Kind)
            {
                case MarkdownBlockKind.Blank:
                    y += fontSize * 0.6;
                    break;
                case MarkdownBlockKind.Rule:
                {
                    var at = y + fontSize * 0.5;
                    _draws.Add(context => context.DrawLine(rulePen, new Point(bounds.Left, at), new Point(bounds.Right, at)));
                    y += fontSize;
                    break;
                }
                case MarkdownBlockKind.Heading:
                {
                    var size = fontSize * block.Level switch { 1 => 1.6, 2 => 1.3, _ => 1.12 };
                    var text = Text(block.Spans, typeface, size, brush, linkBrush, width, true);
                    Place(text, block.Spans, bounds.Left, y);
                    y += text.Height + fontSize * 0.25;
                    break;
                }
                case MarkdownBlockKind.Bullet:
                case MarkdownBlockKind.Numbered:
                {
                    var indent = block.Level * ListIndent;
                    var marker = new FormattedText(block.Kind == MarkdownBlockKind.Bullet ? "•" : block.Number + ".",
                        CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, fontSize, brush);
                    var markerAt = new Point(bounds.Left + indent, y);
                    _draws.Add(context => context.DrawText(marker, markerAt));
                    var gap = Math.Max(ListIndent, marker.Width + 4);
                    var text = Text(block.Spans, typeface, fontSize, brush, linkBrush, Math.Max(1, width - indent - gap), false);
                    Place(text, block.Spans, bounds.Left + indent + gap, y);
                    y += text.Height;
                    break;
                }
                case MarkdownBlockKind.Table:
                    y = Table(block, bounds, y, typeface, fontSize, brush, linkBrush, rulePen);
                    y += fontSize * 0.3;
                    break;
                default:
                {
                    var text = Text(block.Spans, typeface, fontSize, brush, linkBrush, width, false);
                    Place(text, block.Spans, bounds.Left, y);
                    y += text.Height;
                    break;
                }
            }

        Height = y - bounds.Top;
    }

    /// <summary>Draws <paramref name="text" /> at its place, and notes where its links are.</summary>
    private void Place(FormattedText text, IReadOnlyList<MarkdownSpan> spans, double x, double y)
    {
        var origin = new Point(x, y);
        _draws.Add(context =>
        {
            if (TextRenderingGate.IsEnabled) context.DrawText(text, origin);
        });
        var start = 0;
        foreach (var span in spans)
        {
            if (span.Link != null && span.Text.Length > 0)
            {
                var geometry = text.BuildHighlightGeometry(origin, start, span.Text.Length);
                if (geometry != null) _links.Add((geometry.Bounds, span.Link));
            }

            start += span.Text.Length;
        }
    }

    /// <summary>The spans as one piece of text, each in its style, wrapped at <paramref name="width" />.</summary>
    private static FormattedText Text(IReadOnlyList<MarkdownSpan> spans, Typeface typeface, double size, IBrush brush,
        IBrush linkBrush, double width, bool bold)
    {
        var whole = string.Concat(spans.Select(span => span.Text));
        // an empty line still has a line's height
        var text = new FormattedText(whole.Length == 0 ? " " : whole, CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, typeface, size, brush)
        {
            MaxTextWidth = width,
            TextAlignment = TextAlignment.Left
        };
        if (bold) text.SetFontWeight(FontWeight.Bold);
        var start = 0;
        foreach (var span in spans)
        {
            var length = span.Text.Length;
            if (length > 0)
            {
                if (span.Bold) text.SetFontWeight(FontWeight.Bold, start, length);
                if (span.Italic) text.SetFontStyle(FontStyle.Italic, start, length);
                if (span.Code) text.SetFontFamily(new FontFamily("Consolas, Menlo, DejaVu Sans Mono, monospace"), start, length);
                if (span.Link != null)
                {
                    text.SetForegroundBrush(linkBrush, start, length);
                    text.SetTextDecorations(TextDecorations.Underline, start, length);
                }
            }

            start += length;
        }

        return text;
    }

    /// <summary>
    ///     Lays out a table under <paramref name="top" />: each column as wide as its widest cell, narrowed in proportion
    ///     (the cells wrapping) when they don't fit; the header bold and ruled off. Returns the y below it.
    /// </summary>
    private double Table(MarkdownBlock block, Rect bounds, double top, Typeface typeface, double fontSize, IBrush brush,
        IBrush linkBrush, IPen rulePen)
    {
        var rows = block.Rows;
        if (rows == null || rows.Count == 0) return top;
        var columns = rows.Max(row => row.Count);
        var natural = new double[columns];
        for (var r = 0; r < rows.Count; r++)
        for (var c = 0; c < rows[r].Count; c++)
        {
            var cell = Text(rows[r][c], typeface, fontSize, brush, linkBrush, double.PositiveInfinity,
                block.HasHeader && r == 0);
            natural[c] = Math.Max(natural[c], cell.WidthIncludingTrailingWhitespace + CellPadding * 2);
        }

        var total = natural.Sum();
        var scale = total > bounds.Width && total > 0 ? bounds.Width / total : 1;
        var widths = natural.Select(w => Math.Max(CellPadding * 2 + 8, w * scale)).ToArray();

        var y = top;
        for (var r = 0; r < rows.Count; r++)
        {
            var header = block.HasHeader && r == 0;
            var cells = new List<(FormattedText Text, IReadOnlyList<MarkdownSpan> Spans, double X)>();
            var x = bounds.Left;
            var height = 0.0;
            for (var c = 0; c < columns; c++)
            {
                var spans = c < rows[r].Count ? rows[r][c] : Array.Empty<MarkdownSpan>();
                var cell = Text(spans, typeface, fontSize, brush, linkBrush, Math.Max(1, widths[c] - CellPadding * 2), header);
                cells.Add((cell, spans, x + CellPadding));
                height = Math.Max(height, cell.Height);
                x += widths[c];
            }

            foreach (var (text, spans, cellX) in cells) Place(text, spans, cellX, y + CellPadding / 2);
            y += height + CellPadding;
            if (header || r < rows.Count - 1)
            {
                var lineY = y;
                var right = bounds.Left + widths.Sum();
                var pen = header ? rulePen : new Pen(rulePen.Brush, Math.Max(0.5, rulePen.Thickness / 2)) { DashStyle = rulePen.DashStyle };
                _draws.Add(context => context.DrawLine(pen, new Point(bounds.Left, lineY), new Point(right, lineY)));
            }
        }

        return y;
    }
}

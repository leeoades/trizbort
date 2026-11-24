using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Util;

namespace Trizbort.Setup;

/// <summary>
///   A label style ("Title", "Note"), kept in the map (Lee, 24 Nov 2026). It sets only what differs from the style
///   it's <see cref="BasedOn" />; the rest comes from there, and from its base, up to Default. A null value is "from
///   the base". A label's own tweaks are a style too, the last link in the chain (with no name or id).
/// </summary>
public sealed class LabelStyle
{
  /// <summary>The Default label style's id: the top of every chain.</summary>
  public const string DefaultId = "default";

  public string Id { get; set; }
  public string Name { get; set; }

  /// <summary>The style this one is based on; null for Default (and, for a label's own tweaks, set by the label).</summary>
  public string BasedOn { get; set; }

  public string FontName { get; set; }
  public float? FontSize { get; set; }
  public bool? Bold { get; set; }
  public bool? Italic { get; set; }
  public Color? TextColor { get; set; }

  /// <summary>The outline: None for none.</summary>
  public BorderDashStyle? BorderStyle { get; set; }

  public Color? BorderColor { get; set; }
  public bool? HasBackground { get; set; }
  public Color? BackgroundColor { get; set; }
  public RoomShape? Shape { get; set; }

  public bool IsDefault => Id == DefaultId;

  /// <summary>True when it sets nothing of its own.</summary>
  public bool IsEmpty => FontName == null && FontSize == null && Bold == null && Italic == null && TextColor == null &&
                         BorderStyle == null && BorderColor == null && HasBackground == null &&
                         BackgroundColor == null && Shape == null;

  public LabelStyle Clone() => (LabelStyle)MemberwiseClone();

  /// <summary>Every value from <paramref name="other" /> that it sets, set here too.</summary>
  public void SetFrom(LabelStyle other)
  {
    FontName = other.FontName ?? FontName;
    FontSize = other.FontSize ?? FontSize;
    Bold = other.Bold ?? Bold;
    Italic = other.Italic ?? Italic;
    TextColor = other.TextColor ?? TextColor;
    BorderStyle = other.BorderStyle ?? BorderStyle;
    BorderColor = other.BorderColor ?? BorderColor;
    HasBackground = other.HasBackground ?? HasBackground;
    BackgroundColor = other.BackgroundColor ?? BackgroundColor;
    Shape = other.Shape ?? Shape;
  }

  /// <summary>Writes the values it sets as attributes, each name with <paramref name="prefix" /> before it.</summary>
  public void SaveValues(XmlScribe scribe, string prefix = "")
  {
    if (FontName != null) scribe.Attribute(prefix + "fontName", FontName);
    if (FontSize is { } size) scribe.Attribute(prefix + "fontSize", size);
    if (Bold is { } bold) scribe.Attribute(prefix + "bold", bold);
    if (Italic is { } italic) scribe.Attribute(prefix + "italic", italic);
    if (TextColor is { } text) scribe.Attribute(prefix + "textColor", text);
    if (BorderStyle is { } border) scribe.Attribute(prefix + "borderstyle", border.ToString());
    if (BorderColor is { } line) scribe.Attribute(prefix + "borderColor", line);
    if (HasBackground is { } background) scribe.Attribute(prefix + "background", background);
    if (BackgroundColor is { } fill) scribe.Attribute(prefix + "backgroundColor", fill);
    if (Shape is { } shape) scribe.Attribute(prefix + "shape", shape.ToString());
  }

  /// <summary>Reads the values written by <see cref="SaveValues" />; anything missing stays from the base.</summary>
  public void LoadValues(XmlElementReader element, string prefix = "")
  {
    string Text(string name) => element.Attribute(prefix + name).Text;

    if (Text("fontName") is { Length: > 0 } font) FontName = font;
    if (Text("fontSize") is { Length: > 0 }) FontSize = element.Attribute(prefix + "fontSize").ToFloat();
    if (Text("bold") is { Length: > 0 }) Bold = element.Attribute(prefix + "bold").ToBool();
    if (Text("italic") is { Length: > 0 }) Italic = element.Attribute(prefix + "italic").ToBool();
    if (Text("textColor") is { Length: > 0 }) TextColor = element.Attribute(prefix + "textColor").ToColor(Color.Black);
    if (Enum.TryParse<BorderDashStyle>(Text("borderstyle"), out var border)) BorderStyle = border;
    if (Text("borderColor") is { Length: > 0 }) BorderColor = element.Attribute(prefix + "borderColor").ToColor(Color.Black);
    if (Text("background") is { Length: > 0 }) HasBackground = element.Attribute(prefix + "background").ToBool();
    if (Text("backgroundColor") is { Length: > 0 })
      BackgroundColor = element.Attribute(prefix + "backgroundColor").ToColor(Color.White);
    if (Enum.TryParse<RoomShape>(Text("shape"), out var shape)) Shape = shape;
  }
}

/// <summary>A label's look, every value worked out along its style's chain.</summary>
public sealed record ResolvedLabelStyle(
  string FontName,
  float FontSize,
  bool Bold,
  bool Italic,
  Color TextColor,
  BorderDashStyle BorderStyle,
  Color BorderColor,
  bool HasBackground,
  Color BackgroundColor,
  RoomShape Shape)
{
  public FontStyle FontStyle => (Bold ? FontStyle.Bold : FontStyle.Regular) | (Italic ? FontStyle.Italic : FontStyle.Regular);
}

/// <summary>The map's label styles (in <see cref="Settings.LabelStyles" />): finding, resolving, basing and deleting.</summary>
public static class LabelStyles
{
  /// <summary>A new map's styles: just Default, setting nothing, so labels look as they always have.</summary>
  public static List<LabelStyle> Initial() =>
    new() { new LabelStyle { Id = LabelStyle.DefaultId, Name = "Default" } };

  /// <summary>The map's styles.</summary>
  public static LabelStyleSet Current => new(Settings.LabelStyles ??= Initial());

  public static LabelStyle Default => Current.Default;
  public static LabelStyle Find(string id) => Current.Find(id);
  public static bool Has(string id) => Current.Has(id);
  public static ResolvedLabelStyle Resolve(string id, LabelStyle own = null) => Current.Resolve(id, own);
}

/// <summary>A list of label styles (the map's, or a copy being edited in Map Settings), and what's worked out from it.</summary>
public sealed class LabelStyleSet
{
  public LabelStyleSet(List<LabelStyle> styles)
  {
    Styles = styles;
  }

  public List<LabelStyle> Styles { get; }

  public LabelStyle Default => Find(LabelStyle.DefaultId);

  /// <summary>The style with this id; Default for an id the list doesn't have.</summary>
  public LabelStyle Find(string id)
  {
    return Styles.FirstOrDefault(style => style.Id == id) ?? Styles.FirstOrDefault(style => style.IsDefault) ??
           EnsureDefault();
  }

  public bool Has(string id) => Styles.Any(style => style.Id == id);

  private LabelStyle EnsureDefault()
  {
    var created = LabelStyles.Initial()[0];
    Styles.Insert(0, created);
    return created;
  }

  /// <summary>The chain from <paramref name="own" /> (if any) through style <paramref name="id" /> up to Default.</summary>
  public IEnumerable<LabelStyle> Chain(string id, LabelStyle own = null)
  {
    if (own != null) yield return own;
    var seen = new HashSet<string>();
    var style = Find(id);
    while (style != null && seen.Add(style.Id))
    {
      yield return style;
      if (style.IsDefault || style.BasedOn == null) break;
      style = Find(style.BasedOn);
    }

    if (!seen.Contains(LabelStyle.DefaultId)) yield return Default;
  }

  /// <summary>Every value worked out along the chain, from what Recast has always drawn where nothing sets it.</summary>
  public ResolvedLabelStyle Resolve(string id, LabelStyle own = null)
  {
    var chain = Chain(id, own).ToList();
    T Pick<T>(Func<LabelStyle, T?> value, T fallback) where T : struct =>
      chain.Select(value).FirstOrDefault(v => v.HasValue) ?? fallback;

    var roomFont = Settings.RoomFontMetadata;
    return new ResolvedLabelStyle(
      chain.Select(style => style.FontName).FirstOrDefault(name => !string.IsNullOrEmpty(name)) ?? roomFont?.Name ?? "Arial",
      Pick(style => style.FontSize, roomFont?.Size ?? 13f),
      Pick(style => style.Bold, roomFont != null && roomFont.Style.HasFlag(FontStyle.Bold)),
      Pick(style => style.Italic, roomFont != null && roomFont.Style.HasFlag(FontStyle.Italic)),
      Pick(style => style.TextColor, Color.Black),
      Pick(style => style.BorderStyle, BorderDashStyle.None),
      Pick(style => style.BorderColor, Color.Black),
      Pick(style => style.HasBackground, false),
      Pick(style => style.BackgroundColor, Color.White),
      Pick(style => style.Shape, RoomShape.SquareCorners));
  }

  /// <summary>
  ///   Where a value <paramref name="style" /> doesn't set comes from: the first style above it that sets it, or null
  ///   when nothing does (Recast's own default).
  /// </summary>
  public LabelStyle SourceOf(LabelStyle style, Func<LabelStyle, bool> sets)
  {
    if (style.IsDefault) return null;
    return Chain(style.BasedOn ?? LabelStyle.DefaultId).FirstOrDefault(sets);
  }

  /// <summary>The styles based on <paramref name="id" />, directly or further down.</summary>
  public IEnumerable<LabelStyle> Below(string id)
  {
    var below = new List<LabelStyle>();
    var queue = new Queue<string>(new[] { id });
    while (queue.Count > 0)
    {
      var parent = queue.Dequeue();
      foreach (var child in Styles.Where(style => style.BasedOn == parent && !style.IsDefault))
      {
        if (below.Contains(child)) continue;
        below.Add(child);
        queue.Enqueue(child.Id);
      }
    }

    return below;
  }

  /// <summary>The styles <paramref name="style" /> could be based on: not itself, nor any below it.</summary>
  public IEnumerable<LabelStyle> PossibleBases(LabelStyle style)
  {
    var below = Below(style.Id).Select(s => s.Id).ToHashSet();
    return Tree().Select(item => item.Style).Where(s => s.Id != style.Id && !below.Contains(s.Id));
  }

  /// <summary>A new style based on <paramref name="basedOn" />, with a name not yet used.</summary>
  public LabelStyle Add(string name, string basedOn)
  {
    var taken = Styles.Select(style => style.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
    var unique = name;
    for (var n = 2; taken.Contains(unique); n++) unique = $"{name} {n}";
    var style = new LabelStyle {
      Id = Guid.NewGuid().ToString("N")[..8], Name = unique, BasedOn = basedOn ?? LabelStyle.DefaultId
    };
    Styles.Add(style);
    return style;
  }

  /// <summary>
  ///   Deletes a style (not Default): the styles based on it are based on its base instead, keeping what they set.
  ///   Returns the base, which its labels should take.
  /// </summary>
  public string Delete(LabelStyle style)
  {
    if (style == null || style.IsDefault) return LabelStyle.DefaultId;
    var parent = style.BasedOn ?? LabelStyle.DefaultId;
    foreach (var child in Styles.Where(s => s.BasedOn == style.Id)) child.BasedOn = parent;
    Styles.Remove(style);
    return parent;
  }

  /// <summary>The styles in tree order (Default first, each followed by those based on it), with their depth.</summary>
  public IEnumerable<(LabelStyle Style, int Depth)> Tree()
  {
    var tree = new List<(LabelStyle Style, int Depth)>();
    var placed = new HashSet<string>();

    void Walk(LabelStyle style, int depth)
    {
      if (!placed.Add(style.Id)) return;
      tree.Add((style, depth));
      foreach (var child in Styles.Where(s => s.BasedOn == style.Id && !s.IsDefault)
                 .OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase).ToList())
        Walk(child, depth + 1);
    }

    Walk(Default, 0);
    // any left out (based on a style that's gone): under Default
    foreach (var orphan in Styles.Where(s => !placed.Contains(s.Id)).ToList()) Walk(orphan, 1);
    return tree;
  }

  /// <summary>A deep copy, to edit without touching the map until OK.</summary>
  public LabelStyleSet Copy() => new(Styles.Select(style => style.Clone()).ToList());
}

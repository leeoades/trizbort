namespace Trizbort.Domain.Enums;

/// <summary>
///   Where a label's text sits across it (Lee, 25 Nov 2026). Usual: as labels always have been, centred for plain
///   text and on the left for Markdown.
/// </summary>
public enum LabelAlign
{
  Usual,
  Left,
  Centre,
  Right
}

/// <summary>Where a label's text sits up and down it. Usual: in the middle for plain text, at the top for Markdown.</summary>
public enum LabelVerticalAlign
{
  Usual,
  Top,
  Middle,
  Bottom
}

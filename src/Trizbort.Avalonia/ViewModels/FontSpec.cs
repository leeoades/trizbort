namespace Trizbort.Avalonia.ViewModels;

/// <summary>A font choice: family name, size in points, and style flags. An empty family means "default".</summary>
public sealed record FontSpec(string Family, double Size, bool Bold, bool Italic)
{
    public static FontSpec Default { get; } = new FontSpec(string.Empty, 10, false, false);

    public string Describe()
    {
        var family = string.IsNullOrEmpty(Family) ? "(Default)" : Family;
        var style = Bold && Italic ? ", Bold Italic" : Bold ? ", Bold" : Italic ? ", Italic" : string.Empty;
        return $"{family}, {Size:0.##} pt{style}";
    }
}

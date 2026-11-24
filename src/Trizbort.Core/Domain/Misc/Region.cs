using System;
using System.Drawing;
using System.Xml;

namespace Trizbort.Domain.Misc;

public class Region
{
  public Region()
  {
    RColor = Color.White;
    TextColor = Color.Blue;
    RegionName = DefaultRegion;
    RegionId = new Guid();
  }

  /// <summary>The default region's name in the file. Kept as it is, so the original Trizbort reads the map.</summary>
  public static string DefaultRegion => "NoRegion";

  /// <summary>What people see for the default region. Lee, 24 Nov 2026.</summary>
  public const string NoRegionDisplayName = "No Region";

  /// <summary>A region's name as shown: "No Region" for the default (or none), otherwise the name.</summary>
  public static string DisplayName(string regionName)
  {
    return string.IsNullOrEmpty(regionName) || regionName == DefaultRegion ? NoRegionDisplayName : regionName;
  }

  /// <summary>True for a name that reads as the default region's, "No Region" or "NoRegion" in any case.</summary>
  public static bool LooksLikeDefault(string name)
  {
    return (name ?? string.Empty).Replace(" ", string.Empty).Equals(DefaultRegion, StringComparison.OrdinalIgnoreCase);
  }

  public Color RColor { get; set; }

  public Guid RegionId { get; set; }
  public string RegionName { get; set; }
  public Color TextColor { get; set; }

  public string ClearRegionNameObfuscation()
  {
    return RegionName.Replace("____", " ");
  }

  public string FixupRegionNameForSave()
  {
    var s = RegionName.Replace(" ", "____");
    var validXmlChars = XmlConvert.EncodeName(s);
    return validXmlChars;
  }

  public static bool ValidRegionName(string name)
  {
    if (string.IsNullOrWhiteSpace(name))
      return false;

    return true;
  }
}
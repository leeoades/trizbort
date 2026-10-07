using Trizbort.Domain.Misc;

namespace Trizbort.Export.Domain
{
  public class ExportRegion
  {
    public ExportRegion(Region region, string exportName)
    {
      Region = region;
      ExportName = exportName;
    }

    public string ExportName { get; }

    public Region Region { get; }
  }
}
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;

namespace Trizbort.Core.Tests;

[TestFixture]
[NonParallelizable]
public class RoomTooltipRegionTests
{
  [Test]
  public void ToolTipFooter_DefaultRegion_IsEmpty_AndNamedRegionIsShown()
  {
    // I-024: the reference shows the region only when it is not the default (Room.GetToolTipHeader, !IsDefaultRegion()).
    var previous = Project.Current;
    var project = new Project();
    try
    {
      Project.Current = project;
      var room = new Room(project) { Region = Region.DefaultRegion };
      room.GetToolTipFooter().ShouldBe(string.Empty);

      room.Region = string.Empty;
      room.GetToolTipFooter().ShouldBe(string.Empty);

      room.Region = "Cellar";
      room.GetToolTipFooter().ShouldBe("Cellar");
    }
    finally
    {
      Project.Current = previous;
      project.Dispose();
    }
  }
}

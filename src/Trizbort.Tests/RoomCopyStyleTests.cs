using System.Drawing;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Tests;

[TestFixture]
public class RoomCopyStyleTests
{
  [Test]
  public void CopyStyleFrom_CopiesStylingButNotContent()
  {
    var project = new Project();
    var source = new Room(project) {
      Name = "Source",
      Objects = "lamp",
      SubTitle = "sub",
      Region = "Forest",
      Shape = RoomShape.Octagonal,
      AllCornersEqual = false,
      Corners = new CornerRadii { TopLeft = 1, TopRight = 2, BottomLeft = 3, BottomRight = 4 },
      HandDrawnStyle = HandDrawnStyle.HandDrawn,
      BorderStyle = BorderDashStyle.Dash,
      RoomBorderColor = Color.Red,
      RoomFillColor = Color.Green,
      SecondFillColor = Color.Blue,
      SecondFillLocation = "Top",
      RoomNameColor = Color.Yellow,
      RoomSubtitleColor = Color.Purple,
      RoomObjectTextColor = Color.Orange,
      IsDark = true,
      ObjectsPosition = CompassPoint.East
    };
    var target = new Room(project);

    target.CopyStyleFrom(source);

    target.Region.ShouldBe("Forest");
    target.Shape.ShouldBe(RoomShape.Octagonal);
    target.Octagonal.ShouldBeTrue();
    target.Ellipse.ShouldBeFalse();
    target.RoundedCorners.ShouldBeFalse();
    target.AllCornersEqual.ShouldBeFalse();
    target.Corners.TopLeft.ShouldBe(1);
    target.Corners.TopRight.ShouldBe(2);
    target.Corners.BottomLeft.ShouldBe(3);
    target.Corners.BottomRight.ShouldBe(4);
    target.Corners.ShouldNotBeSameAs(source.Corners);
    target.HandDrawnStyle.ShouldBe(HandDrawnStyle.HandDrawn);
    target.BorderStyle.ShouldBe(BorderDashStyle.Dash);
    target.RoomBorderColor.ShouldBe(Color.Red);
    target.RoomFillColor.ShouldBe(Color.Green);
    target.SecondFillColor.ShouldBe(Color.Blue);
    target.SecondFillLocation.ShouldBe("Top");
    target.RoomNameColor.ShouldBe(Color.Yellow);
    target.RoomSubtitleColor.ShouldBe(Color.Purple);
    target.RoomObjectTextColor.ShouldBe(Color.Orange);
    target.IsDark.ShouldBeTrue();
    target.ObjectsPosition.ShouldBe(CompassPoint.East);

    target.Name.ShouldNotBe("Source");
    target.Objects.ShouldBeEmpty();
    target.SubTitle.ShouldBeEmpty();
  }

  [Test]
  public void CopyStyleFrom_Null_DoesNothing()
  {
    var target = new Room(new Project());
    Should.NotThrow(() => target.CopyStyleFrom(null));
  }
}
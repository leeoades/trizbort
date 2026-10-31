using System.Drawing;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests;

/// <summary>Ticket 130: validation state, tooltip colour and tooltip text of rooms that break a rule.</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class RoomValidationIndicatorTests
{
  private Project _previousProject;
  private Project _project;
  private bool _previousShowTooltips;

  [SetUp]
  public void SetUp()
  {
    _previousProject = Project.Current;
    _project = new Project();
    Project.Current = _project;
    _previousShowTooltips = ApplicationSettingsController.AppSettings.ShowDescriptionsInTooltips;
    ApplicationSettingsController.AppSettings.ShowDescriptionsInTooltips = true;
  }

  [TearDown]
  public void TearDown()
  {
    ApplicationSettingsController.AppSettings.ShowDescriptionsInTooltips = _previousShowTooltips;
    var current = Project.Current;
    Project.Current = _previousProject;
    if (current != null && current != _previousProject) current.Dispose();
    if (_project != current) _project.Dispose();
  }

  private static Room AddRoom(string name)
  {
    var room = new Room(Project.Current) { Name = name };
    Project.Current.Elements.Add(room);
    return room;
  }

  [Test]
  public void RulesOff_RoomIsValid()
  {
    var room = AddRoom("Same");
    AddRoom("Same");

    room.CheckValidation();

    room.Valid().ShouldBeTrue();
  }

  [Test]
  public void UniqueNamesRuleOn_DuplicateIsInvalid_AndTurningItOffClearsIt()
  {
    var room = AddRoom("Same");
    AddRoom("Same");
    Project.Current.MustHaveUniqueNames = true;

    room.CheckValidation();
    room.Valid().ShouldBeFalse();

    Project.Current.MustHaveUniqueNames = false;
    room.CheckValidation();
    room.Valid().ShouldBeTrue();
  }

  [Test]
  public void InvalidRoom_TooltipIsRedAndListsTheMessage()
  {
    var room = AddRoom("Same");
    AddRoom("Same");
    Project.Current.MustHaveUniqueNames = true;

    var model = TooltipModel.Create(room);

    model.ShouldNotBeNull();
    model.Title.ShouldBe("Same - Room validation issues:");
    model.Body.ShouldContain("The room name is not unique.");
    room.GetToolTipColor().ToArgb().ShouldBe(Color.Red.ToArgb());
  }

  [Test]
  public void ValidRoom_TooltipIsLightBlueWithoutIssues()
  {
    var room = AddRoom("Alone");

    var model = TooltipModel.Create(room);

    model.ShouldNotBeNull();
    model.Title.ShouldNotContain("validation");
    room.GetToolTipColor().ToArgb().ShouldBe(Color.LightBlue.ToArgb());
  }
}

using Newtonsoft.Json;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.AppSettings;

namespace Trizbort.Tests;

[TestFixture]
public class ApplicationSettingsPersistenceTests
{
  [Test]
  public void MapPreferences_DefaultToDisabled()
  {
    var settings = new ApplicationSettings();
    settings.ApplyStyleToNewRooms.ShouldBeFalse();
    settings.DoubleClickToAddRoom.ShouldBeFalse();
  }

  [Test]
  public void MapPreferences_MissingFromJson_LoadAsDisabled()
  {
    var settings = JsonConvert.DeserializeObject<ApplicationSettings>("{}");
    settings.ApplyStyleToNewRooms.ShouldBeFalse();
    settings.DoubleClickToAddRoom.ShouldBeFalse();
  }

  [Test]
  public void MapPreferences_RoundTripThroughJson()
  {
    var settings = new ApplicationSettings { ApplyStyleToNewRooms = true, DoubleClickToAddRoom = true };
    var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
    var loaded = JsonConvert.DeserializeObject<ApplicationSettings>(json);
    loaded.ApplyStyleToNewRooms.ShouldBeTrue();
    loaded.DoubleClickToAddRoom.ShouldBeTrue();
  }
}
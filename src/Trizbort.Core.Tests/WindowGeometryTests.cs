using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.AppSettings;

namespace Trizbort.Core.Tests;

[TestFixture]
public class WindowGeometryTests
{
  private static readonly List<ScreenRect> _oneScreen = new() { new ScreenRect(0, 0, 1920, 1080) };

  [Test]
  public void Visible_window_is_unchanged()
  {
    var saved = new WindowGeometry { X = 100, Y = 50, Width = 1000, Height = 700, Maximised = true };

    var fitted = WindowGeometry.Fit(saved, _oneScreen);

    fitted.X.ShouldBe(100);
    fitted.Y.ShouldBe(50);
    fitted.Width.ShouldBe(1000);
    fitted.Height.ShouldBe(700);
    fitted.Maximised.ShouldBeTrue();
  }

  [Test]
  public void Offscreen_window_is_centred_on_a_screen()
  {
    var saved = new WindowGeometry { X = 5000, Y = 4000, Width = 800, Height = 600 };

    var fitted = WindowGeometry.Fit(saved, _oneScreen);

    fitted.X.ShouldBe(560);
    fitted.Y.ShouldBe(240);
  }

  [Test]
  public void Partly_offscreen_window_is_pulled_back()
  {
    var saved = new WindowGeometry { X = 1700, Y = 900, Width = 800, Height = 600 };

    var fitted = WindowGeometry.Fit(saved, _oneScreen);

    fitted.X.ShouldBe(1120);
    fitted.Y.ShouldBe(480);
  }

  [Test]
  public void Tiny_sizes_are_raised_to_the_minimum()
  {
    var fitted = WindowGeometry.Fit(new WindowGeometry { X = 0, Y = 0, Width = 10, Height = 10 }, _oneScreen);

    fitted.Width.ShouldBe(WindowGeometry.MinimumWidth);
    fitted.Height.ShouldBe(WindowGeometry.MinimumHeight);
  }

  [Test]
  public void Oversized_window_is_limited_to_the_screen()
  {
    var fitted = WindowGeometry.Fit(new WindowGeometry { X = 0, Y = 0, Width = 5000, Height = 4000 }, _oneScreen);

    fitted.Width.ShouldBe(1920);
    fitted.Height.ShouldBe(1080);
  }

  [Test]
  public void Window_on_a_second_screen_stays_there()
  {
    var screens = new List<ScreenRect> { new(0, 0, 1920, 1080), new(1920, 0, 1920, 1080) };

    var fitted = WindowGeometry.Fit(new WindowGeometry { X = 2000, Y = 100, Width = 800, Height = 600 }, screens);

    fitted.X.ShouldBe(2000);
    fitted.Y.ShouldBe(100);
  }

  [Test]
  public void Nothing_saved_gives_null()
  {
    WindowGeometry.Fit(null, _oneScreen).ShouldBeNull();
  }

  [Test]
  public void Round_trips_through_json()
  {
    var settings = new ApplicationSettings
    {
      WindowState = new WindowGeometry { X = 10, Y = 20, Width = 900, Height = 700, Maximised = true }
    };

    var loaded = JsonConvert.DeserializeObject<ApplicationSettings>(JsonConvert.SerializeObject(settings));

    loaded.WindowState.X.ShouldBe(10);
    loaded.WindowState.Y.ShouldBe(20);
    loaded.WindowState.Width.ShouldBe(900);
    loaded.WindowState.Height.ShouldBe(700);
    loaded.WindowState.Maximised.ShouldBeTrue();
  }

  [Test]
  public void Old_settings_file_without_window_state_loads()
  {
    var loaded = JsonConvert.DeserializeObject<ApplicationSettings>("{\"CanvasWidth\":624,\"CanvasHeight\":450}");

    loaded.ShouldNotBeNull();
    loaded.WindowState.ShouldBeNull();
  }
}

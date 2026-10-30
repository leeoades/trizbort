using System.Linq;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.AppSettings;

namespace Trizbort.Core.Tests;

[TestFixture]
public class RecentFilesTests
{
  private ApplicationSettings _settings;
  private RecentFiles _recent;

  [SetUp]
  public void SetUp()
  {
    _settings = new ApplicationSettings();
    _recent = new RecentFiles(_settings);
  }

  [Test]
  public void Keeps_at_most_four_newest_first()
  {
    foreach (var name in new[] { "a", "b", "c", "d", "e" }) _recent.Add("/maps/" + name + ".trizbort");

    _recent.GetEntries(_ => true).Select(e => e.Path).ShouldBe(
      new[] { "/maps/e.trizbort", "/maps/d.trizbort", "/maps/c.trizbort", "/maps/b.trizbort" });
  }

  [Test]
  public void Adding_again_moves_to_top_without_duplicate()
  {
    _recent.Add("/maps/a.trizbort");
    _recent.Add("/maps/b.trizbort");
    _recent.Add("/maps/a.trizbort");

    _settings.RecentProjects.ShouldBe(new[] { "/maps/a.trizbort", "/maps/b.trizbort" });
  }

  [Test]
  public void Labels_are_numbered()
  {
    _recent.Add("/maps/a.trizbort");
    _recent.Add("/maps/b.trizbort");

    _recent.GetEntries(_ => true).Select(e => e.Label).ShouldBe(
      new[] { "&1 /maps/b.trizbort", "&2 /maps/a.trizbort" });
  }

  [Test]
  public void Missing_files_are_hidden_and_numbering_continues()
  {
    _recent.Add("/maps/a.trizbort");
    _recent.Add("/maps/gone.trizbort");
    _recent.Add("/maps/b.trizbort");

    var entries = _recent.GetEntries(p => !p.Contains("gone"));

    entries.Select(e => e.Label).ShouldBe(new[] { "&1 /maps/b.trizbort", "&2 /maps/a.trizbort" });
  }

  [Test]
  public void Urls_are_kept_without_an_existence_check()
  {
    _recent.Add("https://example.com/map.trizbort");

    _recent.GetEntries(_ => false).Select(e => e.Path).ShouldBe(new[] { "https://example.com/map.trizbort" });
  }

  [Test]
  public void Long_paths_are_ellipsised_in_the_middle()
  {
    var path = "/" + new string('x', 100) + "/map.trizbort";
    _recent.Add(path);

    var label = _recent.GetEntries(_ => true)[0].Label;

    label.ShouldStartWith("&1 /");
    label.ShouldContain("...");
    label.ShouldEndWith("map.trizbort");
    label.Length.ShouldBe(3 + RecentFiles.MaxLabelPathLength);
  }

  [Test]
  public void Remove_and_clear()
  {
    _recent.Add("/maps/a.trizbort");
    _recent.Add("/maps/b.trizbort");

    _recent.Remove("/maps/a.trizbort");
    _settings.RecentProjects.ShouldBe(new[] { "/maps/b.trizbort" });

    _recent.Clear();
    _settings.RecentProjects.ShouldBeEmpty();
  }

  [Test]
  public void Empty_list_has_no_entries()
  {
    _recent.GetEntries(_ => true).ShouldBeEmpty();
  }
}

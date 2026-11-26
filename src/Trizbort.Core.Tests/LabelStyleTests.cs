using System.Drawing;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Controllers;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

namespace Trizbort.Core.Tests;

/// <summary>Label styles (Lee, 24 Nov 2026): kept in the map, based on one another, chosen on each label.</summary>
[TestFixture]
[NonParallelizable]
public class LabelStyleTests : IsolatedProjectTests
{
  private static LabelStyleSet Styles => LabelStyles.Current;

  private static LabelStyle Title()
  {
    var title = Styles.Add("Title", LabelStyle.DefaultId);
    title.FontSize = 24;
    title.Bold = true;
    title.TextColor = Color.Navy;
    return title;
  }

  [Test]
  public void NewMap_HasJustDefault_AndLabelsLookAsTheyAlwaysHave()
  {
    Settings.Reset();
    Settings.LabelStyles.Select(style => style.Name).ShouldBe(new[] { "Default" });

    var label = new MapLabel(Project.Current);
    label.StyleId.ShouldBe(LabelStyle.DefaultId);
    label.HasOwnLook.ShouldBeFalse();
    label.Look.TextColor.ToArgb().ShouldBe(Color.Black.ToArgb());
    label.Look.BorderStyle.ShouldBe(BorderDashStyle.None);
    label.Look.HasBackground.ShouldBeFalse();
    label.Look.Shape.ShouldBe(RoomShape.SquareCorners);
    label.Look.FontName.ShouldBe(Settings.RoomFontMetadata.Name);
    label.Look.FontSize.ShouldBe(Settings.RoomFontMetadata.Size);
  }

  [Test]
  public void BasedOn_TakesWhatItDoesntSet_FromTheStyleAbove()
  {
    Settings.Reset();
    Styles.Default.BorderStyle = BorderDashStyle.Dot;
    var title = Title();
    var subtitle = Styles.Add("Subtitle", title.Id);
    subtitle.FontSize = 18;

    var look = Styles.Resolve(subtitle.Id);
    look.FontSize.ShouldBe(18);
    look.Bold.ShouldBeTrue();
    look.TextColor.ToArgb().ShouldBe(Color.Navy.ToArgb());
    look.BorderStyle.ShouldBe(BorderDashStyle.Dot);

    Styles.SourceOf(subtitle, style => style.Bold != null).ShouldBeSameAs(title);
    Styles.SourceOf(subtitle, style => style.BorderStyle != null).ShouldBeSameAs(Styles.Default);
    Styles.SourceOf(subtitle, style => style.Shape != null).ShouldBeNull();
  }

  [Test]
  public void LabelsOwnTweaks_ComeFirst_AndAreDroppedWhenTheyMatchTheStyle()
  {
    Settings.Reset();
    var title = Title();
    var label = new MapLabel(Project.Current) { StyleId = title.Id };
    label.Look.FontSize.ShouldBe(24);

    label.TextColor = Color.Red;
    label.HasOwnLook.ShouldBeTrue();
    label.Look.TextColor.ToArgb().ShouldBe(Color.Red.ToArgb());

    label.TextColor = Color.Navy;
    label.HasOwnLook.ShouldBeFalse();

    label.Shape = RoomShape.Ellipse;
    label.ResetToStyle();
    label.HasOwnLook.ShouldBeFalse();
    label.Shape.ShouldBe(RoomShape.SquareCorners);
  }

  [Test]
  public void ChangingAStyle_ChangesEveryLabelUsingIt()
  {
    Settings.Reset();
    var title = Title();
    var labels = Enumerable.Range(0, 3).Select(_ => new MapLabel(Project.Current) { StyleId = title.Id }).ToList();
    title.TextColor = Color.Green;
    labels.ShouldAllBe(label => label.Look.TextColor.ToArgb() == Color.Green.ToArgb());
  }

  [Test]
  public void Deleting_RebasesTheStylesBelow_OntoItsBase_KeepingWhatTheySet()
  {
    Settings.Reset();
    var title = Title();
    var subtitle = Styles.Add("Subtitle", title.Id);
    subtitle.Italic = true;

    Styles.Delete(title).ShouldBe(LabelStyle.DefaultId);
    subtitle.BasedOn.ShouldBe(LabelStyle.DefaultId);
    Styles.Resolve(subtitle.Id).Italic.ShouldBeTrue();
    Styles.Resolve(subtitle.Id).Bold.ShouldBe(Settings.RoomFontMetadata.Style.HasFlag(FontStyle.Bold));
    Styles.Delete(Styles.Default).ShouldBe(LabelStyle.DefaultId);
    Styles.Has(LabelStyle.DefaultId).ShouldBeTrue();
  }

  [Test]
  public void PossibleBases_LeaveOutItselfAndThoseBelowIt_SoThereAreNoLoops()
  {
    Settings.Reset();
    var title = Title();
    var subtitle = Styles.Add("Subtitle", title.Id);
    var note = Styles.Add("Note", LabelStyle.DefaultId);

    Styles.PossibleBases(title).ShouldBe(new[] { Styles.Default, note }, ignoreOrder: true);
    Styles.PossibleBases(subtitle).Count().ShouldBe(3);
  }

  [Test]
  public void Tree_ListsEachStyleUnderItsBase()
  {
    Settings.Reset();
    var title = Title();
    var subtitle = Styles.Add("Subtitle", title.Id);
    var note = Styles.Add("Note", LabelStyle.DefaultId);

    Styles.Tree().Select(item => (item.Style.Name, item.Depth)).ShouldBe(new[] {
      ("Default", 0), (note.Name, 1), (title.Name, 1), (subtitle.Name, 2)
    });
  }

  [Test]
  public void Alignment_IsUsualUntilSet_FollowsTheChain_AndIsSavedAndLoaded()
  {
    // Lee, 25 Nov 2026: horizontal and vertical text alignment
    Settings.Reset();
    Styles.Resolve(LabelStyle.DefaultId).Align.ShouldBe(LabelAlign.Usual);
    Styles.Resolve(LabelStyle.DefaultId).VerticalAlign.ShouldBe(LabelVerticalAlign.Usual);

    var sticky = Styles.Add("Sticky", LabelStyle.DefaultId);
    sticky.Align = LabelAlign.Left;
    sticky.VerticalAlign = LabelVerticalAlign.Top;
    var corner = Styles.Add("Corner", sticky.Id);
    corner.Align = LabelAlign.Right;
    Styles.Resolve(corner.Id).Align.ShouldBe(LabelAlign.Right);
    Styles.Resolve(corner.Id).VerticalAlign.ShouldBe(LabelVerticalAlign.Top);

    var label = new MapLabel(Project.Current) { Text = "Note", StyleId = corner.Id };
    label.Own = new LabelStyle { VerticalAlign = LabelVerticalAlign.Bottom, Align = LabelAlign.Right };
    label.TrimOwnToStyle();
    label.Own.Align.ShouldBeNull(); // the style says so already
    Project.Current.Elements.Add(label);

    var path = Files.File("aligned.trizbort");
    new LegacyMapFileEngine(Project.Current).Save(path).ShouldBeTrue();
    Settings.Reset();
    var loaded = new Project();
    PersistenceRegressionTests.Load(loaded, path);
    var back = loaded.Elements.OfType<MapLabel>().Single();
    back.Look.Align.ShouldBe(LabelAlign.Right);
    back.Look.VerticalAlign.ShouldBe(LabelVerticalAlign.Bottom);
    LabelStyles.Find(sticky.Id).Align.ShouldBe(LabelAlign.Left);
  }

  [Test]
  public void NewStyleNames_AreNotReused()
  {
    Settings.Reset();
    Styles.Add("Title", null).Name.ShouldBe("Title");
    Styles.Add("Title", null).Name.ShouldBe("Title 2");
  }

  [Test]
  public void SaveAndLoad_KeepTheStyles_TheLabelsStyle_AndItsTweaks_AndWriteTheLookForOlderVersions()
  {
    Settings.Reset();
    var title = Title();
    var subtitle = Styles.Add("Subtitle", title.Id);
    subtitle.Shape = RoomShape.Ellipse;
    var label = new MapLabel(Project.Current) { Text = "Hello", StyleId = subtitle.Id };
    label.BackgroundColor = Color.Yellow;
    label.HasBackground = true;
    Project.Current.Elements.Add(label);

    var path = Files.File("styles.trizbort");
    new LegacyMapFileEngine(Project.Current).Save(path).ShouldBeTrue();
    var text = File.ReadAllText(path);
    text.ShouldContain("<labelStyles>");
    text.ShouldContain("shape=\"Ellipse\""); // the look as drawn, for older versions

    Settings.Reset();
    var loaded = new Project();
    PersistenceRegressionTests.Load(loaded, path);
    var back = loaded.Elements.OfType<MapLabel>().Single();
    back.StyleId.ShouldBe(subtitle.Id);
    LabelStyles.Find(subtitle.Id).BasedOn.ShouldBe(title.Id);
    back.Look.FontSize.ShouldBe(24);
    back.Look.Shape.ShouldBe(RoomShape.Ellipse);
    back.Own.HasBackground.ShouldBe(true);
    back.Own.Shape.ShouldBeNull();
    back.Look.BackgroundColor.ToArgb().ShouldBe(Color.Yellow.ToArgb());
  }

  [Test]
  public void OldMap_LabelsTakeDefault_KeepingOnlyWhatDiffersFromIt()
  {
    File.WriteAllText(
      Files.File("old.trizbort"),
      "<trizbort version=\"1.0\"><map>" +
      "<label id=\"1\" x=\"0\" y=\"0\" w=\"100\" h=\"40\" shape=\"SquareCorners\" borderstyle=\"None\" background=\"no\" textColor=\"#000000\" borderColor=\"#000000\" backgroundColor=\"#FFFFFF\">Plain</label>" +
      "<label id=\"2\" x=\"0\" y=\"60\" w=\"100\" h=\"40\" shape=\"Ellipse\" borderstyle=\"Dash\" background=\"yes\" textColor=\"#FF0000\" borderColor=\"#000000\" backgroundColor=\"#FFFF00\">Fancy</label>" +
      "</map></trizbort>");
    PersistenceRegressionTests.Load(Project.Current, Files.File("old.trizbort"));

    var labels = Project.Current.Elements.OfType<MapLabel>().OrderBy(label => label.Id).ToList();
    labels.ShouldAllBe(label => label.StyleId == LabelStyle.DefaultId);
    labels[0].HasOwnLook.ShouldBeFalse();
    labels[1].HasOwnLook.ShouldBeTrue();
    labels[1].Look.Shape.ShouldBe(RoomShape.Ellipse);
    labels[1].Look.TextColor.ToArgb().ShouldBe(Color.Red.ToArgb());
    labels[1].Own.BorderColor.ShouldBeNull();
  }

  [Test]
  public void CopyAndPaste_KeepTheStyle_AndTheTweaks()
  {
    Settings.Reset();
    var title = Title();
    var label = new MapLabel(Project.Current) { Text = "Hi", StyleId = title.Id };
    label.Italic();
    Project.Current.Elements.Add(label);

    var controller = new CopyController();
    var copy = JsonConvert.DeserializeObject<CopyController.CopyObject>(
      JsonConvert.SerializeObject(controller.CreateCopyObject(new Element[] { label })));
    var pasted = new MapLabel(Project.Current);
    controller.SetLabel(pasted, copy.Labels.Single());
    pasted.StyleId.ShouldBe(title.Id);
    pasted.Look.Italic.ShouldBeTrue();
  }
}

internal static class LabelTestExtensions
{
  /// <summary>An own tweak with no property of its own on the label: italic.</summary>
  public static void Italic(this MapLabel label)
  {
    var own = label.Own;
    own.Italic = true;
    label.Own = own;
  }
}

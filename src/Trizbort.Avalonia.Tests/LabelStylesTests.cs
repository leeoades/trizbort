using System.Drawing;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Trizbort.Avalonia.Views.Dialogs;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Setup;

namespace Trizbort.Avalonia.Tests;

/// <summary>Label styles in the app: Map Settings > Styles, and the label's Style tab (Lee, 24 Nov 2026).</summary>
[TestFixture]
[NonParallelizable]
[Category("Unit")]
public class LabelStylesTests
{
    private Project? _previousProject;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
        Settings.LabelStyles = LabelStyles.Initial();
    }

    [TearDown]
    public void TearDown()
    {
        var created = Project.Current;
        Project.Current = _previousProject!;
        created.Dispose();
        Settings.LabelStyles = LabelStyles.Initial();
    }

    private static LabelStylesEditorViewModel Editor()
    {
        var editor = new LabelStylesEditorViewModel();
        editor.Initialize();
        return editor;
    }

    [AvaloniaTest]
    public void NewStyle_IsBasedOnTheSelectedOne_AndSaysWhereItsValuesComeFrom()
    {
        var editor = Editor();
        editor.AddStyleCommand.Execute(null);
        editor.Name = "Title";
        editor.TextColor = Color.Navy;

        editor.Selected!.Name.ShouldBe("Title");
        editor.BasedOn!.Id.ShouldBe(LabelStyle.DefaultId);
        editor.TextColorFrom.ShouldBeEmpty();
        editor.ShapeFrom.ShouldBe("from Recast's default");

        editor.AddStyleCommand.Execute(null);
        editor.Name = "Subtitle";
        editor.BasedOn!.Name.ShouldBe("Title");
        editor.TextColor.ToArgb().ShouldBe(Color.Navy.ToArgb());
        editor.TextColorFrom.ShouldBe("from Title");
        editor.Styles.Select(option => (option.Name, option.Depth))
              .ShouldBe(new[] { ("Default", 0), ("Title", 1), ("Subtitle", 2) });
    }

    [AvaloniaTest]
    public void TypingAName_RenamesInPlace_SoTheNameBoxStaysEnabledAndKeepsTheFocus()
    {
        // Lee, 25 Nov 2026: the focus left the Name box after each key. Rebuilding the lists left nothing selected for
        // a moment, and the Name box (enabled only for a style other than Default) lost the focus.
        var editor = Editor();
        editor.AddStyleCommand.Execute(null);
        var options = editor.Styles.ToList();
        var selected = editor.Selected!;
        var disabled = false;
        editor.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(editor.IsNotDefault) && !editor.IsNotDefault) disabled = true;
        };

        foreach (var text in new[] { "P", "Po", "Pos", "Post" }) editor.Name = text;

        disabled.ShouldBeFalse();
        editor.Styles.ShouldBe(options);
        editor.Selected.ShouldBeSameAs(selected);
        selected.Name.ShouldBe("Post");
        LabelStyles.Current.Has(selected.Id).ShouldBeFalse(); // not until OK
    }

    private static void Settle(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaTest]
    public void Reset_TakesTheValueFromTheBaseAgain()
    {
        var editor = Editor();
        editor.AddStyleCommand.Execute(null);
        editor.Shape = RoomShape.Ellipse;
        editor.ShapeFrom.ShouldBeEmpty();

        editor.ResetCommand.Execute("shape");
        editor.Shape.ShouldBe(RoomShape.SquareCorners);
        editor.ShapeFrom.ShouldNotBeEmpty();
    }

    [AvaloniaTest]
    public void BasedOn_LeavesOutTheStyleItselfAndThoseBelowIt()
    {
        var editor = Editor();
        editor.AddStyleCommand.Execute(null); // New Style, under Default
        var top = editor.Selected!;
        editor.AddStyleCommand.Execute(null); // New Style 2, under it
        editor.Selected = top;
        editor.Bases.Select(option => option.Name).ShouldBe(new[] { "Default" });
    }

    [AvaloniaTest]
    public void NothingChanges_UntilApplied_ThenLabelsInADeletedStyleTakeItsBase()
    {
        var editor = Editor();
        editor.AddStyleCommand.Execute(null);
        var titleId = editor.Selected!.Id;
        editor.Name = "Title";
        editor.HasBackground = true;
        editor.ApplyTo(Project.Current);
        LabelStyles.Find(titleId).HasBackground.ShouldBe(true);

        var label = new MapLabel(Project.Current) { StyleId = titleId };
        Project.Current.Elements.Add(label);
        label.HasBackground.ShouldBeTrue();

        var second = Editor();
        second.Selected = second.Styles.Single(option => option.Id == titleId);
        second.DeleteStyleCommand.Execute(null);
        LabelStyles.Has(titleId).ShouldBeTrue(); // not yet

        second.ApplyTo(Project.Current);
        LabelStyles.Has(titleId).ShouldBeFalse();
        label.StyleId.ShouldBe(LabelStyle.DefaultId);
        label.HasBackground.ShouldBeFalse();
    }

    [AvaloniaTest]
    public void DefaultCantBeDeletedOrRebased()
    {
        var editor = Editor();
        editor.CanDelete.ShouldBeFalse();
        editor.IsNotDefault.ShouldBeFalse();
        editor.Bases.ShouldBeEmpty();
    }

    [AvaloniaTest]
    public void LabelDialog_ChoosingAStyle_ShowsItsLook_AndTweaksAreKeptApart()
    {
        var title = LabelStyles.Current.Add("Title", LabelStyle.DefaultId);
        title.TextColor = Color.Navy;
        title.FontSize = 24;
        var label = new MapLabel(Project.Current) { Text = "Hi" };
        Project.Current.Elements.Add(label);

        var vm = new MapLabelPropertiesViewModel();
        vm.Initialize(label);
        vm.StyleOptions.Select(option => option.Name).ShouldBe(new[] { "Default", "Title" });
        vm.SelectedStyle = vm.StyleOptions.Single(option => option.Name == "Title");
        vm.TextColor.ToArgb().ShouldBe(Color.Navy.ToArgb());
        vm.Font.Size.ShouldBe(24);
        vm.Look!.FontSize.ShouldBe(24);
        vm.PreviewText.ShouldBe("Hi");
        vm.HasOwnLook.ShouldBeFalse();

        vm.Shape = RoomShape.Ellipse;
        vm.HasOwnLook.ShouldBeTrue();
        vm.Look.Shape.ShouldBe(RoomShape.Ellipse);
        vm.ApplyTo(label);
        label.StyleId.ShouldBe(title.Id);
        label.Own.Shape.ShouldBe(RoomShape.Ellipse);
        label.Own.TextColor.ShouldBeNull();

        var reopened = new MapLabelPropertiesViewModel();
        reopened.Initialize(label);
        // changed before, so its controls are open already
        reopened.OwnLookOpen.ShouldBeTrue();
        reopened.HasOwnLook.ShouldBeTrue();
        reopened.ResetToStyleCommand.Execute(null);
        reopened.HasOwnLook.ShouldBeFalse();
        reopened.Shape.ShouldBe(RoomShape.SquareCorners);
    }

    [AvaloniaTest]
    public async Task LabelDialog_EditStyles_ListsTheStylesAgainAfterwards()
    {
        var label = new MapLabel(Project.Current);
        Project.Current.Elements.Add(label);
        var vm = new MapLabelPropertiesViewModel();
        vm.Initialize(label);
        string? askedFor = null;
        vm.EditStyles = (id, newFrom) =>
        {
            askedFor = id;
            newFrom.ShouldBeNull();
            LabelStyles.Current.Add("Note", LabelStyle.DefaultId);
            return Task.FromResult<string?>(id);
        };

        await vm.OpenStyleEditorCommand.ExecuteAsync(null);
        askedFor.ShouldBe(LabelStyle.DefaultId);
        vm.StyleOptions.Select(option => option.Name).ShouldBe(new[] { "Default", "Note" });
        vm.SelectedStyle!.Id.ShouldBe(LabelStyle.DefaultId);
    }

    [AvaloniaTest]
    public void LabelDialog_EachStyleShowsTheLabelsTextInIt()
    {
        // Lee, 25 Nov 2026: a list of the label in each style, rather than a drop-down of names
        var title = LabelStyles.Current.Add("Title", LabelStyle.DefaultId);
        title.FontSize = 24;
        var label = new MapLabel(Project.Current) { Text = "# West of House\nmore" };
        Project.Current.Elements.Add(label);

        var vm = new MapLabelPropertiesViewModel();
        vm.Initialize(label);
        vm.StyleOptions.ShouldAllBe(option => option.Sample == "West of House" && option.Look != null);
        vm.StyleOptions.Single(option => option.Name == "Title").Look!.FontSize.ShouldBe(24);

        vm.Text = "Behind House";
        vm.StyleOptions.ShouldAllBe(option => option.Sample == "Behind House");

        vm.ModifyOwnLookCommand.Execute(null);
        vm.OwnLookOpen.ShouldBeTrue();
        vm.Shape = RoomShape.Ellipse;
        // the styles show as they are (Lee, 26 Nov 2026): this label's change is in its own preview
        vm.StyleOptions.ShouldAllBe(option => option.Look!.Shape != RoomShape.Ellipse);
        vm.Look!.Shape.ShouldBe(RoomShape.Ellipse);
    }

    [AvaloniaTest]
    public void LabelDialog_ResetToStyle_ListsTheStylesAgain()
    {
        // Lee, 26 Nov 2026: a label changed from its style opens customised; Reset to Style goes back to the list
        var label = new MapLabel(Project.Current) { Text = "Sticky" };
        label.Own.HasBackground = true;
        label.Own.BackgroundColor = System.Drawing.Color.Gold;
        Project.Current.Elements.Add(label);

        var vm = new MapLabelPropertiesViewModel();
        vm.Initialize(label);
        vm.OwnLookOpen.ShouldBeTrue();
        vm.Look!.BackgroundColor.ToArgb().ShouldBe(System.Drawing.Color.Gold.ToArgb());
        vm.StyleOptions.Single(option => option.Id == LabelStyle.DefaultId).Look!.BackgroundColor.ToArgb()
            .ShouldNotBe(System.Drawing.Color.Gold.ToArgb());

        vm.ResetToStyleCommand.Execute(null);
        vm.HasOwnLook.ShouldBeFalse();
        vm.OwnLookOpen.ShouldBeFalse();
    }

    [AvaloniaTest]
    public async Task LabelDialog_CreateANewStyle_FromThisLabelsLook_ThenTheLabelUsesIt()
    {
        var label = new MapLabel(Project.Current);
        Project.Current.Elements.Add(label);
        var vm = new MapLabelPropertiesViewModel();
        vm.Initialize(label);
        vm.ModifyOwnLookCommand.Execute(null);
        vm.Shape = RoomShape.Octagonal;
        vm.HasBackground = true;

        // what Map Settings does with it, OK'd
        vm.EditStyles = (id, newFrom) =>
        {
            id.ShouldBe(LabelStyle.DefaultId);
            newFrom.ShouldNotBeNull();
            var editor = new LabelStylesEditorViewModel();
            editor.Initialize(id, newFrom);
            editor.Name = "Sign";
            editor.BasedOn!.Id.ShouldBe(LabelStyle.DefaultId);
            editor.Shape.ShouldBe(RoomShape.Octagonal);
            editor.ShapeFrom.ShouldBeEmpty(); // the new style's own
            var chosen = editor.SelectedId;
            editor.ApplyTo(Project.Current);
            return Task.FromResult(chosen);
        };

        await vm.CreateStyleFromLookCommand.ExecuteAsync(null);
        vm.SelectedStyle!.Name.ShouldBe("Sign");
        vm.HasOwnLook.ShouldBeFalse();
        vm.Shape.ShouldBe(RoomShape.Octagonal);
        vm.HasBackground.ShouldBeTrue();

        vm.ApplyTo(label);
        label.StyleId.ShouldBe(vm.SelectedStyle.Id);
        label.HasOwnLook.ShouldBeFalse();
    }

    [AvaloniaTest]
    public async Task LabelDialog_CreateANewStyle_Cancelled_KeepsTheLabelsChanges()
    {
        var label = new MapLabel(Project.Current);
        Project.Current.Elements.Add(label);
        var vm = new MapLabelPropertiesViewModel();
        vm.Initialize(label);
        vm.Shape = RoomShape.Ellipse;
        vm.EditStyles = (_, _) => Task.FromResult<string?>(null);

        await vm.CreateStyleFromLookCommand.ExecuteAsync(null);
        vm.HasOwnLook.ShouldBeTrue();
        vm.Shape.ShouldBe(RoomShape.Ellipse);
        LabelStyles.Current.Styles.Count.ShouldBe(1);
    }

    [AvaloniaTest]
    public void Alignment_Usual_IsCentredForPlainText_AndTopLeftForMarkdown()
    {
        var usual = LabelStyles.Resolve(LabelStyle.DefaultId);
        Trizbort.Avalonia.Drawing.MapLabelDrawingAdapter.PlaceOf(usual, false)
            .ShouldBe((global::Avalonia.Media.TextAlignment.Center, 0.5));
        Trizbort.Avalonia.Drawing.MapLabelDrawingAdapter.PlaceOf(usual, true)
            .ShouldBe((global::Avalonia.Media.TextAlignment.Left, 0.0));

        var set = usual with { Align = LabelAlign.Right, VerticalAlign = LabelVerticalAlign.Bottom };
        Trizbort.Avalonia.Drawing.MapLabelDrawingAdapter.PlaceOf(set, true)
            .ShouldBe((global::Avalonia.Media.TextAlignment.Right, 1.0));
    }

    [AvaloniaTest]
    public void Editor_Alignment_SetAndReset()
    {
        var editor = Editor();
        editor.AddStyleCommand.Execute(null);
        editor.AlignFrom.ShouldNotBeEmpty();
        editor.Align = LabelAlign.Left;
        editor.VerticalAlign = LabelVerticalAlign.Top;
        editor.AlignFrom.ShouldBeEmpty();
        editor.Look!.VerticalAlign.ShouldBe(LabelVerticalAlign.Top);

        editor.ResetCommand.Execute("align");
        editor.Align.ShouldBe(LabelAlign.Usual);
        editor.VerticalAlign.ShouldBe(LabelVerticalAlign.Top);
    }
}

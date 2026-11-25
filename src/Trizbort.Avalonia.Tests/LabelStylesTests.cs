using System.Drawing;
using Avalonia.Controls;
using Avalonia.Headless;
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
    public void TypingAName_RenamesInPlace_SoTheNameBoxKeepsTheFocus()
    {
        // Lee, 25 Nov 2026: the focus left the Name box after each key
        var settings = new MapSettingsViewModel();
        settings.Initialize();
        settings.SelectedTabIndex = MapSettingsViewModel.StylesTab;
        var editor = settings.LabelStyles;
        editor.AddStyleCommand.Execute(null);
        var options = editor.Styles.ToList();
        var selected = editor.Selected!;

        var dialog = new MapSettingsDialog { DataContext = settings };
        dialog.Show();
        try
        {
            dialog.GetVisualDescendants().OfType<TabControl>().First().SelectedIndex = MapSettingsViewModel.StylesTab;
            Dispatcher.UIThread.RunJobs();
            dialog.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            var box = dialog.FindControl<TextBox>("StyleNameBox");
            box.ShouldNotBeNull();
            box.IsEffectivelyVisible.ShouldBeTrue(string.Join(", ",
                dialog.GetVisualDescendants().OfType<TextBox>().Select(b => b.Name)));
            dialog.Activate();
            box.Focus();
            box.SelectAll();
            Dispatcher.UIThread.RunJobs();
            box.IsFocused.ShouldBeTrue(
                $"to start with: enabled {box.IsEffectivelyEnabled}, visible {box.IsEffectivelyVisible}, " +
                $"active {dialog.IsActive}, focused {dialog.FocusManager?.GetFocusedElement()}, selected {editor.Selected?.Name}");

            foreach (var letter in new[] { "P", "o", "s", "t" })
            {
                dialog.KeyTextInput(letter);
                Dispatcher.UIThread.RunJobs();
                box.IsFocused.ShouldBeTrue($"after typing {letter}");
            }

            box.Text.ShouldBe("Post");
        }
        finally
        {
            dialog.Close();
        }

        editor.Styles.ShouldBe(options);
        editor.Selected.ShouldBeSameAs(selected);
        selected.Name.ShouldBe("Post");
        LabelStyles.Current.Has(selected.Id).ShouldBeFalse(); // not until OK
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
        reopened.OwnLookOpen.ShouldBeTrue();
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
        vm.EditStyles = id =>
        {
            askedFor = id;
            LabelStyles.Current.Add("Note", LabelStyle.DefaultId);
            return Task.FromResult(true);
        };

        await vm.OpenStyleEditorCommand.ExecuteAsync(null);
        askedFor.ShouldBe(LabelStyle.DefaultId);
        vm.StyleOptions.Select(option => option.Name).ShouldBe(new[] { "Default", "Note" });
        vm.SelectedStyle!.Id.ShouldBe(LabelStyle.DefaultId);
    }
}

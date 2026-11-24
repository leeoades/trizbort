using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using Trizbort.Avalonia.Services;
using Trizbort.Avalonia.ViewModels;
using Trizbort.Avalonia.Views.Dialogs;
using Trizbort.Domain.Application;
using Trizbort.Setup;
using Region = Trizbort.Domain.Misc.Region;

namespace Trizbort.Avalonia.Tests;

/// <summary>
///   Lee, 24 Nov 2026: Add New Region... for the selected rooms opens Map Settings at the Regions tab, with a new
///   region to name; and the default region reads "No Region".
/// </summary>
[TestFixture]
[NonParallelizable]
public class NewRegionDialogTests
{
    private Project? _previousProject;
    private List<Region>? _savedRegions;

    [SetUp]
    public void SetUp()
    {
        _previousProject = Project.Current;
        Project.Current = new Project();
        _savedRegions = Settings.Regions;
        Settings.Regions = new List<Region> { new() { RegionName = Region.DefaultRegion } };
    }

    [TearDown]
    public void TearDown()
    {
        Settings.Regions = _savedRegions!;
        var created = Project.Current;
        Project.Current = _previousProject!;
        created.Dispose();
    }

    private static (IDialogService Service, Window Owner) Open()
    {
        var provider = App.ConfigureServices();
        var owner = new Window { Width = 800, Height = 600 };
        owner.Show();
        return (provider.GetRequiredService<IDialogService>(), owner);
    }

    private static MapSettingsDialog TheDialog(Window owner)
    {
        Dispatcher.UIThread.RunJobs();
        return owner.OwnedWindows.OfType<MapSettingsDialog>().Single();
    }

    private static void Press(Window dialog, string content)
    {
        var button = dialog.GetVisualDescendants().OfType<Button>().First(b => Equals(b.Content, content));
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaTest]
    public async Task AddNewRegion_OpensTheRegionsTab_WithANewRegionToName_AndOKGivesItsName()
    {
        var (service, owner) = Open();
        try
        {
            var asking = service.ShowNewRegionAsync(owner);
            var dialog = TheDialog(owner);
            var vm = (MapSettingsViewModel)dialog.DataContext!;

            dialog.FindControl<TabControl>("Tabs")!.SelectedIndex.ShouldBe(MapSettingsViewModel.RegionsTab);
            vm.SelectedRegion.ShouldNotBeNull();
            vm.SelectedRegion!.IsDefaultRegion.ShouldBeFalse();
            // the list says No Region, not NoRegion
            vm.Regions.First().DisplayName.ShouldBe("No Region");
            dialog.GetVisualDescendants().OfType<TextBlock>().ShouldContain(t => t.Text == "No Region");
            dialog.GetVisualDescendants().OfType<TextBlock>().ShouldNotContain(t => t.Text == "NoRegion");

            vm.SelectedRegion.Name = "Cellars";
            Press(dialog, "OK");
            var choice = await asking;

            choice.ShouldNotBeNull();
            choice!.Name.ShouldBe("Cellars");
            // nothing changes until the session applies it, as one undo step with the rooms
            Settings.Regions.ShouldNotContain(r => r.RegionName == "Cellars");
            choice.ApplySettings();
            Settings.Regions.ShouldContain(r => r.RegionName == "Cellars");
        }
        finally
        {
            owner.Close();
        }
    }

    [AvaloniaTest]
    public async Task AddNewRegion_Cancelled_GivesNothing()
    {
        var (service, owner) = Open();
        try
        {
            var asking = service.ShowNewRegionAsync(owner);
            var dialog = TheDialog(owner);
            Press(dialog, "Cancel");

            (await asking).ShouldBeNull();
            Settings.Regions.Count.ShouldBe(1);
        }
        finally
        {
            owner.Close();
        }
    }

    [AvaloniaTest]
    public async Task ChangeRegion_ListsNoRegion_AndHasANewRegionButton()
    {
        var owner = new Window { Width = 800, Height = 600 };
        owner.Show();
        try
        {
            var prompts = new AvaloniaEditorPrompts(() => owner);
            var asking = prompts.ChooseRegionAsync(new[] { Region.DefaultRegion, "Cellars" }, Region.DefaultRegion);
            Dispatcher.UIThread.RunJobs();
            var dialog = owner.OwnedWindows.Single();
            var list = dialog.GetVisualDescendants().OfType<ListBox>().Single();
            list.Items.Cast<string>().ShouldBe(new[] { "No Region", "Cellars" });
            list.SelectedIndex.ShouldBe(0);

            Press(dialog, "_New Region...");
            (await asking).ShouldBe(Trizbort.Editor.NewRegionChoice.Chosen);
        }
        finally
        {
            owner.Close();
        }
    }
}

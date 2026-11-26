using System.Collections.ObjectModel;
using Avalonia.Threading;
using Trizbort.Editor;

namespace Trizbort.Avalonia.ViewModels;

/// <summary>
///   A room dialog's region list ends with Add New Region..., as the room's Set Region menu does (Lee, 25 Nov 2026):
///   choosing it makes a region in Map Settings and chooses it. Nothing is saved until the room dialog's OK.
/// </summary>
public sealed class RegionChoices
{
    public const string AddNewText = "Add New Region...";

    /// <summary>Opens Map Settings at a new region; its name and how to keep it, or null if cancelled. Set by the dialog service.</summary>
    public Func<Task<NewRegionChoice?>>? AddRegion { get; set; }

    /// <summary>The region made here, to keep on OK (null: none).</summary>
    public NewRegionChoice? Added { get; private set; }

    /// <summary>The list ends with Add New Region... (when a region can be added).</summary>
    public void Finish(ObservableCollection<string> regions)
    {
        if (!regions.Contains(NewRegionChoice.Chosen)) regions.Add(NewRegionChoice.Chosen);
    }

    /// <summary>
    ///   Call when the chosen region changes: for Add New Region..., goes back to <paramref name="previous" /> and makes a
    ///   region, which is then chosen with <paramref name="choose" />.
    /// </summary>
    public bool Handle(string? chosen, string? previous, ObservableCollection<string> regions, Action<string?> choose)
    {
        if (chosen != NewRegionChoice.Chosen) return false;
        // after the list has finished choosing it
        Dispatcher.UIThread.Post(() => _ = AddAsync(previous, regions, choose));
        return true;
    }

    private async Task AddAsync(string? previous, ObservableCollection<string> regions, Action<string?> choose)
    {
        choose(previous);
        if (AddRegion == null) return;
        var choice = await AddRegion();
        if (choice == null || string.IsNullOrEmpty(choice.Name)) return;

        // one new region at a time: Map Settings started from the map as it is, without one made earlier here
        if (Added?.Name is { } earlier && earlier != choice.Name && regions.Contains(earlier)) regions.Remove(earlier);
        Added = choice;
        if (!regions.Contains(choice.Name)) regions.Insert(Math.Max(0, regions.Count - 1), choice.Name);
        choose(choice.Name);
    }

    /// <summary>On OK: keeps the region made here (Map Settings' changes). Inside the room dialog's undo step.</summary>
    public void Keep() => Added?.ApplySettings?.Invoke();
}

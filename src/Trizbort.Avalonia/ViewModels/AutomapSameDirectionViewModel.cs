using Trizbort.Automap;
using Trizbort.Domain.Elements;

namespace Trizbort.Avalonia.ViewModels;

/// <summary>
///     Texts and outcome for the "same direction" automap prompt. The result defaults to
///     <see cref="AutomapSameDirectionResult.KeepRoom1" />, which is what closing the window gives.
/// </summary>
public class AutomapSameDirectionViewModel : ViewModelBase
{
    public AutomapSameDirectionViewModel(string? existingName, string? newName)
    {
        ExistingName = existingName ?? string.Empty;
        NewName = newName ?? string.Empty;
    }

    public AutomapSameDirectionViewModel(Room existing, string newName)
        : this(existing?.Name, newName)
    {
    }

    public string ExistingName { get; }

    public string NewName { get; }

    public string Message =>
        $"Room '{ExistingName}' is already defined in the same direction as room '{NewName}'.  What would you like to do?";

    public string KeepExistingLabel => $"Keep '{ExistingName}'";

    public string KeepNewLabel => $"Keep '{NewName}'";

    public string KeepBothLabel => "Keep Both";

    /// <summary>Button text with underscores doubled so room names are not read as access keys.</summary>
    public string KeepExistingButtonText => KeepExistingLabel.Replace("_", "__");

    public string KeepNewButtonText => KeepNewLabel.Replace("_", "__");

    public AutomapSameDirectionResult Result { get; private set; } = AutomapSameDirectionResult.KeepRoom1;

    public event EventHandler? CloseRequested;

    public void KeepExisting() => Complete(AutomapSameDirectionResult.KeepRoom1);

    public void KeepNew() => Complete(AutomapSameDirectionResult.KeepRoom2);

    public void KeepBoth() => Complete(AutomapSameDirectionResult.KeepBoth);

    private void Complete(AutomapSameDirectionResult result)
    {
        Result = result;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}

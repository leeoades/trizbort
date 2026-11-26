using CommunityToolkit.Mvvm.ComponentModel;

namespace Trizbort.Avalonia.ViewModels;

public partial class MainWindowViewModel
{
    /// <summary>The bar about new versions of Recast (set at start-up; null in tests that don't need it).</summary>
    [ObservableProperty]
    private UpdateBarViewModel? _updates;
}

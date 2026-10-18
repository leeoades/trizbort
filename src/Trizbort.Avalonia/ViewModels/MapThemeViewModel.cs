using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Trizbort.Domain.Application;
using Trizbort.Setup;

namespace Trizbort.Avalonia.ViewModels;

public partial class MapThemeViewModel : ViewModelBase
{
    public ObservableCollection<MapTheme> Themes { get; } = [];

    [ObservableProperty]
    private MapTheme? _selectedTheme;

    [ObservableProperty]
    private bool _replaceIndividualStyles;

    [ObservableProperty]
    private bool _hasIndividualStyles;

    public void Initialize()
    {
        Themes.Clear();
        foreach (var theme in MapTheme.BuiltInThemes())
        {
            Themes.Add(theme);
        }

        SelectedTheme = Themes.FirstOrDefault();
        HasIndividualStyles = MapTheme.HasIndividualStyles(Project.Current);
        ReplaceIndividualStyles = false;
    }

    public void ApplySelectedTheme()
    {
        if (SelectedTheme != null)
        {
            SelectedTheme.Apply(ReplaceIndividualStyles);
        }
    }

    public void AddCustomTheme(MapTheme theme)
    {
        Themes.Add(theme);
        SelectedTheme = theme;
    }
}

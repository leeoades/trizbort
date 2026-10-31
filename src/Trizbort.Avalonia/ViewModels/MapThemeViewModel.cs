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

    /// <summary>Name of the entry that stands for the map's current settings.</summary>
    public const string CurrentThemeName = "(Current map settings)";

    /// <summary>The map's current look, listed first so it can be selected without changing anything.</summary>
    public MapTheme? CurrentTheme { get; private set; }

    /// <summary>The built-in theme that matches the current map settings, if any.</summary>
    public MapTheme? MatchingBuiltInTheme { get; private set; }

    [ObservableProperty]
    private string _currentMatchText = string.Empty;

    public static string ApplyWarning(bool hasIndividualStyles)
    {
        var text = "Applying a theme replaces map-wide colours, fonts, line and grid settings and the default room shape. " +
                   "Map text, room sizes and positions, region membership and game properties are preserved. " +
                   "You can undo this with Edit > Undo.";
        return hasIndividualStyles
            ? "Some rooms or connections have custom styling. Checking \"Reset individual room/line styles\" replaces individual room styles and room, connection and label colours; unchecked keeps them. " + text
            : text;
    }

    /// <summary>Saves the map's current look as a theme file named after the file (the legacy "Export current map theme").</summary>
    public void ExportCurrentTheme(string path)
    {
        MapTheme.Capture(Path.GetFileNameWithoutExtension(path)).Save(path);
    }

    /// <summary>Analyses the open map for room styling that could become the map default.</summary>
    public RoomStyleInference AnalyzeRoomStyles()
    {
        return RoomStyleInference.Analyze(Project.Current);
    }

    /// <summary>The prompt text shown before the default room style is inferred (legacy wording).</summary>
    public static string InferenceSummary(RoomStyleInference inference)
    {
        var changes = inference.Changes.Count > 0
            ? "  - " + string.Join("\n  - ", inference.Changes)
            : "  (map defaults already match)";
        return $"Styling shared by at least {RoomStyleInference.DefaultThreshold:P0} of rooms will become the map default:\n\n{changes}\n\n" +
               $"{inference.RedundantOverrides} individual colour override(s) on {inference.RoomsSimplified} room(s) will be removed " +
               "because the default now provides them. Rooms that differ keep their own styling, and the map will look the same.\n\n" +
               "Room shapes are stored per room, so a new default shape applies to new rooms and themes. " +
               "You can undo this with Edit > Undo.\n\nContinue?";
    }

    public static string NothingToInferText =>
        $"No room styling is shared by at least {RoomStyleInference.DefaultThreshold:P0} of rooms that isn't already the map default, " +
        "and no rooms have overrides that duplicate the defaults.";

    public void Initialize()
    {
        Themes.Clear();
        CurrentTheme = MapTheme.Capture(CurrentThemeName);
        Themes.Add(CurrentTheme);
        MatchingBuiltInTheme = null;
        foreach (var theme in MapTheme.BuiltInThemes())
        {
            Themes.Add(theme);
            if (MatchingBuiltInTheme == null && MapTheme.SameLook(theme, CurrentTheme)) MatchingBuiltInTheme = theme;
        }

        CurrentMatchText = MatchingBuiltInTheme != null
            ? $"The current map settings match the '{MatchingBuiltInTheme.Name}' theme."
            : "The current map settings do not match a built-in theme.";
        SelectedTheme = CurrentTheme;
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

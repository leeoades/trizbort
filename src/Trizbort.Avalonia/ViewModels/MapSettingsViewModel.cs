using System.Collections.ObjectModel;
using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trizbort.Domain.AppSettings;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using Region = Trizbort.Domain.Misc.Region;

namespace Trizbort.Avalonia.ViewModels;

public partial class RegionItemViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDefaultRegion))]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FillBrush))]
    private Color _fillColor = Color.White;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextBrush))]
    private Color _textColor = Color.MidnightBlue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNameError))]
    private string _nameError = string.Empty;

    public bool HasNameError => !string.IsNullOrEmpty(NameError);

    /// <summary>The default region can't be renamed or deleted.</summary>
    public bool IsDefaultRegion => OriginalName != null ? OriginalName == Region.DefaultRegion : Name == Region.DefaultRegion;

    public bool CanEditName => !IsDefaultRegion;

    public global::Avalonia.Media.IBrush FillBrush => ToBrush(FillColor);

    public global::Avalonia.Media.IBrush TextBrush => ToBrush(TextColor);

    private static global::Avalonia.Media.IBrush ToBrush(Color color) =>
        new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.FromArgb(color.A, color.R, color.G, color.B));

    /// <summary>
    ///   The region's name when the dialog opened, or null for a region added in the dialog. Used to move rooms
    ///   to the new name when a region is renamed, and to the default region when it is deleted.
    /// </summary>
    public string? OriginalName { get; set; }

    public RegionItemViewModel() { }

    public RegionItemViewModel(Region region)
    {
        Name = region.RegionName;
        OriginalName = region.RegionName;
        FillColor = region.RColor;
        TextColor = region.TextColor;
    }

    public Region ToRegion() => new()
    {
        RegionName = Name,
        RColor = FillColor,
        TextColor = TextColor
    };
}

public partial class ColorSettingItemViewModel : ViewModelBase
{
    /// <summary>Friendly names shown in the dialog, in <see cref="Colors"/> index order (legacy Designer:788-799).</summary>
    public static IReadOnlyList<string> DisplayNames { get; } = [
        "Canvas",
        "Border",
        "Connection",
        "Connection (selected)",
        "Connection (hover)",
        "Subtitle Text",
        "Object Text",
        "Connection Text",
        "Grid",
        "Start Room",
        "End Room"
    ];

    public int Index { get; set; }

    /// <summary>The persistence key from <c>Colors.ToName</c> (e.g. "selectedLine"); not shown to the user.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Same as <see cref="Key"/>; kept for existing callers.</summary>
    public string Name => Key;

    public string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    private Color _color = Color.Black;

    public ColorSettingItemViewModel(int index, string key, Color color)
    {
        Index = index;
        Key = key;
        DisplayName = index >= 0 && index < DisplayNames.Count ? DisplayNames[index] : key;
        Color = color;
    }
}

/// <summary>A room shape with its legacy display name for the Default Room Shape drop-down.</summary>
public sealed record ShapeChoice(RoomShape Value, string Display);

public partial class MapSettingsViewModel : ViewModelBase
{
    public const string EmptyRoomNameMessage = "The default room name can't be empty. Please put something in there.";
    public const string NoLetterRoomNameMessage = "The default room name must contain one letter. Please include a letter.";

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private float _handleSize = 12.0f;

    [ObservableProperty]
    private float _snapToElementDistance = 16.0f;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDefaultRoomNameError))]
    private string _defaultRoomNameError = string.Empty;

    public bool HasDefaultRoomNameError => !string.IsNullOrEmpty(DefaultRoomNameError);

    /// <summary>True when any region name failed validation.</summary>
    public bool HasRegionErrors => Regions.Any(r => r.HasNameError);

    public MapSettingsViewModel()
    {
        Regions.CollectionChanged += OnRegionsCollectionChanged;
    }

    private void OnRegionsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (RegionItemViewModel item in e.OldItems)
                item.PropertyChanged -= OnRegionPropertyChanged;
        if (e.NewItems != null)
            foreach (RegionItemViewModel item in e.NewItems)
                item.PropertyChanged += OnRegionPropertyChanged;
        if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            ValidateRegions(false);
    }

    private void OnRegionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RegionItemViewModel.Name)) ValidateRegions(false);
    }

    /// <summary>
    ///   Checks every region name against the legacy rules and sets each item's <c>NameError</c>. When
    ///   <paramref name="sanitize"/> is true, names are first trimmed and double quotes replaced.
    ///   Returns true when all names are valid.
    /// </summary>
    public bool ValidateRegions(bool sanitize)
    {
        if (sanitize)
            foreach (var region in Regions.Where(r => !r.IsDefaultRegion))
            {
                var clean = RegionNaming.Sanitize(region.Name);
                if (clean != region.Name) region.Name = clean;
            }

        foreach (var region in Regions)
        {
            if (region.IsDefaultRegion)
            {
                region.NameError = string.Empty;
                continue;
            }
            var others = Regions.Where(r => !ReferenceEquals(r, region)).Select(r => RegionNaming.Sanitize(r.Name));
            region.NameError = RegionNaming.Validate(region.Name, others, null);
        }

        OnPropertyChanged(nameof(HasRegionErrors));
        return !HasRegionErrors;
    }

    public IReadOnlyList<ShapeChoice> ShapeChoices { get; } = [
        new(RoomShape.SquareCorners, "Straight Edges"),
        new(RoomShape.RoundedCorners, "Rounded Corners"),
        new(RoomShape.Ellipse, "Ellipse"),
        new(RoomShape.Octagonal, "Octagonal")
    ];

    /// <summary>
    ///   Checks the default room name as the reference dialog does when OK is pressed. Returns true when valid;
    ///   otherwise sets <see cref="DefaultRoomNameError"/> and returns false.
    /// </summary>
    public bool Validate()
    {
        var regionsValid = ValidateRegions(true);
        if (!regionsValid) SelectedRegion = Regions.First(r => r.HasNameError);

        if (string.IsNullOrWhiteSpace(DefaultRoomName))
            DefaultRoomNameError = EmptyRoomNameMessage;
        else if (!DefaultRoomName.Any(char.IsLetter))
            DefaultRoomNameError = NoLetterRoomNameMessage;
        else
            DefaultRoomNameError = string.Empty;
        return !HasDefaultRoomNameError && regionsValid;
    }

    /// <summary>Remembers the selected tab for the next time the dialog opens.</summary>
    public void SaveLastTab()
    {
        ApplicationSettingsController.AppSettings.MapSettingsLastTab = SelectedTabIndex;
        ApplicationSettingsController.SaveSettings();
    }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _author = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _history = string.Empty;

    [ObservableProperty]
    private float _gridSize = 32.0f;

    [ObservableProperty]
    private bool _isGridVisible = true;

    [ObservableProperty]
    private bool _snapToGrid = true;

    [ObservableProperty]
    private bool _showOrigin;

    [ObservableProperty]
    private string _defaultRoomName = "Cave";

    [ObservableProperty]
    private RoomShape _defaultRoomShape = RoomShape.SquareCorners;

    [ObservableProperty]
    private float _darknessStripeSize = 24.0f;

    [ObservableProperty]
    private float _objectListOffsetFromRoom = 4.0f;

    [ObservableProperty]
    private float _lineWidth = 2.0f;

    [ObservableProperty]
    private float _connectionArrowSize = 12.0f;

    [ObservableProperty]
    private float _connectionStalkLength = 32.0f;

    [ObservableProperty]
    private float _textOffsetFromConnection = 4.0f;

    [ObservableProperty]
    private float _preferredDistanceBetweenRooms = 64.0f;

    [ObservableProperty]
    private bool _handDrawn;

    [ObservableProperty]
    private bool _wrapTextAtDashes;

    [ObservableProperty]
    private bool _documentSpecificMargins;

    [ObservableProperty]
    private float _docHorizontalMargin = 0.0f;

    [ObservableProperty]
    private float _docVerticalMargin = 0.0f;

    // Font properties (family, size and style via the font picker)
    [ObservableProperty]
    private FontSpec _roomFont = new("Arial", 12, false, false);

    [ObservableProperty]
    private FontSpec _objectFont = new("Arial", 9, false, false);

    [ObservableProperty]
    private FontSpec _subtitleFont = new("Arial", 9, false, false);

    [ObservableProperty]
    private FontSpec _lineFont = new("Arial", 9, false, false);

    private static FontSpec ToFontSpec(ThemeFont font, string fallbackFamily, float fallbackSize) =>
        font == null
            ? new FontSpec(fallbackFamily, fallbackSize, false, false)
            : new FontSpec(font.Name ?? fallbackFamily, font.Size, (font.Style & FontStyle.Bold) != 0,
                (font.Style & FontStyle.Italic) != 0);

    /// <summary>
    ///   Copy of <paramref name="current"/> with the spec's family, size and bold/italic; keeps any underline or
    ///   strikeout. Assigned through the Settings property so the cached font is rebuilt and Changed is raised.
    /// </summary>
    private static ThemeFont WithSpec(ThemeFont current, FontSpec spec)
    {
        var font = current?.Clone() ?? new ThemeFont();
        font.Name = spec.Family;
        font.Size = (float)spec.Size;
        var style = font.Style & ~(FontStyle.Bold | FontStyle.Italic);
        if (spec.Bold) style |= FontStyle.Bold;
        if (spec.Italic) style |= FontStyle.Italic;
        font.Style = style;
        return font;
    }

    public ObservableCollection<ColorSettingItemViewModel> ColorSettings { get; } = [];
    public ObservableCollection<RegionItemViewModel> Regions { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedRegionCommand))]
    private RegionItemViewModel? _selectedRegion;

    /// <summary>Delete is disabled for the default region (and when nothing is selected).</summary>
    public bool CanDeleteSelectedRegion() => SelectedRegion is { IsDefaultRegion: false };

    public IReadOnlyList<RoomShape> AvailableShapes { get; } = [
        RoomShape.SquareCorners,
        RoomShape.RoundedCorners,
        RoomShape.Octagonal,
        RoomShape.Ellipse
    ];

    public void Initialize()
    {
        var project = Project.Current;
        Title = project.Title ?? string.Empty;
        Author = project.Author ?? string.Empty;
        Description = project.Description ?? string.Empty;
        History = project.History ?? string.Empty;

        SelectedTabIndex = Math.Clamp(ApplicationSettingsController.AppSettings.MapSettingsLastTab, 0, 5);
        HandleSize = Settings.HandleSize;
        SnapToElementDistance = Settings.SnapToElementSize;
        DefaultRoomNameError = string.Empty;

        GridSize = Settings.GridSize;
        IsGridVisible = Settings.IsGridVisible;
        SnapToGrid = Settings.SnapToGrid;
        ShowOrigin = Settings.ShowOrigin;

        DefaultRoomName = Settings.DefaultRoomName ?? "Cave";
        DefaultRoomShape = Settings.DefaultRoomShape;
        DarknessStripeSize = Settings.DarknessStripeSize;
        ObjectListOffsetFromRoom = Settings.ObjectListOffsetFromRoom;

        LineWidth = Settings.LineWidth;
        ConnectionArrowSize = Settings.ConnectionArrowSize;
        ConnectionStalkLength = Settings.ConnectionStalkLength;
        TextOffsetFromConnection = Settings.TextOffsetFromConnection;
        PreferredDistanceBetweenRooms = Settings.PreferredDistanceBetweenRooms;

        HandDrawn = Settings.HandDrawn;
        WrapTextAtDashes = Settings.WrapTextAtDashes;
        DocumentSpecificMargins = Settings.DocumentSpecificMargins;
        DocHorizontalMargin = Settings.DocHorizontalMargin;
        DocVerticalMargin = Settings.DocVerticalMargin;

        RoomFont = ToFontSpec(Settings.RoomFontMetadata, "Arial", 12.0f);
        ObjectFont = ToFontSpec(Settings.ObjectFontMetadata, "Arial", 9.0f);
        SubtitleFont = ToFontSpec(Settings.SubtitleFontMetadata, "Arial", 9.0f);
        LineFont = ToFontSpec(Settings.LineFontMetadata, "Arial", 9.0f);

        ColorSettings.Clear();
        for (var index = 0; index < Colors.Count; index++)
        {
            Colors.ToName(index, out var name);
            ColorSettings.Add(new ColorSettingItemViewModel(index, name, Settings.Color[index]));
        }

        Regions.Clear();
        foreach (var region in Settings.Regions)
        {
            Regions.Add(new RegionItemViewModel(region));
        }

        SelectedRegion = Regions.FirstOrDefault();
    }

    public void ApplyToSettings()
    {
        var project = Project.Current;
        if (project.Title != Title) { project.Title = Title; }
        if (project.Author != Author) { project.Author = Author; }
        if (project.Description != Description) { project.Description = Description; }
        if (project.History != History) { project.History = History; }

        Settings.HandleSize = HandleSize;
        Settings.SnapToElementSize = SnapToElementDistance;

        Settings.GridSize = GridSize;
        Settings.IsGridVisible = IsGridVisible;
        Settings.SnapToGrid = SnapToGrid;
        Settings.ShowOrigin = ShowOrigin;

        Settings.DefaultRoomName = DefaultRoomName;
        Settings.DefaultRoomShape = DefaultRoomShape;
        Settings.DarknessStripeSize = DarknessStripeSize;
        Settings.ObjectListOffsetFromRoom = ObjectListOffsetFromRoom;

        Settings.LineWidth = LineWidth;
        Settings.ConnectionArrowSize = ConnectionArrowSize;
        Settings.ConnectionStalkLength = ConnectionStalkLength;
        Settings.TextOffsetFromConnection = TextOffsetFromConnection;
        Settings.PreferredDistanceBetweenRooms = PreferredDistanceBetweenRooms;

        Settings.HandDrawn = HandDrawn;
        Settings.WrapTextAtDashes = WrapTextAtDashes;
        Settings.DocumentSpecificMargins = DocumentSpecificMargins;
        Settings.DocHorizontalMargin = DocHorizontalMargin;
        Settings.DocVerticalMargin = DocVerticalMargin;

        Settings.RoomFontMetadata = WithSpec(Settings.RoomFontMetadata, RoomFont);
        Settings.ObjectFontMetadata = WithSpec(Settings.ObjectFontMetadata, ObjectFont);
        Settings.SubtitleFontMetadata = WithSpec(Settings.SubtitleFontMetadata, SubtitleFont);
        Settings.LineFontMetadata = WithSpec(Settings.LineFontMetadata, LineFont);

        foreach (var cs in ColorSettings)
        {
            Settings.Color[cs.Index] = cs.Color;
        }

        UpdateRoomRegions(project);

        Settings.Regions.Clear();
        foreach (var r in Regions)
        {
            Settings.Regions.Add(r.ToRegion());
            r.OriginalName = r.Name;
        }

        if (Settings.Regions.All(r => r.RegionName != Region.DefaultRegion))
        {
            Settings.Regions.Insert(0, new Region { RegionName = Region.DefaultRegion, RColor = Color.White, TextColor = Color.MidnightBlue });
        }
    }

    /// <summary>
    ///   Moves rooms whose region was renamed to the new name, and rooms whose region was deleted to the default
    ///   region, as the reference SettingsDialog does. Computed as one old-to-new map so swapped names work.
    /// </summary>
    private void UpdateRoomRegions(Project project)
    {
        var renames = new Dictionary<string, string>(StringComparer.Ordinal);
        var kept = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in Regions.Where(r => r.OriginalName != null))
        {
            kept.Add(item.OriginalName!);
            if (item.OriginalName != item.Name) renames[item.OriginalName!] = item.Name;
        }

        foreach (var region in Settings.Regions)
            if (!kept.Contains(region.RegionName) && region.RegionName != Region.DefaultRegion)
                renames[region.RegionName] = Region.DefaultRegion;

        if (renames.Count == 0) return;

        foreach (var room in project.Elements.OfType<Room>())
            if (room.Region != null && renames.TryGetValue(room.Region, out var newName))
            {
                room.Region = newName;
            }
    }

    [RelayCommand]
    public void AddRegion()
    {
        var i = 1;
        var name = "Region1";
        while (Regions.Any(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            i++;
            name = $"Region{i}";
        }

        var newRegion = new RegionItemViewModel
        {
            Name = name,
            FillColor = Color.White,
            TextColor = Settings.Color[Colors.Subtitle]
        };
        Regions.Add(newRegion);
        SelectedRegion = newRegion;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedRegion))]
    public void DeleteSelectedRegion()
    {
        if (SelectedRegion != null && !SelectedRegion.IsDefaultRegion)
        {
            var idx = Regions.IndexOf(SelectedRegion);
            Regions.Remove(SelectedRegion);
            SelectedRegion = Regions.Count > 0 ? Regions[Math.Min(idx, Regions.Count - 1)] : null;
        }
    }
}

using System.Collections.ObjectModel;
using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Misc;
using Trizbort.Setup;
using Region = Trizbort.Domain.Misc.Region;

namespace Trizbort.Avalonia.ViewModels;

public partial class RegionItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private Color _fillColor = Color.White;

    [ObservableProperty]
    private Color _textColor = Color.MidnightBlue;

    public bool IsDefaultRegion => Name == Region.DefaultRegion;

    public RegionItemViewModel() { }

    public RegionItemViewModel(Region region)
    {
        Name = region.RegionName;
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
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;

    [ObservableProperty]
    private Color _color = Color.Black;

    public ColorSettingItemViewModel(int index, string name, Color color)
    {
        Index = index;
        Name = name;
        Color = color;
    }
}

public partial class MapSettingsViewModel : ViewModelBase
{
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

    // Font properties
    [ObservableProperty]
    private string _roomFontName = "Arial";

    [ObservableProperty]
    private float _roomFontSize = 12.0f;

    [ObservableProperty]
    private string _objectFontName = "Arial";

    [ObservableProperty]
    private float _objectFontSize = 9.0f;

    [ObservableProperty]
    private string _subtitleFontName = "Arial";

    [ObservableProperty]
    private float _subtitleFontSize = 9.0f;

    [ObservableProperty]
    private string _lineFontName = "Arial";

    [ObservableProperty]
    private float _lineFontSize = 9.0f;

    public ObservableCollection<ColorSettingItemViewModel> ColorSettings { get; } = [];
    public ObservableCollection<RegionItemViewModel> Regions { get; } = [];

    [ObservableProperty]
    private RegionItemViewModel? _selectedRegion;

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

        RoomFontName = Settings.RoomFontMetadata?.Name ?? "Arial";
        RoomFontSize = Settings.RoomFontMetadata?.Size ?? 12.0f;
        ObjectFontName = Settings.ObjectFontMetadata?.Name ?? "Arial";
        ObjectFontSize = Settings.ObjectFontMetadata?.Size ?? 9.0f;
        SubtitleFontName = Settings.SubtitleFontMetadata?.Name ?? "Arial";
        SubtitleFontSize = Settings.SubtitleFontMetadata?.Size ?? 9.0f;
        LineFontName = Settings.LineFontMetadata?.Name ?? "Arial";
        LineFontSize = Settings.LineFontMetadata?.Size ?? 9.0f;

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
        if (project.Title != Title) { project.Title = Title; project.IsDirty = true; }
        if (project.Author != Author) { project.Author = Author; project.IsDirty = true; }
        if (project.Description != Description) { project.Description = Description; project.IsDirty = true; }
        if (project.History != History) { project.History = History; project.IsDirty = true; }

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

        if (Settings.RoomFontMetadata != null)
        {
            Settings.RoomFontMetadata.Name = RoomFontName;
            Settings.RoomFontMetadata.Size = RoomFontSize;
        }
        if (Settings.ObjectFontMetadata != null)
        {
            Settings.ObjectFontMetadata.Name = ObjectFontName;
            Settings.ObjectFontMetadata.Size = ObjectFontSize;
        }
        if (Settings.SubtitleFontMetadata != null)
        {
            Settings.SubtitleFontMetadata.Name = SubtitleFontName;
            Settings.SubtitleFontMetadata.Size = SubtitleFontSize;
        }
        if (Settings.LineFontMetadata != null)
        {
            Settings.LineFontMetadata.Name = LineFontName;
            Settings.LineFontMetadata.Size = LineFontSize;
        }

        foreach (var cs in ColorSettings)
        {
            Settings.Color[cs.Index] = cs.Color;
        }

        Settings.Regions.Clear();
        foreach (var r in Regions)
        {
            Settings.Regions.Add(r.ToRegion());
        }

        if (Settings.Regions.All(r => r.RegionName != Region.DefaultRegion))
        {
            Settings.Regions.Insert(0, new Region { RegionName = Region.DefaultRegion, RColor = Color.White, TextColor = Color.MidnightBlue });
        }

        project.IsDirty = true;
    }

    [RelayCommand]
    public void AddRegion()
    {
        var baseName = "New Region";
        var name = baseName;
        var i = 1;
        while (Regions.Any(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            i++;
            name = $"{baseName} {i}";
        }

        var newRegion = new RegionItemViewModel
        {
            Name = name,
            FillColor = Color.LightYellow,
            TextColor = Color.MidnightBlue
        };
        Regions.Add(newRegion);
        SelectedRegion = newRegion;
    }

    [RelayCommand]
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

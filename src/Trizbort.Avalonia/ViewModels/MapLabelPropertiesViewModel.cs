using System.Collections.ObjectModel;
using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Avalonia.ViewModels;

public partial class MapLabelPropertiesViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private Color _textColor = Color.Transparent;

    /// <summary>The text is Markdown: headings, bold, lists, tables, and links to rooms.</summary>
    [ObservableProperty]
    private bool _markdown;

    [ObservableProperty]
    private bool _hasOutline;

    /// <summary>Shape changes both the outline and the fill, so it is editable when either one is on.</summary>
    public bool ShapeEnabled => HasOutline || HasBackground;

    [ObservableProperty]
    private RoomShape _shape = RoomShape.SquareCorners;

    [ObservableProperty]
    private BorderDashStyle _borderStyle = BorderDashStyle.Solid;

    [ObservableProperty]
    private Color _borderColor = Color.Transparent;

    [ObservableProperty]
    private bool _hasBackground;

    [ObservableProperty]
    private Color _backgroundColor = Color.Transparent;

    partial void OnHasOutlineChanged(bool value)
    {
        OnPropertyChanged(nameof(ShapeEnabled));
    }

    partial void OnHasBackgroundChanged(bool value)
    {
        OnPropertyChanged(nameof(ShapeEnabled));
    }

    public IReadOnlyList<RoomShape> AvailableShapes { get; } = [
        RoomShape.SquareCorners,
        RoomShape.RoundedCorners,
        RoomShape.Octagonal,
        RoomShape.Ellipse
    ];

    public IReadOnlyList<BorderDashStyle> AvailableBorderStyles { get; } = [
        BorderDashStyle.Solid,
        BorderDashStyle.Dash,
        BorderDashStyle.Dot,
        BorderDashStyle.DashDot,
        BorderDashStyle.DashDotDot
    ];

    // Add Link: the rooms or the objects of the map, alphabetically, narrowed by what's typed in the search; a room
    // clicked on the map is chosen in the list. Lee, 22 Nov 2026.

    private Project? _project;

    /// <summary>The Add Link panel is open, beside the text.</summary>
    [ObservableProperty]
    private bool _linkPanelOpen;

    /// <summary>Objects listed, rather than rooms.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LinkToRooms))]
    private bool _linkToObjects;

    /// <summary>What's typed in the panel's search.</summary>
    [ObservableProperty]
    private string _linkSearch = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLinkChoice))]
    private MarkdownLinkChoice? _selectedLinkChoice;

    /// <summary>Rooms listed, rather than objects.</summary>
    public bool LinkToRooms
    {
        get => !LinkToObjects;
        set => LinkToObjects = !value;
    }

    public bool HasLinkChoice => SelectedLinkChoice != null;

    /// <summary>The rooms or objects matching the search, alphabetically.</summary>
    public ObservableCollection<MarkdownLinkChoice> LinkChoices { get; } = new();

    partial void OnLinkToObjectsChanged(bool value) => RefreshLinkChoices();

    partial void OnLinkSearchChanged(string value) => RefreshLinkChoices();

    partial void OnLinkPanelOpenChanged(bool value)
    {
        if (value) RefreshLinkChoices();
    }

    private void RefreshLinkChoices()
    {
        var project = _project ?? Project.Current;
        var all = LinkToObjects ? MarkdownEditing.Objects(project) : MarkdownEditing.Rooms(project);
        var chosen = SelectedLinkChoice;
        LinkChoices.Clear();
        foreach (var choice in MarkdownEditing.Matching(all, LinkSearch)) LinkChoices.Add(choice);
        SelectedLinkChoice = LinkChoices.FirstOrDefault(choice => choice == chosen) ?? LinkChoices.FirstOrDefault();
    }

    /// <summary>A room clicked on the map, while the panel is open: chosen in the list of rooms.</summary>
    public void ChooseRoomOnMap(Room room)
    {
        if (!LinkPanelOpen) return;
        LinkToObjects = false;
        LinkSearch = string.Empty;
        RefreshLinkChoices();
        SelectedLinkChoice = LinkChoices.FirstOrDefault(choice => choice.Room == room) ?? SelectedLinkChoice;
    }

    /// <summary>
    ///   The link to put in for the room or object chosen; what's selected in the text is kept as the words shown,
    ///   [[Room|selected words]], when it's a room.
    /// </summary>
    public string? LinkFor(string selected)
    {
        if (SelectedLinkChoice is not { } choice) return null;
        if (LinkToObjects || string.IsNullOrWhiteSpace(selected) || selected.Contains('\n')) return choice.Markdown;
        return MarkdownEditing.RoomLink(_project ?? Project.Current, choice.Room, selected);
    }

    public void Initialize(MapLabel label)
    {
        _project = label.Project ?? Project.Current;
        Text = label.Text ?? string.Empty;
        TextColor = label.TextColor;
        Markdown = label.Markdown;
        HasOutline = label.BorderStyle != BorderDashStyle.None;
        Shape = label.Shape == RoomShape.NotARoom ? RoomShape.SquareCorners : label.Shape;
        BorderStyle = HasOutline ? label.BorderStyle : BorderDashStyle.Solid;
        BorderColor = label.BorderColor;
        HasBackground = label.HasBackground;
        BackgroundColor = label.BackgroundColor;
    }

    public void ApplyTo(MapLabel label)
    {
        label.Text = Text;
        label.TextColor = TextColor;
        label.Markdown = Markdown;
        label.Shape = Shape;
        label.BorderStyle = HasOutline ? BorderStyle : BorderDashStyle.None;
        label.BorderColor = BorderColor;
        label.HasBackground = HasBackground;
        label.BackgroundColor = BackgroundColor;
    }
}

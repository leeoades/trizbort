using System.Collections.ObjectModel;
using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Setup;

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

    /// <summary>Where the text sits across the label, and up and down it (Lee, 25 Nov 2026).</summary>
    [ObservableProperty]
    private LabelAlign _align;

    [ObservableProperty]
    private LabelVerticalAlign _verticalAlign;

    public IReadOnlyList<LabelAlign> AvailableAligns { get; } = Enum.GetValues<LabelAlign>();
    public IReadOnlyList<LabelVerticalAlign> AvailableVerticalAligns { get; } = Enum.GetValues<LabelVerticalAlign>();

    [ObservableProperty]
    private BorderDashStyle _borderStyle = BorderDashStyle.Solid;

    [ObservableProperty]
    private Color _borderColor = Color.Transparent;

    [ObservableProperty]
    private bool _hasBackground;

    [ObservableProperty]
    private Color _backgroundColor = Color.Transparent;

    // Styles (Lee, 24 Nov 2026): the label's look comes from its style. The controls below show the look as it will
    // be; changing one makes it this label's own (a tweak, discouraged), kept in _own until OK.

    /// <summary>The styles to choose from, in tree order.</summary>
    public ObservableCollection<LabelStyleOption> StyleOptions { get; } = new();

    [ObservableProperty]
    private LabelStyleOption? _selectedStyle;

    /// <summary>The label's own tweaks to its style.</summary>
    private LabelStyle _own = new();

    /// <summary>Loading the look into the controls: not the user changing it.</summary>
    private bool _showingLook;

    /// <summary>True when this label changes its style's look itself.</summary>
    public bool HasOwnLook => !_own.IsEmpty;

    /// <summary>
    ///   The "just this label" section is open. Closed for a label that looks as its style does, so the controls don't
    ///   invite changes without thinking; open for one already changed, since someone chose Modify for it before
    ///   (Lee, 26 Nov 2026).
    /// </summary>
    [ObservableProperty]
    private bool _ownLookOpen;

    [ObservableProperty]
    private FontSpec _font = FontSpec.Default;

    /// <summary>The look as it will be, for the preview.</summary>
    [ObservableProperty]
    private ResolvedLabelStyle? _look;

    /// <summary>The label's first line, without Markdown marks, for the preview.</summary>
    public string PreviewText
    {
        get
        {
            var line = (Text ?? string.Empty).Replace("\r", "").Split('\n')
                .Select(l => l.Trim().TrimStart('#', '-', '*', '>', ' ').Replace("**", "").Trim())
                .FirstOrDefault(l => l.Length > 0);
            return line ?? "A label in this style";
        }
    }

    partial void OnTextChanged(string value)
    {
        OnPropertyChanged(nameof(PreviewText));
        ShowStylePreviews();
    }

    /// <summary>Each style's item shows this label's text as it would look in that style (with its own changes).</summary>
    private void ShowStylePreviews()
    {
        var sample = PreviewText;
        foreach (var option in StyleOptions)
        {
            option.Look = LabelStyles.Resolve(option.Id, _own);
            option.Sample = sample;
        }
    }

    /// <summary>
    ///   Opens the style editor (Map Settings > Styles) at this style, or at a new style based on it with the given look;
    ///   the style then selected when OK'd, or null. Set by the dialog service.
    /// </summary>
    public Func<string, LabelStyle?, Task<string?>>? EditStyles { get; set; }

    partial void OnSelectedStyleChanged(LabelStyleOption? value) => ShowLook();

    partial void OnHasOutlineChanged(bool value)
    {
        OnPropertyChanged(nameof(ShapeEnabled));
        Own(() => _own.BorderStyle = value ? BorderStyle : BorderDashStyle.None);
    }

    partial void OnHasBackgroundChanged(bool value)
    {
        OnPropertyChanged(nameof(ShapeEnabled));
        Own(() => _own.HasBackground = value);
    }

    partial void OnTextColorChanged(Color value) => Own(() => _own.TextColor = value);
    partial void OnBorderStyleChanged(BorderDashStyle value) => Own(() => { if (HasOutline) _own.BorderStyle = value; });
    partial void OnBorderColorChanged(Color value) => Own(() => _own.BorderColor = value);
    partial void OnBackgroundColorChanged(Color value) => Own(() => _own.BackgroundColor = value);
    partial void OnShapeChanged(RoomShape value) => Own(() => _own.Shape = value);
    partial void OnAlignChanged(LabelAlign value) => Own(() => _own.Align = value);
    partial void OnVerticalAlignChanged(LabelVerticalAlign value) => Own(() => _own.VerticalAlign = value);

    partial void OnFontChanged(FontSpec value) => Own(() =>
    {
        _own.FontName = string.IsNullOrEmpty(value.Family) ? null : value.Family;
        _own.FontSize = (float)value.Size;
        _own.Bold = value.Bold;
        _own.Italic = value.Italic;
    });

    private void Own(Action set)
    {
        if (_showingLook) return;
        set();
        OnPropertyChanged(nameof(HasOwnLook));
        Look = LabelStyles.Resolve(SelectedStyle?.Id ?? LabelStyle.DefaultId, _own);
        ShowStylePreviews();
    }

    /// <summary>The look the chosen style and this label's tweaks give, into the controls.</summary>
    private void ShowLook()
    {
        var look = LabelStyles.Resolve(SelectedStyle?.Id ?? LabelStyle.DefaultId, _own);
        Look = look;
        _showingLook = true;
        try
        {
            TextColor = look.TextColor;
            HasOutline = look.BorderStyle != BorderDashStyle.None;
            BorderStyle = HasOutline ? look.BorderStyle : BorderDashStyle.Solid;
            BorderColor = look.BorderColor;
            HasBackground = look.HasBackground;
            BackgroundColor = look.BackgroundColor;
            Shape = look.Shape == RoomShape.NotARoom ? RoomShape.SquareCorners : look.Shape;
            Font = new FontSpec(look.FontName, look.FontSize, look.Bold, look.Italic);
            Align = look.Align;
            VerticalAlign = look.VerticalAlign;
        }
        finally
        {
            _showingLook = false;
        }
        OnPropertyChanged(nameof(HasOwnLook));
        ShowStylePreviews();
    }

    /// <summary>Drops this label's tweaks: it looks as its style does.</summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ResetToStyle()
    {
        _own = new LabelStyle();
        ShowLook();
    }

    /// <summary>The style editor, at this label's style; its styles listed again afterwards.</summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task OpenStyleEditor()
    {
        if (EditStyles == null) return;
        var id = SelectedStyle?.Id ?? LabelStyle.DefaultId;
        if (await EditStyles(id, null) == null) return;
        LoadStyleOptions(LabelStyles.Has(id) ? id : LabelStyle.DefaultId);
        ShowLook();
    }

    /// <summary>
    ///   A new style with this label's look (its style and what it changes), made in the style editor; on OK the label
    ///   takes the new style and its own changes go.
    /// </summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task CreateStyleFromLook()
    {
        if (EditStyles == null) return;
        var id = SelectedStyle?.Id ?? LabelStyle.DefaultId;
        var chosen = await EditStyles(id, _own.Clone());
        if (chosen == null) return;
        if (LabelStyles.Has(chosen))
        {
            _own = new LabelStyle();
            id = chosen;
        }

        LoadStyleOptions(LabelStyles.Has(id) ? id : LabelStyle.DefaultId);
        ShowLook();
    }

    /// <summary>Shows the controls for this label's own look.</summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ModifyOwnLook() => OwnLookOpen = true;

    private void LoadStyleOptions(string selectedId)
    {
        StyleOptions.Clear();
        foreach (var (style, depth) in LabelStyles.Current.Tree()) StyleOptions.Add(new LabelStyleOption(style.Id, style.Name, depth));
        _showingLook = true;
        try
        {
            SelectedStyle = StyleOptions.FirstOrDefault(option => option.Id == selectedId) ?? StyleOptions.FirstOrDefault();
        }
        finally
        {
            _showingLook = false;
        }
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
        Markdown = label.Markdown;
        _own = label.Own.Clone();
        LoadStyleOptions(label.StyleId);
        ShowLook();
        OwnLookOpen = HasOwnLook;
    }

    public void ApplyTo(MapLabel label)
    {
        label.Text = Text;
        label.Markdown = Markdown;
        label.StyleId = SelectedStyle?.Id ?? LabelStyle.DefaultId;
        label.Own = _own;
        // a tweak that's what the style gives anyway isn't one
        label.TrimOwnToStyle();
    }
}

/// <summary>A label style in a list: indented by how far down the "based on" tree it is.</summary>
public sealed partial class LabelStyleOption(string id, string name, int depth) : ObservableObject
{
    /// <summary>The look a label would have in this style, for a preview.</summary>
    [ObservableProperty]
    private ResolvedLabelStyle? _look;

    /// <summary>The text to preview it with.</summary>
    [ObservableProperty]
    private string _sample = "A label in this style";

    public string Id { get; } = id;

    public int Depth { get; } = depth;

    /// <summary>Changes in place as the style is renamed, so the lists needn't be rebuilt (which took the focus).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Shown))]
    private string _name = name;

    public string Shown => new string(' ', Depth * 4) + Name;

    public override string ToString() => Shown;
}

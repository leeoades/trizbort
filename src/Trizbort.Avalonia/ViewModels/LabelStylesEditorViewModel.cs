using System.Collections.ObjectModel;
using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Setup;

namespace Trizbort.Avalonia.ViewModels;

/// <summary>
///   Map Settings > Styles: the map's label styles as a tree, each based on another (Lee, 24 Nov 2026). The selected
///   style's values show as they come out; one it doesn't set says where it comes from ("from Default"), and changing
///   it makes it the style's own, with a reset to take it from its base again. Edits a copy until OK.
/// </summary>
public partial class LabelStylesEditorViewModel : ViewModelBase
{
    private LabelStyleSet _set = new(LabelStyles.Initial());

    /// <summary>Styles deleted here, and the style each one's labels go to.</summary>
    private readonly Dictionary<string, string> _deletedTo = new();

    private bool _loading;

    public ObservableCollection<LabelStyleOption> Styles { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete), nameof(IsNotDefault))]
    private LabelStyleOption? _selected;

    /// <summary>The styles the selected one could be based on: not itself, nor one based on it.</summary>
    public ObservableCollection<LabelStyleOption> Bases { get; } = new();

    [ObservableProperty]
    private LabelStyleOption? _basedOn;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty] private FontSpec _font = FontSpec.Default;
    [ObservableProperty] private Color _textColor;
    [ObservableProperty] private bool _hasOutline;
    [ObservableProperty] private BorderDashStyle _outlineStyle = BorderDashStyle.Solid;
    [ObservableProperty] private Color _outlineColor;
    [ObservableProperty] private bool _hasBackground;
    [ObservableProperty] private Color _backgroundColor;
    [ObservableProperty] private RoomShape _shape;
    [ObservableProperty] private LabelAlign _align;
    [ObservableProperty] private LabelVerticalAlign _verticalAlign;

    /// <summary>The selected style's look, for the preview.</summary>
    [ObservableProperty]
    private ResolvedLabelStyle? _look;

    // for each value: where it comes from when the style doesn't set it ("from Default"), or empty when it does
    [ObservableProperty] private string _fontFrom = string.Empty;
    [ObservableProperty] private string _textColorFrom = string.Empty;
    [ObservableProperty] private string _outlineFrom = string.Empty;
    [ObservableProperty] private string _outlineColorFrom = string.Empty;
    [ObservableProperty] private string _backgroundFrom = string.Empty;
    [ObservableProperty] private string _backgroundColorFrom = string.Empty;
    [ObservableProperty] private string _shapeFrom = string.Empty;
    [ObservableProperty] private string _alignFrom = string.Empty;
    [ObservableProperty] private string _verticalAlignFrom = string.Empty;

    public IReadOnlyList<LabelAlign> AvailableAligns { get; } = Enum.GetValues<LabelAlign>();
    public IReadOnlyList<LabelVerticalAlign> AvailableVerticalAligns { get; } = Enum.GetValues<LabelVerticalAlign>();

    public bool CanDelete => Selected != null && Selected.Id != LabelStyle.DefaultId;
    public bool IsNotDefault => CanDelete;

    public IReadOnlyList<BorderDashStyle> AvailableOutlineStyles { get; } =
        [BorderDashStyle.Solid, BorderDashStyle.Dash, BorderDashStyle.Dot, BorderDashStyle.DashDot, BorderDashStyle.DashDotDot];

    public IReadOnlyList<RoomShape> AvailableShapes { get; } =
        [RoomShape.SquareCorners, RoomShape.RoundedCorners, RoomShape.Octagonal, RoomShape.Ellipse];

    /// <summary>The style being edited.</summary>
    private LabelStyle? Style => Selected == null ? null : _set.Find(Selected.Id);

    /// <summary>The map's styles, copied to edit; <paramref name="selectId" /> selected.</summary>
    public void Initialize(string? selectId = null, LabelStyle? newFrom = null)
    {
        _set = LabelStyles.Current.Copy();
        _deletedTo.Clear();
        selectId ??= LabelStyle.DefaultId;
        if (newFrom != null)
        {
            // Save as a New Style, from a label's own look: based on its style, with what the label changed
            var style = _set.Add("New Style", _set.Has(selectId) ? selectId : LabelStyle.DefaultId);
            style.SetFrom(newFrom);
            selectId = style.Id;
        }

        Refresh(selectId);
    }

    /// <summary>The style selected (when OK is pressed, the one a label might take).</summary>
    public string? SelectedId => Selected?.Id;

    /// <summary>The edited styles become the map's; labels in a deleted style take the style it was based on.</summary>
    public void ApplyTo(Project project)
    {
        Settings.LabelStyles = _set.Styles;
        foreach (var label in project.Elements.OfType<MapLabel>())
        {
            var id = label.StyleId;
            var guard = 0;
            while (_deletedTo.TryGetValue(id, out var next) && guard++ < 100) id = next;
            if (!_set.Has(id)) id = LabelStyle.DefaultId;
            if (id != label.StyleId) label.StyleId = id;
        }
    }

    [RelayCommand]
    private void AddStyle()
    {
        var basedOn = Selected?.Id ?? LabelStyle.DefaultId;
        var style = _set.Add("New Style", basedOn);
        Refresh(style.Id);
    }

    [RelayCommand]
    private void DeleteStyle()
    {
        if (Style is not { IsDefault: false } style) return;
        var to = _set.Delete(style);
        _deletedTo[style.Id] = to;
        Refresh(to);
    }

    partial void OnSelectedChanged(LabelStyleOption? value) => ShowSelected();

    partial void OnNameChanged(string value)
    {
        if (_loading || Style is not { } style || string.IsNullOrWhiteSpace(value)) return;
        style.Name = value.Trim();
        // the lists show the new name: renamed in place, as rebuilding them takes the focus from the Name box
        foreach (var option in Styles.Concat(Bases).Where(option => option.Id == style.Id)) option.Name = style.Name;
    }

    partial void OnBasedOnChanged(LabelStyleOption? value)
    {
        if (_loading || Style is not { IsDefault: false } style || value == null) return;
        style.BasedOn = value.Id;
        Refresh(style.Id);
    }

    partial void OnFontChanged(FontSpec value) => Set(style =>
    {
        style.FontName = string.IsNullOrEmpty(value.Family) ? null : value.Family;
        style.FontSize = (float)value.Size;
        style.Bold = value.Bold;
        style.Italic = value.Italic;
    });

    partial void OnTextColorChanged(Color value) => Set(style => style.TextColor = value);
    partial void OnHasOutlineChanged(bool value) => Set(style => style.BorderStyle = value ? OutlineStyle : BorderDashStyle.None);
    partial void OnOutlineStyleChanged(BorderDashStyle value) => Set(style => { if (HasOutline) style.BorderStyle = value; });
    partial void OnOutlineColorChanged(Color value) => Set(style => style.BorderColor = value);
    partial void OnHasBackgroundChanged(bool value) => Set(style => style.HasBackground = value);
    partial void OnBackgroundColorChanged(Color value) => Set(style => style.BackgroundColor = value);
    partial void OnShapeChanged(RoomShape value) => Set(style => style.Shape = value);
    partial void OnAlignChanged(LabelAlign value) => Set(style => style.Align = value);
    partial void OnVerticalAlignChanged(LabelVerticalAlign value) => Set(style => style.VerticalAlign = value);

    /// <summary>A value changed by hand: the style's own now.</summary>
    private void Set(Action<LabelStyle> set)
    {
        if (_loading || Style is not { } style) return;
        set(style);
        ShowSelected();
    }

    /// <summary>Takes a value from the base again: "font", "text", "outline", "outlineColour", "background", "backgroundColour", "shape", "align" or "verticalAlign".</summary>
    [RelayCommand]
    private void Reset(string what)
    {
        if (Style is not { IsDefault: false } style) return;
        switch (what)
        {
            case "font":
                style.FontName = null;
                style.FontSize = null;
                style.Bold = null;
                style.Italic = null;
                break;
            case "text": style.TextColor = null; break;
            case "outline": style.BorderStyle = null; break;
            case "outlineColour": style.BorderColor = null; break;
            case "background": style.HasBackground = null; break;
            case "backgroundColour": style.BackgroundColor = null; break;
            case "shape": style.Shape = null; break;
            case "align": style.Align = null; break;
            case "verticalAlign": style.VerticalAlign = null; break;
        }

        ShowSelected();
    }

    private void Refresh(string selectId, bool showSelected = true)
    {
        var wasLoading = _loading;
        _loading = true;
        try
        {
            Styles.Clear();
            foreach (var (style, depth) in _set.Tree()) Styles.Add(new LabelStyleOption(style.Id, style.Name, depth));
            Selected = Styles.FirstOrDefault(option => option.Id == selectId) ?? Styles.FirstOrDefault();
        }
        finally
        {
            _loading = wasLoading;
        }

        if (showSelected) ShowSelected();
    }

    /// <summary>The selected style's values (as they come out) into the controls, with where each comes from.</summary>
    private void ShowSelected()
    {
        if (Style is not { } style) return;
        var wasLoading = _loading;
        _loading = true;
        try
        {
            var look = _set.Resolve(style.Id);
            Look = look;
            Name = style.Name ?? string.Empty;
            Font = new FontSpec(look.FontName, look.FontSize, look.Bold, look.Italic);
            TextColor = look.TextColor;
            HasOutline = look.BorderStyle != BorderDashStyle.None;
            OutlineStyle = HasOutline ? look.BorderStyle : BorderDashStyle.Solid;
            OutlineColor = look.BorderColor;
            HasBackground = look.HasBackground;
            BackgroundColor = look.BackgroundColor;
            Shape = look.Shape == RoomShape.NotARoom ? RoomShape.SquareCorners : look.Shape;
            Align = look.Align;
            VerticalAlign = look.VerticalAlign;

            Bases.Clear();
            if (!style.IsDefault)
                foreach (var option in _set.Tree().Where(item => _set.PossibleBases(style).Contains(item.Style)))
                    Bases.Add(new LabelStyleOption(option.Style.Id, option.Style.Name, option.Depth));
            BasedOn = Bases.FirstOrDefault(option => option.Id == (style.BasedOn ?? LabelStyle.DefaultId));

            FontFrom = From(style, s => s.FontName != null || s.FontSize != null || s.Bold != null || s.Italic != null);
            TextColorFrom = From(style, s => s.TextColor != null);
            OutlineFrom = From(style, s => s.BorderStyle != null);
            OutlineColorFrom = From(style, s => s.BorderColor != null);
            BackgroundFrom = From(style, s => s.HasBackground != null);
            BackgroundColorFrom = From(style, s => s.BackgroundColor != null);
            ShapeFrom = From(style, s => s.Shape != null);
            AlignFrom = From(style, s => s.Align != null);
            VerticalAlignFrom = From(style, s => s.VerticalAlign != null);
        }
        finally
        {
            _loading = wasLoading;
        }
    }

    /// <summary>"" when the style sets it; otherwise "from Default" (or the style it comes from).</summary>
    private string From(LabelStyle style, Func<LabelStyle, bool> sets)
    {
        if (sets(style)) return string.Empty;
        if (style.IsDefault) return "Recast's default";
        var source = _set.SourceOf(style, sets);
        return "from " + (source?.Name ?? "Recast's default");
    }
}

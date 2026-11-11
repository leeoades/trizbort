using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Avalonia.ViewModels;

public enum TextPreset
{
    UpDown,
    DownUp,
    InOut,
    OutIn,
    Custom
}

public partial class ConnectionPropertiesViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _connectionName = string.Empty;

    [ObservableProperty]
    private string _connectionDescription = string.Empty;

    [ObservableProperty]
    private string _startText = string.Empty;

    [ObservableProperty]
    private string _midText = string.Empty;

    [ObservableProperty]
    private string _endText = string.Empty;

    [ObservableProperty]
    private TextPreset _selectedPreset = TextPreset.Custom;

    private bool _updatingPreset;

    [ObservableProperty]
    private bool _isDirectional;

    [ObservableProperty]
    private bool _isDotted;

    [ObservableProperty]
    private Color _connectionColor = Color.Transparent;

    [ObservableProperty]
    private bool _hasDoor;

    [ObservableProperty]
    private bool _lockable;

    [ObservableProperty]
    private bool _locked;

    [ObservableProperty]
    private bool _open;

    [ObservableProperty]
    private bool _openable;

    public void Initialize(Connection connection)
    {
        ConnectionName = connection.Name ?? string.Empty;
        ConnectionDescription = connection.Description ?? string.Empty;
        // an end with no label shows the direction its side gives it ("east" on the right of a room), so it can be
        // seen, and changed; left as it is, it stays hidden on the map
        _startSide = ConnectionEndLabels.SideDirection(connection, true);
        _endSide = ConnectionEndLabels.SideDirection(connection, false);
        StartText = Shown(connection.StartText, _startSide);
        MidText = connection.MidText ?? string.Empty;
        EndText = Shown(connection.EndText, _endSide);
        SelectedPreset = MatchPreset();
        IsDirectional = connection.Flow == ConnectionFlow.OneWay;
        IsDotted = connection.Style == ConnectionStyle.Dashed;
        ConnectionColor = connection.ConnectionColor;

        if (connection.Door != null)
        {
            HasDoor = true;
            Lockable = connection.Door.Lockable;
            Locked = connection.Door.Locked;
            Open = connection.Door.Open;
            Openable = connection.Door.Openable;
        }
        else
        {
            HasDoor = false;
            Lockable = false;
            Locked = false;
            Open = false;
            Openable = false;
        }
    }

    public void ApplyTo(Connection connection)
    {
        connection.Name = ConnectionName;
        connection.Description = ConnectionDescription;
        connection.StartText = Stored(StartText, _startSide);
        connection.MidText = MidText;
        connection.EndText = Stored(EndText, _endSide);

        connection.Flow = IsDirectional ? ConnectionFlow.OneWay : ConnectionFlow.TwoWay;
        connection.Style = IsDotted ? ConnectionStyle.Dashed : ConnectionStyle.Solid;

        connection.ConnectionColor = ConnectionColor;

        if (HasDoor)
        {
            connection.Door = new Door
            {
                Lockable = Lockable,
                Locked = Locked,
                Open = Open,
                Openable = Openable
            };
        }
        else
        {
            connection.Door = null;
        }
    }

    private MappableDirection? _startSide;
    private MappableDirection? _endSide;

    private static string Shown(string label, MappableDirection? side) =>
        string.IsNullOrEmpty(label) && side != null ? DirectionWords.Word(side.Value) : label ?? string.Empty;

    /// <summary>The side's own direction, alone, is left off the map, as before.</summary>
    private static string Stored(string text, MappableDirection? side)
    {
        var parts = DirectionWords.SplitLabel(text);
        return side != null && parts.Count == 1 && DirectionWords.TryParse(parts[0], out var direction) &&
               direction == side
            ? string.Empty
            : text ?? string.Empty;
    }

    partial void OnStartTextChanged(string value) => UpdatePresetFromText();

    partial void OnMidTextChanged(string value) => UpdatePresetFromText();

    partial void OnEndTextChanged(string value) => UpdatePresetFromText();

    partial void OnSelectedPresetChanged(TextPreset value)
    {
        if (_updatingPreset || value == TextPreset.Custom) return;
        _updatingPreset = true;
        try
        {
            SetLabelPreset(ToLabel(value));
        }
        finally
        {
            _updatingPreset = false;
        }
    }

    private void UpdatePresetFromText()
    {
        if (_updatingPreset) return;
        var match = MatchPreset();
        if (match == SelectedPreset) return;
        _updatingPreset = true;
        try
        {
            SelectedPreset = match;
        }
        finally
        {
            _updatingPreset = false;
        }
    }

    private bool MatchText(ConnectionLabel label)
    {
        Connection.GetText(label, out var start, out var end);
        return StartText == start && EndText == end && string.IsNullOrEmpty(MidText);
    }

    private TextPreset MatchPreset()
    {
        if (MatchText(ConnectionLabel.Up)) return TextPreset.UpDown;
        if (MatchText(ConnectionLabel.Down)) return TextPreset.DownUp;
        if (MatchText(ConnectionLabel.In)) return TextPreset.InOut;
        if (MatchText(ConnectionLabel.Out)) return TextPreset.OutIn;
        return TextPreset.Custom;
    }

    private static ConnectionLabel ToLabel(TextPreset preset) => preset switch
    {
        TextPreset.UpDown => ConnectionLabel.Up,
        TextPreset.DownUp => ConnectionLabel.Down,
        TextPreset.InOut => ConnectionLabel.In,
        _ => ConnectionLabel.Out
    };

    /// <summary>Writes Start/End for the label; Middle is left untouched.</summary>
    public void SetLabelPreset(ConnectionLabel label)
    {
        Connection.GetText(label, out var start, out var end);
        StartText = start;
        EndText = end;
    }

    /// <summary>Empties Start, Middle and End and selects Custom.</summary>
    public void ClearText()
    {
        StartText = string.Empty;
        MidText = string.Empty;
        EndText = string.Empty;
        UpdatePresetFromText();
    }
}

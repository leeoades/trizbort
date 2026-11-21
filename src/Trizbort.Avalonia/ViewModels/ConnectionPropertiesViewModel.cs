using System;
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

    /// <summary>The room above the Start box: the one on the left, or the upper one when they're one above the other.</summary>
    [ObservableProperty]
    private string _startRoomName = string.Empty;

    /// <summary>The room above the End box.</summary>
    [ObservableProperty]
    private string _endRoomName = string.Empty;

    private string _fromRoom = string.Empty;
    private string _toRoom = string.Empty;

    /// <summary>
    ///   Whether the boxes show the join the other way round from how it's stored: Start is always the room on the
    ///   left (or above), whichever end the join was drawn from.
    /// </summary>
    public bool ShownTheOtherWay { get; private set; }

    /// <summary>OK reverses the join, so its arrow points from the Start room to the End room.</summary>
    private bool _reverse;

    /// <summary>Whether the Start box is the join's start end once OK has reversed it, or not.</summary>
    private bool _leftIsStart;

    /// <summary>The arrow was turned round to point Start to End when One Way Arrow was ticked (and back if unticked).</summary>
    private bool _reversedForArrow;

    /// <summary>
    ///     A one-way join points from the Start room to the End room. Ticking One Way Arrow makes it so, whichever end
    ///     the join was drawn from (Lee, 21 Nov 2026: it pointed back to the room he'd just come from).
    /// </summary>
    partial void OnIsDirectionalChanged(bool value)
    {
        if (_initializing) return;
        if (value && !_leftIsStart)
        {
            _reverse = !_reverse;
            _leftIsStart = true;
            _reversedForArrow = true;
        }
        else if (!value && _reversedForArrow)
        {
            _reverse = !_reverse;
            _leftIsStart = false;
            _reversedForArrow = false;
        }
    }

    /// <summary>
    ///     Flip: the one-way arrow points the other way, from the End room to the Start room; the boxes swap round so
    ///     Start is still the room it leaves.
    /// </summary>
    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void Flip()
    {
        _reverse = !_reverse;
        _reversedForArrow = false;
        (StartRoomName, EndRoomName) = (EndRoomName, StartRoomName);
        (StartText, EndText) = (EndText, StartText);
        (_startSide, _endSide) = (_endSide, _startSide);
    }

    private bool _initializing;

    [ObservableProperty]
    private bool _isDotted;

    /// <summary>Drawn as two short stubs, one out of each room, each labelled with the room at the other end.</summary>
    [ObservableProperty]
    private bool _isSplit;

    /// <summary>It doesn't always lead where it's drawn: directions take it only when there's no other way.</summary>
    [ObservableProperty]
    private bool _isUnreliable;

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
        _initializing = true;
        ConnectionName = connection.Name ?? string.Empty;
        ConnectionDescription = connection.Description ?? string.Empty;
        var startRoom = connection.GetSourceRoom();
        var endRoom = connection.GetTargetRoom();
        // a one-way join reads the way its arrow points: Start is the room it leaves, End the room it goes to
        ShownTheOtherWay = connection.Flow != ConnectionFlow.OneWay && IsTheOtherWay(startRoom, endRoom);
        _fromRoom = NameOf(startRoom);
        _toRoom = NameOf(endRoom);
        StartRoomName = ShownTheOtherWay ? _toRoom : _fromRoom;
        EndRoomName = ShownTheOtherWay ? _fromRoom : _toRoom;
        // an end with no label shows the direction its side gives it ("east" on the right of a room), so it can be
        // seen, and changed; left as it is, it stays hidden on the map
        var startSide = ConnectionEndLabels.SideDirection(connection, true);
        var endSide = ConnectionEndLabels.SideDirection(connection, false);
        _startSide = ShownTheOtherWay ? endSide : startSide;
        _endSide = ShownTheOtherWay ? startSide : endSide;
        StartText = Shown(ShownTheOtherWay ? connection.EndText : connection.StartText, _startSide);
        MidText = connection.MidText ?? string.Empty;
        EndText = Shown(ShownTheOtherWay ? connection.StartText : connection.EndText, _endSide);
        SelectedPreset = MatchPreset();
        IsDirectional = connection.Flow == ConnectionFlow.OneWay;
        _reverse = false;
        _reversedForArrow = false;
        _leftIsStart = !ShownTheOtherWay;
        _initializing = false;
        IsDotted = connection.Style == ConnectionStyle.Dashed;
        IsSplit = connection.Split;
        IsUnreliable = connection.Unreliable;
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
        var left = Stored(StartText, _startSide);
        var right = Stored(EndText, _endSide);
        if (_reverse) connection.Reverse();
        connection.StartText = _leftIsStart ? left : right;
        connection.MidText = MidText;
        connection.EndText = _leftIsStart ? right : left;

        connection.Flow = IsDirectional ? ConnectionFlow.OneWay : ConnectionFlow.TwoWay;
        connection.Style = IsDotted ? ConnectionStyle.Dashed : ConnectionStyle.Solid;
        connection.Split = IsSplit;
        connection.Unreliable = IsUnreliable;

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

    private static string NameOf(Room? room) => room == null ? string.Empty : room.Name ?? string.Empty;

    /// <summary>
    ///   Whether the join's end room is to the left of its start room, or above it when the two are more one above
    ///   the other than side by side.
    /// </summary>
    private static bool IsTheOtherWay(Room? start, Room? end)
    {
        if (start == null || end == null || start == end) return false;
        var dx = end.InnerBounds.Center.X - start.InnerBounds.Center.X;
        var dy = end.InnerBounds.Center.Y - start.InnerBounds.Center.Y;
        return Math.Abs(dx) >= Math.Abs(dy) ? dx < 0 : dy < 0;
    }

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

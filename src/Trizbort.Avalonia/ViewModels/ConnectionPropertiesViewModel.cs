using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;

namespace Trizbort.Avalonia.ViewModels;

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
        StartText = connection.StartText ?? string.Empty;
        MidText = connection.MidText ?? string.Empty;
        EndText = connection.EndText ?? string.Empty;
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
        connection.StartText = StartText;
        connection.MidText = MidText;
        connection.EndText = EndText;

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

    public void SetLabelPreset(ConnectionLabel label)
    {
        Connection.GetText(label, out var start, out var end);
        StartText = start;
        EndText = end;
    }
}

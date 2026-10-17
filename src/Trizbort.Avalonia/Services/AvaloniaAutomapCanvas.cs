using Avalonia.Threading;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using AutomapEngine = Trizbort.Automap.Automap;

namespace Trizbort.Avalonia.Services;

public class AvaloniaAutomapCanvas : IAutomapCanvas
{
    private readonly AutomapCanvas _innerCanvas;

    public AvaloniaAutomapCanvas(Project? project = null, AutomapEngine? automap = null)
    {
        _innerCanvas = new AutomapCanvas(project, automap);
    }

    public Room FindRoom(string roomName, string roomDescription, string line, RoomMatcher matcher)
    {
        return Dispatcher.UIThread.CheckAccess()
            ? _innerCanvas.FindRoom(roomName, roomDescription, line, matcher)
            : Dispatcher.UIThread.Invoke(() => _innerCanvas.FindRoom(roomName, roomDescription, line, matcher));
    }

    public Room CreateRoom(Room existing, string name)
    {
        return Dispatcher.UIThread.CheckAccess()
            ? _innerCanvas.CreateRoom(existing, name)
            : Dispatcher.UIThread.Invoke(() => _innerCanvas.CreateRoom(existing, name));
    }

    public Room CreateRoom(Room existing, MappableDirection directionFromExisting, string roomName, string line)
    {
        return Dispatcher.UIThread.CheckAccess()
            ? _innerCanvas.CreateRoom(existing, directionFromExisting, roomName, line)
            : Dispatcher.UIThread.Invoke(() => _innerCanvas.CreateRoom(existing, directionFromExisting, roomName, line));
    }

    public void Connect(Room source, MappableDirection directionFromSource, Room target, bool assumeTwoWayConnections)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            _innerCanvas.Connect(source, directionFromSource, target, assumeTwoWayConnections);
        }
        else
        {
            Dispatcher.UIThread.Invoke(() => _innerCanvas.Connect(source, directionFromSource, target, assumeTwoWayConnections));
        }
    }

    public void AddExitStub(Room room, MappableDirection direction)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            _innerCanvas.AddExitStub(room, direction);
        }
        else
        {
            Dispatcher.UIThread.Invoke(() => _innerCanvas.AddExitStub(room, direction));
        }
    }

    public void RemoveExitStub(Room room, MappableDirection direction)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            _innerCanvas.RemoveExitStub(room, direction);
        }
        else
        {
            Dispatcher.UIThread.Invoke(() => _innerCanvas.RemoveExitStub(room, direction));
        }
    }

    public void SelectRoom(Room room)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            _innerCanvas.SelectRoom(room);
        }
        else
        {
            Dispatcher.UIThread.Invoke(() => _innerCanvas.SelectRoom(room));
        }
    }

    public void RemoveRoom(Room otherRoom)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            _innerCanvas.RemoveRoom(otherRoom);
        }
        else
        {
            Dispatcher.UIThread.Invoke(() => _innerCanvas.RemoveRoom(otherRoom));
        }
    }
}

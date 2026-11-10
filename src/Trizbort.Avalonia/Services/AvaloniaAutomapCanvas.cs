using System;
using Avalonia.Threading;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Editor;
using AutomapEngine = Trizbort.Automap.Automap;

namespace Trizbort.Avalonia.Services;

public class AvaloniaAutomapCanvas : IAutomapCanvas
{
    private readonly AutomapCanvas _innerCanvas;

    public AvaloniaAutomapCanvas(Project? project = null, AutomapEngine? automap = null, EditorSession? session = null)
    {
        _innerCanvas = new AutomapCanvas(project, automap, session);
    }

    public RoomSearchResult FindRooms(string roomName, string roomDescription, RoomMatcher matcher)
    {
        return Dispatcher.UIThread.CheckAccess()
            ? _innerCanvas.FindRooms(roomName, roomDescription, matcher)
            : Dispatcher.UIThread.Invoke(() => _innerCanvas.FindRooms(roomName, roomDescription, matcher));
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

    public bool MakeSpaceForNewRooms
    {
        get => _innerCanvas.MakeSpaceForNewRooms;
        set => _innerCanvas.MakeSpaceForNewRooms = value;
    }

    public void ConnectLoop(Room room, MappableDirection direction)
    {
        _innerCanvas.ConnectLoop(room, direction);
    }

    public bool AbbreviateDirections
    {
        get => _innerCanvas.AbbreviateDirections;
        set => _innerCanvas.AbbreviateDirections = value;
    }

    public void TidyAround(Room room)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            _innerCanvas.TidyAround(room);
        }
        else
        {
            Dispatcher.UIThread.Invoke(() => _innerCanvas.TidyAround(room));
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

    public void ConnectByCommand(Room source, Room target, string command)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            _innerCanvas.ConnectByCommand(source, target, command);
        }
        else
        {
            Dispatcher.UIThread.Invoke(() => _innerCanvas.ConnectByCommand(source, target, command));
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

    public IDisposable BeginBatch(string title)
    {
        return Dispatcher.UIThread.CheckAccess()
            ? _innerCanvas.BeginBatch(title)
            : Dispatcher.UIThread.Invoke(() => _innerCanvas.BeginBatch(title));
    }

    public IReadOnlyList<Room> RoomsAlong(Room room, MappableDirection direction)
    {
        return Dispatcher.UIThread.CheckAccess()
            ? _innerCanvas.RoomsAlong(room, direction)
            : Dispatcher.UIThread.Invoke(() => _innerCanvas.RoomsAlong(room, direction));
    }

    public Room ResolveRoom(Room room)
    {
        return Dispatcher.UIThread.CheckAccess()
            ? _innerCanvas.ResolveRoom(room)
            : Dispatcher.UIThread.Invoke(() => _innerCanvas.ResolveRoom(room));
    }
}

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
            _innerCanvas.RethinkElsewhere = false;
            _innerCanvas.TidyAround(room);
            return;
        }

        // the quick tidy on the UI thread; a rethink (the solver, up to a second or two) is worked out here, on
        // automap's thread, so the window stays responsive, then put on the map on the UI thread
        var plan = Dispatcher.UIThread.Invoke(() =>
        {
            _innerCanvas.RethinkElsewhere = true;
            _innerCanvas.TidyAround(room);
            return _innerCanvas.TakeRethink();
        });
        if (plan == null) return;
        var moves = plan.Run();
        Dispatcher.UIThread.Invoke(() => _innerCanvas.FinishRethink(plan, moves));
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

    public void ConnectStops(Room source, Room target, string command)
    {
        if (Dispatcher.UIThread.CheckAccess()) _innerCanvas.ConnectStops(source, target, command);
        else Dispatcher.UIThread.Invoke(() => _innerCanvas.ConnectStops(source, target, command));
    }

    public Trizbort.Domain.Misc.Rect? NewRoomBounds(Room existing, MappableDirection? direction)
    {
        return Dispatcher.UIThread.CheckAccess()
            ? _innerCanvas.NewRoomBounds(existing, direction)
            : Dispatcher.UIThread.Invoke(() => _innerCanvas.NewRoomBounds(existing, direction));
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

    public void ForgetWay(Room from, MappableDirection direction, Room to)
    {
        if (Dispatcher.UIThread.CheckAccess()) _innerCanvas.ForgetWay(from, direction, to);
        else Dispatcher.UIThread.Invoke(() => _innerCanvas.ForgetWay(from, direction, to));
    }

    public Room ResolveRoom(Room room)
    {
        return Dispatcher.UIThread.CheckAccess()
            ? _innerCanvas.ResolveRoom(room)
            : Dispatcher.UIThread.Invoke(() => _innerCanvas.ResolveRoom(room));
    }
}

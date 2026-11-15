using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Trizbort.Automap;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
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

        // the quick tidy on the UI thread; a rethink (the solver, up to a second) is worked out in the background
        // while automap carries on, then put on the map on the UI thread as its own undo step
        var plan = Dispatcher.UIThread.Invoke(() =>
        {
            _innerCanvas.RethinkElsewhere = true;
            _innerCanvas.TidyAround(room);
            return _innerCanvas.TakeRethink();
        });
        if (plan == null) return;

        // a newer rethink replaces one still being worked out
        _rethinkCancel?.Cancel();
        var cancel = new CancellationTokenSource();
        _rethinkCancel = cancel;
        _ = Task.Run(() =>
        {
            try
            {
                var moves = plan.Run(cancel.Token);
                if (!cancel.IsCancellationRequested) ApplyRethink(plan, moves, cancel.Token, 0);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("Rethinking the layout failed: " + ex.Message);
            }
        });
    }

    private CancellationTokenSource? _rethinkCancel;

    /// <summary>Puts a rethink on the map, waiting while the user is part-way through a change of their own.</summary>
    private void ApplyRethink(
        LayoutTidy.TidyPlan plan, IReadOnlyDictionary<Room, Vector>? moves, CancellationToken token, int attempt)
    {
        if (moves == null) return;
        Dispatcher.UIThread.Post(() =>
        {
            if (token.IsCancellationRequested) return;
            if (_innerCanvas.FinishRethinkInTheBackground(plan, moves) < 0 && attempt < 100)
                DispatcherTimer.RunOnce(() => ApplyRethink(plan, moves, token, attempt + 1),
                    TimeSpan.FromMilliseconds(100));
        });
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

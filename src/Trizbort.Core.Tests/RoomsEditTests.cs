using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using Trizbort.Domain.Application;
using Trizbort.Domain.Elements;
using Trizbort.Domain.Enums;
using Trizbort.Domain.Misc;
using Trizbort.Editor;

namespace Trizbort.Core.Tests;

/// <summary>Lee, 24 Nov 2026: select several rooms and set their darkness, shape, colours and so on together.</summary>
[TestFixture]
[Category("Unit")]
public class RoomsEditTests : IsolatedProjectTests
{
  private EditorSession _session;

  [SetUp]
  public void MakeSession() => _session = new EditorSession();

  private Room Add(float x, bool dark, Color fill)
  {
    var room = _session.AddRoom(new Vector(x, 0));
    room.IsDark = dark;
    room.RoomFillColor = fill;
    return room;
  }

  private sealed class SetsRooms : IEditorPrompts
  {
    public RoomsEdit Edit { get; init; } = new();
    public IReadOnlyList<Room> Asked { get; private set; }

    public Task<bool> EditRoomsAsync(IReadOnlyList<Room> rooms, CancellationToken token = default)
    {
      Asked = rooms;
      Edit.ApplyTo(rooms);
      return Task.FromResult(true);
    }

    public Task<bool> ConfirmAsync(string message, string title, CancellationToken token = default) =>
      Task.FromResult(true);
  }

  [Test]
  public void WhatTheRoomsShare_IsKnown_AndWhereTheyDiffer_IsNull()
  {
    var a = Add(0, true, Color.Red);
    var b = Add(200, false, Color.Red);
    a.Shape = b.Shape = RoomShape.Octagonal;

    var shared = RoomsEdit.Of(new[] { a, b });

    shared.IsDark.ShouldBeNull();
    shared.FillColor.Value.ToArgb().ShouldBe(Color.Red.ToArgb());
    shared.Shape.ShouldBe(RoomShape.Octagonal);
  }

  [Test]
  public void OnlyWhatsSet_Changes_EachRoomKeepsTheRest()
  {
    var a = Add(0, true, Color.Red);
    var b = Add(200, false, Color.Blue);

    new RoomsEdit { Shape = RoomShape.Ellipse }.ApplyTo(new[] { a, b });

    a.Shape.ShouldBe(RoomShape.Ellipse);
    b.Shape.ShouldBe(RoomShape.Ellipse);
    a.IsDark.ShouldBeTrue();
    b.IsDark.ShouldBeFalse();
    a.RoomFillColor.ToArgb().ShouldBe(Color.Red.ToArgb());
    b.RoomFillColor.ToArgb().ShouldBe(Color.Blue.ToArgb());
  }

  [Test]
  public async Task Properties_WithSeveralRoomsSelected_EditsThemAll_AsOneUndoStep()
  {
    var a = Add(0, false, Color.Red);
    var b = Add(200, false, Color.Blue);
    var c = Add(400, false, Color.Green);
    _session.Selection.Set(new Element[] { a, b });
    var prompts = new SetsRooms { Edit = new RoomsEdit { IsDark = true } };
    _session.Prompts = prompts;

    _session.CanShowProperties.ShouldBeTrue();
    (await _session.ShowSelectedPropertiesAsync()).ShouldBeTrue();

    prompts.Asked.ShouldBe(new[] { a, b }, ignoreOrder: true);
    a.IsDark.ShouldBeTrue();
    b.IsDark.ShouldBeTrue();
    c.IsDark.ShouldBeFalse();

    _session.PerformUndo();
    Project.Current.Elements.OfType<Room>().ShouldAllBe(room => !room.IsDark);
  }

  [Test]
  public void OneRoom_StillOpensItsOwnProperties()
  {
    var a = Add(0, false, Color.Red);
    _session.Selection.Set(new Element[] { a });
    _session.CanShowProperties.ShouldBeTrue();
    _session.SelectedRooms.Count.ShouldBe(1);
  }
}

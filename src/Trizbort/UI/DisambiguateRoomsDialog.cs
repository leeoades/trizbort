using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Trizbort.Domain.Elements;

namespace Trizbort.UI;

internal partial class DisambiguateRoomsDialog : Form {
  public DisambiguateRoomsDialog()
  {
    InitializeComponent();

    _thisRoomButton.Enabled = false;
  }

  public Room Disambiguation {
    get {
      if (DialogResult == DialogResult.Yes && _roomNamesListBox.SelectedIndex != -1)
        return (_roomNamesListBox.SelectedItem as AmbiguousRoom).Room;
      return null;
    }
  }

  public bool UserDoesntCareAnyMore => DialogResult == DialogResult.Abort;

  public void SetTranscriptContext(string roomName, string roomDescription, string line)
  {
    _transcriptContextTextBox.Text = string.Format("{0}\n{1}", line, roomDescription).Replace("\r", string.Empty)
                                           .Replace("\n", "\r\n");
  }

  protected override void OnLoad(EventArgs e)
  {
    if (_roomNamesListBox.Items.Count == 1)
      // if there's only one option, select it
      _roomNamesListBox.SelectedIndex = 0;

    _transcriptContextTextBox.Focus();
    _transcriptContextTextBox.Select(0, 0);

    base.OnLoad(e);
  }

  public void AddAmbiguousRoom(Room room)
  {
    _roomNamesListBox.Items.Add(new AmbiguousRoom(room));
  }

  public void AddAmbiguousRooms(IEnumerable<Room> rooms)
  {
    foreach (var room in rooms) AddAmbiguousRoom(room);
  }

  private void RoomNamesListBox_SelectedIndexChanged(object sender, EventArgs e)
  {
    if (_roomNamesListBox.SelectedIndex != -1) {
      _thisRoomButton.Enabled = true;
      var room = (_roomNamesListBox.SelectedItem as AmbiguousRoom).Room;
      _roomDescriptionTextBox.Text = room.PrimaryDescription;
    }
    else {
      _thisRoomButton.Enabled = false;
      _roomDescriptionTextBox.Text = string.Empty;
    }
  }

  /// <summary>
  ///   A Room reference which will ToString() as the room name.
  ///   For use in the room names list box.
  /// </summary>
  private class AmbiguousRoom {
    public AmbiguousRoom(Room room)
    {
      Room = room;
    }

    public Room Room { get; }

    public override string ToString()
    {
      return Room.Name;
    }
  }
}
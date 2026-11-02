Hollywood Hijinx (Infocom, 1986): Lee's earlier transcript (GitHub #25), four play sessions appended by Frotz in one
file, two-way connections on (the default). It crashed automap with "Sequence contains no matching element". Fixed
on the way:
- Re-pointing an existing connection renamed the room's shared port (two "East" ports, no "South"), so the next
  connection needing that direction crashed. Now only that connection's end moves.
- "There's already a room that way" took the connection's own end (the current room) as the other room, so the Patio
  heading was swallowed. Now it is the room at the other end.
- Each new session starts back in the first room (the second session's "e" is from South Junction, not Front Porch).
The paths round the house bend, so "a room is already that way" is answered "keep both" (settings.txt), as you would
in the app. expected.txt is the map automap made after those fixes. The loop round the house was checked against the
transcript by hand; the rest (maze edge, cliff, beach stairs) has not been, so correct it if it is wrong.

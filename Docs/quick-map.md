# A quick map, step by step

[Back to the guide](index.md)

This page builds a small game's map from the keyboard, a few minutes' work: a city, a river, a forest, a desert, lava
pits, gold mines and a palace. It shows the quickest ways to add rooms, regions and joins. The finished map is
[generic-game.trizbort](maps/generic-game.trizbort), if you'd like to open it alongside.

![The finished quick map: the grey city, the blue river, the forest, the desert, lava pits, gold mines, the palace and
its jails.](images/quick-map.png)

## The city

1. **Ctrl+N** for a new map. Point at the middle of the map and press **R** to add a room, then **F2** to name it
   *City Center*.
2. With City Center selected, press **Ctrl+Up**. A new room appears to the north, joined to it, with its properties
   open: name it *N City* and press **Enter**.
3. Carry on round in a ring with **Ctrl+arrows**: **Ctrl+Left** for *NW City*, **Ctrl+Down** for *W City* and
   *SW City*, **Ctrl+Right** for *S City* and *SE City*, **Ctrl+Up** for *E City* and *NE City*. **Ctrl+Left** from
   NE City finds N City already there and just joins them.
4. Now join the middle to the ring. Select City Center and press **Ctrl** with each arrow: where there's a room that
   way, Recast joins it and moves the selection there, so press the arrow back the other way (without Ctrl) to return
   to the middle. The number pad's **7**, **9**, **1** and **3** do the diagonals the same way.

## A region for the city

1. **Ctrl+A** selects every room.
2. Right-click one of them and choose **Rooms > Set Region > Add New Region...**. Name it *City*, make its fill grey,
   and press **OK**. The whole city turns grey.

## Death traps

1. Select N City, and **Ctrl+Up** for *City Entry*, the way out.
2. From W City, **Ctrl+Left** for *Behind the Barrel*, and somewhere else *Varmints' Pub*. Players die in both, so
   mark them.
3. Select the two (**Shift+click** adds the second), right-click and add a new region, *Trap Rooms*, in red. Any
   death trap you add later goes in it too.

## The river, the shore and the forest

1. **Ctrl+Up** from City Entry for *C River*, then **Ctrl+Left** and **Ctrl+Right** from it for *W River* and
   *E River*. Put them in a blue *River* region.
2. Crossing the river takes something clever, so its joins should be dotted. Press **T**: from now on the joins you
   add are dotted. **Ctrl+Up** from each river room for *W Shore*, *C Shore* and *E Shore*, then press **P** to go back
   to plain joins.
3. **Ctrl+Up** from each shore room for *W Forest*, *C Forest* and *E Forest*. The middle of the forest is dark: select
   it and press **K**.

## Lava pits, gold mines and a palace

1. Use the number pad with **Ctrl** (**Ctrl+7**, **8**, **9**) to add a *Desert* beyond the forest, in a region of
   its own. Its fill doesn't have to be one of the usual colours: pick any.
2. Add a ring of lava pits to one side and gold mines to the other the same way, each in an orange or yellow region,
   with *PERDITION* (a death trap, so into **Trap Rooms**) at the bottom of the pits.
3. Some ways can't be walked back: select those joins (**Shift+click** to pick several) and press **A** to make them
   one way.
4. **Ctrl+Up** from the Desert for *Palace Gates*, in a purple *Palace* region. Purple is dark, so give the region white
   text (**Map Settings > Regions**). Add a subtitle, *start of palace*, in the room's properties.

## Putting mistakes right

The palace has a few last tricks: a hall, a throne room, a balcony and two jails.

- Joins drawn the wrong way round? Labelled up and down (**D**) when they go down and up? Select them and press **V**
  to reverse them. Press it again to watch them flip back.
- **Ctrl+Z** undoes anything, a step at a time, and **Ctrl+Y** does it again.
- A room that shouldn't be joined: add it with **Ctrl+Up** and then delete the join (select it, **Delete**), or add it
  with **R** and set its region yourself.
- Rooms in a tangle? **Rooms > Tidy Map...** (right-click a room) straightens them out.

The finished map has objects in its rooms too ("Whiner #1"). Add them as you go, or come back afterwards: open each
room's properties and list them, one a line. Rooms that look alike can be copied with **Ctrl+C** and **Ctrl+V**, or **Ctrl+D** to duplicate.

You'll find your own favourite ways and shortcuts as you go: [Keyboard shortcuts](keyboard.md) lists them all.

Next: [Drawing the map](map.md)

# More help and resources

[Back to the guide](index.md)

## Something wrong, or an idea?

Report problems and ask for features on [GitHub Issues](https://github.com/leeoades/trizbort/issues). Have a quick
look to see whether it's there already, but don't worry about searching hard. A map, or a transcript for automap
problems, helps a lot.

## Maps of games

- The [IF Archive's maps](https://ifarchive.org/indexes/if-archive/solutions/maps) hold maps of many games, plenty of
  them `.trizbort` files. Copy the link to one and open it straight from the web with **File > Open Map from Web...**
  (**Ctrl+Shift+O**). Maps often give away puzzles, so look only if you want to know.
- [CASA, the Classic Adventures Solution Archive](https://solutionarchive.com/), has maps and solutions for thousands
  of games, many of them from the 1980s.
- [David Welbourn's walkthroughs](https://plover.net/~davidw/sol/index.html) come with clear box-and-line maps.
- If you've mapped a game, you can send your map to the IF Archive for others to use.
- [IFWiki](https://ifwiki.org/Map) has more about game maps.

## Other mapping tools

The [IF Archive's mapping tools](https://ifarchive.org/indexes/if-archive/mapping-tools) has other ways of mapping
games, in case Recast isn't quite what you need.

## Several maps in one PDF

Recast's PDF export is one page per map. To put several together (a game's upstairs and downstairs, or maps at the
back of a walkthrough), a PDF tool such as **pdftk** joins them:

```text
pdftk upstairs.pdf downstairs.pdf cat output both.pdf
```

## Where your settings are kept

Your [app settings](settings.md#app-settings) are in a file called `appsettings.json`:

- **Windows:** `%APPDATA%\Trizbort`
- **macOS:** `~/Library/Application Support/Trizbort`
- **Linux:** `~/.config/trizbort`

Close Recast and delete the file to go back to the defaults. Map settings are saved in each map, not here.

## Trizbort's story

Trizbort was written by Genstein, up to version 1.5. Jason Lautzenheiser carried it on from 2014, adding regions,
colours and more, with help from Andrew Schultz (who wrote much of the original guide this one grew from), Tymian and
Matt Watkins. Lee Oades brought it up to date as Trizbort 2, with labels, curved joins and themes, and then rebuilt it as
Trizbort: Recast, which runs on Windows, Linux and macOS.

Recast opens and saves the same `.trizbort` maps as the original Trizbort and Trizbort 2. If you still use Trizbort 2
on Windows, its own guide is here: [Using Trizbort](../legacy/Docs/index.md).

Want to help? Recast is open source: [the code is on GitHub](https://github.com/leeoades/trizbort).

[Back to the guide](index.md)

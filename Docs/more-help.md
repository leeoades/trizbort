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

## The original Trizbort

Recast opens and saves the same `.trizbort` maps as the original Trizbort and Trizbort 2. Their own guide is still
here: [Using Trizbort](../legacy/Docs/index.md).

[Back to the guide](index.md)

# File association

Double-clicking a `.trizbort` file should start Trizbort: Recast with that file (it is passed as the first argument).

## Linux
```
cp packaging/linux/trizbort.desktop ~/.local/share/applications/
xdg-mime install --mode user packaging/linux/trizbort-mime.xml
mkdir -p ~/.local/share/icons/hicolor/256x256/apps
cp packaging/icons/trizbort-recast-256.png ~/.local/share/icons/hicolor/256x256/apps/trizbort-recast.png
update-desktop-database ~/.local/share/applications
xdg-mime default trizbort.desktop application/x-trizbort
```
The release tarball contains `trizbort-recast`, both files above and the icon (`trizbort-recast.png`; use it in place
of `packaging/icons/trizbort-recast-256.png`). `Exec=trizbort-recast %f` assumes it is on your PATH
(for example, copy or link it into `~/.local/bin`); otherwise edit `Exec` to its full path.

## Windows
`TrizbortRecast-win-x64-Setup.exe` sets this up when it installs Recast (per user, under `HKCU\Software\Classes`,
pointing at Velopack's launcher so it survives updates) and removes it when Recast is uninstalled. Any copy of a
release also sets it at start-up while **App Settings > General > Open .trizbort files with Recast** is on (the default),
and turning that off removes it (`Services/FileAssociation.cs`; development builds don't set it by themselves). For a
copy you'd rather set by hand, edit the path to `TrizbortRecast.exe` in `packaging/windows/register-file-association.reg`
and double-click it. If you once chose another app with "Always use this app", Windows keeps that choice: pick Recast
in **Open with > Choose another app** instead.

## macOS
The release zips (`osx-arm64` for Apple Silicon, `osx-x64` for Intel) contain `Trizbort Recast.app`, built by
`packaging/macos/make-app.sh` on a macOS runner. Its `Info.plist` (from `packaging/macos/Info.plist`) declares the
`.trizbort` document type, so Finder opens maps in the app once it is in Applications. The app is signed ad hoc only
(no Apple Developer ID, not notarised), so the first time macOS refuses to open it: open **System Settings > Privacy &
Security** and choose **Open Anyway**, or run `xattr -dr com.apple.quarantine "/Applications/Trizbort Recast.app"`.
Finder open events reach the app through `FileActivationHandler` (untested on a Mac).

## Updates
Each release also has Velopack packages, which update themselves: `TrizbortRecast-win-x64-Setup.exe`,
the Linux `.AppImage` and `TrizbortRecast-osx-arm64-Portable.zip` / `-osx-x64-Portable.zip`. See
`agent-docs/09-build-test-and-dotnet8-port.md` (Updates). The AppImage (and any copy of a release on Linux) writes its own `.desktop` file, MIME type and icon under
`~/.local/share` at start-up while **Open .trizbort files with Recast** is on, so the steps above aren't needed.

## Icons
`packaging/icons/make_icon.py` (Python + Pillow) draws the app icon and writes the PNG, `.ico` and `.icns` files. The
`.ico` used for the Windows exe and window icons is in `src/Trizbort.Avalonia/Assets/AppIcon/`.

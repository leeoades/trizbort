# File association

Double-clicking a `.trizbort` file should start Trizbort Recast with that file (it is passed as the first argument).

## Linux
```
cp packaging/linux/trizbort.desktop ~/.local/share/applications/
xdg-mime install --mode user packaging/linux/trizbort-mime.xml
update-desktop-database ~/.local/share/applications
xdg-mime default trizbort.desktop application/x-trizbort
```
The release tarball contains `trizbort-recast` and both files above. `Exec=trizbort-recast %f` assumes it is on your PATH
(for example, copy or link it into `~/.local/bin`); otherwise edit `Exec` to its full path.

## Windows
Edit the path to `TrizbortRecast.exe` in `packaging/windows/register-file-association.reg`, then double-click it (HKCU only, no admin needed).

## macOS (not done, needs a Mac)
Add to the `.app` Info.plist: `CFBundleDocumentTypes` (extension `trizbort`, role Editor) and an exported `UTExportedTypeDeclarations` UTI such as `com.trizbort.map` conforming to `public.xml`. The running app handles Finder open events through `FileActivationHandler` (unverified on a Mac).

#!/usr/bin/env bash
# Makes a Mac disk image of Trizbort Recast (Lee, 27 Nov 2026): the app beside an Applications shortcut, on the
# background make_dmg_background.py draws, arrow between them. Give it the app (signed already: the one in
# vpk's -Portable.zip, so it updates itself) and the .dmg to write:
#   make-dmg.sh "path/Trizbort Recast.app" TrizbortRecast-osx-arm64.dmg
# Laying out the window needs Finder; if create-dmg can't, the disk image is still made, just without the layout.
set -euo pipefail
app="$1"; out="$2"
here="$(cd "$(dirname "$0")" && pwd)"
work="$(mktemp -d)"
mkdir "$work/source"
ditto "$app" "$work/source/Trizbort Recast.app"
# one picture for normal and Retina screens
tiffutil -cathidpicheck "$here/dmg-background.png" "$here/dmg-background@2x.png" -out "$work/background.tiff"
rm -f "$out"

if command -v create-dmg >/dev/null && create-dmg \
    --volname "Trizbort Recast" \
    --volicon "$here/../icons/trizbort-recast.icns" \
    --background "$work/background.tiff" \
    --window-pos 200 120 --window-size 660 400 \
    --icon-size 96 \
    --icon "Trizbort Recast.app" 170 190 --hide-extension "Trizbort Recast.app" \
    --app-drop-link 490 190 \
    --no-internet-enable \
    "$out" "$work/source"; then
  echo "Wrote $out"
else
  echo "::warning::create-dmg couldn't lay out the disk image; making a plain one."
  rm -f "$out"
  ln -s /Applications "$work/source/Applications"
  hdiutil create -volname "Trizbort Recast" -srcfolder "$work/source" -fs HFS+ -format UDZO -ov "$out"
fi

# the app inside must still be signed
mount="$(mktemp -d)"
hdiutil attach -nobrowse -readonly -mountpoint "$mount" "$out" >/dev/null
trap 'hdiutil detach "$mount" -quiet || true' EXIT
codesign --verify --deep --strict "$mount/Trizbort Recast.app"
test -L "$mount/Applications"
echo "Checked $out"

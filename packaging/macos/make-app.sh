#!/usr/bin/env bash
# Builds "Trizbort Recast.app" for one macOS architecture and zips it.
# Must run on macOS (it uses codesign and ditto).
#   packaging/macos/make-app.sh <rid: osx-arm64|osx-x64> <version e.g. 0.9.0> <informational e.g. 0.9.0-beta.1> <zip name>
set -euo pipefail
rid="$1"; version="$2"; info="$3"; zip="$4"
root="$(cd "$(dirname "$0")/../.." && pwd)"
work="$(mktemp -d)"
app="$work/Trizbort Recast.app"

# A normal (not single-file) self-contained publish straight into the bundle, so every
# native library sits in Contents/MacOS where it is signed with the app.
dotnet publish "$root/src/Trizbort.Avalonia/Trizbort.Avalonia.csproj" -c Release -r "$rid" \
  --self-contained true -p:UseAppHost=true -p:DebugType=none \
  -p:Version="$version" -p:InformationalVersion="$info" \
  -o "$app/Contents/MacOS"
mv "$app/Contents/MacOS/Trizbort.Avalonia" "$app/Contents/MacOS/TrizbortRecast"

mkdir -p "$app/Contents/Resources"
cp "$root/packaging/icons/trizbort-recast.icns" "$app/Contents/Resources/"
sed "s/@VERSION@/$version/g" "$root/packaging/macos/Info.plist" > "$app/Contents/Info.plist"
cp "$root/LICENSE.txt" "$root/PdfSharp.License.txt" "$app/Contents/Resources/"

# Apple Silicon refuses to run unsigned code, so sign ad hoc (no Apple Developer ID).
codesign --force --deep --sign - "$app"
codesign --verify --deep --strict "$app"

ditto -c -k --sequesterRsrc --keepParent "$app" "$root/$zip"
echo "Wrote $zip"

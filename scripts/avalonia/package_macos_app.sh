#!/usr/bin/env bash
set -euo pipefail

RID="${1:-osx-arm64}"
case "$RID" in
  osx-arm64|osx-x64) ;;
  *)
    echo "Usage: $0 [osx-arm64|osx-x64]" >&2
    exit 2
    ;;
esac

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT="$REPO_ROOT/ProfilerStudy.Avalonia/ProfilerStudy.Avalonia.csproj"
DOTNET_BIN="${DOTNET_BIN:-/Users/bytedance/.dotnet/dotnet}"
if [[ ! -x "$DOTNET_BIN" ]]; then
  DOTNET_BIN="dotnet"
fi

PUBLISH_DIR="$REPO_ROOT/ProfilerStudy.Avalonia/bin/Release/net8.0/publish/$RID"
APP_DIR="$PUBLISH_DIR/ProfilerStudy.app"
CONTENTS_DIR="$APP_DIR/Contents"
MACOS_DIR="$CONTENTS_DIR/MacOS"
RESOURCES_DIR="$CONTENTS_DIR/Resources"
EXECUTABLE_NAME="ProfilerStudy.Avalonia"
BUNDLE_VERSION="${BUNDLE_VERSION:-0.1.0}"
BUNDLE_IDENTIFIER="${BUNDLE_IDENTIFIER:-com.profilerstudy.avalonia}"

"$DOTNET_BIN" publish "$PROJECT" --no-restore -p:PublishProfile="$RID" -p:NuGetAudit=false

rm -rf "$APP_DIR"
mkdir -p "$MACOS_DIR" "$RESOURCES_DIR"

find "$PUBLISH_DIR" -maxdepth 1 -type f -print0 | while IFS= read -r -d '' file; do
  cp "$file" "$MACOS_DIR/"
done
find "$PUBLISH_DIR" -maxdepth 1 -type d ! -path "$PUBLISH_DIR" ! -path "$APP_DIR" -print0 | while IFS= read -r -d '' dir; do
  cp -R "$dir" "$MACOS_DIR/"
done

cp "$REPO_ROOT/ProfilerStudy.Avalonia/Assets/AppIcon.icns" "$RESOURCES_DIR/AppIcon.icns"
chmod +x "$MACOS_DIR/$EXECUTABLE_NAME"

cat > "$CONTENTS_DIR/Info.plist" <<EOF_PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDevelopmentRegion</key>
  <string>en</string>
  <key>CFBundleDisplayName</key>
  <string>ProfilerStudy</string>
  <key>CFBundleExecutable</key>
  <string>$EXECUTABLE_NAME</string>
  <key>CFBundleIconFile</key>
  <string>AppIcon</string>
  <key>CFBundleIdentifier</key>
  <string>$BUNDLE_IDENTIFIER</string>
  <key>CFBundleInfoDictionaryVersion</key>
  <string>6.0</string>
  <key>CFBundleName</key>
  <string>ProfilerStudy</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleShortVersionString</key>
  <string>$BUNDLE_VERSION</string>
  <key>CFBundleVersion</key>
  <string>$BUNDLE_VERSION</string>
  <key>LSMinimumSystemVersion</key>
  <string>12.0</string>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
EOF_PLIST

echo "Created $APP_DIR"

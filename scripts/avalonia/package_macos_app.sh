#!/usr/bin/env bash
set -euo pipefail

RID="osx-arm64"
SKIP_PUBLISH=0
VERIFY_ONLY=0
FROM_BUILD_OUTPUT=0

usage() {
  echo "Usage: $0 [osx-arm64|osx-x64] [--skip-publish] [--verify-only] [--from-build-output]" >&2
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    osx-arm64|osx-x64)
      RID="$1"
      ;;
    --skip-publish)
      SKIP_PUBLISH=1
      ;;
    --verify-only)
      SKIP_PUBLISH=1
      VERIFY_ONLY=1
      ;;
    --from-build-output)
      SKIP_PUBLISH=1
      FROM_BUILD_OUTPUT=1
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      usage
      exit 2
      ;;
  esac
  shift
done

case "$RID" in
  osx-arm64|osx-x64) ;;
  *)
    usage
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
BUILD_OUTPUT_DIR="$REPO_ROOT/ProfilerStudy.Avalonia/bin/Release/net8.0"
APP_DIR="$PUBLISH_DIR/ProfilerStudy.app"
CONTENTS_DIR="$APP_DIR/Contents"
MACOS_DIR="$CONTENTS_DIR/MacOS"
RESOURCES_DIR="$CONTENTS_DIR/Resources"
EXECUTABLE_NAME="ProfilerStudy.Avalonia"
BUNDLE_VERSION="${BUNDLE_VERSION:-0.1.0}"
BUNDLE_IDENTIFIER="${BUNDLE_IDENTIFIER:-com.profilerstudy.avalonia}"

verify_bundle() {
  local executable_path="$MACOS_DIR/$EXECUTABLE_NAME"
  local plist_path="$CONTENTS_DIR/Info.plist"
  local icon_path="$RESOURCES_DIR/AppIcon.icns"

  [[ -d "$APP_DIR" ]] || { echo "Missing app bundle: $APP_DIR" >&2; exit 1; }
  [[ -x "$executable_path" ]] || { echo "Missing executable: $executable_path" >&2; exit 1; }
  [[ -f "$plist_path" ]] || { echo "Missing Info.plist: $plist_path" >&2; exit 1; }
  [[ -f "$icon_path" ]] || { echo "Missing icon: $icon_path" >&2; exit 1; }
  grep -q "<string>$EXECUTABLE_NAME</string>" "$plist_path" || { echo "Info.plist does not name $EXECUTABLE_NAME" >&2; exit 1; }

  echo "Verified $APP_DIR"
}

if [[ "$SKIP_PUBLISH" -eq 0 ]]; then
  "$DOTNET_BIN" publish "$PROJECT" --no-restore -p:PublishProfile="$RID" -p:NuGetAudit=false
fi

if [[ "$VERIFY_ONLY" -eq 1 ]]; then
  verify_bundle
  exit 0
fi

SOURCE_DIR="$PUBLISH_DIR"
if [[ "$FROM_BUILD_OUTPUT" -eq 1 ]]; then
  SOURCE_DIR="$BUILD_OUTPUT_DIR"
fi

if [[ ! -d "$SOURCE_DIR" ]]; then
  if [[ "$FROM_BUILD_OUTPUT" -eq 1 ]]; then
    echo "Missing Release build output directory: $SOURCE_DIR" >&2
    echo "Run dotnet build -c Release first or omit --from-build-output." >&2
    exit 1
  fi

  echo "Missing publish directory: $PUBLISH_DIR" >&2
  echo "Run dotnet publish first or omit --skip-publish." >&2
  exit 1
fi

rm -rf "$APP_DIR"
mkdir -p "$MACOS_DIR" "$RESOURCES_DIR"

find "$SOURCE_DIR" -maxdepth 1 -type f -print0 | while IFS= read -r -d '' file; do
  cp "$file" "$MACOS_DIR/"
done
find "$SOURCE_DIR" -maxdepth 1 -type d ! -path "$SOURCE_DIR" ! -path "$APP_DIR" -print0 | while IFS= read -r -d '' dir; do
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
verify_bundle

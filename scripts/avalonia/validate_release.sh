#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="${CONFIGURATION:-Debug}"
RID=""
PROFILER_FILE=""
VERIFY_BUNDLE=0
NO_RESTORE=1
RUN_BUILD=0

usage() {
  cat >&2 <<EOF_USAGE
Usage: $0 [--configuration Debug|Release] [--build] [--restore] [--profiler-file PATH] [--verify-bundle osx-arm64|osx-x64]

Runs the Avalonia release gates that can be automated:
  - generated sample smoke test
  - optional real profiler file smoke test
  - optional dotnet build
  - optional macOS .app bundle verification
EOF_USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration)
      CONFIGURATION="${2:-}"
      shift
      ;;
    --build)
      RUN_BUILD=1
      ;;
    --restore)
      NO_RESTORE=0
      ;;
    --profiler-file)
      PROFILER_FILE="${2:-}"
      shift
      ;;
    --verify-bundle)
      RID="${2:-}"
      VERIFY_BUNDLE=1
      shift
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

case "$CONFIGURATION" in
  Debug|Release) ;;
  *)
    echo "Unsupported configuration: $CONFIGURATION" >&2
    exit 2
    ;;
esac

if [[ "$VERIFY_BUNDLE" -eq 1 ]]; then
  case "$RID" in
    osx-arm64|osx-x64) ;;
    *)
      echo "--verify-bundle requires osx-arm64 or osx-x64" >&2
      exit 2
      ;;
  esac
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
DOTNET_BIN="${DOTNET_BIN:-/Users/bytedance/.dotnet/dotnet}"
if [[ ! -x "$DOTNET_BIN" ]]; then
  DOTNET_BIN="dotnet"
fi

PROJECT="$REPO_ROOT/ProfilerStudy.Avalonia/ProfilerStudy.Avalonia.csproj"
APP_EXE="$REPO_ROOT/ProfilerStudy.Avalonia/bin/$CONFIGURATION/net8.0/ProfilerStudy.Avalonia"

if [[ "$RUN_BUILD" -eq 1 ]]; then
  echo "Building ProfilerStudy.Avalonia ($CONFIGURATION)"
  BUILD_ARGS=(build "$PROJECT" -c "$CONFIGURATION")
  if [[ "$NO_RESTORE" -eq 1 ]]; then
    BUILD_ARGS+=(--no-restore)
  fi
  "$DOTNET_BIN" "${BUILD_ARGS[@]}"
elif [[ ! -x "$APP_EXE" ]]; then
  echo "Missing built executable: $APP_EXE" >&2
  echo "Run dotnet build first or pass --build." >&2
  exit 1
fi

echo "Running generated sample smoke test"
"$APP_EXE" --smoke-test

if [[ -n "$PROFILER_FILE" ]]; then
  if [[ ! -f "$PROFILER_FILE" ]]; then
    echo "Profiler file not found: $PROFILER_FILE" >&2
    exit 1
  fi

  echo "Running real profiler smoke test: $PROFILER_FILE"
  "$APP_EXE" --smoke-test "$PROFILER_FILE"
else
  echo "Skipping real profiler smoke test; pass --profiler-file PATH to enable it."
fi

if [[ "$VERIFY_BUNDLE" -eq 1 ]]; then
  echo "Verifying macOS app bundle for $RID"
  "$SCRIPT_DIR/package_macos_app.sh" "$RID" --verify-only
fi

echo "Avalonia release validation passed"

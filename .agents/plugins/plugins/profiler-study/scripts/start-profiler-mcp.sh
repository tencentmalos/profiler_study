#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

get_codex_home() {
	if [[ -n "${CODEX_HOME:-}" ]]; then
		printf '%s\n' "$CODEX_HOME"
	else
		printf '%s\n' "$HOME/.codex"
	fi
}

get_marketplace_source() {
	local config_path
	config_path="$(get_codex_home)/config.toml"
	if [[ ! -f "$config_path" ]]; then
		return 0
	fi

	awk '
		/^\[marketplaces\.profiler-study\]/ { in_section = 1; next }
		/^\[/ { in_section = 0 }
		in_section && /^[[:space:]]*source[[:space:]]*=/ {
			line = $0
			sub(/^[^=]*=[[:space:]]*/, "", line)
			gsub(/^'\''|'\''$/, "", line)
			gsub(/^"|"$/, "", line)
			print line
			exit
		}
	' "$config_path"
}

resolve_repo_root() {
	if [[ -n "${PROFILER_STUDY_REPO_ROOT:-}" && -d "$PROFILER_STUDY_REPO_ROOT" ]]; then
		(cd "$PROFILER_STUDY_REPO_ROOT" && pwd)
		return 0
	fi

	local source
	source="$(get_marketplace_source)"
	if [[ -n "$source" && -d "$source" ]]; then
		(cd "$source" && pwd)
		return 0
	fi

	local fallback
	fallback="$(cd "$script_dir/../../../../.." && pwd)"
	if [[ -f "$fallback/ProfilerStudy.McpServer/ProfilerStudy.McpServer.csproj" ]]; then
		printf '%s\n' "$fallback"
		return 0
	fi

	printf 'Unable to locate ProfilerStudy repository source. Set PROFILER_STUDY_REPO_ROOT or install from a local Codex marketplace source.\n' >&2
	return 1
}

repo_root="$(resolve_repo_root)"
project_path="$repo_root/ProfilerStudy.McpServer/ProfilerStudy.McpServer.csproj"
server_path="$repo_root/ProfilerStudy.McpServer/bin/Release/net8.0/ProfilerStudy.McpServer.dll"

if [[ ! -f "$server_path" ]]; then
	dotnet build "$project_path" -c Release -nologo -v minimal >&2
fi

if [[ ! -f "$server_path" ]]; then
	printf 'ProfilerStudy MCP server build did not produce %s\n' "$server_path" >&2
	exit 1
fi

exec dotnet "$server_path" "$@"

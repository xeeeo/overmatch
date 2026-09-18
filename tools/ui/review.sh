#!/usr/bin/env bash
set -euo pipefail

# Deliberately runs a separate scene; the game's main scene stays untouched.
review_root="$(cd "$(dirname "$0")/../.." && pwd)"
if [[ -n "${GODOT_BIN:-}" ]]; then
  review_godot="$GODOT_BIN"
elif command -v godot-mono >/dev/null 2>&1; then
  review_godot="$(command -v godot-mono)"
elif [[ -x /Applications/Godot_mono.app/Contents/MacOS/Godot ]]; then
  review_godot=/Applications/Godot_mono.app/Contents/MacOS/Godot
elif command -v godot >/dev/null 2>&1; then
  review_godot="$(command -v godot)"
else
  echo 'Set GODOT_BIN to your Godot 4.7 .NET executable.' >&2
  exit 1
fi

dotnet build "$review_root/game/Overmatch.csproj" -p:NuGetAudit=false
mkdir -p "$review_root/build"
if ! "$review_godot" --headless --editor --path "$review_root/game" --import --quit \
  --log-file "$review_root/build/ui-import.log" > "$review_root/build/ui-import-output.log" 2>&1; then
  cat "$review_root/build/ui-import-output.log" >&2
  exit 1
fi

exec "$review_godot" --path "$review_root/game" \
  --scene res://ui/command/review/command_review.tscn --resolution 1600x1000 -- "$@"

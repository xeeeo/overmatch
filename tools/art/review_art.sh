#!/usr/bin/env bash
set -euo pipefail

art_root="$(cd "$(dirname "$0")/../.." && pwd)"
art_mode="${1:-gallery}"
if [[ $# -gt 0 ]]; then shift; fi
case "$art_mode" in
  gallery) art_scene=res://art_review/art_gallery.tscn; art_size=1920x1200 ;;
  terrain) art_scene=res://art_review/terrain_review.tscn; art_size=1600x1000 ;;
  *) echo 'Usage: review_art.sh [gallery|terrain] [scene arguments]' >&2; exit 1 ;;
esac
if [[ -n "${GODOT_BIN:-}" ]]; then
  art_godot="$GODOT_BIN"
elif command -v godot-mono >/dev/null 2>&1; then
  art_godot="$(command -v godot-mono)"
elif [[ -x /Applications/Godot_mono.app/Contents/MacOS/Godot ]]; then
  art_godot=/Applications/Godot_mono.app/Contents/MacOS/Godot
else
  art_godot=godot
fi
dotnet build "$art_root/game/Overmatch.csproj" -p:NuGetAudit=false
mkdir -p "$art_root/build"
if ! "$art_godot" --headless --editor --path "$art_root/game" --import --quit > "$art_root/build/art-import.log" 2>&1; then
  cat "$art_root/build/art-import.log" >&2; exit 1
fi
exec "$art_godot" --path "$art_root/game" --scene "$art_scene" --resolution "$art_size" -- "$@"

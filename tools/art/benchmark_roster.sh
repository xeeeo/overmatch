#!/bin/sh
# Run after the normal C# build and Godot asset import. Uses native rendering,
# not --headless. Keep other render jobs idle for this bounded A/B comparison.
set -eu

art_root=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
art_godot=${GODOT_BIN:-/Applications/Godot_mono.app/Contents/MacOS/Godot}
art_output=${1:-"$art_root/artifacts/art-pass/performance"}
art_baseline=${2:-/tmp/overmatch-art-baseline-3bf70dd}
mkdir -p "$art_output"
art_output=$(CDPATH= cd -- "$art_output" && pwd)

"$art_godot" --path "$art_root/game" --resolution 1600x1000 \
    res://art_review/art_benchmark.tscn -- \
    "--benchmark-baseline=$art_baseline" \
    "--benchmark-output=$art_output/baseline.json" \
    "--benchmark-capture=$art_output/baseline.png" >"$art_output/baseline.log" 2>&1

"$art_godot" --path "$art_root/game" --resolution 1600x1000 \
    res://art_review/art_benchmark.tscn -- \
    "--benchmark-output=$art_output/current.json" \
    "--benchmark-capture=$art_output/current.png" >"$art_output/current.log" 2>&1

printf 'Art benchmark results: %s\n' "$art_output"

#!/usr/bin/env bash
# All production GLBs, including the previously approved Bulwark.
set -euo pipefail
art_root="$(cd "$(dirname "$0")/../.." && pwd)"
art_blender="${BLENDER_BIN:-blender}"
for art_builder in build_models build_buildings build_directorate build_network build_extras; do
  "$art_blender" -b -P "$art_root/tools/blender/$art_builder.py"
done
python3 "$art_root/tools/art/audit_models.py"

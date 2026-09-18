# Full roster and battlefield art review

The approved Bulwark establishes the level of detail. This pass extends that work
to the remaining 41 units, all 30 faction buildings and four neutral props, then
the presentation of all six maps. All models are original procedural geometry.

## Run the game or review scenes

The art is used by the normal game automatically. No alternate gameplay mode is
needed. These isolated scenes make the roster and terrain easier to inspect:

```bash
bash tools/art/review_art.sh gallery
bash tools/art/review_art.sh gallery --gallery-faction=network --gallery-kind=buildings
bash tools/art/review_art.sh terrain --terrain-map=twin_rivers
```

Set `GODOT_BIN` if the Godot .NET executable is not on the usual path. Launchers
compile the C# project and import changed resources before opening the scene.

**Gallery:** faction/category buttons; 1/2/3 for player colours; T for a 90° turret
turn; Z for matched RTS pixel scale. Detail cameras fit each model, so contact
sheets compare design quality rather than relative unit sizes. RTS mode preserves
the live 45°/32-unit camera scale at the 1200-pixel reference height. Aircraft are
shown in their local model space. No simulation runs in this scene.

**Terrain:** F1–F6 select maps; O toggles overview; arrows/WASD pan; Q/E rotate;
wheel zooms. The fixture uses normal game lighting, the real map definitions,
neutral buildings and supplies, plus three stationary units for scale. It reveals
the map only inside this review; `--terrain-fog` checks normal vision masking.

Captures are native viewport images, with no raster retouching:

```bash
bash tools/art/review_art.sh gallery --gallery-faction=directorate --gallery-turned --gallery-capture=/absolute/path/roster.png
bash tools/art/review_art.sh terrain --terrain-map=highlands --terrain-overview --terrain-capture=/absolute/path/map.png
```

## Art direction

- **Coalition:** faceted composite hulls, visible running gear, protected optics,
  broad player markings, efficient modular buildings and swept aircraft.
- **Directorate:** heavy armour, industrial fittings, reinforced concrete,
  large mechanical silhouettes and strongly identifiable weapon systems.
- **Network:** patched steel and rust, canvas, field equipment, segmented
  sandbags, converted civilian vehicles and improvised structures.
- **Neutral:** pitched tile roofs, framed windows, roof plant, a mechanical
  pumpjack and an excavated trench, within their existing footprints.
- **Maps:** layered grass/soil/asphalt shading, gravel shoulders, dashed road
  markings, animated water and shore colour, low ground cover and faceted rock
  formations. Each map gets a terrain palette appropriate to its setting.

## Scope and compatibility

No simulation, stats, balance, collision radii, map JSON, pathfinding, vision,
camera, HUD, minimap, audio or VFX code changes. The sole modified runtime C# file
is `MapView.cs`: its ground material and decorative rocks delegate to the new
`TerrainArt` helper. Its fog creation/update path is unchanged. `MapView.cs` was
not modified by the M5 commit, and this branch starts after M5 and the live UI
adoption at `d4019b9`.

Terrain art reads a separate `MapGrid.FromDef` copy. Rocks fit inside blocked
rectangles. Markings stay on road cells and do not cross water. Ground cover has
no collision and is kept clear of spawn pads, resources and neutral approaches.
The shader's map rows use the same north/south convention as the original ground.

Every existing `Turret` and `Rotor*` name, parent and transform is preserved;
`TeamColour` remains available wherever the original asset had it. GLB model IDs
are unchanged. The Engineering Vehicle's static mesh is named `EngineeringHull`
because its former `_vehicle` suffix made Godot import an unintended physics body.
The approved Bulwark and shared `omlib.py` are unchanged.

## Rebuild and audit

```bash
BLENDER_BIN=/path/to/blender bash tools/art/build_roster.sh
python3 tools/art/audit_models.py
dotnet build game/Overmatch.csproj -p:NuGetAudit=false
dotnet test sim.tests --no-restore
```

Original per-faction commands and individual selectors still work, for example
`blender -b -P tools/blender/build_directorate.py -- colossus`.
The audit compares exact transformed vertices and articulation against the
committed `3bf70dd` baseline manifest; it checks all 76 production GLBs.
The repository already tracked one generated `omlib` bytecode file; restore that
generated file after Blender runs if it changes. New bytecode is ignored.

The bounded performance scene contains 120 mixed units and 12 buildings in one
viewport, with production lighting and animated turrets/rotors. It warms up for
60 frames and samples 180 with vsync disabled. It compares artwork only, not AI,
combat, UI, terrain shaders or a complete match. Wall time is the primary metric;
Godot monitor snapshots may retain startup values.

```bash
mkdir -p build/art-baseline
git archive 3bf70dd:game/assets/models | tar -x -C build/art-baseline
sh tools/art/benchmark_roster.sh artifacts/art-pass/performance "$PWD/build/art-baseline"
```

See [screenshots and validation](../../artifacts/art-pass/README.md).

# Bulwark MBT — first unit review

One unit, built from the current M5 + live UI branch (`d4019b9`).

The new silhouette uses a sloped hull, faceted composite turret, segmented skirts,
visible tracks and road wheels, protected optics and top-visible team markings.
The geometry is the actual game asset, generated from Python.

## Images

- [Studio hero](01-bulwark-hero.png)
- [Studio before / after](02-bulwark-comparison.png)
- [Native Godot before / after](native-comparison.png)
- [Normal RTS scale](native-rts.png)
- [Red team, turret rotated 90°](native-red-turret.png)
- [Gold team](native-gold.png)
- [Existing battlefield and command-card context](in-game-context.png)

The context image uses the frozen UI-review scene, not an AI match. Studio lighting
is for shape review; the native captures show Godot rendering and the actual team
palette. Both sides of each native comparison use the same camera and lighting.

## Run

```bash
bash tools/art/review_bulwark.sh
```

Use **1 / 2 / 3** for team colour, **T** for turret rotation and **Z** for RTS scale.
[Full run and regeneration instructions](../../../game/unit_review/README.md).

## Verified

- Standard `build_models.py -- bulwark` generates the new asset successfully.
- 4,496 triangles, two mesh nodes, exact `Turret` and `TeamColour` contracts.
- Original turret pivot retained; geometry occupies essentially the same envelope,
  with slightly lower tread contact. Exact bounds are recorded below.
- Blue/red/gold team recolouring, 90° turret articulation and normal RTS-scale appearance.
- Model loads in the existing battlefield and command-card portrait.
- Game build: zero warnings/errors. Simulation suite: **106 passed**.
- Normal game starts cleanly; native comparison captures complete without runtime errors.

See [asset-check.json](asset-check.json) for measured geometry.

Only two existing files change: the Bulwark GLB and its generator dispatch function.
All remaining additions are its dedicated builder, review scene, baseline snapshot,
render/launch helpers and this evidence. The shared modelling helper, gameplay data,
M5 code, other units and terrain are unchanged.

This is the review stop for unit 01. No next unit or map-texture pass has begun.

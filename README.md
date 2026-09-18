# Overmatch

**An open-source real-time strategy game in the spirit of Command & Conquer: Generals – Zero Hour, set in the 2030s.**

Generals gave us build-anywhere bases, supply-pile economies, three brutally asymmetric factions, generals' powers and superweapons on a countdown. Then the sequel was cancelled. Overmatch is the sequel we're building ourselves: from scratch, fully open, for macOS and Windows.

The world has moved on since 2003. Overmatch's battlefield is today's: cheap FPV drones and loitering munitions, electronic warfare and GPS denial, cyber attacks on infrastructure, hypersonic strikes, private military companies, and the endless tension between cheap mass and exquisite tech.

## Factions

| | Archetype | Identity |
|---|---|---|
| **Coalition** | high-tech, air power, drones | Precision, expensive, drone swarms, EW, orbital kinetic strike |
| **Directorate** | mass, armour, artillery | Hordes, heavy tanks, rocket artillery, cyber, propaganda, nukes |
| **Network** | insurgent, hybrid warfare | Cheap, stealth, tunnels, IEDs, FPV drones, salvage, no power needed |

## Status

Pre-alpha. Playable skirmish with all three factions, sound and six maps. M0–M5 are done: movement, combat, fog of war, base building, economy, production, upgrades, three full faction trees (Coalition, Directorate, Network), generals' promotions and powers, superweapons, garrisons, transports, tunnels, capturable oil derricks, stealth and detection, veterancy, salvage, hazards, a skirmish AI with four difficulty tiers, victory and defeat, menus. M5 added synthesised sound effects, radio voice lines, an announcer, a music loop, a minimap, control groups, six maps, battle-damage effects and a balance pass with a headless AI-vs-AI harness. M6 (packaged releases, contributor and modding docs) is next. See [docs/design](docs/design/) for the design and [docs/roadmap.md](docs/roadmap.md) for where this is going.

## Building from source

Requirements:

- [Godot 4.7.2 (.NET build)](https://godotengine.org/download)
- [.NET SDK 8+](https://dotnet.microsoft.com/download)
- [Blender 5.2 LTS](https://www.blender.org/download/lts/) (only to regenerate models)

```bash
dotnet test sim.tests                     # simulation unit tests
dotnet build game/Overmatch.csproj        # compile the game assembly
godot-mono --path game                    # run the game (godot-mono is the Homebrew cask name; use your Godot .NET binary)
blender -b -P tools/blender/build_models.py   # regenerate all models (optional)
godot-mono --headless --path game --export-release macOS   # export (see game/export_presets.cfg)
```

Smoke test used by Claude for visual checks: `godot-mono --path game -- --smoke=/tmp/out.png --speed=8 --ai=medium` skips the menu, plays a Medium AI for six game minutes at 8x, screenshots its base and quits. `--ai=<difficulty>` alone starts a match straight away.

## Controls

| | |
|---|---|
| Left-drag / click | select; Shift adds; double-click selects every unit of that type on screen |
| Right-click | move, attack, enter a building or transport, capture a tech building, harvest, help build, set a rally point |
| A then click | attack-move |
| S | stop |
| Ctrl+1–9 / 1–9 | set / recall a control group; double-tap to jump to it |
| H | jump to headquarters |
| Space | jump to the last alert |
| Click anything not yours | inspect it |
| WASD, screen edges, middle-drag | pan; Q/E rotate; wheel, two-finger scroll or +/- zoom |
| Minimap | left-click to look, right-click to send the selection |
| M | music on/off; Esc pauses |

Hover any build, unit, upgrade or power button to read what it costs, what it needs, and what it can attack (ground, air or both).

## Tools

```bash
dotnet run --project tools/harness -c Release -- --seeds 3          # AI-vs-AI balance matrix
dotnet run --project tools/harness -c Release -- --timeline coalition,network,plain
python3 tools/maps/build_maps.py        # regenerate the symmetric maps
python3 tools/audio/build_sfx.py        # synthesise sound effects (pure Python)
python3 tools/audio/build_voices.py     # voice lines; needs `brew install espeak-ng`
python3 tools/audio/build_music.py      # the ambient loop
```

## Repository layout

```
docs/design/       game design: pillars, factions, systems, data schema
sim/               Overmatch.Sim – engine-independent simulation (C#)
sim.tests/         xUnit tests for the sim
game/              Godot 4.7 project (presentation, input, UI)
tools/blender/     Python scripts that generate every model as glTF
tools/voices/      TTS scripts for unit voice lines
tools/harness/     headless AI-vs-AI match runner
tools/maps/        map generator
tools/audio/       sound, voice and music generators
```

## Licence

Code is licensed under the [GNU GPL v3](LICENSE). Original art, audio and data are licensed under [CC BY 4.0](LICENSE-ASSETS). Third-party assets are listed with their licences in [CREDITS.md](CREDITS.md).

Overmatch is a fan project. It is not affiliated with or endorsed by Electronic Arts. Command & Conquer and Generals are trademarks of Electronic Arts Inc.

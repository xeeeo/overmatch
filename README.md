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

Pre-alpha. Milestone 0 (bootstrap) in progress. See [docs/design](docs/design/) for the design and [docs/roadmap.md](docs/roadmap.md) for where this is going.

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

Smoke test used by CI and by Claude for visual checks: `godot-mono --path game -- --smoke=/tmp/out.png` issues a scripted move order, saves a screenshot and quits.

## Repository layout

```
docs/design/       game design: pillars, factions, systems, data schema
sim/               Overmatch.Sim – engine-independent simulation (C#)
sim.tests/         xUnit tests for the sim
game/              Godot 4.7 project (presentation, input, UI)
tools/blender/     Python scripts that generate every model as glTF
tools/voices/      TTS scripts for unit voice lines
tools/harness/     headless AI-vs-AI match runner
```

## Licence

Code is licensed under the [GNU GPL v3](LICENSE). Original art, audio and data are licensed under [CC BY 4.0](LICENSE-ASSETS). Third-party assets are listed with their licences in [CREDITS.md](CREDITS.md).

Overmatch is a fan project. It is not affiliated with or endorsed by Electronic Arts. Command & Conquer and Generals are trademarks of Electronic Arts Inc.

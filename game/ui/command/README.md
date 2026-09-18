# Command console

> **Status: adopted.** The live HUD (`game/scripts/Hud.cs`), minimap, menus (`MainMenu.cs`) and pause/result overlay (`ResultOverlay.cs`) are now built on this design system with real match state behind every element. `Ui.cs` holds the shared builders and `PortraitCache.cs` renders each model once for cards, dossier and queue. The review scene below remains as a design fixture; the radar in the live game is `game/scripts/Minimap.cs` with fog-of-war rules.

## UI review 01 (original notes)

An original Generals-inspired command console for Overmatch: graphite panel housings,
amber command accents, condensed typography, recognisable model portraits and a
stable radar → selection → commands → orders layout.

## Run

From the repository root:

```bash
bash tools/ui/review.sh
```

Requires the same Godot 4.7.2 .NET and .NET SDK as the game. Set `GODOT_BIN` if it is
not installed in a standard location. The launcher builds and imports only this
worktree, then opens `review/command_review.tscn`. It never changes the main scene.

| Control | Review action |
| --- | --- |
| F1 / Construction | Dozer dossier and building cards |
| F2 / Production | Motor Pool, unit production and cancellable queue |
| F3 / Low power | Power alert, resource meter and warning treatment |
| F4 / Front end | Title and skirmish setup |
| Escape / Menu | Pause overlay; Escape again returns to the field console |
| Radar click / drag | Navigate the frozen battlefield |
| Center Base | Return camera and radar frame to the base |
| Card hover | Costs, build time and requirement tooltip |
| Card click | Select a structure, or add a unit to the preview queue |
| Queued unit click | Cancel that preview item |
| Right-click on battlefield | Clear the selected construction card |

The interface scales uniformly down to 1280 × 720. Wider windows center the console;
taller windows retain the same bottom anchoring. Buttons have keyboard focus states.

## Scope boundary

This is an **interactive presentation prototype**, ready for visual review, not a
replacement for the live HUD. Cash, power, rank, support timers, health and queues
are staged values. Skirmish controls preserve selections within the setup screen;
Preview Deployment always opens the same Coalition fixture. The review never ticks
the simulation, submits gameplay commands, spends funds, spawns production results,
or implements gameplay fog/radar rules. The command-card numbers and descriptions
come from the current game definitions. Preview card numerals show slot positions,
not new gameplay hotkeys.

The original plan and `docs/roadmap.md` put UI polish and minimap in M5. For that
reason this work adds files only under `game/ui/command/`, plus a launcher and review
artifacts. Existing `Hud.cs`, `MainMenu.cs`, `ResultOverlay.cs`, `GameRoot.cs`, input,
map rendering, VFX, simulation, definitions, unit models and project settings remain
unchanged. Integration with those files requires Toby's explicit permission after
this review and coordination with the M5 work.

Reusable components: `CommandTheme`, `ConsolePanel`, `ModelPortrait` (auto-fits
unmodified models), and the vector icon family. `TacticalRadar` and everything in
`review/` are fixtures; do not adopt them as production gameplay logic. Once approved,
the skin should be bound to the M5 HUD's actual state and commands, and radar data
must be filtered through its fog-of-war implementation.

## Captures

```bash
bash tools/ui/review.sh --ui-screen=menu
bash tools/ui/review.sh --ui-screen=setup
bash tools/ui/review.sh --ui-state=production
bash tools/ui/review.sh --ui-state=lowpower
bash tools/ui/review.sh --ui-screen=pause
bash tools/ui/review.sh --ui-capture=/absolute/path/construction.png
```

Capture exits after 60 rendered frames. With a built/imported project, the equivalent
direct invocation is `godot-mono --path game --scene res://ui/command/review/command_review.tscn`
followed by `--` and any review options above. To inspect a smaller viewport, add
`--resolution 1280x720` before `--`.

## Assets and license

The panels and 19 SVG glyphs are original, under the repository's CC BY 4.0 asset
license. There are no EA assets. Portraits render the repository's existing models;
no unit or terrain art is changed.

Barlow Regular and Barlow Condensed SemiBold, by Jeremy Tribby, are bundled under
the SIL Open Font License. Their license texts are alongside the fonts. Sources:
[Barlow](https://github.com/google/fonts/tree/main/ofl/barlow) and
[Barlow Condensed](https://github.com/google/fonts/tree/main/ofl/barlowcondensed).

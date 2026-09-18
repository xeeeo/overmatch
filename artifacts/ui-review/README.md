# UI review 01

Native Godot captures from branch `codex/ui-command-center`, based on `ad30d04`.
The design is isolated from the M5 game implementation. All existing tracked files
are unchanged; unit models and terrain are shown only as context.

| Capture | What to review |
| --- | --- |
| [Construction](01-construction.png) | Overall console, radar, selection dossier, model cards and locked state |
| [Production](02-production.png) | Vehicle cards, research and production queue |
| [Title menu](03-menu.png) | Type, faction emblem and command-room visual direction |
| [Skirmish setup](04-skirmish.png) | Map schematic, player slots and setup controls |
| [Low power](05-low-power.png) | Alert and resource-state hierarchy |
| [Pause](06-pause.png) | Modal treatment |
| [1280 × 720](07-compact.png) | Compact layout and legibility |

Run and scope notes: [command console README](../../game/ui/command/README.md).

Validation completed:

- Game assembly builds, with only three pre-existing warnings in Hud/ResultOverlay.
- 94 simulation tests pass.
- Original main scene starts headlessly without errors.
- Seven native Metal renders completed without runtime errors; each inspected visually.
- Native interaction checks: difficulty dropdown, setup-to-console navigation,
  production tab, enqueue feedback/progress, queue cancellation and pause.
- Review launcher builds, imports and captures successfully.
- No existing tracked files were changed.

The review scene uses staged match values and UI-only command callbacks. It is not
yet wired to gameplay. UI integration is the next decision after Toby's review;
M5 files must not be edited without his explicit permission. Unit redesigns and map
texture changes are deliberately outside this pass.

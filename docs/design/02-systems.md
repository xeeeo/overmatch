# Systems specification (v0)

The simulation (`sim/`) is a deterministic fixed-tick model with no engine dependency. The Godot project (`game/`) renders it, gathers input, and forwards commands. This split is what makes headless AI-vs-AI testing, unit tests, and later multiplayer possible.

## Time
- Logic runs at **20 ticks/second**. All sim numbers (speeds, cooldowns, build times) are expressed in seconds in data and converted to ticks at load.
- Rendering interpolates between the last two sim states.
- Game speed is a multiplier on ticks per real second; pause stops ticks.
- Determinism holds for the same build on the same CPU architecture (replays, AI harness, tests). Float results differ between x64 and arm64, so cross-platform lockstep multiplayer would need the sim moved to fixed-point arithmetic. Noted, not planned for v1.

## World
- The map is a **heightfield** with a **cell grid** (1 cell = 1 world unit; a tank is ~1 cell wide). Typical skirmish map 200×200 cells.
- Per cell: height, terrain type (ground / cliff / water / road / impassable), passability per locomotor class, occupancy (building, prop, wreck).
- Locomotor classes: `infantry`, `wheeled`, `tracked`, `air`, `hover`. Infantry crosses rough ground; wheeled is fast on road; air ignores terrain; nothing crosses cliffs.
- Static props (trees, rocks, civilian buildings) are entities with armour type `structure` or `fortification`; civilian buildings are garrisonable.

## Entities and components
Entities are ID-indexed with component structs. v0 components:
`Transform`, `Health`, `Armour`, `Owner`, `Locomotor`, `Weapons`, `Vision`, `Stealth`, `Detector`, `Producer`, `Builder`, `Harvester`, `SupplyPile`, `Power`, `Garrison`, `Passenger`, `Capturable`, `Veterancy`, `Abilities`, `Upgrades`, `Timer`, `Hole`.

## Commands
Everything the player or AI does is a command: `Move`, `AttackMove`, `Attack`, `Stop`, `Build(building, cell, rotation)`, `Produce(unit)`, `CancelProduce`, `Research(upgrade)`, `Ability(id, target)`, `Power(id, target)`, `Garrison`, `Ungarrison`, `Capture`, `Sell`, `Rally(cell)`, `Harvest(pile)`, `Repair`. Commands carry the issuing tick, a player ID, and the selected entity set. Command sequences are recorded, which gives replays for free.

## Movement
- **Flow fields** per destination cell, computed lazily and cached for the group. A group move shares one field. Fields respect locomotor passability and treat buildings as blocked.
- **Local avoidance**: simple separation steering between nearby units plus "push" for stationary friendlies; units queue behind slower ones rather than overlapping.
- Formation move: group speed capped to the slowest unit when requested (default on for attack-move).
- Air units fly straight; helicopters hover at a target, jets do strafing passes and return to the Airfield to rearm.

## Combat
- Each weapon has a damage type, damage, range, min range, cooldown, projectile description, splash and target filters.
- Damage dealt = `damage × armourTable[targetArmour][damageType] × veterancyMult × upgradeMult`.
- Projectiles are sim entities (shells arc, rockets home, lasers are instant). Splash applies falloff.
- Target acquisition: idle units auto-engage the nearest valid target within vision; attack-move engages anything en route; units respect a `preferredTargets` ordering.
- Status effects: `jammed` (guidance off, drones fall), `disabled` (cyber, no actions), `poisoned` (toxin DoT), `suppressed` (future).
- Death spawns a wreck (vehicles) that blocks for a while and may drop a **salvage crate** if the killer is Network.

## Vision and fog
- Per player, three layers on the cell grid: `shroud` (never seen), `explored` (seen once, static), `visible` (currently seen).
- Each entity with `Vision` reveals a radius; height gives a small bonus.
- `Stealth` entities are invisible unless within a `Detector` radius or attacking.
- Radar (from the HQ) enables the minimap; EW jamming removes it locally.

## Economy
- **Supply piles** hold a finite value (default 30 000 near start, 15 000 contested). Harvesters take `supplyPerTrip`, path to the nearest Supply Center of their owner, unload, repeat.
- **Trickle buildings** add cash on a timer. **Oil Derricks** are neutral capturables that pay per tick.
- **Power**: buildings sum `power`. When demand exceeds supply, production and defences run at 50% speed and turrets go offline.
- **Production**: each producer has a queue of 9. Cost is charged up front; cancelling refunds. Build time scales with power state.
- **Selling** refunds 50%.

## Building
- Builders receive `Build` with a footprint placement; the site must be on flat-enough, passable, unoccupied cells within a short distance of the builder, and not inside enemy vision-of-defences for fairness (no restriction in v0).
- Construction is a progress bar with HP scaling; multiple builders speed it up.
- Network buildings leave a Hole on death; the Hole rebuilds the building after 60 s unless destroyed.

## Garrisons and capture
- Buildings with `garrisonSlots` accept infantry who fire from inside with a range bonus and take reduced damage until the building is cleared (flame, toxin, flashbang) or destroyed.
- Neutral capturables (Oil Derrick, Repair Bay, Hospital, Radar Station) are captured by capture-capable infantry over a timer.

## Promotions and powers
- Kill value accrues as XP per player; rank thresholds unlock points; points buy powers of rank ≤ current rank.
- Powers have cooldowns and a target type (`none`, `point`, `area`, `unit`). Activation broadcasts a global alert for the biggest ones.
- **Superweapons**: a building with a `Timer`; when it reaches zero the player can fire; everyone sees the countdown once the building exists.

## Veterancy
Levels 0–3 with thresholds per unit. Each level: +10% damage, +10% HP, level 3 self-heals. Chevrons shown over units.

## Skirmish AI
Per faction and difficulty, a **profile** (data) drives four managers on a 1 s cadence:
1. **Economy** – keeps harvester count, expands to new piles, builds trickle buildings, maintains power headroom.
2. **Base** – executes the build order, places buildings around the HQ on a spiral, adds defences facing the nearest enemy, rebuilds losses.
3. **Army** – queues the wave composition, researches upgrades in order, keeps a defence group at home.
4. **Attack** – launches waves on a timer or when army value exceeds a threshold; targets harvesters first on Hard+, retreats damaged waves; uses powers on cooldown against the densest enemy cluster.
Difficulty scales: reaction delay, wave size, use of powers, harassment, and (Brutal only) an income multiplier.

## Win condition
A player is eliminated when they own no buildings and no builders (checked once a second). Last player standing wins; teams come later. Eliminated players' remaining units linger but their AI stops.

## Skirmish AI (as built in M3)
`AiController` lives in the sim and issues ordinary commands, so AI games are deterministic and replayable. A profile in `data/ai/<faction>/<difficulty>.json` gives it a build order, unit composition weights, upgrade order, wave sizes and timings, cash reserve, reaction delay, and an income multiplier (Brutal cheats; Easy is handicapped). Each think: builders → power headroom → build order → defences toward the enemy → expansion when nearby piles run dry → harvesters → weighted round-robin production → upgrades → defend when enemies come within range of a building → attack waves that retarget and retreat when mauled. Placement is a spiral search around the HQ with a one-cell margin. The AI reads the world directly for base decisions, as the original did.

## Presentation contract (game/)
- Reads sim state each frame; owns nothing gameplay-relevant.
- Maps entity `model` IDs to glTF scenes, `voice` IDs to audio banks.
- Input converts clicks and hotkeys into commands and submits them to the sim.
- UI: bottom command bar (selection portrait, production/ability buttons, build menu), top bar (cash, power, rank/points), minimap, superweapon timers, alerts.

## M4 systems (as built)
- **Statuses** on entities with a duration: `disabled` (no actions), `jammed`, `poisoned`, `revealed`, `empowered`.
- **Auras** on units/buildings: `heal` (Speaker, Propaganda Center, Troop Crawler), `jam` (Jammer: burns drones, breaks missile guidance), `horde` (via the `horde` flag: 5+ horde units within 8 cells get +25%, Nationalism adds +25%).
- **Abilities** on units with cooldowns and a target kind (none/point/unit/building); effects share the power effect applier. Flashbang, Hack, Cyber Pulse, Laser Designate, Active Protection.
- **Effects** (powers, abilities, superweapons): `spawn`, `strike` (scheduled impacts with scatter, optional hazard/status), `reveal`, `status`, `heal`, `damage`, `hazard`, `bounty`, `discount`.
- **Hazards**: lingering ground damage (toxin pools, radiation) that respects the armour table, so tanks shrug off toxin.
- **Stealth / detection**: stealthed objects are unseen unless within a detector's radius or `revealed` (2 s after firing). Camouflage grants stealth to a unit tag by upgrade.
- **Garrisons**: infantry enter buildings/transports with slots, fire from inside with +2.5 range, die with the container. Flame/toxin/flashbang weapons hit occupants (`clearsGarrison`). Tunnels share one pool per player; exit at any tunnel.
- **Capture**: `canCapture` infantry take 12 s next to a `capturable` building (oil derricks pay 150 per 10 s).
- **Promotions**: player XP from kills; ranks at 300/800/1600/2800; one point per rank; powers cost one point and need rank 1/3/5; cooldowns per power. A newly bought power charges for one full cooldown before its first use; passive powers (Cash Bounty) apply at once.
- **Superweapons**: buildings with `superweapon` charge over 5–6 minutes (half speed when under-powered), fire an effect at a target, and reset.
- **Veterancy**: per-unit XP thresholds; +10% damage and HP per level; level 3 self-heals. **Salvage**: Network vehicles pick crates from enemy vehicle wrecks (+30% damage, +25% HP per level, max 2).
- **Holes**: Network buildings leave a stealthed hole that regrows the building in 60 s unless destroyed.
- **Spawners**: the Hive keeps four loitering drones that follow it. **Lifetime** units (power drones) expire. **Ammo**: jets fly home to an airfield pad to rearm; pads cap the jet count.
- **Suicide weapons**: Saboteur, Bomb Truck, IED, FPV and loitering drones detonate on contact. **Death damage**: Reactors explode and irradiate. **Crush**: the Colossus kills infantry it drives over.
- **Neutral map objects**: civilian houses/blocks to garrison, oil derricks to capture. Neutral owner is -1 and is never auto-targeted.
- The AI buys powers in faction order. It uses offensive powers only to support a wave that has reached its target or to hit intruders at its own base, never as a cold opening strike. It fires superweapons at the enemy HQ. Hard and Brutal build the superweapon. Waves reinforce: fresh units join or relaunch the attack whenever enough gather, so a lingering wave cannot leave the AI passive.

# Data schema (v0)

All game data lives in `game/data/` as JSON, one file per definition, grouped by type. Files are loaded by the sim at match start into an immutable `GameRules` object. IDs are lowercase snake_case and globally unique within their type.

```
game/data/
  factions/<faction>.json
  units/<faction>/<unit>.json
  buildings/<faction>/<building>.json
  weapons/<weapon>.json
  armour.json               # damage-type × armour-type multiplier table
  upgrades/<upgrade>.json
  powers/<power>.json
  ai/<faction>/<difficulty>.json
  locomotors.json
```

## Unit

```json
{
  "id": "coalition_bulwark",
  "name": "Bulwark MBT",
  "faction": "coalition",
  "cost": 900,
  "buildTime": 12,
  "builtAt": "coalition_motor_pool",
  "prereqs": [],
  "locomotor": "tracked",
  "speed": 3.2,
  "turnRate": 180,
  "hp": 420,
  "armour": "tank",
  "vision": 9,
  "weapons": ["bulwark_cannon"],
  "abilities": [],
  "upgradesAccepted": ["coalition_guided_rounds", "coalition_composite_armour"],
  "model": "coalition/bulwark",
  "voice": "coalition/bulwark",
  "veterancy": { "xpValue": 40, "thresholds": [100, 250, 500] },
  "tags": ["vehicle", "tank"]
}
```

## Building

```json
{
  "id": "coalition_power_plant",
  "name": "Power Plant",
  "faction": "coalition",
  "cost": 800,
  "buildTime": 20,
  "footprint": [3, 3],
  "hp": 1500,
  "armour": "structure",
  "power": 10,
  "prereqs": ["coalition_command_post"],
  "produces": [],
  "upgradesOffered": [],
  "garrisonSlots": 0,
  "model": "coalition/power_plant",
  "tags": ["structure", "power"]
}
```

Negative `power` means consumption. `produces` lists unit IDs. `provides` (optional) lists tech tags used by `prereqs`.

## Weapon

```json
{
  "id": "bulwark_cannon",
  "damage": 60,
  "damageType": "armour_piercing",
  "range": 8.5,
  "minRange": 0,
  "cooldown": 2.0,
  "projectile": { "kind": "shell", "speed": 40, "arc": false },
  "splash": { "radius": 0.5, "falloff": 0.5 },
  "targets": ["ground"],
  "burst": 1
}
```

Damage types (v0): `small_arms`, `armour_piercing`, `explosive`, `flame`, `toxin`, `laser`, `sniper`, `crush`, `jam` (EW, non-damaging), `cyber` (disable), `demolition`.
Armour types (v0): `infantry`, `light_vehicle`, `tank`, `heavy_tank`, `aircraft`, `drone`, `structure`, `fortification`, `harvester`.

## Armour table

`armour.json` maps `armourType -> damageType -> multiplier`. Missing entries default to 1.0.

## Upgrade

```json
{ "id": "coalition_guided_rounds", "name": "Guided Rounds", "cost": 1500, "time": 45,
  "researchedAt": "coalition_strategy_center", "effects": [ { "type": "weaponDamage", "weapon": "bulwark_cannon", "mult": 1.25 } ] }
```

## Power (general's power)

```json
{ "id": "coalition_loitering_swarm", "name": "Loitering Swarm", "rank": 1, "cooldown": 180,
  "target": "area", "radius": 3, "effect": { "type": "spawnTemporary", "unit": "coalition_loiter_drone", "count": 6, "lifetime": 30 } }
```

## Faction

```json
{ "id": "coalition", "name": "Coalition", "colour": "#3b7dd8", "builder": "coalition_dozer", "hq": "coalition_command_post",
  "harvester": "coalition_tiltrotor", "supplyPerTrip": 300, "needsPower": true, "superweapon": "coalition_orbital_uplink",
  "powers": ["coalition_loitering_swarm", "..."], "rankThresholds": [0, 200, 500, 1000, 2000] }
```

## AI profile

```json
{ "faction": "coalition", "difficulty": "medium",
  "buildOrder": ["coalition_dozer", "coalition_power_plant", "coalition_supply_center", "coalition_barracks", "..."],
  "economy": { "targetHarvesters": 3, "expandWhenPilesBelow": 4000 },
  "attack": { "firstWaveAt": 300, "waveInterval": 150, "composition": { "coalition_bulwark": 4, "coalition_rocket_trooper": 4 } },
  "defence": { "reactRadius": 20 }, "modifiers": { "incomeMult": 1.0, "reactionDelay": 2.0 } }
```

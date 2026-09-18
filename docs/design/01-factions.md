# Factions (v1 rosters)

Three factions, deliberately asymmetric. Names are working titles. All three rosters below are implemented as of M4 (data in `game/data/`); costs and stats are starting points for balance, not gospel. Small deviations from this doc live in the data files, which win.

Shared: every faction has a builder, a harvester, a basic infantry, an anti-tank/anti-air infantry, a main tank, an artillery piece, a fast scout/raider, a transport, and an air or drone answer. How they do each of those is where the asymmetry lives.

---

## Coalition
*High-tech, air power, drones. The exquisite-tech faction.*

**Identity.** Everything is expensive and good. Air superiority, precision fires, drone swarms and electronic warfare. Best supply income per harvester. Small armies that must be used well. Loses to attrition if it lets the game drag without pressing its tech.

**Economy.** Cargo Tiltrotor harvesters carry the most supplies per trip and fly over terrain. Drop Zone gives a steady airdrop trickle. Needs power.

### Buildings
| Building | Cost | Role |
|---|---|---|
| Command Post | – | HQ. Builds Dozers. Radar. Fires powers. |
| Power Plant | 800 | 10 power. Upgrade: Turbine Overdrive (+5). |
| Barracks | 600 | Infantry. |
| Supply Center | 2000 | Unloads harvesters; builds Tiltrotors. |
| Motor Pool | 2000 | Vehicles. |
| Airfield | 1000 | Aircraft; 4 pads; rearms/repairs. |
| Strategy Center | 2500 | Tech building. Battle plans (bombardment / hold-the-line / search-and-destroy). Upgrades. |
| Drop Zone | 1500 | Trickle: airdrop every 2 min. |
| Sentry Battery | 900 | Defence: automated turret, anti-ground and anti-air. Needs power. |
| Orbital Uplink | 4000 | Superweapon: Orbital Kinetic Strike. 6-minute timer. |

### Units
| Unit | Cost | Role and flavour |
|---|---|---|
| Dozer | 1000 | Builder. Repairs. Clears mines. |
| Cargo Tiltrotor | 1200 | Flying harvester. Can carry 8 infantry and fast-rope them. |
| Rifleman | 225 | Basic infantry. Flashbang ability to clear garrisons. |
| Rocket Trooper | 300 | Anti-tank, anti-air. |
| Pathfinder | 600 | Sniper. Stealthed when still. Can laser-designate for Lancer. |
| Warden | 700 | Light armoured vehicle. 5 fire ports. Roof drone. Fast. |
| Bulwark MBT | 900 | Main tank. Composite armour upgrade. Active protection intercepts one rocket per 10s. |
| Lancer | 1100 | HIMARS-style rocket artillery. Long range, needs vision. Slow reload. |
| Jammer | 1000 | EW vehicle. Aura: enemy drones drop, enemy radar blind, guided missiles miss. Unarmed. |
| Hive | 1400 | Drone carrier. Launches 4 loitering drones that auto-engage; rebuilds them over time. |
| Kestrel | 1500 | Attack helicopter. Missiles, rockets. Hovers; stealth-detects. |
| Vantage | 1400 | Multirole jet. Anti-ground missiles; upgrade for anti-air. |
| Spectre | 2400 | Stealth bomber. Devastating single strike; visible while bombing. |

### General's powers (by rank)
1. **Loitering Swarm** – six drones orbit an area for 30 s and hit anything that moves. 
1. **Spy Satellite** – reveal an area for 20 s. 
3. **Precision Strike** – cruise missile on a point; big single-target damage. 
3. **EW Blackout** – area: enemy drones fall, radar blind, structures slowed for 30 s. 
5. **Emergency Resupply** – airdrop repairs and heals everything in an area.

### Upgrades
Composite Armour (vehicles), Guided Rounds (tanks +25%), Drone Autonomy (Hive/Warden drones +50% HP, rebuild faster), Countermeasures (aircraft dodge first missile), Turbine Overdrive (power), Advanced Training (infantry start veteran).

---

## Directorate
*Mass, armour, artillery, cyber, propaganda. The industrial-power faction.*

**Identity.** Numbers. Cheap tanks and conscripts get a **horde bonus** (+damage when 5+ of the same type are together). Heavy tanks, rocket artillery, flak, propaganda that heals, hackers that steal and disable, and a nuke. Weak air. Reactors explode when destroyed.

**Economy.** Supply Trucks are cheap and armoured but slow. Cyber Center hacks funds continuously (more hackers inside, more cash). Needs power; reactors are cheap and dangerous.

### Buildings
| Building | Cost | Role |
|---|---|---|
| Command Bunker | – | HQ. Builds Engineering Vehicles. Radar. |
| Reactor | 700 | 12 power. Explodes on death. Upgrade: Overclock (+50% power, more explosive). |
| Barracks | 500 | Infantry. |
| Supply Depot | 1800 | Unloads trucks; builds trucks. |
| Vehicle Plant | 2000 | Vehicles. |
| Airfield | 1000 | Aircraft. |
| Propaganda Center | 2000 | Tech. Horde and propaganda upgrades. Emits healing aura. |
| Cyber Center | 2500 | Trickle: hacks cash; garrison Hackers inside. Enables Cyber Attack. |
| Flak Tower | 1100 | Defence: flak + gatling; garrison 5 infantry. |
| Missile Silo | 5000 | Superweapon: Ballistic Nuclear Missile. 6-minute timer. |

### Units
| Unit | Cost | Role and flavour |
|---|---|---|
| Engineering Vehicle | 1000 | Builder. Repairs. Lays mines. |
| Supply Truck | 600 | Ground harvester. Armoured. |
| Conscript | 150 | Cheap horde infantry. Capture buildings. |
| Rocket Squad | 300 | Anti-tank, anti-air. Horde bonus. |
| Hacker | 500 | Disables buildings; garrison Cyber Center for cash. |
| Vanguard | 700 | Cheap battle tank. Horde bonus. |
| Colossus | 2000 | Heavy twin-cannon tank. Mounts a Speaker, Gatling or drone bay. Crushes vehicles. |
| Typhoon | 1200 | MLRS rocket artillery. Long range, area damage. |
| Hailstorm | 800 | Flak vehicle. Anti-air, anti-infantry. |
| Speaker | 600 | Propaganda truck. Heals nearby units; morale aura (+speed). |
| Ghost Van | 900 | Mobile cyber: disable vehicles in radius briefly, spoof enemy drones. |
| Talon | 1300 | Fighter-bomber. Bombs; weak dogfighter. |
| Troop Crawler | 1000 | Transport 8 infantry; stealth detection; heals passengers. |

### General's powers
1. **Artillery Barrage** – shells rain on an area for 15 s. 
1. **Cyber Intrusion** – disable enemy buildings and units in an area for 30 s. 
3. **Mass Production** – 25% cheaper units for 60 s. 
3. **Carpet Bomb** – bombers lay a carpet of bombs. 
5. **Emergency Conscription** – twelve Conscripts arrive at the point.

### Upgrades
Nationalism (horde +25%), Uranium Shells (tanks +25%), Flak Autoloaders, Subliminal Messaging (+10% speed), Reactor Overclock, Conscript Training (+30% HP).

---

## Network
*Insurgent, hybrid warfare, improvised. The cheap-mass-and-cunning faction.*

**Identity.** Cheap everything, no power at all, stealth by default, tunnels that move armies across the map, IEDs, FPV drone ambushes, salvage that upgrades vehicles from wrecks, toxins, and buildings that rebuild themselves from holes. Weak straight-up; deadly on its own terms. No true air force; drones and Stinger Sites instead.

**Economy.** Workers both build and harvest, so numbers matter. Black Market gives a trickle and sells upgrades. Cash Bounty rewards kills. Salvage crates from wrecks upgrade vehicles for free.

### Buildings
| Building | Cost | Role |
|---|---|---|
| Command Cell | – | HQ. Builds Workers. No radar until upgraded. |
| Safehouse | 500 | Infantry. Stealthed. |
| Supply Stash | 1500 | Unloads Workers. |
| Arms Dealer | 1800 | Vehicles. |
| Tunnel Network | 800 | Units enter one tunnel, exit any other. Garrison 10. Auto-turret. |
| Black Market | 2500 | Trickle + upgrades. |
| Compound | 2000 | Tech. Garrison 5, heavy armour. Enables powers 3+. |
| Stinger Site | 900 | Defence: anti-air and anti-ground rockets. Stealthed. |
| Drone Workshop | 1200 | Builds FPV operators and drone units; toxin upgrades. |
| Launch Site | 4000 | Superweapon: Ballistic Rocket Salvo. 5-minute timer. |

Every building leaves a **hole** on destruction; a Worker or the hole itself rebuilds it in time unless the hole is destroyed.

### Units
| Unit | Cost | Role and flavour |
|---|---|---|
| Worker | 200 | Builder and harvester. |
| Rebel | 150 | Basic infantry. Camouflage upgrade. Capture buildings. |
| RPG Trooper | 300 | Anti-tank, anti-air. |
| Saboteur | 500 | Runs a demolition charge into a target. Stealthed. |
| Angry Mob | 800 | Crowd of civilians with mixed weapons; grows with kills. |
| Technical | 500 | Pickup truck with a gun. Fast. Carries 5. Salvage upgrades. |
| Raider Quad | 400 | Very fast scout. Salvage upgrades. |
| Marauder | 800 | Improvised tank. Salvage upgrades make it a monster. |
| Sprayer | 700 | Toxin tractor. Clears garrisons; poisons ground. |
| Rocket Buggy | 900 | Long-range rocket pod artillery. Fragile. |
| FPV Operator | 350 | Infantry with 3 FPV drones: pilot them into targets. Rebuilds drones over time. |
| Bomb Truck | 1200 | Disguises as an enemy vehicle; detonates. |
| Radar Van | 600 | Reveals radar; stealth detection. |

### General's powers
1. **Rebel Ambush** – 8 Rebels appear anywhere. 
1. **Cash Bounty** – earn cash per enemy killed (passive, stacks). 
3. **IED Belt** – mine a wide area instantly. 
3. **Drone Ambush** – 12 FPV drones strike from a Tunnel. 
5. **Toxin Storm** – toxin bombs on an area; lingering cloud.

### Upgrades
Camouflage (Rebels stealth), Better Rifles, Junk Repair (+25% vehicle HP), Toxin Shells, Salvage Efficiency (+12% vehicle speed), Fortified Structures (+40% building HP), Drone Swarm (FPV +35%).

---

## Counters at a glance

| | Infantry | Light vehicle | Tank | Aircraft | Drone | Structure |
|---|---|---|---|---|---|---|
| Small arms | strong | weak | none | none | ok | weak |
| Armour piercing | weak | ok | strong | none | none | ok |
| Explosive (artillery) | strong | strong | ok | none | none | strong |
| Rockets (AT/AA) | weak | ok | strong | strong | ok | ok |
| Flame / toxin | strong | weak | none | none | none | ok (clears garrison) |
| EW jam | none | none | guidance off | guidance off | **kill** | slow |
| Cyber | none | disable | disable | none | disable | disable |

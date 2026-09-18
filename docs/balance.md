# Balance notes

Balance is measured with the headless harness: every faction pairing, from both sides, on several maps and seeds, Medium AI on both sides.

```bash
dotnet run --project tools/harness -c Release -- --maps plain,highlands,open_steppe,twin_rivers --seeds 3 --minutes 30
```

## 2026-09-18, first pass (108 matches)

| win % vs | Coalition | Directorate | Network | overall |
|---|---|---|---|---|
| Coalition | – | 63 | 39 | 51 |
| Directorate | 38 | – | 51 | 44 |
| Network | 61 | 49 | – | 55 |

Average match 19–21 minutes; 18 of 108 hit the 30-minute limit and were scored on remaining value.

### What the harness found on the way here
- **Economy four times too fast.** A Tiltrotor brought in about $3,000 a minute, so both starting piles were empty by minute five and whoever expanded best won. Harvest loads and times were reset to roughly Generals' pace: Tiltrotor 500 per ~18 s trip, Supply Truck 220, Worker 90. Derricks pay 100 per 12 s.
- **Aircraft were nearly invulnerable.** The armour table gave aircraft 0.0 against armour-piercing and explosive, so rockets and flak did nothing. Now 0.9 and 0.8.
- **The AI starved itself.** It spent every dollar on infantry as it arrived and could never afford a harvester or the next tech building. It now reserves most of its income for a pending essential purchase once it has a minimal defence, and saves for an expansion before its piles run dry.
- **Harvester gridlock.** The AI packed buildings one cell apart; loaded trucks wedged in the lanes and income froze. Working harvesters now pass through each other, stuck units stop colliding until they move again, and the AI leaves two-cell lanes.
- **IEDs counted as army.** The Network AI's IED Belt mines filled its army cap and were "sent to attack". Mines, escorts and timed units no longer count.

This is AI-vs-AI balance. It says nothing yet about how the factions feel in human hands; that needs play.

## 2026-09-18, after adding repair (144 matches, same command)

| win % vs | Coalition | Directorate | Network | overall |
|---|---|---|---|---|
| Coalition | – | 63 | 40 | 51 |
| Directorate | 38 | – | 57 | 47 |
| Network | 60 | 43 | – | 52 |

14 timeouts. Getting here took three corrections, each found by ablation: free building repair under fire stalled sieges (Coalition 61%), fast airfield repair plus AI retreat made helicopters unkillable for Network (Coalition 78% against it), so repair now pauses under fire, rates are modest, and only Hard and Brutal retreat aircraft.

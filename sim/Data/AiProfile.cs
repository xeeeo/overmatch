namespace Overmatch.Sim.Data;

/// <summary>How a computer player plays one faction at one difficulty. Loaded from data/ai/&lt;faction&gt;/&lt;difficulty&gt;.json.</summary>
public sealed class AiProfile
{
    public string Faction { get; set; } = "";
    public string Difficulty { get; set; } = "medium";
    public string Name { get; set; } = "";
    /// <summary>Buildings to construct in order. Power plants are added automatically when demand needs them.</summary>
    public List<string> BuildOrder { get; set; } = new();
    public string PowerBuilding { get; set; } = "";
    public string SupplyBuilding { get; set; } = "";
    public string DefenceBuilding { get; set; } = "";
    public string HarvesterUnit { get; set; } = "";
    public string BuilderUnit { get; set; } = "";
    public int TargetBuilders { get; set; } = 2;
    public int TargetHarvesters { get; set; } = 3;
    public int MaxHarvesters { get; set; } = 6;
    /// <summary>Defence buildings to keep between the base and the enemy.</summary>
    public int Defences { get; set; } = 2;
    /// <summary>Unit id → weight in the army mix.</summary>
    public Dictionary<string, int> Composition { get; set; } = new();
    public List<string> UpgradeOrder { get; set; } = new();
    /// <summary>Units held at home before the first wave goes out.</summary>
    public int WaveSize { get; set; } = 8;
    public int MaxArmy { get; set; } = 30;
    public float FirstWaveAt { get; set; } = 240f;
    public float WaveInterval { get; set; } = 120f;
    /// <summary>Retreat when the wave is down to this fraction of its starting count.</summary>
    public float RetreatFraction { get; set; } = 0.3f;
    public int CashReserve { get; set; } = 200;
    /// <summary>Seconds between decisions.</summary>
    public float ReactionDelay { get; set; } = 1f;
    /// <summary>Multiplier on harvested and trickled income (Brutal cheats).</summary>
    public float IncomeMult { get; set; } = 1f;
    /// <summary>Attack harvesters and expansions before the main base.</summary>
    public bool Harass { get; set; }
    public bool Expand { get; set; } = true;
    /// <summary>How far from own buildings enemies trigger a defensive response.</summary>
    public float DefenceRadius { get; set; } = 24f;
}

using Godot;

namespace Overmatch.Game;

public sealed class PlayerSlot
{
    public string Name = "Player";
    public string Faction = "coalition";
    public bool IsAi;
    public string Difficulty = "medium";
    public Color Colour = new(0.23f, 0.49f, 0.85f);
}

/// <summary>What the skirmish setup screen produces and GameRoot consumes.</summary>
public sealed class MatchSettings
{
    public string MapId = "plain";
    public int StartingCash = 10000;
    public List<PlayerSlot> Players = new();

    public static readonly Color[] Palette =
    {
        new(0.23f, 0.49f, 0.85f), // blue
        new(0.85f, 0.25f, 0.25f), // red
        new(0.25f, 0.70f, 0.35f), // green
        new(0.90f, 0.75f, 0.20f), // yellow
    };

    public static MatchSettings Default(int aiCount = 1, string difficulty = "medium")
    {
        var s = new MatchSettings();
        s.Players.Add(new PlayerSlot { Name = "You", Colour = Palette[0] });
        for (var i = 0; i < aiCount; i++)
            s.Players.Add(new PlayerSlot { Name = $"AI {i + 1}", IsAi = true, Difficulty = difficulty, Colour = Palette[(i + 1) % Palette.Length] });
        return s;
    }
}

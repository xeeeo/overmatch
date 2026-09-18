using Godot;
using Overmatch.Sim;

namespace Overmatch.Game;

/// <summary>A stack of crates that shrinks as the pile is harvested.</summary>
public partial class SupplyPileView : Node3D
{
    private SupplyPile _pile = null!;
    private readonly List<MeshInstance3D> _crates = new();
    private float _shown = -1f;

    public static SupplyPileView Create(SupplyPile pile)
    {
        var v = new SupplyPileView { _pile = pile, Position = MapView.ToWorld(pile.Pos) };
        var rng = new Random(pile.Id);
        var mat = new StandardMaterial3D { AlbedoColor = new Color(0.85f, 0.65f, 0.18f), Roughness = 0.85f };
        var dark = new StandardMaterial3D { AlbedoColor = new Color(0.55f, 0.42f, 0.12f), Roughness = 0.9f };
        var box = new BoxMesh { Size = new Vector3(0.9f, 0.9f, 0.9f) };
        // 12 crates in three layers; hide from the top as the pile drains.
        for (var i = 0; i < 12; i++)
        {
            var layer = i / 5;
            var idx = i % 5;
            var pos = layer switch
            {
                0 => new Vector3(-1f + idx % 3 * 1f, 0.45f, -0.5f + idx / 3 * 1f),
                1 => new Vector3(-0.5f + idx % 2 * 1f, 1.35f, -0.2f + idx / 2 * 0.9f),
                _ => new Vector3(0f, 2.2f, 0.2f),
            };
            var c = new MeshInstance3D
            {
                Mesh = box, Position = pos,
                RotationDegrees = new Vector3(0, (float)rng.NextDouble() * 20f - 10f, 0),
                MaterialOverride = i % 3 == 0 ? dark : mat,
            };
            v.AddChild(c);
            v._crates.Add(c);
        }
        v.Refresh();
        return v;
    }

    public void Refresh()
    {
        var f = _pile.Fraction;
        if (Mathf.Abs(f - _shown) < 0.01f) return;
        _shown = f;
        var visible = Mathf.CeilToInt(f * _crates.Count);
        for (var i = 0; i < _crates.Count; i++) _crates[i].Visible = i < visible;
    }
}

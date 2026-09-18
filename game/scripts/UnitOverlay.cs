using Godot;

namespace Overmatch.Game;

/// <summary>2D overlay: health bars above selected or damaged units.</summary>
public partial class UnitOverlay : Control
{
    public GameRoot Root { get; set; } = null!;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        var cam = Root.Camera.Camera;
        foreach (var view in Root.Views.Values)
        {
            if (!view.Visible) continue;
            var e = view.Entity;
            var frac = e.HpFraction;
            if (!view.Selected && frac >= 0.999f) continue;
            var world = view.Position + new Vector3(0, 2.1f, 0);
            if (cam.IsPositionBehind(world)) continue;
            var s = cam.UnprojectPosition(world);
            const float w = 34f, h = 5f;
            var rect = new Rect2(s.X - w / 2f, s.Y - h, w, h);
            DrawRect(rect, new Color(0, 0, 0, 0.7f));
            var colour = frac > 0.6f ? new Color(0.3f, 0.9f, 0.3f) : frac > 0.3f ? new Color(0.95f, 0.8f, 0.2f) : new Color(0.95f, 0.25f, 0.2f);
            DrawRect(new Rect2(rect.Position + new Vector2(1, 1), new Vector2((w - 2) * frac, h - 2)), colour);
        }
    }
}

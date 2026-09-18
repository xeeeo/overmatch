using Godot;

namespace Overmatch.Game;

/// <summary>2D overlay: health bars above selected or damaged units, construction progress on sites.</summary>
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
            var building = e.IsBuilding;
            var capture = 0f;
            if (building)
                foreach (var u in Root.World.Entities)
                    if (u.CaptureTargetId == e.Id && u.CaptureProgress > capture) capture = u.CaptureProgress;
            if (!view.Selected && frac >= 0.999f && !e.UnderConstruction && capture <= 0f && e.Level == 0 && Root.Selection.InspectId != e.Id) continue;
            var lift = building ? e.Radius * 0.9f + 1.5f : 2.1f + (e.Unit?.FlightHeight ?? 0f);
            var world = view.Position + new Vector3(0, lift - (e.Unit?.FlightHeight ?? 0f), 0);
            if (cam.IsPositionBehind(world)) continue;
            var s = cam.UnprojectPosition(world);
            var w = building ? 56f : 34f;
            const float h = 5f;
            var rect = new Rect2(s.X - w / 2f, s.Y - h, w, h);
            DrawRect(rect, new Color(0, 0, 0, 0.7f));
            var colour = frac > 0.6f ? new Color(0.3f, 0.9f, 0.3f) : frac > 0.3f ? new Color(0.95f, 0.8f, 0.2f) : new Color(0.95f, 0.25f, 0.2f);
            DrawRect(new Rect2(rect.Position + new Vector2(1, 1), new Vector2((w - 2) * frac, h - 2)), colour);
            // Veterancy chevrons.
            for (var c = 0; c < e.Level; c++)
            {
                var chx = s.X - w / 2f - 9f;
                var chy = s.Y - 2f - c * 5f;
                DrawPolyline(new[] { new Vector2(chx - 4, chy), new Vector2(chx, chy - 4), new Vector2(chx + 4, chy) }, new Color(1f, 0.85f, 0.3f), 2f);
            }
            if (capture > 0f)
            {
                var rc = new Rect2(s.X - w / 2f, s.Y + 2, w, h);
                DrawRect(rc, new Color(0, 0, 0, 0.7f));
                DrawRect(new Rect2(rc.Position + new Vector2(1, 1), new Vector2((w - 2) * capture, h - 2)), new Color(1f, 0.85f, 0.3f));
            }
            if (e.UnderConstruction)
            {
                var r2 = new Rect2(s.X - w / 2f, s.Y + 2, w, h);
                DrawRect(r2, new Color(0, 0, 0, 0.7f));
                DrawRect(new Rect2(r2.Position + new Vector2(1, 1), new Vector2((w - 2) * e.BuildProgress, h - 2)), new Color(0.4f, 0.7f, 1f));
            }
        }
    }
}

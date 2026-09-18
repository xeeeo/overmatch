using Godot;
using Overmatch.Game.UiReview;

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

    private readonly List<(Vector3 World, string Text, float Age, float Lift)> _popups = new();
    private static readonly Font Font = CommandTheme.Display;
    private static readonly Color Gold = new(1f, 0.85f, 0.3f);

    /// <summary>Floating cash text that rises from a sim position and fades.</summary>
    public void Popup(Overmatch.Sim.Vec2 simPos, string text, float lift) => _popups.Add((MapView.ToWorld(simPos), text, 0f, lift));

    public override void _Process(double delta)
    {
        for (var i = _popups.Count - 1; i >= 0; i--)
        {
            var p = _popups[i];
            p.Age += (float)delta;
            if (p.Age > 1.8f) _popups.RemoveAt(i); else _popups[i] = p;
        }
        QueueRedraw();
    }

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
            // Income buildings the player owns always show a countdown to the next payout.
            var income = building && e.Owner == Root.LocalPlayer && e.Building is { Trickle: not null } && !e.UnderConstruction;
            if (!view.Selected && frac >= 0.999f && !e.UnderConstruction && capture <= 0f && e.Level == 0 && !income && !e.BeingRepaired && Root.Selection.InspectId != e.Id) continue;
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
            if (e.BeingRepaired && frac < 0.999f)
            {
                // A pulsing green cross: this one is being repaired.
                var pulse = 0.6f + 0.4f * Mathf.Sin(Time.GetTicksMsec() / 160f);
                var cx = s.X + w / 2f + (income ? 50f : 9f);
                var green = new Color(0.35f, 1f, 0.45f, pulse);
                DrawRect(new Rect2(cx - 1.5f, s.Y - 8f, 3f, 11f), green);
                DrawRect(new Rect2(cx - 5.5f, s.Y - 4f, 11f, 3f), green);
            }
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
            if (income && capture <= 0f)
            {
                var interval = e.Building!.Trickle!.Interval;
                var ri = new Rect2(s.X - w / 2f, s.Y + 2, w, h);
                DrawRect(ri, new Color(0, 0, 0, 0.7f));
                DrawRect(new Rect2(ri.Position + new Vector2(1, 1), new Vector2((w - 2) * Mathf.Clamp(1f - e.TrickleTimer / interval, 0f, 1f), h - 2)), e.Operational ? Gold : new Color(0.5f, 0.5f, 0.5f));
                var slowed = e.Building.NeedsPower && Root.World.Player(e.Owner).LowPower;
                var left = Mathf.CeilToInt(Mathf.Max(0f, e.TrickleTimer) * (slowed ? 2f : 1f));
                var label = e.Operational ? $"$ {left / 60}:{left % 60:00}" : "$ PAUSED";
                DrawString(Font, new Vector2(s.X + w / 2f + 6, s.Y + 10), label, HorizontalAlignment.Left, -1, 15, new Color(0, 0, 0, 0.85f));
                DrawString(Font, new Vector2(s.X + w / 2f + 5, s.Y + 9), label, HorizontalAlignment.Left, -1, 15, slowed ? new Color(1f, 0.55f, 0.4f) : Gold);
            }
            if (e.UnderConstruction)
            {
                var r2 = new Rect2(s.X - w / 2f, s.Y + 2, w, h);
                DrawRect(r2, new Color(0, 0, 0, 0.7f));
                DrawRect(new Rect2(r2.Position + new Vector2(1, 1), new Vector2((w - 2) * e.BuildProgress, h - 2)), new Color(0.4f, 0.7f, 1f));
            }
        }
        foreach (var p in _popups)
        {
            var at = p.World + new Vector3(0, p.Lift + p.Age * 1.2f, 0);
            if (cam.IsPositionBehind(at)) continue;
            var sp = cam.UnprojectPosition(at);
            var a = Mathf.Clamp(1.8f - p.Age, 0f, 1f);
            DrawString(Font, sp + new Vector2(-21, 1), p.Text, HorizontalAlignment.Left, -1, 20, new Color(0, 0, 0, a * 0.8f));
            DrawString(Font, sp + new Vector2(-22, 0), p.Text, HorizontalAlignment.Left, -1, 20, new Color(Gold, a));
        }
    }
}

using Godot;
using Overmatch.Sim;

namespace Overmatch.Game;

/// <summary>Mouse picking that matches what the player sees: footprints for buildings, a generous circle for units, screen space for aircraft.</summary>
public static class EntityExtensions
{
    /// <summary>Too small to bother with smoke and fire effects.</summary>
    public static bool IsInfantryLike(this Entity e) => e.Def.IsInfantry || e.Def.Tags.Contains("drone") || e.Def.Tags.Contains("mine");
}

public static class Picking
{
    public static Entity? PickAt(GameRoot root, Vector2 screen, Func<Entity, bool> filter)
    {
        var cam = root.Camera.Camera;
        var ground = root.Camera.GroundPoint(screen);
        Entity? best = null;
        var bestScore = float.MaxValue;

        foreach (var e in root.World.Entities)
        {
            if (!e.Alive || e.IsInside || !filter(e)) continue;
            if (!root.Views.TryGetValue(e.Id, out var view) || !view.Visible) continue;

            var height = e.Unit?.FlightHeight ?? 0f;
            var mid = MapView.ToWorld(e.Pos, height + (e.IsBuilding ? 1.0f : 0.7f));
            if (cam.IsPositionBehind(mid)) continue;
            var centre = cam.UnprojectPosition(mid);
            // How big the thing is on screen: project a point one radius to the side.
            var edge = cam.UnprojectPosition(mid + cam.GlobalBasis.X * MathF.Max(e.Radius, 0.6f));
            var pxRadius = MathF.Max(16f, (edge - centre).Length() * 1.15f);
            var screenDist = (centre - screen).Length();

            var hit = screenDist <= pxRadius;
            if (!hit && height <= 0f && ground is { } g)
            {
                // Ground test in sim space: inside the footprint (buildings) or within the unit's circle.
                var p = MapView.ToSim(g);
                if (e.IsBuilding)
                {
                    var (x0, y0, x1, y1) = e.Bounds;
                    hit = p.X >= x0 - 0.3f && p.X <= x1 + 0.3f && p.Y >= y0 - 0.3f && p.Y <= y1 + 0.3f;
                }
                else hit = (p - e.Pos).Length <= e.Radius + 0.5f;
            }
            if (!hit) continue;

            // Prefer units over the building they stand next to, then whatever is closest to the cursor.
            var score = screenDist / pxRadius + (e.IsBuilding ? 0.75f : 0f);
            if (score < bestScore) { bestScore = score; best = e; }
        }
        return best;
    }
}

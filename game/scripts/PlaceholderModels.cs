using Godot;

namespace Overmatch.Game;

/// <summary>Primitive stand-ins until the Blender pipeline lands. Built from boxes and cylinders in team colour.</summary>
public static class PlaceholderModels
{
    public static Node3D Build(string modelId, Color team)
    {
        return modelId switch
        {
            "coalition/dozer" => Dozer(team),
            _ => Tank(team),
        };
    }

    private static StandardMaterial3D Mat(Color c) => new() { AlbedoColor = c, Roughness = 0.8f };

    private static MeshInstance3D Box(Vector3 size, Vector3 pos, Color c)
    {
        var m = new MeshInstance3D { Mesh = new BoxMesh { Size = size }, Position = pos };
        m.MaterialOverride = Mat(c);
        return m;
    }

    private static MeshInstance3D Cylinder(float radius, float height, Vector3 pos, Color c, Vector3? rotDeg = null)
    {
        var m = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = height, RadialSegments = 12 },
            Position = pos,
        };
        if (rotDeg is { } r) m.RotationDegrees = r;
        m.MaterialOverride = Mat(c);
        return m;
    }

    /// <summary>Model space: +X is the unit's forward (matches sim facing 0 = east), +Y up.</summary>
    private static Node3D Tank(Color team)
    {
        var root = new Node3D();
        var dark = team.Darkened(0.35f);
        var track = new Color(0.12f, 0.12f, 0.12f);
        root.AddChild(Box(new Vector3(1.5f, 0.45f, 1.0f), new Vector3(0, 0.45f, 0), team));            // hull
        root.AddChild(Box(new Vector3(1.6f, 0.35f, 0.28f), new Vector3(0, 0.2f, 0.5f), track));         // left track
        root.AddChild(Box(new Vector3(1.6f, 0.35f, 0.28f), new Vector3(0, 0.2f, -0.5f), track));        // right track
        root.AddChild(Cylinder(0.38f, 0.3f, new Vector3(-0.1f, 0.8f, 0), dark));                       // turret
        root.AddChild(Cylinder(0.06f, 1.1f, new Vector3(0.6f, 0.82f, 0), dark, new Vector3(0, 0, 90))); // barrel
        return root;
    }

    private static Node3D Dozer(Color team)
    {
        var root = new Node3D();
        var yellow = new Color(0.9f, 0.7f, 0.15f);
        var track = new Color(0.12f, 0.12f, 0.12f);
        root.AddChild(Box(new Vector3(1.3f, 0.5f, 0.9f), new Vector3(-0.1f, 0.5f, 0), yellow));         // body
        root.AddChild(Box(new Vector3(0.6f, 0.5f, 0.7f), new Vector3(-0.2f, 1.0f, 0), team));           // cab in team colour
        root.AddChild(Box(new Vector3(0.15f, 0.6f, 1.3f), new Vector3(0.75f, 0.4f, 0), yellow.Darkened(0.3f))); // blade
        root.AddChild(Box(new Vector3(1.4f, 0.35f, 0.25f), new Vector3(0, 0.2f, 0.45f), track));
        root.AddChild(Box(new Vector3(1.4f, 0.35f, 0.25f), new Vector3(0, 0.2f, -0.45f), track));
        return root;
    }
}

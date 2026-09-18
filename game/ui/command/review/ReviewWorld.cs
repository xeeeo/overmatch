using Godot;
using Overmatch.Sim;

namespace Overmatch.Game.UiReview;

/// <summary>
/// A frozen presentation fixture using the current map and models. It never steps
/// the simulation or creates gameplay input, so the UI review is independent of M5.
/// </summary>
public partial class ReviewWorld : Node3D
{
    public World World { get; private set; } = null!;
    public Camera3D Camera { get; private set; } = null!;
    public Vector2 MapFocus { get; private set; }
    private readonly List<EntityView> _views = new();

    private static readonly Color CoalitionColour = new(0.23f, 0.49f, 0.85f);
    private static readonly Vec2 BaseFocus = new(24f, 16f);
    private const float CameraDistance = 34f;
    private const float CameraPitch = 52f;

    public override void _Ready()
    {
        var rules = DataLoader.LoadRules();
        World = new World(rules, rules.Map("plain"), new[] { "coalition", "directorate" });
        World.Player(0).Name = "Coalition Command";
        World.Player(0).Cash = 12500;

        // Build the existing terrain before placing structures, as GameRoot does.
        var map = new MapView { Name = "ExistingBattlefield" };
        AddChild(map);
        map.Build(World, 0);
        AddLighting();

        Place("coalition_command_post", 13, 13);
        Place("coalition_power_plant", 9, 19);
        Place("coalition_barracks", 20, 27);
        Place("coalition_supply_center", 27, 11);
        Place("coalition_motor_pool", 25, 23);

        var dozer = World.Spawn("coalition_dozer", 0, new Vec2(21f, 18f), 0.3f);
        World.Spawn("coalition_bulwark", 0, new Vec2(26f, 19f), 0.65f);
        World.Spawn("coalition_bulwark", 0, new Vec2(29f, 18f), 0.65f);
        World.Spawn("coalition_warden", 0, new Vec2(25f, 15.5f), 0.25f);
        World.Spawn("coalition_rifleman", 0, new Vec2(21f, 23f), -0.4f);
        World.Spawn("coalition_rifleman", 0, new Vec2(22.2f, 23.5f), -0.4f);
        World.Spawn("coalition_rifleman", 0, new Vec2(23.2f, 22.5f), -0.4f);
        World.Spawn("coalition_tiltrotor", 0, new Vec2(32f, 13f), 1.1f);

        var entities = new Node3D { Name = "ExistingUnitsAndStructures" };
        AddChild(entities);
        foreach (var entity in World.Entities)
        {
            var colour = entity.Owner == 0 ? CoalitionColour : new Color(0.5f, 0.5f, 0.5f);
            var view = EntityView.Create(entity, colour);
            view.SetTeamColour(colour);
            view.Selected = entity.Id == dozer.Id;
            entities.AddChild(view);
            _views.Add(view);
        }

        var supplies = new Node3D { Name = "ExistingSupplyPiles" };
        AddChild(supplies);
        foreach (var pile in World.Piles) supplies.AddChild(SupplyPileView.Create(pile));

        World.Vision.RevealAll(0);
        Camera = new Camera3D { Name = "ReviewCamera", Fov = 36f, Far = 600f, Current = true };
        AddChild(Camera);
        FocusBase();
    }

    private void Place(string id, int x, int y)
    {
        var definition = World.Rules.Building(id);
        if (!World.CanPlace(definition, x, y, out var reason))
            throw new InvalidOperationException($"UI review fixture cannot place {id}: {reason}");
        World.PlaceBuilding(id, 0, x, y);
    }

    public void FocusBase() => Focus(BaseFocus);

    public void Select(string definitionId)
    {
        foreach (var view in _views) view.Selected = view.Entity.Owner == 0 && view.Entity.Def.Id == definitionId;
    }

    /// <summary>Radar coordinates: top-left is north-west, bottom-right is south-east.</summary>
    public void FocusMap(Vector2 normalized)
    {
        Focus(new Vec2(
            Mathf.Clamp(normalized.X, 0f, 1f) * World.Grid.Width,
            (1f - Mathf.Clamp(normalized.Y, 0f, 1f)) * World.Grid.Height));
    }

    private void Focus(Vec2 point)
    {
        if (Camera is null) return;
        MapFocus = new Vector2(point.X / World.Grid.Width, 1 - point.Y / World.Grid.Height);
        var target = MapView.ToWorld(point);
        var pitch = Mathf.DegToRad(CameraPitch);
        Camera.Position = target + new Vector3(0f, Mathf.Sin(pitch) * CameraDistance, Mathf.Cos(pitch) * CameraDistance);
        Camera.LookAt(target, Vector3.Up);
    }

    private void AddLighting()
    {
        // Match GameRoot's lighting exactly; map and model improvements are a later review.
        AddChild(new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-55, 35, 0),
            LightEnergy = 1.3f,
            ShadowEnabled = true,
            DirectionalShadowMaxDistance = 150f,
        });
        AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new Color(0.55f, 0.65f, 0.75f),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color(0.7f, 0.75f, 0.8f),
                AmbientLightEnergy = 0.6f,
                FogEnabled = true,
                FogLightColor = new Color(0.6f, 0.68f, 0.75f),
                FogDensity = 0.002f,
            },
        });
    }
}

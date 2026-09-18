using Godot;

namespace Overmatch.Game;

/// <summary>Classic RTS camera: a pivot on the ground that pans, rotates and zooms; the Camera3D looks down at it from a fixed pitch.</summary>
public partial class RtsCamera : Node3D
{
    [Export] public float PanSpeed = 40f;
    [Export] public float EdgeMargin = 12f;
    [Export] public float RotateSpeed = 90f;
    [Export] public float MinDistance = 15f;
    [Export] public float MaxDistance = 90f;
    [Export] public float ZoomStep = 6f;
    [Export] public float Pitch = -52f;
    /// <summary>World-space rectangle the pivot may roam (x, z ranges). Set from the map.</summary>
    public Vector2 MinBounds = new(-100, -100);
    public Vector2 MaxBounds = new(100, 100);

    public Camera3D Camera { get; private set; } = null!;

    private float _distance = 32f;
    private float _targetDistance = 32f;
    private bool _middleDrag;
    private float _shake;
    private readonly Random _shakeRng = new();

    /// <summary>Kick the camera; bigger for nearer, larger blasts.</summary>
    public void Shake(float amount) => _shake = Mathf.Min(1.5f, MathF.Max(_shake, amount));

    public override void _Ready()
    {
        Camera = new Camera3D { Fov = 45f, Far = 600f, Current = true };
        AddChild(Camera);
        ApplyCameraTransform();
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        var move = Vector2.Zero;
        if (Input.IsActionPressed("camera_forward")) move.Y -= 1;
        if (Input.IsActionPressed("camera_back")) move.Y += 1;
        if (Input.IsActionPressed("camera_left")) move.X -= 1;
        if (Input.IsActionPressed("camera_right")) move.X += 1;

        // Edge scrolling, only when the window has focus and the mouse is inside.
        var vp = GetViewport();
        var mouse = vp.GetMousePosition();
        var size = vp.GetVisibleRect().Size;
        if (GameSettings.EdgeScroll && mouse.X >= 0 && mouse.Y >= 0 && mouse.X <= size.X && mouse.Y <= size.Y && !_middleDrag)
        {
            if (mouse.X < EdgeMargin) move.X -= 1;
            else if (mouse.X > size.X - EdgeMargin) move.X += 1;
            if (mouse.Y < EdgeMargin) move.Y -= 1;
            else if (mouse.Y > size.Y - EdgeMargin) move.Y += 1;
        }

        if (move != Vector2.Zero)
        {
            move = move.Normalized();
            // Pan relative to the camera's yaw; speed scales with zoom so it feels constant on screen.
            var speed = PanSpeed * GameSettings.ScrollSpeed * (_distance / 45f) * dt;
            var forward = -Basis.Z;
            var right = Basis.X;
            Position += (right * move.X + forward * -move.Y) * speed;
        }

        if (Input.IsActionPressed("camera_rotate_left")) RotateY(Mathf.DegToRad(RotateSpeed * dt));
        if (Input.IsActionPressed("camera_rotate_right")) RotateY(Mathf.DegToRad(-RotateSpeed * dt));

        Position = new Vector3(
            Mathf.Clamp(Position.X, MinBounds.X, MaxBounds.X), 0f,
            Mathf.Clamp(Position.Z, MinBounds.Y, MaxBounds.Y));

        _distance = Mathf.Lerp(_distance, _targetDistance, Mathf.Min(1f, 10f * dt));
        ApplyCameraTransform();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (@event)
        {
            // Mouse wheel. `Factor` is fractional for precise-scrolling devices, 1 for click wheels.
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true } up:
                Zoom(-ZoomStep * Mathf.Max(up.Factor, 0.2f));
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true } down:
                Zoom(ZoomStep * Mathf.Max(down.Factor, 0.2f));
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Middle } mid:
                _middleDrag = mid.Pressed;
                break;
            // macOS trackpad two-finger scroll arrives as a pan gesture rather than wheel clicks.
            case InputEventPanGesture pan:
                Zoom(pan.Delta.Y * ZoomStep * 0.25f);
                break;
            // Pinch.
            case InputEventMagnifyGesture mag:
                Zoom((1f - mag.Factor) * _distance);
                break;
            case InputEventMouseMotion mm when _middleDrag:
            {
                var speed = 0.06f * (_distance / 45f);
                Position += (Basis.X * -mm.Relative.X + -Basis.Z * mm.Relative.Y) * speed;
                break;
            }
            case InputEventKey { Pressed: true } key:
                if (key.Keycode is Key.Equal or Key.Plus or Key.KpAdd) Zoom(-ZoomStep);
                else if (key.Keycode is Key.Minus or Key.KpSubtract) Zoom(ZoomStep);
                break;
        }
    }

    private void Zoom(float delta) => _targetDistance = Mathf.Clamp(_targetDistance + delta, MinDistance, MaxDistance);

    private void ApplyCameraTransform()
    {
        var pitch = Mathf.DegToRad(Pitch);
        // Camera sits behind (+Z local) and above the pivot, looking down at it.
        var offset = new Vector3(0f, -Mathf.Sin(pitch) * _distance, Mathf.Cos(pitch) * _distance);
        Camera.Position = offset;
        Camera.LookAt(GlobalPosition, Vector3.Up);
        if (_shake > 0.01f)
        {
            Camera.Position += new Vector3((float)_shakeRng.NextDouble() - 0.5f, (float)_shakeRng.NextDouble() - 0.5f, (float)_shakeRng.NextDouble() - 0.5f) * _shake;
            _shake *= 0.88f;
        }
    }

    /// <summary>Intersect the mouse ray with the ground plane (y = 0). Returns null if looking at the sky.</summary>
    public Vector3? GroundPoint(Vector2 screenPos)
    {
        var origin = Camera.ProjectRayOrigin(screenPos);
        var dir = Camera.ProjectRayNormal(screenPos);
        if (Mathf.Abs(dir.Y) < 1e-5f) return null;
        var t = -origin.Y / dir.Y;
        if (t < 0) return null;
        return origin + dir * t;
    }
}

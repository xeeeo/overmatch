namespace Overmatch.Sim;

/// <summary>2D world position on the map plane. X east, Y north. Engine-independent.</summary>
public readonly record struct Vec2(float X, float Y)
{
    public static readonly Vec2 Zero = new(0, 0);

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator *(Vec2 a, float s) => new(a.X * s, a.Y * s);
    public static Vec2 operator /(Vec2 a, float s) => new(a.X / s, a.Y / s);
    public static Vec2 operator -(Vec2 a) => new(-a.X, -a.Y);

    public float Length => MathF.Sqrt(X * X + Y * Y);
    public float LengthSq => X * X + Y * Y;
    public Vec2 Normalized => Length > 1e-6f ? this / Length : Zero;
    public float Angle => MathF.Atan2(Y, X);

    public static Vec2 FromAngle(float radians) => new(MathF.Cos(radians), MathF.Sin(radians));
    public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;
    public static float Distance(Vec2 a, Vec2 b) => (a - b).Length;
    public static Vec2 Lerp(Vec2 a, Vec2 b, float t) => a + (b - a) * t;

    public override string ToString() => $"({X:0.00}, {Y:0.00})";
}

public static class Angles
{
    public const float Tau = MathF.PI * 2f;

    /// <summary>Wrap to (-pi, pi].</summary>
    public static float Wrap(float a)
    {
        a %= Tau;
        if (a > MathF.PI) a -= Tau;
        if (a <= -MathF.PI) a += Tau;
        return a;
    }

    /// <summary>Rotate <paramref name="from"/> toward <paramref name="to"/> by at most <paramref name="maxStep"/> radians.</summary>
    public static float TurnToward(float from, float to, float maxStep)
    {
        var diff = Wrap(to - from);
        if (MathF.Abs(diff) <= maxStep) return to;
        return Wrap(from + MathF.Sign(diff) * maxStep);
    }

    public static float LerpAngle(float a, float b, float t) => Wrap(a + Wrap(b - a) * t);
    public static float DegToRad(float d) => d * (MathF.PI / 180f);
}

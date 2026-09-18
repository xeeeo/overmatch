using Overmatch.Sim;

namespace Overmatch.Sim.Tests;

public class MathTests
{
    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(MathF.PI * 3, MathF.PI)]
    [InlineData(-MathF.PI * 1.5f, MathF.PI * 0.5f)]
    public void Wrap_StaysWithinPi(float input, float expected)
    {
        Assert.Equal(expected, Angles.Wrap(input), 4);
    }

    [Fact]
    public void TurnToward_TakesShortestPath()
    {
        // From just above -pi to just below +pi: the short way crosses the seam.
        var from = -MathF.PI + 0.1f;
        var to = MathF.PI - 0.1f;
        var result = Angles.TurnToward(from, to, 0.05f);
        Assert.True(Angles.Wrap(result - from) < 0, "should turn negative (across the seam)");
    }

    [Fact]
    public void TurnToward_ClampsToMaxStep()
    {
        var r = Angles.TurnToward(0f, 1f, 0.25f);
        Assert.Equal(0.25f, r, 5);
        Assert.Equal(1f, Angles.TurnToward(0f, 1f, 2f), 5);
    }

    [Fact]
    public void Vec2_Basics()
    {
        var v = new Vec2(3, 4);
        Assert.Equal(5f, v.Length, 5);
        Assert.Equal(1f, v.Normalized.Length, 5);
        Assert.Equal(new Vec2(1.5f, 2f), Vec2.Lerp(Vec2.Zero, v, 0.5f));
    }
}

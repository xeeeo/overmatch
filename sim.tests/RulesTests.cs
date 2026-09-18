using Overmatch.Sim.Data;

namespace Overmatch.Sim.Tests;

public class RulesTests
{
    [Fact]
    public void Load_ParsesEverything()
    {
        var rules = TestRules.Basic();
        Assert.Equal(4, rules.Units.Count);
        Assert.Equal(6, rules.Buildings.Count);
        Assert.True(rules.Building("hq").Hq);
        Assert.Equal(4f, rules.Unit("tank").Speed);
        Assert.True(rules.Unit("tank").HasTurret);
        Assert.False(rules.Unit("scout").HasTurret);
        Assert.Equal(40f, rules.Weapon("cannon").Damage);
        Assert.Equal("shell", rules.Weapon("cannon").Projectile.Kind);
        Assert.Equal("Test", rules.Factions["test"].Name);
    }

    [Fact]
    public void ArmourTable_DefaultsToOne()
    {
        var a = TestRules.Basic().Armour;
        Assert.Equal(0.1f, a.Multiplier("tank", "small_arms"));
        Assert.Equal(1.5f, a.Multiplier("light_vehicle", "armour_piercing"));
        Assert.Equal(1f, a.Multiplier("tank", "toxin"));
        Assert.Equal(1f, a.Multiplier("nothing", "toxin"));
    }

    [Fact]
    public void Load_RejectsUnitWithoutId()
    {
        Assert.Throws<InvalidDataException>(() => GameRules.Load(new[] { new DataFile("units/x.json", "{ \"name\": \"no id\" }") }));
    }

    [Fact]
    public void Load_RejectsUnknownWeaponReference()
    {
        Assert.Throws<InvalidDataException>(() => GameRules.Load(new[]
        {
            new DataFile("units/x.json", """{ "id": "x", "weapons": ["missing"] }"""),
        }));
    }

    [Fact]
    public void Unit_UnknownThrows()
    {
        Assert.Throws<KeyNotFoundException>(() => TestRules.Basic().Unit("nope"));
    }
}
